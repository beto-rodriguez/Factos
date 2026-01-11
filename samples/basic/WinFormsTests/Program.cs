using Factos;
using Factos.WinForms;

namespace WinFormsTests
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            //ApplicationConfiguration.Initialize();

            var form = new Form1();

#if !NET8_0_OR_GREATER
            // in net framework, we need to ensure the static constructor is called
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(Factos.SGTests).TypeHandle);
#endif


            form.UseFactosApp();

            Application.Run(form);
        }
    }
}