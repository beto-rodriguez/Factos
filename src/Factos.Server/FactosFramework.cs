using Factos.Abstractions.Dto;
using Factos.Server.ClientConnection;
using Factos.Server.Settings;
using Factos.Server.Settings.Apps;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.OutputDevice;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
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
            // MTP forwards the user's selection via context.Request.Filter.
            // - NopFilter / non-filter request => run everything on every app (null sentinel).
            // - TestNodeUidListFilter => only the listed UIDs, partitioned per app.
            // Filter UIDs include the `[appName]` prefix that MTPResultsMapper attaches
            // during discovery; strip it before forwarding to the client.
            string[]? selectedUids = null;
            if (context.Request is TestExecutionRequest req && req.Filter is TestNodeUidListFilter list)
            {
                selectedUids = [.. list.TestNodeUids.Select(u => (string)u.Value)];
            }

            var appNames = new HashSet<string?>();

            for (int j = 0; j < testedApps.Count; j++)
            {
                TestedApp? testedApp = testedApps[j];
                var appName = testedApp.Uid ?? "test runner";
                // sanitize name to be file system friendly
                foreach (var c in Path.GetInvalidFileNameChars())
                    appName = appName.Replace(c, '_');

                // prevent repeated names
                var i = 1;
                while (!appNames.Add(appName))
                    appName = $"{testedApp.Uid} ({i++})";
                var cacheFile = $"{appName}_cache.json";

                var (skip, appUids) = TestFilterPartitioner.PartitionForApp(selectedUids, appName);
                if (skip)
                {
                    await deviceWriter.Dimmed(
                        $"Skipping {appName}: no tests in the current filter target this app.",
                        cancellationToken);
                    continue;
                }

                await deviceWriter.Title($"Starting {appName}...", cancellationToken);
                await appRunner.StartApp(testedApp, appName, cancellationToken);

                var timeOut = new CancellationTokenSource(TimeSpan.FromSeconds(settings.ConnectionTimeout));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeOut.Token, cancellationToken);

                var nodesStream = GetNodes(appName, appUids, context, 0, cancellationToken);

                await foreach (var node in nodesStream.WithCancellation(linkedCts.Token))
                {
                    await context.MessageBus.PublishAsync(
                        this, new TestNodeUpdateMessage(context.Request.Session.SessionUid, node));

                    if (node.Properties.Any<FailedTestNodeStateProperty>() || node.Properties.Any<ErrorTestNodeStateProperty>())
                    {
                        // end test on first failure, we are not sure if the client app is still running properly
                        await deviceWriter.Red(
                            $"Test node '{node.Uid}' reported failure or error. Ending test session for '{appName}'.",
                            cancellationToken);

                        break;
                    }
                }

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
        string appName, string[] testUids, ExecuteRequestContext context, int retry, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await deviceWriter.Blue(
            "Waiting for test to run in the client app...", cancellationToken);

        var protocol = ProtocolosLifeTime.ActiveProtocols.First();
        cancellationToken = CancellationToken.None;

        var isEmpty = true;
        await foreach (var node in protocol.RequestClient(appName, testUids, cancellationToken))
        {
            isEmpty = false;
            yield return MTPResultsMapper.ReadNode(appName, node);
        }

        if (isEmpty)
        {
            await deviceWriter.Green(
                $"No test results were received from the client app, attempt {retry + 1}/3", cancellationToken);

            if (retry < 2)
            {
                await foreach (var node in GetNodes(appName, testUids, context, retry + 1, cancellationToken))
                    yield return node;
            }
            else
            {
                await deviceWriter.Red(
                    "Maximum retry attempts reached. No test results were received from the client app.", cancellationToken);
            }
        }
    }
}
