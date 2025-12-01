using System.IO;

namespace Factos.Server.Settings;

public abstract class TestApp
{
    public required string ProjectPath { get; set; }
    public string OutputPath { get; set; } = "bin/artifacts";
    public string? DisplayName { get; set; }
    public string? PublishArgs { get; set; }
    public string[]? TestGroups { get; set; }
    public string[]? Commands { get => field ?? GetDefaultStartCommands(); set; }
    public Dictionary<string, Func<Task>> Tasks { get; set; } = [];

    protected virtual string[]? GetDefaultStartCommands() => null;
    protected virtual string[]? GetDefaultEndCommands() => null;
}
