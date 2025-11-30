using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// adb path
string adbPath = Environment.ExpandEnvironmentVariables(
    @"%ProgramFiles(x86)%\Android\android-sdk\platform-tools\adb.exe");

// Point directly to the Visual Studio SDK emulator path
string emulatorPath = Environment.ExpandEnvironmentVariables(
    @"%ProgramFiles(x86)%\Android\android-sdk\emulator\emulator.exe");

// Set environment variables like Visual Studio does
Environment.SetEnvironmentVariable("ANDROID_SDK_ROOT",
    Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\Android\android-sdk"));
Environment.SetEnvironmentVariable("ANDROID_AVD_HOME",
    Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\.android\avd"));

// Step 1: List AVDs
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
{
    Console.WriteLine("No valid AVDs found.");
    return 1;
}

// Check if any emulator is already running
bool emulatorRunning = (RunAdb(adbPath, "devices") ?? string.Empty).Split('\n')
    .Any(line => line.StartsWith("emulator-") && line.Contains("device"));

if (emulatorRunning)
{
    Console.WriteLine("An emulator is already running. Skipping start.");
    return 0;
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
    var devices = RunAdb(adbPath, "devices");
    if (devices == null)
    {
        Console.WriteLine("Failed to run adb.");
        return 1;
    }

    if (devices.Contains("emulator-"))
        break;

    Thread.Sleep(2000);
}

Console.WriteLine("Emulator detected, waiting for boot...");

// Wait until sys.boot_completed=1
while (true)
{
    var boot = RunAdb(adbPath, "shell getprop sys.boot_completed");

    if (boot == null)
    {
        Console.WriteLine("Failed to run adb.");
        return 1;
    }

    if (boot.Trim() == "1")
        break;

    Thread.Sleep(2000);
}

Console.WriteLine("Emulator fully booted!");

return 0;

static string? RunAdb(string adbPath, string args)
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