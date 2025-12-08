namespace Factos.Server.Settings.Apps;

/// <summary>
/// Gets or sets the context information for the tested application, including project path,
/// </summary>
public record struct TestedAppContext(
    string? Project,
    string? TargetFramework,
    string? Configuration,
    string? RuntimeIdentifier,
    MSBuildArg[]? MsBuildArgs,
    AppHost AppHost
);
