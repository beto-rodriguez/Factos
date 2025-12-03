using Factos.Abstractions;
using System.Net.Sockets;
using System.Text.Json;

namespace Factos.Protocols;

public class TcpProtocolHandler : IProtocolHandler
{
    static string? cachedResults;

    public async Task<bool> Execute(AppController controller)
    {
        var commands = new Dictionary<string, Func<Task<string>>>()
        {
            [Constants.EXECUTE_TESTS] = ExecuteCommand(controller),
            [Constants.QUIT_APP] = QuitAppCommand(controller),
        };

        var address = controller.GetIsAndroid()
            ? "10.0.2.2"
            : "localhost";

        controller.LogMessage($"Tcp conecting to {address}:{controller.Settings.TcpPort}...");

        using var client = new TcpClient();
        await client.ConnectAsync(address, controller.Settings.TcpPort, DefaultCT());

        controller.LogMessage($"connected.");

        using var stream = client.GetStream();
        using var reader = new StreamReader(stream);
        using var writter = new StreamWriter(stream) { AutoFlush = true };

        controller.LogMessage($"waiting for message...");

        var command = await reader.ReadLineAsync(DefaultCT()) ?? string.Empty;

        controller.LogMessage($"message recived.");

        if (!commands.TryGetValue(command, out var commandTask))
        {
            controller.LogMessage($"the command {command} was not found, skipping execution.");
            return false;
        }

        var content = await commandTask();

        await writter.WriteLineAsync(content);
        await writter.WriteLineAsync(Constants.END_STREAM);

        controller.LogMessage($"response for command {command} sent, content: {content}.");

        // in tcp we always return false, it means the protocol is not finished and
        // is constantly listening for new commands.
        // then the app can be closed only by the QUIT_APP command.
        return false;
    }

    private static Func<Task<string>> ExecuteCommand(AppController controller) =>
        async () =>
        {
            if (cachedResults is not null)
                return cachedResults;

            var result = await controller.TestExecutor.Execute();
            var serializedResult = JsonSerializer.Serialize(
                result,
                JsonGenerationContext.Default.ExecutionResponse);

            return cachedResults = serializedResult;
        };

    private static Func<Task<string>> QuitAppCommand(AppController controller) =>
        () => {
            controller.QuitApp();
            return Task.FromResult(Constants.QUIT_APP); 
        };

    private static CancellationToken DefaultCT() =>
        new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token;
}
