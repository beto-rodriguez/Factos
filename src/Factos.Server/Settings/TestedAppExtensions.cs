using Factos.Abstractions;
using Factos.Server.Settings.Apps;
using System.Text;

namespace Factos.Server.Settings;

public static class TestedAppExtensions
{
    public static IList<TestedApp> Add(
        this IList<TestedApp> appsList,
        string? project = null,
        string? targetFramework = null,
        string? configuration = null,
        string? runtimeIdentifier = null,
        MSBuildArg[]? msBuildArgs = null,
        AppHost appHost = AppHost.Auto,
        int browserServePort = 5080,
        string? uid = null)
    {
#if !DEBUG
            configuration ??= "Release";
#endif

        var ctx = new TestedAppContext(
            project, targetFramework, configuration, runtimeIdentifier, msBuildArgs, appHost);

        var app = new TestedApp
        {
            Context = ctx,
            ProjectPath = project,
            Uid = uid ?? Path.GetFileNameWithoutExtension(project),
            StartupCommands = [
                TestedApp.CD_AT_PROJECT_COMMAND,
                    Dotnet("build", ctx),
                    Dotnet("run", ctx, $"--no-build {Constants.BACKGROUND_TASK}"),
                    TestedApp.CD_POP_COMMAND
            ]
        };

        if (appHost.HasFlag(AppHost.Browser))
        {
            // if browser, we publish and serve instead, it is easier to manage that way
            var outputPath = $"{project}/bin/publish";

            app.StartupCommands = [
                Dotnet($"publish {ctx.Project}", ctx, $"-o {outputPath}"),
                    "dotnet tool install --global dotnet-serve",
                    $"dotnet serve -d {outputPath}/wwwroot -p {browserServePort} {Constants.BACKGROUND_TASK}",
                    GetBrowserStartCommands(appHost, browserServePort)
            ];
        }

        appsList.Add(app);

        return appsList;
    }

    private static string Dotnet(string command, TestedAppContext ctx, string? extras = null)
    {
        var sb = new StringBuilder();
        sb.Append($"dotnet {command}");

        var targetFramework = ctx.TargetFramework;
        var configuration = ctx.Configuration;
        var runtimeIdentifier = ctx.RuntimeIdentifier;
        var msBuildArgs = ctx.MsBuildArgs;
        
        if (!string.IsNullOrEmpty(targetFramework))
            sb.Append($" -f {targetFramework}");

        if (!string.IsNullOrEmpty(configuration))
            sb.Append($" -c {configuration}");

        if (!string.IsNullOrEmpty(runtimeIdentifier))
            sb.Append($" -r {runtimeIdentifier}");

        if (msBuildArgs is not null)
        {
            foreach (var arg in msBuildArgs)
                sb.Append($" -p:{arg.Name}={arg.Value}");
        }

        if (extras is not null)
            sb.Append($" {extras}");

        return sb.ToString();
    }

    private static string GetBrowserStartCommands(AppHost appHost, int port)
    {
        if (appHost.HasFlag(AppHost.Browser))
        {
            var chromeArgs = string.Empty;

            if (appHost.HasFlag(AppHost.HeadlessChrome))
                chromeArgs =
                    "--headless=new " +     // run without UI
                    "--disable-gpu " +      // disable GPU usage
                    "--no-sandbox " +       // disable sandboxing (required in some environments)
                    "--enable-logging " +   // enable internal chrome logging
                    "--v=1";                // verbosity level

            if (OperatingSystem.IsWindows())
                return $"cmd.exe /c start http://localhost:{port} {chromeArgs}";
            else if (OperatingSystem.IsLinux())
                return $"google-chrome http://localhost:{port} {chromeArgs} {Constants.BACKGROUND_TASK}";
            else if (OperatingSystem.IsMacOS())
                return $"/usr/bin/open -a \"Google Chrome\" http://localhost:{port} --args {chromeArgs}";
        }

        throw new PlatformNotSupportedException("Opening browser is not supported on this OS.");
    }
}
