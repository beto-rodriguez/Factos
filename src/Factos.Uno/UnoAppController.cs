using Microsoft.UI.Dispatching;

namespace Factos.Uno;

public class UnoAppController(ControllerSettings settings)
    : AppController(settings)
{
    public override Task NavigateToView(object view)
    {
        if (FactosShell.Current is null)
            throw new InvalidOperationException("FactosShell is not initialized.");

        var platformView = view as FrameworkElement ??
            throw new ArgumentException($"The view is not compatible with {nameof(UnoAppController)}", nameof(view));

        FactosShell.Current.UIContentControl.Content = platformView;

        return Task.CompletedTask;
    }

    public override Task PopNavigation()
    {
        if (FactosShell.Current is null)
            throw new InvalidOperationException("FactosShell is not initialized.");

        FactosShell.Current.UIContentControl.Content = null;

        return Task.CompletedTask;
    }

    public override Task WaitUntilLoaded(object element)
    {
        var platformElement = element as FrameworkElement
            ?? throw new ArgumentException($"The element is not compatible with {nameof(UnoAppController)}", nameof(element));

        if (platformElement.IsLoaded)
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource();

        async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            platformElement.Loaded -= OnLoaded;

            // in the browser, it seems that we need more time?
            if (OperatingSystem.IsBrowser())
                await Task.Delay(1000);

            tcs.SetResult();
        }

        platformElement.Loaded += OnLoaded;

        return tcs.Task;
    }

    public override void QuitApp() =>
        // false positive warning?
        // https://github.com/unoplatform/uno/issues/10436
        Application.Current.Exit();

    internal override Task InvokeOnUIThread(Task task)
    {
        var tcs = new TaskCompletionSource();

        DispatcherQueue.GetForCurrentThread().TryEnqueue(async () =>
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

    // HACK FOR UNO, OperatingSystem.IsAndroid() does not work correctly here
    internal override bool GetIsAndroid()
    {
#if ANDROID
        return true;
#else
        return false;
#endif
    }
}
