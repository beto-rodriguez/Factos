namespace Factos.MAUI;

public partial class WelcomeView : ContentPage
{
    public WelcomeView()
    {
        Title = "Welcome"; // optional

        var label = new Label
        {
            Text = "Tests will run shortly...",
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            FontSize = 24,
            TextColor = Colors.WhiteSmoke,
            Padding = new Thickness(50)
        };

        Content = new Grid
        {
            BackgroundColor = Color.FromRgb(15, 23, 43),
            Children = { label }
        };
    }
}
