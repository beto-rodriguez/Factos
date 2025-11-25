using Factos.Abstractions;
using Factos.Abstractions.Dto;
using Factos.Server.Settings;
using Microsoft.Testing.Extensions.TrxReport.Abstractions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.OutputDevice;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Extensions.TestHost;
using Microsoft.Testing.Platform.OutputDevice;
using Microsoft.Testing.Platform.Requests;
using Microsoft.Testing.Platform.Services;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace Factos.Server.ClientConnection;

internal sealed class TcpServerTestSession
    : BaseExtension, ITestHostApplicationLifetime, IOutputDeviceDataProducer
{
    TcpListener listener;
    readonly AppRunner appRunner;
    readonly DeviceWritter deviceWritter;
    readonly FactosSettings settings;

    public TcpServerTestSession(
        FactosSettings settings, IOutputDevice outputDevice)
    {
        this.settings = settings;
        listener = new(IPAddress.Loopback, settings.Port);
        appRunner = new(outputDevice, settings);
        deviceWritter = new(this, outputDevice);
    }

    protected override string Id =>
        nameof(TcpServerTestSession);

    public static TcpServerTestSession Current { get; private set; } = null!;

    public static TcpServerTestSession Create(IServiceProvider serviceProvider) 
    {
        if (Current is not null)
            return Current;

        var settings = FactosSettings.ReadFrom(serviceProvider);
        Current = new TcpServerTestSession(settings, serviceProvider.GetOutputDevice());

        return Current;
    }

    async Task ITestHostApplicationLifetime.BeforeRunAsync(CancellationToken cancellationToken)
    {
        listener.Start();

        await deviceWritter.Title(
            "Test session started", cancellationToken);

        await deviceWritter.Dimmed(
            $"""
             TCP server listening on {listener.LocalEndpoint}
             Test runners app will timeout after {settings.Timeout} seconds if they don't connect.
             """, cancellationToken);
    }

    async Task ITestHostApplicationLifetime.AfterRunAsync(int exitCode, CancellationToken cancellationToken)
    {
        listener.Stop();
        listener.Server.Dispose();
        listener = null!;

        await deviceWritter.Title("Test session finished", cancellationToken, true);
    }

    public IAsyncEnumerable<TestNode> RequestTcpClientExecution(
        ExecuteRequestContext context)
    {
        if (context.Request is DiscoverTestExecutionRequest)
            return GetTestNodesStream(this, Constants.START_DISCOVER_STREAM, context.CancellationToken);

        if (context.Request is RunTestExecutionRequest)
            return GetTestNodesStream(this, Constants.START_RUN_STREAM, context.CancellationToken);

        return AsyncEnumerable.Empty<TestNode>();
    }

    private async IAsyncEnumerable<TestNode> GetTestNodesStream(
        TcpServerTestSession session, string streamName, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var testRunner in settings.TestedApps)
        {
            var appName = testRunner.Name ?? "test runner";

            await deviceWritter.Title($"Running {appName}", cancellationToken);

            await appRunner.EnsureProcessIsRunning(testRunner.Command, appName, cancellationToken);

            var testNodesJson = await session.ReadStream(
                streamName, appName, settings.Timeout, cancellationToken);

            var testNodes = JsonSerializer.Deserialize<TestNodeDto[]>(testNodesJson);

            foreach (var nodeDto in testNodes ?? [])
            {
                var methodId = nodeDto.Properties
                    .FirstOrDefault(x => x is TestMethodIdentifierPropertyDto);

                if (methodId is not null)
                {
                    var tmip = (TestMethodIdentifierPropertyDto)methodId;

                    // hack to display properly tests in the VS UI
                    // specially when running the same project for different targets.
                    tmip.Namespace = $"[{appName}] {tmip.Namespace}";
                }
                
                var testNode = new TestNode
                {
                    DisplayName = $"[{appName}]{nodeDto.DisplayName}",
                    Uid = $"[{testRunner.Name}]{nodeDto.Uid}",
                    Properties = nodeDto.Properties.AsPropertyBagResult()
                };

                FillTrxProperties(testNode, nodeDto);

                yield return testNode;
            }

            await appRunner.DisposeProcess(
                session, testRunner.Command, appName, cancellationToken);

            var count = MTPResultsMapper.LogCount(appName, deviceWritter, cancellationToken);

            await deviceWritter.Title($"Ending {appName}", cancellationToken);
        }

        await deviceWritter.Dimmed(
            "The result of all the apps is displayed below, " +
            "each app logged its own results (see log above).", cancellationToken);
    }

    public async Task<string> ReadStream(
        string name, string appName, int timeOut, CancellationToken cancellationToken)
    {
        await deviceWritter.Normal(
            $"Waiting for {appName} on {listener.LocalEndpoint}...", cancellationToken);

        var ct = new CancellationTokenSource(TimeSpan.FromSeconds(timeOut)).Token;

        using var client = await listener.AcceptTcpClientAsync(ct);
        using var stream = client.GetStream();
        using var writer = new StreamWriter(stream) { AutoFlush = true };

        await deviceWritter.Dimmed(
            $"Requesting the client with the '{name}' command.", cancellationToken);

        writer.WriteLine(name);

        using var reader = new StreamReader(stream);

        var sb = new StringBuilder();
        string? line = null;

        while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
        {
            if (line is null || line.Length == 0) 
                continue;

            if (line == Constants.END_STREAM)
            {
                await deviceWritter.Dimmed(
                    "Message received, client connection will be closed soon.", cancellationToken);

                break;
            }

            sb.AppendLine(line);
        }

        return sb.ToString();
    }

    private static void FillTrxProperties(TestNode testNode, TestNodeDto dto)
    {
        testNode.Properties.Add(new TrxFullyQualifiedTypeNameProperty(dto.Uid));

        var errors = dto.Properties
            .Where(x => x is ErrorTestNodeStatePropertyDto or FailedTestNodeStatePropertyDto)
            .Aggregate(string.Empty, (a, b) => a + ((TestNodePropertyDto)b).Explanation);

        if (!string.IsNullOrEmpty(errors))
            testNode.Properties.Add(new TrxExceptionProperty("Exception", errors));
    }
}
