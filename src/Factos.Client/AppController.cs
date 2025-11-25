using Factos.Abstractions;
using Factos.RemoteTesters;
using System.Net.Sockets;
using System.Reflection;

namespace Factos;

public abstract class AppController(Assembly assembly, int port)
{
    readonly TestExecutor _testExecutor = new ReflectionTestExecutor(assembly);

    public static AppController Current { get; internal set; } = null!;
    internal string Name { get; set; } = "?";
    private static Dictionary<string, Func<string, Task<string>>> Commands => new()
    {
        [Constants.START_DISCOVER_STREAM] = Current._testExecutor.Discover,
        [Constants.START_RUN_STREAM] = Current._testExecutor.Run,
        [Constants.QUIT_APP] = Current.QuitAppTask,
    };

    public static async Task InitializeController(AppController controller, bool isAndroid)
    {
        await controller.NavigateToView(controller.GetWelcomeView());
        Current = controller;
        _ = controller.Listen(isAndroid);
    }

    public abstract Task NavigateToView(object view);

    public abstract Task PopNavigation();

    public abstract Task WaitUntilLoaded(object element);

    public abstract void QuitApp();

    internal abstract Task InvokeOnUIThread(Task task);

    internal abstract object GetWelcomeView();

    internal abstract object GetResultsView(string message);

    internal virtual async Task Listen(bool isAndroid)
    {
        var addess = isAndroid 
            ? "10.0.2.2"
            : "localhost";

        try
        {
            while (true)
            {
                // ToDo, reuse the client connection instead of creating a new one each time
                using var client = new TcpClient();
                await client.ConnectAsync(addess, port);

                using var stream = client.GetStream();
                using var reader = new StreamReader(stream);
                using var writter = new StreamWriter(stream) { AutoFlush = true };

                var commandAndParams = await reader.ReadLineAsync() ?? string.Empty;
                var command = commandAndParams;

                if (command.Contains(':'))
                    command = commandAndParams.Split(':')[0];

                if (!Commands.TryGetValue(command, out var commandTask))
                    continue;

                var content = await commandTask(commandAndParams);

                await writter.WriteLineAsync(content);
                await writter.WriteLineAsync(Constants.END_STREAM);

                // special case to break the loop and end the app
                if (content == Constants.QUIT_APP)
                    break;
            }
        }
        catch
        {
            // most likely occurs when the tcp client connection failed
            // because the server is not running, in this case we assume
            // this is a debug session, lets just run tests
            var result = await Current._testExecutor.Run(string.Empty);
            await NavigateToView(GetResultsView(result));
        }
    }

    private Task<string> QuitAppTask(string command)
    {
        QuitApp();
        return Task.FromResult(Constants.QUIT_APP);
    }
}
