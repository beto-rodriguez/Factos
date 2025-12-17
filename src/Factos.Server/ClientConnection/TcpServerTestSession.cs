using Factos.Abstractions;
using Factos.RemoteTesters;
using Factos.Server.Settings;
using Microsoft.Testing.Platform.Extensions.TestFramework;
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

        await deviceWritter.Dimmed($"TCP server listening on {listener.LocalEndpoint}", cancellationToken);
    }

    public async Task Finish(CancellationToken cancellationToken)
    {
        listener.Stop();
        listener.Server.Dispose();
        listener = null!;

        await deviceWritter.Dimmed("TCP server stopped", cancellationToken);
    }

    public Task<ExecutionResponse> RequestClient(string clientName, ExecuteRequestContext context) =>
        GetTestNodesStream(
            this, Constants.EXECUTE_TESTS, clientName, context);

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
        try
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
                if (line.Length == 0)
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
        catch (Exception ex)
        {
            await deviceWritter.Red(
                $"Error reading stream '{name}' from client '{appName}': {ex.Message}\n{ex.StackTrace}", cancellationToken);

            throw new InvalidOperationException(
                $"Could not read stream '{name}' from client '{appName}'.", ex);
        }
    }

    private static async Task<ExecutionResponse> GetTestNodesStream(
        TcpServerTestSession session, string streamName, string clientName, ExecuteRequestContext request)
    {
        var executionResponseJson = await session.ReadStream(
            streamName, clientName, request.CancellationToken);

        var executionResponse = JsonSerializer.Deserialize(executionResponseJson, JsonGenerationContext.Default.ExecutionResponse);

        return executionResponse 
            ?? throw new InvalidOperationException("Could not deserialize the execution response.");
    }
}
