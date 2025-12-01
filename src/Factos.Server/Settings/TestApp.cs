namespace Factos.Server.Settings;

public class TestApp
{
    public string? Name { get; set; }
    public string[] StartCommands { get; set; } = [];
    public string[] EndCommands { get; set; } = [];
}