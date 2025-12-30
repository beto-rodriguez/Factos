using Factos.Abstractions;
using Factos.Abstractions.Dto;
using Factos.Protocols;
using Factos.RemoteTesters;

namespace Factos;

public abstract class AppController(ControllerSettings settings)
{
    public static AppController Current { get; internal set; } = null!;
    public TestExecutor TestExecutor { get; } = new SourceGeneratedTestExecutor();
    public ControllerSettings Settings { get; set; } = settings;

    public event Action<string>? LogMessageReceived;

    public static async Task InitializeController(AppController controller)
    {
        await controller.NavigateToView(controller.GetWelcomeView());
        Current = controller;
        _ = controller.Listen();
    }

    public abstract Task NavigateToView(object view);

    public abstract Task PopNavigation();

    public abstract Task WaitUntilLoaded(object element);

    public abstract void QuitApp();

    internal abstract Task InvokeOnUIThread(Func<Task> task, TestStreamHandler streamHandler);

    internal abstract object GetWelcomeView();

    internal abstract object GetResultsView(string message);

    internal virtual async Task Listen()
    {
        var protocolHandler = new WebSocketsProtocolHandler();

        try
        {
            await protocolHandler.Execute(this);
        }
        catch (Exception? ex)
        {
            LogMessage($"the protocol execution failed.");

            while (ex is not null)
            {
                LogMessage($" - {ex.Message}");
                LogMessage(ex.StackTrace ?? string.Empty);
                ex = ex.InnerException;
            }

            // if the server is not reachable, most likely this is 
            // a local test run, so we run and show the results locally

            var results = new List<TestNodeDto>();
            await foreach(var result in TestExecutor.Execute())
                results.Add(result);

            var formatted = OutputTransform.SummarizeResults(results);

            await NavigateToView(GetResultsView(formatted));
        }
    }

    internal virtual bool GetIsAndroid() =>
        OperatingSystem.IsAndroid();

    internal void LogMessage(string message) =>
        LogMessageReceived?.Invoke(message);
}
