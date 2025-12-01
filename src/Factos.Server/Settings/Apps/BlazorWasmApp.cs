namespace Factos.Server.Settings.Apps;

public class BlazorWasmApp : TestApp
{
    public int Port { get; set; } = 5080;

    protected override string[]? GetDefaultStartCommands()
    {
        var commands = new List<string>
        {
            $"dotnet restore {ProjectPath}",
            $"dotnet publish {ProjectPath} -o {ProjectPath}/{OutputPath} {PublishArgs}",
            "dotnet tool install --global dotnet-serve",
            $"dotnet serve -d {ProjectPath}/{OutputPath}/wwwroot -p {Port} &"
        };

        if (OperatingSystem.IsWindows())
            commands.Add($"cmd.exe /c start http://localhost:{Port}");

        if (OperatingSystem.IsLinux())
            commands.Add($"xdg-open http://localhost:{Port} &");

        if (OperatingSystem.IsMacOS())
            commands.Add($"open http://localhost:{Port}");

        return [.. commands];
    }
}
