namespace Factos.Server.Settings.Apps;

public class BlazorWasmApp : TestApp
{
    public int Port { get; set; } = 5080;
    public bool HeadlessChrome { get; set; } = false;

    protected override string[]? GetDefaultCommands()
    {
        var commands = new List<string>
        {
            $"dotnet restore {ProjectPath}",
            $"dotnet publish {ProjectPath} -o {ProjectPath}/{OutputPath} {PublishArgs}",
            "dotnet tool install --global dotnet-serve",
            $"dotnet serve -d {ProjectPath}/{OutputPath}/wwwroot -p {Port} &"
        };

        var chromeArgs = string.Empty;

        if (HeadlessChrome)
            chromeArgs =
                "--headless=new " +     // run without UI
                "--disable-gpu " +      // disable GPU usage
                "--no-sandbox " +       // disable sandboxing (required in some environments)
                "--enable-logging " +   // enable internal chrome logging
                "--v=1";                // verbosity level

        if (OperatingSystem.IsWindows())
        {
            commands.Add($"cmd.exe /c start http://localhost:{Port} {chromeArgs}");
        }

        if (OperatingSystem.IsLinux())
            commands.Add($"xdg-open http://localhost:{Port} {chromeArgs} &");

        if (OperatingSystem.IsMacOS())
            commands.Add($"open http://localhost:{Port}  {chromeArgs} &");

        return [.. commands];
    }
}
