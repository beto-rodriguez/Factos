using Factos.Server.Settings;
using Microsoft.Testing.Platform.Extensions.OutputDevice;
using Microsoft.Testing.Platform.OutputDevice;

namespace Factos.Server.ClientConnection;

internal class AppRunner
    : BaseExtension, IOutputDeviceDataProducer
{
    readonly FactosSettings settings;
    readonly DeviceWritter deviceWritter;
    List<ProcessHandler> _activeProcesses = [];

    public AppRunner(IOutputDevice outputDevice, FactosSettings factosSettings)
    {
        settings = factosSettings;
        deviceWritter = new(this, outputDevice);
    }

    protected override string Id =>
        nameof(AppRunner);

    public virtual async Task StartApp(string[] startCommands, string appName, CancellationToken cancellationToken)
    {
        await deviceWritter.Dimmed(
            $"{appName} app is starting...", cancellationToken);

        for (int i = 0; i < startCommands.Length; i++)
        {
            string? command = startCommands[i];

            if (command == "{wait-for-process}")
            {
                await deviceWritter.Banner(
                    "The special command '{wait-for-process}' was detected. " +
                    "The test runner will wait for an external process to be started manually.", cancellationToken);

                return;
            }

            await deviceWritter.Normal($"Executing '{command}'...", cancellationToken);
            await deviceWritter.Dimmed($"Creating new process for command '{command}'.", cancellationToken);

            _activeProcesses.Add(new ProcessHandler(command, deviceWritter, cancellationToken));
        }
    }

    public virtual async Task EndApp(
        IServerSessionProtocol session, string[] endCommands, string appName, CancellationToken cancellationToken)
    {
        await deviceWritter.Normal($"{appName} is quitting...", cancellationToken);

        // request the test session to close the client application (if supported)
        await session.CloseClient(appName, cancellationToken);

        // dispose handled processes
        foreach (var process in _activeProcesses)
        {
            process.Dispose();
            await deviceWritter.Dimmed(
                $"Process for command '{process.Command}' has been disposed.", cancellationToken);
        }

        foreach (var command in endCommands)
        {
            await deviceWritter.Normal($"Executing '{command}'...", cancellationToken);
            var p = new ProcessHandler(command, deviceWritter, cancellationToken);
            p.Dispose();
        }
    }
}
