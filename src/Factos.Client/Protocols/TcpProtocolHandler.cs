using Factos.Abstractions;
using Factos.RemoteTesters;
using System.Net.Sockets;
using System.Text.Json;

namespace Factos.Protocols;

public class TcpProtocolHandler : IProtocolHandler
{
    public async Task<bool> Execute(AppController controller)
    {
        var commands = new Dictionary<string, Func<Task<string>>>()
        {
            [Constants.EXECUTE_TESTS] = ExecuteCommand(controller),
            [Constants.QUIT_APP] = QuitAppCommand(controller),
        };

        var address = controller.Settings.IsAndroid
            ? "10.0.2.2"
            : "localhost";

        using var client = new TcpClient();
        await client.ConnectAsync(address, controller.Settings.TcpPort, DefaultCT());

        using var stream = client.GetStream();
        using var reader = new StreamReader(stream);
        using var writter = new StreamWriter(stream) { AutoFlush = true };

        var command = await reader.ReadLineAsync(DefaultCT()) ?? string.Empty;

        if (!commands.TryGetValue(command, out var commandTask))
            return false;

        var content = await commandTask();

        await writter.WriteLineAsync(content);
        await writter.WriteLineAsync(Constants.END_STREAM);

        // in tcp we always return false, it means the protocol is not finished and
        // is constantly listening for new commands.
        // then the app can be closed only by the QUIT_APP command.
        return false;
    }

    private static Func<Task<string>> ExecuteCommand(AppController controller) =>
        async () => JsonSerializer.Serialize(await controller.TestExecutor.Execute());

    private static Func<Task<string>> QuitAppCommand(AppController controller) =>
        () => {
            controller.QuitApp();
            return Task.FromResult(Constants.QUIT_APP); 
        };

    private static CancellationToken DefaultCT() =>
        new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token;
}
