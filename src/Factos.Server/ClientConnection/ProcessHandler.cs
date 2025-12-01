using System.Diagnostics;

namespace Factos.Server.ClientConnection;

internal class ProcessHandler
{
    readonly Process _process;
    readonly List<int> _alsoKill = [];
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

            if (e.Data.StartsWith("FactosKillOnFinish "))
            {
                var pidText = e.Data["FactosKillOnFinish ".Length..].Trim();
                if (int.TryParse(pidText, out var pid))
                {
                    await deviceWritter.Dimmed(
                        $"The process with PID {pid} will be killed with this process.", cancellationToken);
                    
                    _alsoKill.Add(pid);
                }
            }

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

        if (!isBackground)
            _process.WaitForExit();
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

        foreach (var pid in _alsoKill)
        {
            try
            {
                var proc = Process.GetProcessById(pid);
                if (!proc.HasExited)
                {
                    proc.Kill(true);
                    proc.WaitForExit();
                }
            }
            catch { }
        }

        _process.Dispose();
    }
}