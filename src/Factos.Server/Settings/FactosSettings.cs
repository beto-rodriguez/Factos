using Factos.Abstractions;
using Microsoft.Testing.Platform.Services;
using System.Text.Json;

namespace Factos.Server.Settings;

internal class FactosSettings
{
    public string Version { get; set; } = "1.0";
    public int Timeout { get; set; } = 300;
    public int Port { get; set; } = Constants.DEFAULT_TCP_PORT;
    public TestRunnerAppSettings[] TestedApps { get; set; } = [];

    public static JsonSerializerOptions JsonOptions { get; } =
        new() { PropertyNameCaseInsensitive = true };

    public static FactosSettings ReadFrom(IServiceProvider serviceProvider)
    {
        var configFile = serviceProvider.GetConfiguration();
        var fromConfigFile = configFile["CustomTestingFramework:DisableParallelism"];
        if (fromConfigFile is null)
        {
            // i dont know why no matter what i do this is always null...
            // so instead we fallback to command line args 
        }

        var cliOptions = serviceProvider.GetCommandLineOptions();

        if (!cliOptions.TryGetOptionArgumentList(CommandLineOptions.CONFIG_FILE_PATH, out var configFilePathArgs))
            configFilePathArgs = ["./factos.json"]; // fallback default

        var filePath = configFilePathArgs[0];

        using var settingsReader = new StreamReader(filePath);
        var content = settingsReader.ReadToEnd();

        var config = JsonSerializer.Deserialize<FactosSettings>(content, JsonOptions) ??
            throw new InvalidOperationException(
                $"Unable to read or create the factos setting file, ensure '{filePath}' is valid.");

        return config;
    }
}
