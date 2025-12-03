using Eto.Forms;

namespace Factos.EtoForms;

public class EtoFormsAppController(Form form, ControllerSettings settings)
    : AppController(settings)
{
    public override Task NavigateToView(object view)
    {
        var platformView = view as Control ??
            throw new ArgumentException($"The view is not compatible with {nameof(EtoFormsAppController)}", nameof(view));

        form.SuspendLayout();
        form.Content = platformView;
        form.ResumeLayout();

        return Task.CompletedTask;
    }

    public override Task PopNavigation()
    {
        form.SuspendLayout();
        form.Content = null;
        form.ResumeLayout();

        return Task.CompletedTask;
    }

    public override async Task WaitUntilLoaded(object element)
    {
        await Task.Delay(1000); // allow time for layout to occur
    }

    public override void QuitApp() =>
        Application.Instance.Quit();

    internal override Task InvokeOnUIThread(Task task)
    {
        var tcs = new TaskCompletionSource();

        Application.Instance.InvokeAsync(async () =>
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
