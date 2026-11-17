using Factos.Abstractions;
using System.Diagnostics;

namespace Factos.Server.ClientConnection;

internal class ProcessHandler
{
    readonly Process _process;
    bool isIntentionalDispose;

    /// <param name="onBackgroundProcessFailed">
    /// Called with an explanation when a background process (e.g. the tested app) exits
    /// with a non-zero code; foreground commands (e.g. the build) end the run instead.
    /// </param>
    public ProcessHandler(
        string command,
        DeviceWritter deviceWritter,
        CancellationToken cancellationToken,
        Action<string>? onBackgroundProcessFailed = null)
    {
        Command = command;
        var elements = command.Split(' ');

        var fileName = elements[0];
        var rawArgs = command[fileName.Length..].Trim();

        var isBackground = false;
        if (rawArgs.EndsWith(Constants.BACKGROUND_TASK))
        {
            rawArgs = rawArgs[..^Constants.BACKGROUND_TASK.Length].Trim();
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

            await deviceWritter.Dimmed($"{_process.Id} - {e.Data}", cancellationToken);
        };

        _process.ErrorDataReceived += async (sender, e) =>
        {
            if (string.IsNullOrEmpty(e.Data)) 
                return;

            await deviceWritter.Red($"{_process.Id} - {e.Data}", cancellationToken);
        };

        _process.Exited += async (sender, e) =>
        {
            if (isIntentionalDispose)
                return;

            // != 0, not > 0: Windows reports crashes as NTSTATUS codes such as
            // 0xC0000005 or 0xC000027B, which are negative as an int, so a crashed
            // app used to be logged as a clean exit and the run waited until it timed out.
            if (_process.ExitCode != 0)
            {
                var message =
                $"""
                The process has exited unexpectedly.
                It seems that the command you are trying to run is not working as expected.
                Please verify that the command is correct and that all necessary dependencies are installed.
                Exit Code:  {_process.ExitCode} (0x{_process.ExitCode:X8})
                Command:    '{command}'
                """;

                await deviceWritter.Red($"{_process.Id} - {message}", CancellationToken.None);

                if (isBackground && onBackgroundProcessFailed is not null)
                {
                    // the app died while the tests were running, end its session now
                    // so the run reports the failure and moves on instead of hanging
                    onBackgroundProcessFailed(message);
                    return;
                }

                Environment.Exit(1);
            }
            else
            {
                await deviceWritter.Dimmed(
                    $"{_process.Id} - The process finished successfully for command '{command}'", cancellationToken);
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
                $"Started background process for command '{fileName} {rawArgs}' with PID {_process.Id}", cancellationToken);
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
            // disable for now? it seems to hang sometimes in macOS
            // in background processes
            // _process.WaitForExit();
        }

        _process.Dispose();
    }
}