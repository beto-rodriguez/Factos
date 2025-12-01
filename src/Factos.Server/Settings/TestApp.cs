using System.IO;

namespace Factos.Server.Settings;

public abstract class TestApp
{
    public required string ProjectPath { get; set; }
    public string OutputPath { get; set; } = "bin/artifacts";
    public string? DisplayName { get; set; }
    public string? PublishArgs { get; set; } = "-c Release";
    public string[]? TestGroups { get; set; }
    public string[]? Commands { get => field ?? GetDefaultCommands(); set; }
    public Dictionary<string, Func<Task>> Tasks { get; set; } = [];

    protected virtual string[]? GetDefaultCommands() => null;
}
