using Factos.Abstractions;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Factos.WinUI;

public partial class ResultsView : UserControl
{
    public ResultsView(string message)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical
        };

        var titleBlock = new TextBlock
        {
            Text = "Tests results",
            FontSize = 24,
            Foreground = new SolidColorBrush(Colors.WhiteSmoke),
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 25)
        };

        var messageBox = new TextBox
        {
            FontSize = 16,
            Text = message,
            BorderBrush = new SolidColorBrush(Colors.Transparent),
            Foreground = new SolidColorBrush(Colors.WhiteSmoke),
            Background = new SolidColorBrush(Colors.Transparent),
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4),
            MinHeight = 100
        };

        panel.Children.Add(titleBlock);
        panel.Children.Add(messageBox);

        var scrollViewer = new ScrollViewer
        {
            Content = panel,
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 43)),
            Padding = new Thickness(20),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        Content = scrollViewer;
    }
}
