using Factos.Abstractions;
using System.Net.Sockets;

namespace Factos.Protocols;

public class TcpProtocolHandler : IProtocolHandler
{
    public async Task<bool> Execute(AppController controller)
    {
        var commands = new Dictionary<string, Func<Task<string>>>()
        {
            [Constants.START_DISCOVER_STREAM] = controller.TestExecutor.Discover,
            [Constants.START_RUN_STREAM] = controller.TestExecutor.Run,
            [Constants.QUIT_APP] = () => { controller.QuitApp(); return Task.FromResult(Constants.QUIT_APP); },
        };

        var address = controller.Settings.IsAndroid
            ? "10.0.2.2"
            : "localhost";

        using var client = new TcpClient();
        var ct = new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token;
        await client.ConnectAsync(address, controller.Settings.TcpPort, ct);

        using var stream = client.GetStream();
        using var reader = new StreamReader(stream);
        using var writter = new StreamWriter(stream) { AutoFlush = true };

        var command = await reader.ReadLineAsync() ?? string.Empty;

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
}
