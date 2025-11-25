using Factos.Abstractions;
using System.Reflection;

namespace Factos.MAUI;

public static class SetupExtensions
{
    public static MauiAppBuilder UseFactosApp(
        this MauiAppBuilder app, Assembly testAssembly, int port = Constants.DEFAULT_TCP_PORT)
    {
        var controller = new MAUIAppController(port, testAssembly);

        FactosApp.Started += async () =>
            await AppController.InitializeController(controller, DeviceInfo.Platform == DevicePlatform.Android);

        return app.UseMauiApp<FactosApp>();
    }
}
