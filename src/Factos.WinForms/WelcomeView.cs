using System.Drawing;
using System.Windows.Forms;

namespace Factos.WinForms;

public class WelcomeView : UserControl
{
    public WelcomeView()
    {
        BackColor = Color.FromArgb(15, 23, 43);
        Dock = DockStyle.Fill;

        var label = new Label
        {
            Text = "Tests will run shortly...",
            ForeColor = Color.WhiteSmoke,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font(FontFamily.GenericSansSerif, 24f, FontStyle.Regular),
            Dock = DockStyle.Fill,
            Padding = new Padding(50),
        };

        Controls.Add(label);
    }
}
