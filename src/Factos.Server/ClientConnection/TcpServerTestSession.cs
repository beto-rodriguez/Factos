using Factos.Abstractions;
using Factos.Abstractions.Dto;
using Factos.Server.Settings;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
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

        await deviceWritter.Dimmed($"TCP server listening on {listener.LocalEndpoint}", cancellationToken);
    }

    public async Task Finish(CancellationToken cancellationToken)
    {
        listener.Stop();
        listener.Server.Dispose();
        listener = null!;

        await deviceWritter.Dimmed("TCP server stopped", cancellationToken);
    }

    public async IAsyncEnumerable<TestNodeDto> RequestClient(string clientName, ExecuteRequestContext context)
    {
        const string nodePrefix = "node ";

        await foreach (var line in ReadStream(Constants.EXECUTE_TESTS, clientName, context.CancellationToken))
        {
            if (line.StartsWith(nodePrefix))
            {
                var json = line[nodePrefix.Length..];

                var testNode = JsonSerializer.Deserialize(
                    json, JsonGenerationContext.Default.TestNodeDto);

                yield return testNode ??
                    throw new InvalidOperationException("Could not deserialize the test node.");
            }
        }
    }

    public async Task CloseClient(string clientName, CancellationToken cancellationToken)
    {
        await foreach (var line in ReadStream(Constants.QUIT_APP, clientName, cancellationToken))
        {
            if (line == Constants.QUIT_APP)
            {
                // at this point the client answers to the quit request
                await deviceWritter.Dimmed(
                    $"Client has acknowledged the quit request.", cancellationToken);
            }
        }
    }

    private async IAsyncEnumerable<string> ReadStream(
        string name, string appName, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await deviceWritter.Dimmed(
            $"Waiting for {appName} to respond '{name}' on {listener.LocalEndpoint}...", cancellationToken);

        using var client = await listener.AcceptTcpClientAsync(cancellationToken);
        using var stream = client.GetStream();
        using var writer = new StreamWriter(stream) { AutoFlush = true };

        writer.WriteLine(name);

        using var reader = new StreamReader(stream);

        string? line = null;

        while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
        {
            if (line.Length == 0)
                continue;

            if (line == Constants.END_STREAM)
            {
                await deviceWritter.Dimmed(
                    "Message received, client connection will be closed soon.", cancellationToken);

                yield break;
            }

            yield return line;
        }
    }
}
