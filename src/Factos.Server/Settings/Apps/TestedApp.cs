using Factos.Abstractions;

namespace Factos.Server.Settings.Apps;

/// <summary>
/// Represents an application under test, including its project path, startup commands, and associated tasks.
/// </summary>
/// <remarks>The TestedApp class provides properties for configuring the file system path to the project, a unique
/// identifier, startup commands, and a collection of tasks that can be executed in the context of the tested
/// application. Tasks are stored as a dictionary mapping task names to asynchronous functions, which can be used to
/// automate common operations such as changing directories or restoring previous states. This class is intended to be
/// used as a container for application-specific configuration and automation routines during testing
/// scenarios.</remarks>
public class TestedApp
{
    public TestedApp()
    {
        Tasks.Add("cd-at-project", CurrentDirectoryAtProjectPathTask);
        Tasks.Add("cd-pop", RestorePreviousDirectory);
    }

    private string _previousDirectory = string.Empty;

    /// <summary>
    /// Gets or sets the file system path to the project file.
    /// </summary>
    public string? ProjectPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier associated with the tested application, no spaces allowed.
    /// </summary>
    public string? Uid { get; set; } = nameof(TestedApp);

    /// <summary>
    /// Gets or sets the list of commands to execute to start the application.
    /// </summary>
    public string[] StartupCommands { get; set; } = [];

    /// <summary>
    /// Gets or sets the application context used for testing operations.
    /// </summary>
    public TestedAppContext Context { get; set; } = new();

    /// <summary>
    /// Gets or sets the dictionary of tasks associated with the tested application.
    /// </summary>
    public Dictionary<string, Func<DeviceWritter, CancellationToken, Task>> Tasks { get; set; } = [];

    /// <summary>
    /// Gets the command string used to change the current directory to the project directory within task automation
    /// scripts.
    /// </summary>
    public static string CD_AT_PROJECT_COMMAND => $"{Constants.TASK_COMMAND} cd-at-project";

    /// <summary>
    /// Gets the command string used to trigger the 'cd-pop', it restores the previous directory before 'cd-at-project'.
    /// </summary>
    public static string CD_POP_COMMAND => $"{Constants.TASK_COMMAND} cd-pop";

    private async Task CurrentDirectoryAtProjectPathTask(DeviceWritter deviceWritter, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(ProjectPath)) return;

        _previousDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(ProjectPath);

        await deviceWritter.Dimmed(
            $"Changed current directory to project path: {ProjectPath}",
            cancellationToken);
    }

    private async Task RestorePreviousDirectory(DeviceWritter deviceWritter, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_previousDirectory)) return;

        Directory.SetCurrentDirectory(_previousDirectory);
        _previousDirectory = string.Empty;

        await deviceWritter.Dimmed(
            $"Restored previous directory: {Directory.GetCurrentDirectory()}",
            cancellationToken);
    }
}
