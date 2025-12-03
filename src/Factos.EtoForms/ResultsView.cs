using Eto.Drawing;
using Eto.Forms;

namespace Factos.EtoForms;

public class ResultsView : Panel
{
    public ResultsView(string message)
    {
        BackgroundColor = Color.FromArgb(15, 23, 43);
        Padding = new Padding(20);

        var textBox = new TextBox
        {
            ReadOnly = true,
            TextColor = Colors.WhiteSmoke,
            Font = new Font(FontFamilies.Sans, 16),
            BackgroundColor = Color.FromArgb(15, 23, 43),
            Text = "Tests results\n" + message
        };

        Content = textBox;
    }
}
