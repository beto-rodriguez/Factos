using Factos.Server.Settings;
using Microsoft.Testing.Platform.Builder;

namespace Factos.Server;

public static class FactosExtensions
{
    public static ITestApplicationBuilder AddFactos(
        this ITestApplicationBuilder builder,
        FactosSettings settings)
    {
        builder.CommandLine.AddProvider(
            () => new CommandLineOptionsProvider());

        builder.TestHost.AddTestHostApplicationLifetime(
            serviceProvider => new ProtocolosLifeTime(serviceProvider, settings));

        builder.RegisterTestFramework(
            (serviceProvider) => new FactosCapabilities(),
            (capabilities, serviceProvider) => new FactosFramework(serviceProvider, settings));

        return builder;
    }
}
