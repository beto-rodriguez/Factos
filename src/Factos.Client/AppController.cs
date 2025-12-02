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

        var resultsShown = false;
        var finished = false;

        while (!finished)
        {
            try
            {
                finished = await protocolHandler.Execute(this);
            }
            catch(Exception ex)
            {
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

                    //resultsShown = true;
                }

                await Task.Delay(2000);
            }
        }
    }

    public static bool HasSingleFlag(ProtocolType value)
    {
        if (value == ProtocolType.None) 
            return false;

        var intValue = (int)value;
        return (intValue & (intValue - 1)) == 0;
    }

    internal virtual bool GetIsAndroid() =>
        OperatingSystem.IsAndroid();
}
