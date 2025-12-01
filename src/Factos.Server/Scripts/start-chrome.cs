using System;
using System.Diagnostics;

// starts chrome at a specific url
// args:
//      --at <app-target-url>

if (OperatingSystem.IsWindows())
{
    using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe");
    var chromePath = key?.GetValue(null) as string ?? string.Empty;

    if (string.IsNullOrEmpty(chromePath))
        Console.WriteLine("Chrome not found.");

    var proc = Process.Start(new ProcessStartInfo
    {
        FileName = chromePath,
        Arguments = GetArgsValue(args, "--at"),
        UseShellExecute = false
    });

    Console.WriteLine($"FactosKillOnFinish {proc?.Id}");
}

static string? GetArgsValue(string[] args, string arg)
{
    for (var i = 0; i < args.Length; i++)
    {
        string? item = args[i];
        if (item == arg)
        {
            if (i + 1 < args.Length)
                return args[i + 1];
        }
    }

    throw new ArgumentException($"The command {arg} was not found or has no value.");
}
