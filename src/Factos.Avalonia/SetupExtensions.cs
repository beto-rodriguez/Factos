using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;

namespace Factos.Avalonia;

public static class SetupExtensions
{
    public static void UseFactosApp(this Application app)
    {
        var settings = OperatingSystem.IsBrowser()
            ? ControllerSettings.Default with { Protocol = ProtocolType.Http }
            : ControllerSettings.Default;

        UseFactosApp(app, settings);
    }

    public static void UseFactosApp(this Application app, ControllerSettings settings)
    {
        var content = new ContentControl
        {
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Background = new SolidColorBrush(Color.FromRgb(15, 23, 43)),
            Content = new WelcomeView()
        };

        var controller = new AvaloniaAppController(content, settings);

        if (app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new Window
            {
                Title = "Factos.Avalonia",
                Content = content
            };
        }
        else if (app.ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = content;
        }

        content.Loaded += async (s, e) =>
            await AppController.InitializeController(controller);
    }
}
