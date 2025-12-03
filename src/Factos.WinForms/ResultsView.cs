using Factos.Abstractions;

namespace Factos.WinForms;

public class ResultsView : UserControl
{
    public ResultsView(string message)
    {
        BackColor = Color.FromArgb(15, 23, 43);
        Dock = DockStyle.Fill;

        var container = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            BackColor = Color.FromArgb(15, 23, 43)
        };

        var title = new Label
        {
            Text = "Tests results",
            ForeColor = Color.WhiteSmoke,
            Font = new Font(FontFamily.GenericSansSerif, 24f, FontStyle.Bold),
            Dock = DockStyle.Top,
            Padding = new Padding(0, 0, 0, 25),
            AutoSize = false,
            Height = 40
        };

        var textBox = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.FixedSingle,
            ForeColor = Color.WhiteSmoke,
            BackColor = Color.FromArgb(15, 23, 43),
            Font = new Font(FontFamily.GenericSansSerif, 16f, FontStyle.Regular),
            Dock = DockStyle.Fill,
            Text = message
        };

        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };

        panel.Controls.Add(textBox);
        panel.Controls.Add(title);

        var scroll = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(15, 23, 43)
        };

        // WinForms TextBox has built-in scroll; wrapping panel keeps padding/layout similar to WPF
        scroll.Controls.Add(panel);
        container.Controls.Add(scroll);

        Controls.Add(container);
    }
}
