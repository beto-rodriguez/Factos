using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Factos.Abstractions;

namespace Factos.Avalonia;

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
            FontWeight = FontWeight.Bold,
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
            Padding = new Thickness(20)
        };

        Content = scrollViewer;
    }
}
