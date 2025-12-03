using Microsoft.UI;
using Windows.UI;

namespace Factos.Uno;

public partial class WelcomeView : UserControl
{
    public WelcomeView()
    {
        var textBlock = new TextBlock
        {
            Text = "Tests will run shortly...",
            Foreground = new SolidColorBrush(Colors.WhiteSmoke),
            TextAlignment = TextAlignment.Center,
            FontSize = 24,
            Padding = new Thickness(50)
        };

        var grid = new Grid
        {
            Background = new SolidColorBrush(Color.FromArgb(255, 15, 23, 43))
        }; 

        grid.Children.Add(textBlock);

        Content = grid;
    }
}
