using Factos.Server.Settings.Apps;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.CommandLine;

namespace Factos.Server;

internal class CommandLineOptionsProvider
    : BaseExtension, ICommandLineOptionsProvider
{
    public const string OPTION_SELECT = "select";
    public const string OPTION_ENVIRONMENT = "test-env";

    override protected string Id => nameof(CommandLineOptionsProvider);

    public IReadOnlyCollection<CommandLineOption> GetCommandLineOptions() => [
        new CommandLineOption(
            OPTION_SELECT,
            $"Defines the apps to run, either by {nameof(TestedApp.ProjectPath)} or {nameof(TestedApp.Uid)}",
            ArgumentArity.OneOrMore,
            false),
        new CommandLineOption(
            OPTION_ENVIRONMENT,
            "Defines test environment variables, this variables will be replaced in commands. Use the format 'key=value', " +
            "then the command can use [key] to be replaced with value.",
            ArgumentArity.OneOrMore,
            false)
        ];

    public Task<ValidationResult> ValidateCommandLineOptionsAsync(ICommandLineOptions commandLineOptions) =>
        Task.FromResult(ValidationResult.Valid());

    public Task<ValidationResult> ValidateOptionArgumentsAsync(CommandLineOption commandOption, string[] arguments) =>
        Task.FromResult(ValidationResult.Valid());
}