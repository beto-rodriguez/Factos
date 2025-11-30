using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.CommandLine;

namespace Factos.Server.Settings;

internal sealed class CommandLineOptions : ICommandLineOptionsProvider
{
    public const string CONFIG_FILE_PATH = "run-settings";
    public const string DISABLE_HTTP = "no-http";
    public const string DISABLE_TCP = "no-tcp";

    public string Uid => nameof(CommandLineOptions);

    public string Version => "1.0.0";

    public string DisplayName => Uid;

    public string Description => $"Command line options for {nameof(FactosFramework)}";

    public IReadOnlyCollection<CommandLineOption> GetCommandLineOptions() => [
            new(CONFIG_FILE_PATH, "Indicates the path of the config file to use.", ArgumentArity.ExactlyOne, false),
            new(DISABLE_HTTP, "Disables the http listener.", ArgumentArity.Zero, false),
            new(DISABLE_TCP, "Disables the tcp listener.", ArgumentArity.Zero, false)
        ];

    public Task<bool> IsEnabledAsync() => 
        Task.FromResult(true);

    public Task<ValidationResult> ValidateCommandLineOptionsAsync(ICommandLineOptions commandLineOptions) =>
        ValidationResult.ValidTask;

    public Task<ValidationResult> ValidateOptionArgumentsAsync(CommandLineOption commandOption, string[] arguments)
    {
        if (commandOption.Name == CONFIG_FILE_PATH)
        {
            if (string.IsNullOrWhiteSpace(arguments[0]))
                return ValidationResult.InvalidTask("Config file path cannot be empty");
        }

        return ValidationResult.ValidTask;
    }
}