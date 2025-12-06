using Factos.Abstractions;
using Factos.Server.Settings;
using Microsoft.Testing.Platform.Extensions.OutputDevice;
using Microsoft.Testing.Platform.OutputDevice;

namespace Factos.Server.ClientConnection;

internal class AppRunner
    : BaseExtension, IOutputDeviceDataProducer
{
    readonly DeviceWritter deviceWritter;
    readonly List<ProcessHandler> _activeProcesses = [];

    public AppRunner(IOutputDevice outputDevice)
    {
        deviceWritter = new(this, outputDevice);
    }

    protected override string Id =>
        nameof(AppRunner);

    public virtual async Task StartApp(TestApp app, string appName, CancellationToken cancellationToken)
    {
        await deviceWritter.Dimmed(
            $"{appName} app is starting...", cancellationToken);

        var startCommands = app.Commands ?? [];

        for (int i = 0; i < startCommands.Length; i++)
        {
            string? command = startCommands[i];

            if (command == "{wait-for-process}")
            {
                await deviceWritter.Banner(
                    "The special command '{wait-for-process}' was detected. " +
                    "The test runner will wait for an external process to be started manually.", cancellationToken);

                continue;
            }

            await deviceWritter.Normal($"Executing '{command}'...", cancellationToken);
            await deviceWritter.Dimmed($"Creating new process for command '{command}'.", cancellationToken);

            if (command.StartsWith(Constants.TASK_COMMAND))
            {
                // then it is a task registered in the app
                var taskKey = command[Constants.TASK_COMMAND.Length..].Trim();

                if (!app.Tasks.TryGetValue(taskKey, out var taskToRun))
                    throw new InvalidOperationException(
                        $"The task '{taskKey}' was not found in the registered tasks for app '{appName}'.");

                await taskToRun(deviceWritter, cancellationToken);
            }
            else
            {
                _activeProcesses.Add(new ProcessHandler(command, deviceWritter, cancellationToken));
            }
        }
    }

    public virtual async Task EndApp(
        IServerSessionProtocol? session, string appName, CancellationToken cancellationToken)
    {
        await deviceWritter.Normal($"{appName} is quitting...", cancellationToken);

        // request the test session to close the client application (if supported)
        if (session is not null)
            await session.CloseClient(appName, cancellationToken);

        // dispose handled processes
        foreach (var process in _activeProcesses)
        {
            process.Dispose();
            await deviceWritter.Dimmed(
                $"Process for command '{process.Command}' has been disposed.", cancellationToken);
        }

        _activeProcesses.Clear();
    }
}
