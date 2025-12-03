using Factos.Abstractions;
using Factos.Protocols;
using Factos.RemoteTesters;
using System.Text.Json;

namespace Factos;

public abstract class AppController
{
    public AppController(ControllerSettings settings)
    {
        Settings = settings;
        TestExecutor = new SourceGeneratedTestExecutor();
    }

    public static AppController Current { get; internal set; } = null!;
    public TestExecutor TestExecutor { get; }
    public ControllerSettings Settings { get; set; }
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

    internal abstract Task InvokeOnUIThread(Task task);

    internal abstract object GetWelcomeView();

    internal abstract object GetResultsView(string message);

    internal virtual async Task Listen()
    {
        if (!HasSingleFlag(Settings.Protocol))
            throw new InvalidOperationException("The client can only implement one protocol.");

        IProtocolHandler protocolHandler = Settings.Protocol == ProtocolType.Http
            ? new HTTPProtocolHandler()
            : new TcpProtocolHandler();

        LogMessage($"using {Settings.Protocol} by client settings.");

        var resultsShown = false;
        var finished = false;

        while (!finished)
        {
            try
            {
                finished = await protocolHandler.Execute(this);
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

                // if there was an error connecting to the server (TCP/HTTP)
                // we wait a bit before trying again

                if (!resultsShown)
                {
                    // if the server is not reachable, most likely this is 
                    // a local test run, so we run and show the results locally

                    var result = await TestExecutor.Execute();

                    var serialized = JsonSerializer.Serialize(
                        result.Results,
                        JsonGenerationContext.Default.IEnumerableTestNodeDto);

                    await NavigateToView(GetResultsView(serialized));

                    resultsShown = true;
                }

                await Task.Delay(2000);
            }
        }
    }

    internal virtual bool GetIsAndroid() =>
        OperatingSystem.IsAndroid();

    internal void LogMessage(string message) =>
        LogMessageReceived?.Invoke(message);

    private static bool HasSingleFlag(ProtocolType value)
    {
        if (value == ProtocolType.None)
            return false;

        var intValue = (int)value;
        return (intValue & (intValue - 1)) == 0;
    }
}
