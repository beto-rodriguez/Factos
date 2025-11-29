using Factos.Server.ClientConnection;
using Factos.Server.Settings;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.OutputDevice;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Services;

namespace Factos.Server;

internal sealed class FactosFramework
    : BaseExtension, ITestFramework, IDataProducer, IOutputDeviceDataProducer
{
    readonly FactosSettings settings;
    readonly DeviceWritter deviceWritter;
    readonly AppRunner appRunner;

    public FactosFramework(IServiceProvider serviceProvider)
    {
        var outputDevice = serviceProvider.GetOutputDevice();
        settings = FactosSettings.ReadFrom(serviceProvider);
        deviceWritter = new(this, outputDevice);
        appRunner = new(outputDevice, settings);
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
            foreach (var testedApp in settings.TestedApps)
            {
                var appName = testedApp.Name ?? "test runner";

                await deviceWritter.Title($"Starting {appName}...", cancellationToken);
                await appRunner.StartApp(testedApp.StartCommands, appName, cancellationToken);

                var requests = new List<Task<NodesResponse>>();

                // listen for test nodes from all protocols
                foreach (var protocol in ProtocolosLifeTime.ActiveProtocols)
                    requests.Add(protocol.RequestClient(appName, context));

                var timeoutTask = GetTimeOutTask();
                requests.Add(GetTimeOutTask());

                // wait for the first protocol to return test nodes
                var response = await Task.WhenAny(requests);

                if (response == timeoutTask)
                {
                    await deviceWritter.Red(
                        $"Connection Error.\n" +
                        $"No test nodes received from any client within the timeout period of " +
                        $"{settings.ConnectionTimeout} seconds.", cancellationToken);

                    throw new TimeoutException("No test nodes received from any client.");
                }

                foreach (var node in response.Result.Nodes)
                    await context.MessageBus.PublishAsync(
                        this,
                        new TestNodeUpdateMessage(context.Request.Session.SessionUid, node));

                await appRunner.EndApp(response.Result.Sender, testedApp.EndCommands, appName, cancellationToken);
                await MTPResultsMapper.LogCount(appName, deviceWritter, cancellationToken);
                await deviceWritter.Title($"{appName} Finished!", cancellationToken);
            }

            await deviceWritter.Dimmed(
                "The result of all the apps is displayed below, " +
                "each app logged its own results (see log above).", cancellationToken);
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

    private async Task<NodesResponse> GetTimeOutTask()
    {
        await  Task.Delay(TimeSpan.FromSeconds(settings.ConnectionTimeout));
        return new NodesResponse { Nodes = [], Sender = null! };
    }
}
