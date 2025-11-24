using Factos.Abstractions;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Factos.WPF;

public static class SetupExtensions
{
    public static void UseFactosApp(
        this Application app, Assembly testAssembly, int port = Constants.DEFAULT_TCP_PORT)
    {
        var window = new Window { Title = "Factos.WPF" };
        app.MainWindow = window;
        window.Show();

        var controller = new WPFAppController(window, port, testAssembly);

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
            AppController.InitializeController(controller);
    }
}
