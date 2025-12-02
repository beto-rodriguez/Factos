namespace Factos.Uno;

public sealed partial class FactosShell : UserControl, IContentControlProvider
{
    public FactosShell()
    {
        InitializeComponent();

        Current = this;
        UIContentControl.Content = new WelcomeView();

        var controller = new UnoAppController(Settings);

        Loaded += async (s, e) =>
            await AppController.InitializeController(controller);
    }

    internal static FactosShell? Current { get; private set; }
    internal static ControllerSettings Settings { get; set; }

    public ContentControl ContentControl => UIContentControl;
}
