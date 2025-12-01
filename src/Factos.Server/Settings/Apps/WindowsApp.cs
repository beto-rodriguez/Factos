namespace Factos.Server.Settings.Apps;

public class WindowsApp : TestApp
{
    public required string ExecutableName { get; set; }

    protected override string[]? GetDefaultStartCommands() => [
            $"dotnet restore {ProjectPath}",
            $"dotnet publish {ProjectPath} -o {ProjectPath}/{OutputPath} {PublishArgs}",
            $"{ProjectPath}/{OutputPath}/{ExecutableName} &"
        ];
}
