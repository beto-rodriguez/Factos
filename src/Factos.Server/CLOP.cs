using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.CommandLine;

namespace Factos.Server;

internal class CLOP(IReadOnlyCollection<string> flags)
    : BaseExtension, ICommandLineOptionsProvider
{
    private readonly CommandLineOption[] options = 
        [.. flags.Select(flag => new CommandLineOption(flag, $"Runs the apps marked with '{flag}'.", ArgumentArity.Zero, false))];

    override protected string Id => nameof(CLOP);

    public IReadOnlyCollection<CommandLineOption> GetCommandLineOptions() => options;

    public Task<ValidationResult> ValidateCommandLineOptionsAsync(ICommandLineOptions commandLineOptions) =>
        Task.FromResult(ValidationResult.Valid());

    public Task<ValidationResult> ValidateOptionArgumentsAsync(CommandLineOption commandOption, string[] arguments) =>
        Task.FromResult(ValidationResult.Valid());
}