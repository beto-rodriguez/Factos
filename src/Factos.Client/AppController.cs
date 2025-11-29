using Factos.Protocols;
using Factos.RemoteTesters;

namespace Factos;

public abstract class AppController
{
    public AppController(ControllerSettings settings)
    {
        Settings = settings;
        TestExecutor = new SourceGeneratedTestExecutor();
        //TestExecutor = new ReflectionTestExecutor();
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
            catch
            {
                // if there was an error connecting to the server (TCP/HTTP)
                // we wait a bit before trying again

                if (!resultsShown)
                {
                    // if the server is not reachable, most likely this is 
                    // a local test run, so we run and show the results locally

                    var result = await TestExecutor.Run();
                    await NavigateToView(GetResultsView(result));

                    resultsShown = true;
                }

                await Task.Delay(2000);
            }
        }
    }
}
