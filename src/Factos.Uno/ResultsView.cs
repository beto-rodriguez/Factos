using Factos.Abstractions;
using Microsoft.UI;
using Microsoft.UI.Text;

namespace Factos.Uno;

public partial class ResultsView : UserControl
{
    private TextBox messageBox;

    public ResultsView(AppController controller, string message)
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

        var m = message;

        messageBox = new TextBox
        {
            FontSize = 16,
            Text = m.Replace(Environment.NewLine, " "),
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

        var a = messageBox.Text;
        // uno bug? a != m...

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

        controller.LogMessageReceived += OnMessageReceived;
    }

    private void OnMessageReceived(string message)
    {
        messageBox.Text += Environment.NewLine + message;
    }
}
