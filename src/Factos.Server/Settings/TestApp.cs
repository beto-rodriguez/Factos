namespace Factos.Server.Settings;

public class TestApp
{
    public string? RunWhen { get; set; }
    public string? Name { get; set; }
    public string[] StartCommands { get; set; } = [];
    public string[] EndCommands { get; set; } = [];
}