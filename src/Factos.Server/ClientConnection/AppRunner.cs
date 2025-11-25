using Factos.Abstractions;
using Factos.Server.Settings;
using Microsoft.Testing.Platform.Extensions.OutputDevice;
using Microsoft.Testing.Platform.OutputDevice;

namespace Factos.Server.ClientConnection;

internal class AppRunner : BaseExtension, IOutputDeviceDataProducer
{
    readonly Dictionary<string, ProcessHandler> _activeProcesses = [];
    readonly FactosSettings settings;
    readonly DeviceWritter deviceWritter;

    public AppRunner(IOutputDevice outputDevice, FactosSettings factosSettings)
    {
        settings = factosSettings;
        deviceWritter = new(this, outputDevice);
    }

    protected override string Id =>
        nameof(AppRunner);

    public virtual async Task StartApp(
        string[] commands, string appName, CancellationToken cancellationToken)
    {
        await deviceWritter.Dimmed(
            $"{appName} app is starting...", cancellationToken);

        foreach (var command in commands)
        {
            if (command == "{wait-for-process}")
            {
                await deviceWritter.Banner(
                    "The special command '{wait-for-process}' was detected. " +
                    "The test runner will wait for an external process to be started manually.", cancellationToken);

                return;
            }

            await deviceWritter.Normal($"Executing '{command}'...", cancellationToken);

            if (_activeProcesses.TryGetValue(command, out var process) && process.IsRunning)
            {
                await deviceWritter.Dimmed(
                    $"Process for command '{command}' is already running.", cancellationToken);

                return;
            }

            await deviceWritter.Dimmed($"Creating new process for command '{command}'.", cancellationToken);

            var newProcess = new ProcessHandler(command, deviceWritter, cancellationToken);
            _activeProcesses[command] = newProcess;
        }
    }

    public virtual async Task EndApp(
        TcpServerTestSession session, string[] commands, string appName, CancellationToken cancellationToken)
    {
        await deviceWritter.Normal(
           $"{appName} is quitting...", cancellationToken);

        var quitRequest = await TcpServerTestSession.Current.ReadStream(
            Constants.QUIT_APP, appName, settings.ConnectionTimeout, cancellationToken);

        if (quitRequest == Constants.QUIT_APP)
            // at this point the client answers to the quit request
            await deviceWritter.Dimmed(
                $"Client has acknowledged the quit request.", cancellationToken);

        foreach (var command in commands)
        {
            // Dispose the process as it is no longer needed
            if (!_activeProcesses.TryGetValue(command, out var process))
            {
                await deviceWritter.Dimmed(
                    $"No active process found for command '{command}', closing was skipped.", cancellationToken);

                return;
            }

            process.Dispose();
            _activeProcesses.Remove(command);

            await deviceWritter.Dimmed(
                $"Process for command '{command}' has been disposed.", cancellationToken);
        }
    }
}
