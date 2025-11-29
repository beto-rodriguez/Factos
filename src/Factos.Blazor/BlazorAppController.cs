namespace Factos.Blazor;

public class BlazorAppController(int port) 
    : AppController(port)
{
    public static ContentControl Content { get; internal set; } = null!;

    public async Task<T> NavigateToView<T>()
    {
        await NavigateToView(typeof(T));

        return (T?)Content.DynamicComponent.Instance
            ?? throw new Exception("Unable to get view instance.");
    }

    public override Task NavigateToView(object view)
    {
        var platformView = view as Type ??
            throw new ArgumentException($"Please pass a type, where the type is a valid razor component", nameof(view));

        return Content.SetContent(platformView).Task;
    }

    public override Task PopNavigation()
    {
        return Content.SetContent(typeof(Welcome)).Task;
    }

    public override Task WaitUntilLoaded(object element)
    {
        throw new NotImplementedException(
            "This method is not implement in Blazor and normally not required. " +
            "Use NavigateToView<T> and it will return an instance of the component already loaded in the UI.");
    }

    public override void QuitApp()
    {
        // nothing to do here, the browser must be closed externally
    }

    internal override Task InvokeOnUIThread(Task task)
    {
        var tcs = new TaskCompletionSource();

        Task.Run(async () =>
        {
            try
            {
                await task;
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        return tcs.Task;
    }

    internal override object GetWelcomeView() =>
        typeof(Welcome);

    internal override object GetResultsView(string message)
    {
        Results.data = message;
        return typeof(Results);
    }
}