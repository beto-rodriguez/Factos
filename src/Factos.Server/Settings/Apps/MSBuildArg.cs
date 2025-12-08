namespace Factos.Server.Settings.Apps;

/// <summary>
/// Represents an MSBuild argument with a name and value.
/// </summary>
public record struct MSBuildArg(
    string Name,
    string Value
);