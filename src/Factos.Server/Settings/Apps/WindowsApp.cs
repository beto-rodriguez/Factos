namespace Factos.Server.Settings.Apps;

public class WindowsApp : TestApp
{
    public required string ExecutableName { get; set; }

    protected override string GetDefaultDisplayName() => nameof(WindowsApp);

    protected override string[]? GetDefaultCommands() => [
            $"dotnet restore {ProjectPath}",
            $"dotnet publish {ProjectPath} -o {ProjectPath}/{OutputPath} {PublishArgs}",
            $"{ProjectPath}/{OutputPath}/{ExecutableName} &"
        ];
}
