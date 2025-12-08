namespace Factos.Server.Settings.Apps;

[Flags]
public enum AppHost
{
    Auto = 0,
    Browser = 1 << 0,
    HeadlessChrome = 1 << 1 | Browser
}