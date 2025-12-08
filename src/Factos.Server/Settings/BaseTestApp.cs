using Factos.Abstractions;
using Factos.Server.Settings.Apps;
using System.Text;

namespace Factos.Server.Settings;

public abstract class BaseTestApp : TestedApp
{
    public string OutputPath { get; set; } = "bin/artifacts";
    
    public string? PublishArgs { get; set; } = "-c Release";
    
    public bool ManualStart { get; set; } = false;

    public static CustomApp FromCommands(
        (string projectPath, string? outputPath, string[]? groups) config,
        Func<CustomApp, string[]> commands)
    {
        var app = new CustomApp() { ProjectPath = config.projectPath };

        app.OutputPath = config.outputPath ?? app.OutputPath;
        app.StartupCommands = commands(app);
        app.StartupCommands = [.. // clean commands multiple lines
            app.StartupCommands
                .Select(x => x
                    .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                    .Aggregate("", (a, b) => a + " " + b.Trim())
                    .Trim())
            ];

        return app;
    }

    public class CustomApp : BaseTestApp
    {
    }
}

public static class TestedAppExtensions
{
    extension (IList<TestedApp> appsList)
    {
        public IList<TestedApp> Add(
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

        public IList<TestedApp> AddFromCommands(
            string[] commands,
            string? displayName)
        {
            var app = new TestedApp
            {
                Uid = displayName ?? "Tested App",
                StartupCommands = commands
            };

            appsList.Add(app);

            return appsList;
        }
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
