using Factos.Abstractions;
using System.Net.Sockets;
using System.Text.Json;

namespace Factos.Protocols;

public class TcpProtocolHandler : IProtocolHandler
{
    public async Task Execute(AppController controller)
    {
        while (true)
        {
            var commands = new Dictionary<string, Func<IAsyncEnumerable<string>>>()
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
                return;// false;
            }

            var message = commandTask();

            controller.LogMessage($"responding to {command}:");
            await foreach (var content in message)
            {
                await writter.WriteLineAsync(content);
                controller.LogMessage($"[message sent] {content}");
            }

            //var content = await commandTask();
            //await writter.WriteLineAsync(content);
            await writter.WriteLineAsync(Constants.END_STREAM);
        }
    }

    private static Func<IAsyncEnumerable<string>> ExecuteCommand(AppController controller)
    {
        async IAsyncEnumerable<string> ExecuteAsyncEnumerable()
        {
            await foreach (var test in controller.TestExecutor.Execute())
            {
                controller.LogMessage($"Executing test: {test.Uid}");
                
                var serializedTest = JsonSerializer.Serialize(
                    test, JsonGenerationContext.Default.TestNodeDto);

                yield return $"node {serializedTest}";
            }
        }

        return ExecuteAsyncEnumerable;
    }

    private static Func<IAsyncEnumerable<string>> QuitAppCommand(AppController controller)
    {
        async IAsyncEnumerable<string> QuitAsyncEnumerable()
        {
            controller.QuitApp();
            yield return $"action {Constants.QUIT_APP}";
        }

        return QuitAsyncEnumerable;
    }

    private static CancellationToken DefaultCT() =>
        new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token;
}
