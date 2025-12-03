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
}
