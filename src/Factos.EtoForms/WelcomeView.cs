using Eto.Drawing;
using Eto.Forms;

namespace Factos.EtoForms;

public class WelcomeView : Panel
{
    public WelcomeView()
    {
        BackgroundColor = Color.FromArgb(15, 23, 43);
        Padding = new Padding(20);

        var label = new Label
        {
            Text = "Tests will run shortly...",
            TextColor = Colors.WhiteSmoke,
            Font = new Font(FontFamilies.Sans, 22),
        };

        Content = label;
    }
}
