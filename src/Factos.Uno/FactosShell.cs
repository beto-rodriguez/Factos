namespace Factos.Uno;

public sealed partial class FactosShell : UserControl, IContentControlProvider
{
    private ContentControl content;

    public FactosShell()
    {
        Current = this;

        var controller = new UnoAppController(Settings);

        Loaded += async (s, e) =>
        {
            await Task.Delay(5000); // allow UI to settle
            await AppController.InitializeController(controller);
        };

        Content = content = new ContentControl
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
            Content = new WelcomeView()
        };
    }

    internal static FactosShell? Current { get; private set; }
    internal static ControllerSettings Settings { get; set; }

    public ContentControl ContentControl => content;
}
