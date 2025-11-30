using System.Diagnostics;

namespace Factos.Server.ClientConnection;

internal class ProcessHandler
{
    Process _process;
    bool isIntentionalDispose;

    public ProcessHandler(string command, DeviceWritter deviceWritter, CancellationToken cancellationToken)
    {
        Command = command;
        var elements = command.Split(' ');

        var fileName = elements[0];
        var rawArgs = command[fileName.Length..].Trim();

        if (fileName == "start")
        {
            // special case for Windows 'start' command
            fileName = "cmd.exe";
            rawArgs = $"/c start {rawArgs}";
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

            await deviceWritter.Dimmed(e.Data, cancellationToken);
        };

        _process.ErrorDataReceived += async (sender, e) =>
        {
            if (string.IsNullOrEmpty(e.Data)) 
                return;

            await deviceWritter.Red(e.Data, cancellationToken);
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

                await deviceWritter.Red(message, cancellationToken);

                Environment.Exit(1);
            }
            else
            {
                await deviceWritter.Dimmed(
                    $"The process finished successfully for command '{command}'", cancellationToken);
            }
        };

        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        cancellationToken.Register(Dispose);
    }

    public bool IsRunning => !_process?.HasExited == true;
    public string Command { get; }

    public void Dispose()
    {
        isIntentionalDispose = true;

        if (!_process.HasExited)
        {
            _process.Kill(true);
            _process.WaitForExit();
        }

        _process.Dispose();
        _process = null!;
    }
}