using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Factos.Avalonia;

public class WelcomeView : UserControl
{
    public WelcomeView()
    {
        var textBlock = new TextBlock
        {
            Text = "Tests will run shortly...",
            Foreground = Brushes.WhiteSmoke,
            Background = new SolidColorBrush(Color.FromRgb(15, 23, 43)),
            TextAlignment = TextAlignment.Center,
            FontSize = 24,
            Padding = new Thickness(50)
        };

        Content = textBlock;
    }
}
