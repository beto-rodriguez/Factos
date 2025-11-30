using Factos.Abstractions.Dto;
using Factos.Server.Settings;
using Microsoft.Testing.Platform.Extensions.OutputDevice;
using Microsoft.Testing.Platform.Extensions.TestHost;
using Microsoft.Testing.Platform.Services;

namespace Factos.Server.ClientConnection;

internal sealed class TestServerHost
    : BaseExtension, ITestHostApplicationLifetime, IOutputDeviceDataProducer
{
    readonly DeviceWritter deviceWritter;
    readonly IEnumerable<IServerSessionProtocol> protocols;

    public TestServerHost(IServiceProvider serviceProvider)
    {
        deviceWritter = new(this, serviceProvider.GetOutputDevice());
        var settings = FactosSettings.ReadFrom(serviceProvider);
        var cliOptions = serviceProvider.GetCommandLineOptions();

        var activeProtocols = new List<IServerSessionProtocol>();

        if (!cliOptions.IsOptionSet(CommandLineOptions.DISABLE_HTTP))
            activeProtocols.Add(new HTTPServerTestSession(deviceWritter, settings));

        if (!cliOptions.IsOptionSet(CommandLineOptions.DISABLE_TCP))
            activeProtocols.Add(new TcpServerTestSession(deviceWritter, settings));

        protocols = activeProtocols;
    }

    public event Action<TestNodeDto[]>? NodesReceived;

    protected override string Id =>
        nameof(TestServerHost);

    async Task ITestHostApplicationLifetime.BeforeRunAsync(CancellationToken cancellationToken)
    {
        await deviceWritter.Title($"Test session is starting...", cancellationToken);

        foreach (var protocol in protocols)
            await protocol.Start(cancellationToken);
    }

    async Task ITestHostApplicationLifetime.AfterRunAsync(int exitCode, CancellationToken cancellationToken)
    {
        foreach (var protocol in protocols)
            await protocol.Finish(cancellationToken);

        await deviceWritter.Title("Test session finished", cancellationToken, true);
    }
}