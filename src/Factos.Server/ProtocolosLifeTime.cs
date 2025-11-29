using Factos.Abstractions.Dto;
using Factos.Server.ClientConnection;
using Factos.Server.Settings;
using Microsoft.Testing.Platform.Extensions.OutputDevice;
using Microsoft.Testing.Platform.Extensions.TestHost;
using Microsoft.Testing.Platform.Services;

namespace Factos.Server;

internal sealed class ProtocolosLifeTime
    : BaseExtension, ITestHostApplicationLifetime, IOutputDeviceDataProducer
{
    readonly DeviceWritter deviceWritter;
    static IEnumerable<IServerSessionProtocol>? protocols;

    public ProtocolosLifeTime(IServiceProvider serviceProvider)
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

    public static IEnumerable<IServerSessionProtocol> ActiveProtocols => 
        protocols is null || !protocols.Any()
            ? throw new InvalidOperationException("No active protocols available.")
            : protocols;

    protected override string Id =>
        nameof(ProtocolosLifeTime);

    async Task ITestHostApplicationLifetime.BeforeRunAsync(CancellationToken cancellationToken)
    {
        await deviceWritter.Title($"Test session is starting...", cancellationToken);

        foreach (var protocol in ActiveProtocols)
            await protocol.Start(cancellationToken);
    }

    async Task ITestHostApplicationLifetime.AfterRunAsync(int exitCode, CancellationToken cancellationToken)
    {
        foreach (var protocol in ActiveProtocols)
            await protocol.Finish(cancellationToken);

        await deviceWritter.Title("Test session finished", cancellationToken, true);
    }
}