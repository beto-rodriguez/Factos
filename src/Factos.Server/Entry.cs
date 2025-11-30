using Factos.Server.Settings;
using Microsoft.Testing.Extensions;
using Microsoft.Testing.Platform.Builder;

namespace Factos.Server;

public class Entry
{
    public static async Task<int> RunTests(string[] args)
    {
        var builder = await TestApplication.CreateBuilderAsync(args);

        builder.CommandLine.AddProvider(
            ()                              => new CommandLineOptions());

        builder.TestHost.AddTestHostApplicationLifetime(
            serviceProvider                 => new ProtocolosLifeTime(serviceProvider));

        builder.RegisterTestFramework(
            (serviceProvider)               => new FactosCapabilities(),
            (capabilities, serviceProvider) => new FactosFramework(serviceProvider));

        builder.AddTrxReportProvider();

        using ITestApplication testApp = await builder.BuildAsync();

        return await testApp.RunAsync();
    }
}
