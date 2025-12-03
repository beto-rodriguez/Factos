using Eto.Forms;
using Factos.EtoForms;

namespace EtoFormsTests;

static class Program
{
    [STAThread]
    static void Main()
    {
        new Application(Eto.Platform.Detect).Run(GetFactosForm());
    }
    
    private static Form GetFactosForm()
    {
        var form = new Form();
        form.UseFactosApp();
        return form;
    }
}
