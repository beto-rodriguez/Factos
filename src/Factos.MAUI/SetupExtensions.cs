namespace Factos.MAUI;

public static class SetupExtensions
{
    public static MauiAppBuilder UseFactosApp(this MauiAppBuilder app) =>
        app.UseFactosApp(ControllerSettings.Default with { IsAndroid = DeviceInfo.Platform == DevicePlatform.Android });

    public static MauiAppBuilder UseFactosApp(this MauiAppBuilder app, ControllerSettings settings)
    {
        var controller = new MAUIAppController(settings);

        FactosApp.Started += async () =>
            await AppController.InitializeController(controller);

        return app.UseMauiApp<FactosApp>();
    }
}
