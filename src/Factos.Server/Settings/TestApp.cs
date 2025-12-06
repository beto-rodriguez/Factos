using System.IO;

namespace Factos.Server.Settings;

public abstract class TestApp
{
    private string _previousDirectory = string.Empty;

    public TestApp()
    {
        Tasks.Add("cd-at-project", CurrentDirectoryAtProjectPathTask);
        Tasks.Add("cd-pop", RestorePreviousDirectory);
    }

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
    public Dictionary<string, Func<DeviceWritter, CancellationToken, Task>> Tasks { get; set; } = [];

    protected abstract string GetDefaultDisplayName();
    protected virtual string[]? GetDefaultCommands() => null;

    private async Task CurrentDirectoryAtProjectPathTask(DeviceWritter deviceWritter, CancellationToken cancellationToken)
    {
        _previousDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(ProjectPath);

        await deviceWritter.Dimmed(
            $"Changed current directory to project path: {ProjectPath}",
            cancellationToken);
    }

    public async Task RestorePreviousDirectory(DeviceWritter deviceWritter, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_previousDirectory)) return;
        
        Directory.SetCurrentDirectory(_previousDirectory);
        _previousDirectory = string.Empty;

        await deviceWritter.Dimmed(
            $"Restored previous directory: {Directory.GetCurrentDirectory()}",
            cancellationToken);
    }

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
