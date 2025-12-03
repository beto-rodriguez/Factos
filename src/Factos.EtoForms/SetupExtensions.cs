using Eto.Drawing;
using Eto.Forms;

namespace Factos.EtoForms;

public static class SetupExtensions
{
    public static void UseFactosApp(this Form form)
        => form.UseFactosApp(ControllerSettings.Default);

    public static void UseFactosApp(this Form form, ControllerSettings settings)
    {
        var controller = new EtoFormsAppController(form, settings);

        form.Load += async (s, e) =>
            await AppController.InitializeController(controller);

        form.MinimumSize = new Size(1000, 1000);
    }
}
