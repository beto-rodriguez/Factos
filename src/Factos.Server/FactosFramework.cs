using Factos.Server.ClientConnection;
using Factos.Server.Settings;
using Factos.Server.Settings.Apps;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.OutputDevice;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Services;
using System.Runtime.CompilerServices;

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

                await deviceWriter.Title($"Starting {appName}...", cancellationToken);
                await appRunner.StartApp(testedApp, appName, cancellationToken);

                var ct = new CancellationTokenSource(TimeSpan.FromSeconds(settings.ConnectionTimeout));
                var nodesStream = GetNodes(appName, context, cancellationToken).WithCancellation(ct.Token);

                await foreach (var node in nodesStream)
                    await context.MessageBus.PublishAsync(
                        this, new TestNodeUpdateMessage(context.Request.Session.SessionUid, node));

                //await appRunner.EndApp(clientResponse.Protocol, appName, cancellationToken);
                var protocol = ProtocolosLifeTime.ActiveProtocols.First();
                await appRunner.EndApp(protocol, appName, cancellationToken);

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

    private async IAsyncEnumerable<TestNode> GetNodes(
        string appName, ExecuteRequestContext context, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await deviceWriter.Blue(
            "Waiting for test nodes from the test app...", cancellationToken);

        var protocol = ProtocolosLifeTime.ActiveProtocols.First();

        await foreach (var node in protocol.RequestClient(appName, context))
        {
            yield return MTPResultsMapper.ReadNode(appName, node);
        }
    }
}
