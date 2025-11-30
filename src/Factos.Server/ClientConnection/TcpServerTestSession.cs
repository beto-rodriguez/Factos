using Factos.Abstractions;
using Factos.Abstractions.Dto;
using Factos.Server.Settings;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Factos.Server.ClientConnection;

internal sealed class TcpServerTestSession(
    DeviceWritter serviceProvider,
    FactosSettings factosSettings) 
        : IServerSessionProtocol
{
    TcpListener listener = new(IPAddress.Loopback, factosSettings.TcpPort);
    readonly DeviceWritter deviceWritter = serviceProvider;

    public string Id =>
        nameof(TcpServerTestSession);

    public async Task Start(CancellationToken cancellationToken)
    {
        listener.Start();

        await deviceWritter.Dimmed(
            $"TCP server listening on {listener.LocalEndpoint}", cancellationToken);
    }

    public async Task Finish(CancellationToken cancellationToken)
    {
        listener.Stop();
        listener.Server.Dispose();
        listener = null!;

        await deviceWritter.Title("TCP server stopped", cancellationToken, true);
    }

    public Task<NodesResponse> RequestClient(string clientName, ExecuteRequestContext context)
    {
        if (context.Request is DiscoverTestExecutionRequest)
            return GetTestNodesStream(this, Constants.START_DISCOVER_STREAM, clientName, context.CancellationToken);

        if (context.Request is RunTestExecutionRequest)
            return GetTestNodesStream(this, Constants.START_RUN_STREAM, clientName, context.CancellationToken);

        throw new NotImplementedException("Only discover and run requests are supported.");
    }

    public async Task CloseClient(string clientName, CancellationToken cancellationToken)
    {
        var quitRequest = await ReadStream(
                Constants.QUIT_APP, clientName, cancellationToken);

        if (quitRequest == Constants.QUIT_APP)
            // at this point the client answers to the quit request
            await deviceWritter.Dimmed(
                $"Client has acknowledged the quit request.", cancellationToken);
    }

    private async Task<string> ReadStream(
        string name, string appName, CancellationToken cancellationToken)
    {
        await deviceWritter.Dimmed($"Waiting for {appName} to respond '{name}' on {listener.LocalEndpoint}...", cancellationToken);

        using var client = await listener.AcceptTcpClientAsync(cancellationToken);
        using var stream = client.GetStream();
        using var writer = new StreamWriter(stream) { AutoFlush = true };

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

    private async Task<NodesResponse> GetTestNodesStream(
        TcpServerTestSession session, string streamName, string clientName, CancellationToken cancellationToken)
    {
        var testNodesJson = await session.ReadStream(
            streamName, clientName, cancellationToken);

        var testNodes = JsonSerializer.Deserialize<TestNodeDto[]>(testNodesJson);
        var results = new List<TestNode>();

        foreach (var nodeDto in testNodes ?? [])
        {
            var methodId = nodeDto.Properties
                .FirstOrDefault(x => x is TestMethodIdentifierPropertyDto);

            if (methodId is not null)
            {
                var tmip = (TestMethodIdentifierPropertyDto)methodId;

                // hack to display properly tests in the VS UI
                // specially when running the same project for different targets.
                tmip.Namespace = $"[{clientName}] {tmip.Namespace}";
            }

            var testNode = new TestNode
            {
                DisplayName = $"[{clientName}]{nodeDto.DisplayName}",
                Uid = $"[{clientName}]{nodeDto.Uid}",
                Properties = nodeDto.Properties.AsPropertyBagResult()
            };

            testNode.FillTrxProperties(nodeDto);

            results.Add(testNode);
        }

        return new() { Nodes = results, Sender = this };
    }
}
