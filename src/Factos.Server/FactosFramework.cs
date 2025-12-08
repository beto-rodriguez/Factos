using Factos.Abstractions;
using Factos.RemoteTesters;
using Factos.Server.ClientConnection;
using Factos.Server.Settings;
using Factos.Server.Settings.Apps;
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
    readonly IList<TestedApp> testedApps = [];

    public FactosFramework(IServiceProvider serviceProvider, FactosSettings factosSettings)
    {
        var outputDevice = serviceProvider.GetOutputDevice();
        var cliOptions = serviceProvider.GetCommandLineOptions();
        deviceWriter = new(this, outputDevice);
        appRunner = new(outputDevice, cliOptions);
        settings = factosSettings;

        if (cliOptions.TryGetOptionArgumentList(CommandLineOptionsProvider.OPTION_SELECT, out var group))
        {
            testedApps = [..
                settings.TestedApps
                    .Where(app =>
                    {
                        foreach (var g in group)
                        {
                            if (string.Equals(app.ProjectPath, g, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(app.Uid, g, StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }

                        return false;
                    })
            ];
        }
        else
        {
            testedApps = [.. settings.TestedApps ];
        }

        if (testedApps.Count == 0)
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

            foreach (var testedApp in testedApps)
            {
                var appName = testedApp.Uid ?? "test runner";
                // sanitize name to be file system friendly
                foreach (var c in Path.GetInvalidFileNameChars())
                    appName = appName.Replace(c, '_');

                // prevent repeated names
                var i = 1;
                while (!appNames.Add(appName))
                    appName = $"{testedApp.Uid} ({i++})";
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
        string appName, ExecuteRequestContext context, CancellationToken cancellationToken, int retryCount = 0)
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
            $"{response.Result.Response.Results.Count()} nodes received by {response.Result.Protocol}", cancellationToken);

        if (response.Result.Protocol is null)
        {
            // im not completely sure when this happens, but it happens on ios simulators sometimes
            // maybe a race condition... ToDo: investigate further
            // for now we will retry a few times before failing
            if (retryCount < 3)
            {
                retryCount++;
                await deviceWriter.Green(
                    $"No active protocol found, retrying to get test nodes... (attempt {retryCount}/3)", cancellationToken);
                return await GetActiveProtocolsClientResponse(appName, context, cancellationToken, retryCount);
            }
        }

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
