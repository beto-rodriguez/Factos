using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Factos.WPF;

public static class SetupExtensions
{
    public static void UseFactosApp(this Application app)
        => app.UseFactosApp(ControllerSettings.Default);

    public static void UseFactosApp(this Application app, ControllerSettings settings)
    {
        var window = new Window { Title = "Factos.WPF" };
        var controller = new WPFAppController(window, settings);

        var content = new ContentControl
        {
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Background = new SolidColorBrush(Color.FromRgb(15, 23, 43)),
            Content = new WelcomeView()
        };

        window.Content = content;

        content.Loaded += async (s, e) =>
            await AppController.InitializeController(controller);

        app.MainWindow = window;
        window.Show();
    }
}
