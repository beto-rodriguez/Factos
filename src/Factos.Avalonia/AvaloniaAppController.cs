using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Factos.RemoteTesters;

namespace Factos.Avalonia;

public class AvaloniaAppController(ContentControl contentControl, ControllerSettings settings)
    : AppController(settings)
{
    public override Task NavigateToView(object view)
    {
        var platformView = view as Control ??
            throw new ArgumentException($"The view is not compatible with {nameof(AvaloniaAppController)}", nameof(view));

        contentControl.Content = platformView;

        return Task.CompletedTask;
    }

    public override Task PopNavigation()
    {
        contentControl.Content = null;

        return Task.CompletedTask;
    }

    public override Task WaitUntilLoaded(object element)
    {
        var platformElement = element as Control
            ?? throw new ArgumentException($"The element is not compatible with {nameof(AvaloniaAppController)}", nameof(element));

        if (platformElement.IsLoaded)
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource();

        async void OnLoaded(object? sender, EventArgs e)
        {
            platformElement.Loaded -= OnLoaded;

            if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
                await Task.Delay(1000); // idk we need more time on mobile?

            tcs.SetResult();
        }

        platformElement.Loaded += OnLoaded;

        return tcs.Task;
    }

    public override void QuitApp()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
        else
            Environment.Exit(0);
    }

    internal override Task InvokeOnUIThread(Func<Task> task, TestStreamHandler streamHandler)
    {
        var tcs = new TaskCompletionSource();

        Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                await task();
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
