namespace Factos.Server.Settings.Apps;

public class DesktopApp : TestApp
{
    /// <summary>
    /// Gets or sets the name of the executable to run the app, if the extension is not provided,
    /// it will be resolved based on the OS (.exe for Windows, .app for MacOS or none for Linux).
    /// </summary>
    public required string ExecutableName { get; set; }

    protected override string GetDefaultDisplayName() => nameof(DesktopApp);

    protected override string[]? GetDefaultCommands()
    {
        List<string> commands = [
            $"dotnet publish {ProjectPath} -o {ProjectPath}/{OutputPath} {PublishArgs}"
        ];

        var extensionName = Path.GetExtension(ExecutableName).ToLowerInvariant();

        if (OperatingSystem.IsWindows())
        {
            extensionName ??= ".exe";
            commands.Add($"{ProjectPath}/{OutputPath}/{ExecutableName} &");
        }

        if (OperatingSystem.IsMacOS())
        {
            extensionName ??= ".app";
            commands.Add($"open {ProjectPath}/{OutputPath}/{ExecutableName}");
        }

        if (OperatingSystem.IsLinux())
        {
            extensionName ??= string.Empty;
            commands.Add($"{ProjectPath}/{OutputPath}/{ExecutableName} &");
        }

        return [.. commands];
    }
}
