using Factos.Abstractions;
using Factos.RemoteTesters;
using Factos.Server.ClientConnection;
using Factos.Server.Settings;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.OutputDevice;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using Microsoft.Testing.Platform.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Factos.Server;

internal sealed class FactosFramework
    : BaseExtension, ITestFramework, IDataProducer, IOutputDeviceDataProducer
{
    readonly FactosSettings settings;
    readonly DeviceWritter deviceWriter;
    readonly AppRunner appRunner;

    public FactosFramework(IServiceProvider serviceProvider, FactosSettings factosSettings)
    {
        var outputDevice = serviceProvider.GetOutputDevice();
        deviceWriter = new(this, outputDevice);
        appRunner = new(outputDevice);
        settings = factosSettings;

        var cliOptions = serviceProvider.GetCommandLineOptions();

        if (cliOptions.TryGetOptionArgumentList(CommandLineOptionsProvider.OPTION_TEST_GROUP, out var group))
        {
            var groupSet = new HashSet<string>(group);

            settings.TestedApps = [..
                settings.TestedApps
                    .Where(app =>
                    {
                        if (app.TestGroups is null) return false;
                        return app.TestGroups.Any(g => groupSet.Contains(g));
                    })
            ];
        }
        else
        {
            settings.TestedApps = [..
                settings.TestedApps
                    .Where(app => app.TestGroups is null)
            ];
        }

        if (cliOptions.TryGetOptionArgumentList(CommandLineOptionsProvider.OPTION_ENVIRONMENT, out var envVars))
        {
            foreach (var envVar in envVars)
            {
                var parts = envVar.Split('=', 2);
                if (parts.Length != 2) throw new ArgumentException(
                    $"Invalid environment test variable format: {envVar}. Expected format is 'key=value'.");

                foreach (var testedApp in settings.TestedApps)
                {
                    if (testedApp.Commands is null) continue;

                    testedApp.Commands = [..
                        testedApp.Commands.Select(cmd =>
                            cmd.Replace($"[{parts[0]}]", parts[1]))
                    ];
                }
                
            }
        }

        if (settings.TestedApps.Count == 0)
        {
            _ = deviceWriter.Red(
                "No tested apps were selected to run. " +
                "Please check that the test groups specified in the CLI match the apps configuration.",
                CancellationToken.None);

            throw new InvalidOperationException(
                "No tested apps were selected to run.");
        }
    }

    protected override string Id =>
        "FactosTestingFramework";

    Type[] IDataProducer.DataTypesProduced => 
        [typeof(TestNodeUpdateMessage)];

    async Task<CreateTestSessionResult> ITestFramework.CreateTestSessionAsync(CreateTestSessionContext context) =>
        new() { IsSuccess = true };

    async Task ITestFramework.ExecuteRequestAsync(ExecuteRequestContext context)
    {
        var cancellationToken = context.CancellationToken;

        try
        {
            var appNames = new HashSet<string?>();

            foreach (var testedApp in settings.TestedApps)
            {
                var appName = testedApp.DisplayName ?? "test runner";
                // sanitize name to be file system friendly
                foreach (var c in Path.GetInvalidFileNameChars())
                    appName = appName.Replace(c, '_');

                // prevent repeated names
                var i = 1;
                while (!appNames.Add(appName))
                    appName = $"{testedApp.DisplayName} ({i++})";
                var cacheFile = $"{appName}_cache.json";

                // lets try to cache results on discovery to prevent
                // re-running the app multiple times during test exploration
                TestSessionResponse? clientResponse;

                if (context.Request is RunTestExecutionRequest && File.Exists(cacheFile))
                {
                    using (var stream = File.OpenRead(cacheFile))
                    {
                        var cachedResponse = await JsonSerializer.DeserializeAsync(
                            stream, JsonGenerationContext.Default.ExecutionResponse) 
                                ?? throw new Exception("an error occurred while reading tests cache.");
                        clientResponse = new TestSessionResponse(null, cachedResponse);
                    }
                    
                    File.Delete(cacheFile);
                    
                    await deviceWriter.Dimmed($"Using cached results for {appName}...", cancellationToken);
                }
                else
                {
                    await deviceWriter.Title($"Starting {appName}...", cancellationToken);
                    await appRunner.StartApp(testedApp, appName, cancellationToken);

                    clientResponse = await GetActiveProtocolsClientResponse(
                       appName, context, cancellationToken);

                    if (context.Request is DiscoverTestExecutionRequest)
                    {
                        File.WriteAllText(
                            $"{appName}_cache.json",
                            JsonSerializer.Serialize(clientResponse.Response, JsonGenerationContext.Default.ExecutionResponse));
                        await deviceWriter.Dimmed($"Cached discovered tests for {appName}.", cancellationToken);
                    }
                }

                var requestedNodes = context.Request is DiscoverTestExecutionRequest
                    ? MTPResultsMapper.ReadNodes(appName, clientResponse.Response.Discovered)
                    : MTPResultsMapper.ReadNodes(appName, clientResponse.Response.Results);

                foreach (var node in requestedNodes)
                    await context.MessageBus.PublishAsync(
                        this, new TestNodeUpdateMessage(context.Request.Session.SessionUid, node));

                await appRunner.EndApp(clientResponse.Protocol, appName, cancellationToken);
                await MTPResultsMapper.LogCount(appName, deviceWriter, cancellationToken);
                await deviceWriter.Title($"{appName} Finished!", cancellationToken);
            }

            await deviceWriter.Dimmed(
                "The result of all the apps is displayed below, " +
                "each app logged its own results (see log above).", cancellationToken);
        }
        catch (Exception ex)
        {
            await deviceWriter.Red(
                $"An error occurred during test execution: {ex.Message}\n{ex.StackTrace}", cancellationToken);
            throw;
        }
        finally
        {
            context.Complete();
        }
    }

    async Task<CloseTestSessionResult> ITestFramework.CloseTestSessionAsync(CloseTestSessionContext context) =>
        new() { IsSuccess = true };

    Task<bool> Microsoft.Testing.Platform.Extensions.IExtension.IsEnabledAsync() =>
        Task.FromResult(true);

    private async Task<TestSessionResponse> GetActiveProtocolsClientResponse(
        string appName, ExecuteRequestContext context, CancellationToken cancellationToken)
    {
        var requests = new List<Task<TestSessionResponse>>();

        await deviceWriter.Blue(
            "Waiting for test nodes from the test app...", cancellationToken);

        // listen for test nodes from all protocols
        foreach (var protocol in ProtocolosLifeTime.ActiveProtocols)
            requests.Add(protocol
                .RequestClient(appName, context)
                .ContinueWith(r => r.IsFaulted || r.Result == null
                    ? new TestSessionResponse(protocol, new(), r.Exception)
                    : new TestSessionResponse(protocol, r.Result)));

        var timeoutTask = GetTimeOutTask();
        requests.Add(GetTimeOutTask());

        // wait for the first protocol to return test nodes
        var response = await Task.WhenAny(requests);

        if (response == timeoutTask)
        {
            await deviceWriter.Red(
                $"Connection Error.\n" +
                $"No test nodes received from any client within the timeout period of " +
                $"{settings.ConnectionTimeout} seconds.", cancellationToken);

            throw new TimeoutException("No test nodes received from any client.");
        }

        if (response.Exception is not null)
        {
            await deviceWriter.Red(
                $"Error receiving test nodes from the test app: {response.Exception.Message}\n" +
                $"{response.Exception.StackTrace}", cancellationToken);

            throw new InvalidOperationException(
                "Error receiving test nodes from the test app.", response.Exception);
        }

        await deviceWriter.Blue(
            $"Test nodes received from the test app!\n" +
            $"protocol {response.Result.Protocol}\n" +
            $"{response.Result.Response.Results.Count()} nodes found", cancellationToken);

        return response.Result;
    }

    private async Task<TestSessionResponse> GetTimeOutTask()
    {
        await  Task.Delay(TimeSpan.FromSeconds(settings.ConnectionTimeout));
        return new(null, new());
    }

    private class TestSessionResponse(
        IServerSessionProtocol? protocol, ExecutionResponse response, Exception? exception = null)
    {
        [JsonIgnore]
        public IServerSessionProtocol? Protocol { get; } = protocol;
        public ExecutionResponse Response { get; } = response;
        public Exception? Exception { get; } = exception;
    }
}
