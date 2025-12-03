namespace Factos.WinForms;

public class WinFormsAppController(Form form, ControllerSettings settings)
    : AppController(settings)
{
    public override Task NavigateToView(object view)
    {
        var platformView = view as Control ??
            throw new ArgumentException($"The view is not compatible with {nameof(WinFormsAppController)}", nameof(view));

        form.SuspendLayout();
        form.Controls.Clear();
        form.Controls.Add(platformView);
        form.ResumeLayout();

        return Task.CompletedTask;
    }

    public override Task PopNavigation()
    {
        form.SuspendLayout();
        form.Controls.Clear();
        form.ResumeLayout();

        return Task.CompletedTask;
    }

    public override Task WaitUntilLoaded(object element)
    {
        return Task.CompletedTask;
    }

    public override void QuitApp() =>
        Application.Exit();

    internal override Task InvokeOnUIThread(Task task)
    {
        var tcs = new TaskCompletionSource();

        form.Invoke(async () =>
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
