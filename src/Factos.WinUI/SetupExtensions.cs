using Factos.Abstractions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Factos.WinUI;

public static class SetupExtensions
{
    public static void UseFactosApp(
        this Application app, int port = Constants.DEFAULT_TCP_PORT)
    {
        var window = new Window { Title = "Factos.WinUI" };
        var controller = new WinUIAppController(window, port);

        var content = new ContentControl
        {
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Background = new SolidColorBrush(Color.FromArgb(255, 15, 23, 43)),
            Content = new WelcomeView()
        };

        window.Content = content;

        content.Loaded += async (s, e) =>
            await AppController.InitializeController(controller, false);

        window.Activate();
    }
}
