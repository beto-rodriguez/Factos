using Factos.Server.ClientConnection;
using System.Diagnostics;

namespace Factos.Server.Settings.Apps;

public class AndroidApp : TestApp
{
    public AndroidApp()
    {
        Tasks.Add("start-emulator", StartEmulator);
    }

    public required string AppName { get; set; }
    public string? AdbPath { get; set; }
    public string? EmulatorPath { get; set; }

    protected override string GetDefaultDisplayName() => nameof(AndroidApp);

    protected override string[]? GetDefaultCommands() => [
        $"dotnet restore {ProjectPath}",
        $"dotnet publish {ProjectPath} -o {ProjectPath}/{OutputPath} {PublishArgs}",
        $"{AppRunner.TASK_COMMAND} start-emulator",
        $"adb install -r {ProjectPath}/{OutputPath}/{AppName}-Signed.apk",
        $"adb shell monkey -p {AppName} -c android.intent.category.LAUNCHER 1"
    ];

    protected virtual async Task StartEmulator()
    {
        if (AdbPath is null && !OperatingSystem.IsWindows())
            throw new InvalidOperationException(
                "AdbPath must be set on non-Windows systems.");

        var adbPath = AdbPath ?? Environment.ExpandEnvironmentVariables(
            @"%ProgramFiles(x86)%\Android\android-sdk\platform-tools\adb.exe");
        
        Console.WriteLine($"Using adb at: {adbPath}");

        if (EmulatorPath is null && !OperatingSystem.IsWindows())
            throw new InvalidOperationException(
                "EmulatorPath must be set on non-Windows systems.");

        var emulatorPath = EmulatorPath ?? Environment.ExpandEnvironmentVariables(
            @"%ProgramFiles(x86)%\Android\android-sdk\emulator\emulator.exe");
        
        Console.WriteLine($"Using emulator at: {emulatorPath}");

        // List AVDs
        var listInfo = new ProcessStartInfo
        {
            FileName = emulatorPath,
            Arguments = "-list-avds",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        string? firstAvd;
        using (var listProcess = Process.Start(listInfo))
        {
            var output = listProcess?.StandardOutput.ReadToEnd();
            listProcess?.WaitForExit();

            firstAvd = output?
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(line =>
                    !line.StartsWith("INFO") &&
                    !line.StartsWith("ERROR"));
        }

        if (string.IsNullOrEmpty(firstAvd))
            throw new FileNotFoundException(
                "No valid AVDs found. At least an Android emulator must be installed.");

        // Check if any emulator is already running
        bool emulatorRunning = (RunAdb(adbPath, "devices") ?? string.Empty).Split('\n')
            .Any(line => line.StartsWith("emulator-") && line.Contains("device"));

        if (emulatorRunning)
        {
            Console.WriteLine("An emulator is already running. Skipping start.");
            return;
        }

        Console.WriteLine($"Starting emulator: {firstAvd}");

        var startInfo = new ProcessStartInfo
        {
            FileName = emulatorPath,
            Arguments = "-avd pixel_7_-_api_36 -netdelay none -netspeed full",
            UseShellExecute = true,
            CreateNoWindow = false,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        Process.Start(startInfo);
        Console.WriteLine("Emulator launched, waiting for device...");

        // Wait until emulator shows up in adb devices
        while (true)
        {
            var devices = RunAdb(adbPath, "devices") ?? throw new Exception("Failed to run adb.");

            if (devices.Contains("emulator-"))
                break;

            await Task.Delay(2000);
        }

        Console.WriteLine("Emulator detected, waiting for boot...");

        // Wait until sys.boot_completed=1
        while (true)
        {
            var boot = RunAdb(adbPath, "shell getprop sys.boot_completed")
                ?? throw new Exception("Failed to get boot status from adb.");

            if (boot.Trim() == "1")
                break;

            await Task.Delay(2000);
        }

        Console.WriteLine("Emulator fully booted!");
    }

    private static string? RunAdb(string adbPath, string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = adbPath,
            Arguments = args,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var proc = Process.Start(psi);
        var output = proc?.StandardOutput.ReadToEnd();
        proc?.WaitForExit();
        return output;
    }
}