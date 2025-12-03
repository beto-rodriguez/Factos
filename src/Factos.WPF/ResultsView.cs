using Factos.Abstractions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Factos.WPF;

public class ResultsView : UserControl
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
            Foreground = Brushes.WhiteSmoke,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 25)
        };

        var messageBox = new TextBox
        {
            FontSize = 16,
            Text = message,
            BorderBrush = Brushes.Transparent,
            Foreground = Brushes.WhiteSmoke,
            Background = Brushes.Transparent,
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4),
            MinHeight = 100
        };

        panel.Children.Add(titleBlock);
        panel.Children.Add(messageBox);

        var scrollViewer = new ScrollViewer
        {
            Content = panel,
            Background = new SolidColorBrush(Color.FromRgb(15, 23, 43)),
            Padding = new Thickness(20),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        Content = scrollViewer;
    }
}
