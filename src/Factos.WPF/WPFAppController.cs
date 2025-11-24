using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace Factos.WPF;

public class WPFAppController(Window window, int port, Assembly assembly) 
    : AppController(assembly, port)
{
    public Window Window { get; } = window;

    public override Task NavigateToView(object view)
    {
        var platformView = view as FrameworkElement ??
            throw new ArgumentException($"The view is not compatible with {nameof(WPFAppController)}", nameof(view));

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
            ?? throw new ArgumentException($"The element is not compatible with {nameof(WPFAppController)}", nameof(element));

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
        Application.Current.Shutdown();

    internal override Task InvokeOnUIThread(Task task)
    {
        var tcs = new TaskCompletionSource();

        Window.Dispatcher.InvokeAsync(async () =>
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

    private ContentControl GetContentControl() =>
        (ContentControl)Window.Content;
}
