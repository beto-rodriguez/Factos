using System.Diagnostics;

namespace Factos.Server.ClientConnection;

internal class ProcessHandler
{
    readonly Process _process;
    bool isIntentionalDispose;

    public ProcessHandler(string command, DeviceWritter deviceWritter, CancellationToken cancellationToken)
    {
        Command = command;
        var elements = command.Split(' ');

        var fileName = elements[0];
        var rawArgs = command[fileName.Length..].Trim();

        var isBackground = false;
        if (rawArgs.EndsWith('&'))
        {
            rawArgs = rawArgs[..^1].Trim();
            isBackground = true;
        }

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = rawArgs,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        _process.OutputDataReceived += async (sender, e) =>
        {
            if (string.IsNullOrEmpty(e.Data)) 
                return;

            await deviceWritter.Dimmed($"[{fileName}:{_process.Id}]{e.Data}", cancellationToken);
        };

        _process.ErrorDataReceived += async (sender, e) =>
        {
            if (string.IsNullOrEmpty(e.Data)) 
                return;

            await deviceWritter.Red($"[{fileName}:{_process.Id}]{e.Data}", cancellationToken);
        };

        _process.Exited += async (sender, e) =>
        {
            if (isIntentionalDispose)
                return;

            if (_process.ExitCode > 0)
            {
                var message =
                $"""
                The process has exited unexpectedly.
                It seems that the command you are trying to run is not working as expected.
                Please verify that the command is correct and that all necessary dependencies are installed.
                Exit Code:  {_process.ExitCode}
                Command:    '{command}'
                """;

                await deviceWritter.Red($"[{fileName}:{_process.Id}]{message}", cancellationToken);

                Environment.Exit(1);
            }
            else
            {
                await deviceWritter.Dimmed(
                    $"[{fileName}:{_process.Id}] The process finished successfully for command '{command}'", cancellationToken);
            }
        };

        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        cancellationToken.Register(Dispose);

        if (!isBackground)
        {
            _process.WaitForExit();
        }
        else
        {
            _ = deviceWritter.Dimmed(
                $"[{fileName}:{_process.Id}] Started background process for command '{command}' with PID {_process.Id}", cancellationToken);
        }
    }

    public bool IsRunning => !_process?.HasExited == true;
    public string Command { get; }
    public Process Process => _process;

    public void Dispose()
    {
        isIntentionalDispose = true;

        if (!_process.HasExited)
        {
            _process.Kill(true);
            _process.WaitForExit();
        }

        _process.Dispose();
    }
}