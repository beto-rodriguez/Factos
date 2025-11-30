namespace Factos.Server.Settings;

internal class TestRunnerAppSettings
{
    public string? Name { get; set; }
    public string[] StartCommands { get; set; } = [];
    public string[] EndCommands { get; set; } = [];
}