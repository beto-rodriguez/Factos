namespace Factos.MAUI;

public partial class FactosApp : Application
{
	internal static event Action? Started;

	public FactosApp()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}

    protected override void OnStart()
    {
        base.OnStart();
		Started?.Invoke();
    }
}