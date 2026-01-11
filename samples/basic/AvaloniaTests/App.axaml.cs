using Avalonia;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Factos.Avalonia;

namespace AvaloniaTests;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        // useful to ensure the Factos.SGTests static constructor has run
        // specially it prevents issues with tests detection on iOS.
        System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(Factos.SGTests).TypeHandle);

        this.UseFactosApp();

        base.OnFrameworkInitializationCompleted();
    }
}
