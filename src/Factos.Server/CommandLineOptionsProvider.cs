using Factos.Server.Settings;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.CommandLine;

namespace Factos.Server;

internal class CommandLineOptionsProvider
    : BaseExtension, ICommandLineOptionsProvider
{
    public const string OPTION_TEST_GROUP = "test-groups";
    public const string OPTION_ENVIRONMENT = "test-env";

    override protected string Id => nameof(CommandLineOptionsProvider);

    public IReadOnlyCollection<CommandLineOption> GetCommandLineOptions() => [
        new CommandLineOption(
            OPTION_TEST_GROUP,
            $"Defines the tests to run based on the defined {nameof(TestApp.TestGroups)} of each {nameof(TestApp)}.",
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