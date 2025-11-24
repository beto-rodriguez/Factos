using System.Reflection;

namespace Factos.MAUI;

public class MAUIAppController(int port, Assembly assembly)
    : AppController(assembly, port)
{
    public override async Task NavigateToView(object view)
    {
        var platformView = view as Page ??
            throw new ArgumentException($"The view is not compatible with {nameof(MAUIAppController)}", nameof(view));

        await Shell.Current.Navigation.PushAsync(platformView);
    }

    public override Task PopNavigation() =>
        Shell.Current.Navigation.PopAsync();

    public override Task WaitUntilLoaded(object element)
    {
        var platformElement = element as VisualElement
            ?? throw new ArgumentException($"The element is not compatible with {nameof(MAUIAppController)}", nameof(element));

        if (platformElement.IsLoaded)
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource();

        void OnLoaded(object? sender, EventArgs e)
        {
            platformElement.Loaded -= OnLoaded;
            tcs.SetResult();
        }

        platformElement.Loaded += OnLoaded;

        return tcs.Task;
    }

    public override void QuitApp() =>
        Application.Current?.Quit();

    internal override Task InvokeOnUIThread(Task task)
    {
        var tcs = new TaskCompletionSource();

        MainThread.InvokeOnMainThreadAsync(async () =>
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
        new WelcomeView();

    internal override object GetResultsView(string message) =>
        new ResultsView(message);
}
