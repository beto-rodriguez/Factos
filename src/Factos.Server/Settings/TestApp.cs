using System.IO;

namespace Factos.Server.Settings;

public abstract class TestApp
{
    public required string ProjectPath { get; set; }
    public string OutputPath { get; set; } = "bin/artifacts";
    public string? DisplayName { get => field ?? GetDefaultDisplayName(); set; }
    public string? PublishArgs { get; set; } = "-c Release";
    public string[]? TestGroups { get; set; }
    public bool ManualStart { get; set; } = false;
    public string[]? Commands 
    {
        get => ManualStart
            ? []
            : field ?? GetDefaultCommands();
        set;
    }
    public Dictionary<string, Func<Task>> Tasks { get; set; } = [];

    protected abstract string GetDefaultDisplayName();
    protected virtual string[]? GetDefaultCommands() => null;

    public static CustomApp FromCommands(
        (string projectPath, string? outputPath, string[]? groups) config,
        Func<CustomApp, string[]> commands)
    {
        var app = new CustomApp() { ProjectPath = config.projectPath };
        app.Commands = commands(app);
        app.Commands = [.. // clean commands multiple lines
            app.Commands
                .Select(x => x
                    .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                    .Aggregate("", (a, b) => a + " " + b.Trim())
                    .Trim())
            ];
        app.OutputPath = config.outputPath ?? app.OutputPath;
        app.TestGroups = config.groups;

        return app;
    }

    public class CustomApp : TestApp
    {
        protected override string GetDefaultDisplayName() => nameof(CustomApp);
    }
}
