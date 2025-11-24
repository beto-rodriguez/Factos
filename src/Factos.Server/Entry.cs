using Factos.Server.ClientConnection;
using Factos.Server.Settings;
using Microsoft.Testing.Extensions;
using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Services;

namespace Factos.Server;

public class Entry
{
    public static async Task<int> RunTests(string[] args)
    {
        var builder = await TestApplication.CreateBuilderAsync(args);

        builder.CommandLine.AddProvider(CommandLineOptions.Create);
        builder.TestHost.AddTestHostApplicationLifetime(TcpServerTestSession.Create);

        builder.RegisterTestFramework(
            TestingFrameworkCapabilities.Create,
            (capabilities, serviceProvider) =>
                new TestingFramework(serviceProvider.GetOutputDevice()));

        builder.AddTrxReportProvider();

        using ITestApplication testApp = await builder.BuildAsync();

        return await testApp.RunAsync();
    }
}
