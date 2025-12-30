using Factos.RemoteTesters;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Factos.WinUI;

public class WinUIAppController(Window window, ControllerSettings settings)
    : AppController(settings)
{
    public Window Window { get; } = window;

    public override Task NavigateToView(object view)
    {
        var platformView = view as FrameworkElement ??
            throw new ArgumentException($"The view is not compatible with {nameof(WinUIAppController)}", nameof(view));

        GetContentControl().Content = platformView;

        return Task.CompletedTask;
    }

    public override Task PopNavigation()
    {
        GetContentControl().Content = null;

        return Task.CompletedTask;
    }

    public override Task WaitUntilLoaded(object element)
    {
        var platformElement = element as FrameworkElement
            ?? throw new ArgumentException($"The element is not compatible with {nameof(WinUIAppController)}", nameof(element));

        if (platformElement.IsLoaded)
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource();

        void OnLoaded(object? sender, RoutedEventArgs e)
        {
            platformElement.Loaded -= OnLoaded;
            tcs.SetResult();
        }

        platformElement.Loaded += OnLoaded;

        return tcs.Task;
    }

    public override void QuitApp() =>
        Application.Current.Exit();

    internal override Task InvokeOnUIThread(Func<Task> task, TestStreamHandler streamHandler)
    {
        var tcs = new TaskCompletionSource();

        Application.Current.UnhandledException += (sender, e) =>
        {
            streamHandler.Cancel(e.Exception);
            tcs.TrySetException(e.Exception);
            e.Handled = true;
        };

        var q = Window.DispatcherQueue ?? DispatcherQueue.GetForCurrentThread();

        q.TryEnqueue(async () =>
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

    private ContentControl GetContentControl() =>
        (ContentControl)Window.Content;
}
