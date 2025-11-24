using Factos.Abstractions;

namespace Factos.MAUI;

public partial class ResultsView : ContentPage
{
    public ResultsView(string message)
    {
        Title = "Test Results"; // optional
        BackgroundColor = Color.FromRgb(15, 23, 43);

        var titleLabel = new Label
        {
            Text = "Tests results",
            FontSize = 24,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.WhiteSmoke,
            Margin = new Thickness(0,0,0,25)
        };

        var resultsEditor = new Editor
        {
            Text = OutputTransform.SummarizeResults(message),
            FontSize = 16,
            TextColor = Colors.WhiteSmoke,
            BackgroundColor = Colors.Transparent,
            IsReadOnly = true
        };

        var layout = new VerticalStackLayout
        {
            Padding = new Thickness(20),
            Spacing = 10,
            Children =
            {
                titleLabel,
                resultsEditor
            }
        };

        Content = new ScrollView
        {
            Content = layout
        };
    }
}
