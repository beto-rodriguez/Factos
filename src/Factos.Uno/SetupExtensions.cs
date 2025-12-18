using Microsoft.Extensions.Hosting;

namespace Factos.Uno;

public static class SetupExtensions
{
    public static void UseFactosApp(this IHostBuilder builder)
    {
        builder.UseFactosApp(ControllerSettings.Default);
    }

    public static void UseFactosApp(this IHostBuilder builder, ControllerSettings settings)
    {
        builder.UseNavigation(RegisterFactosRoutes);
        FactosShell.Settings = settings;
    }

    private static void RegisterFactosRoutes(IViewRegistry views, IRouteRegistry routes)
    {
        views.Register(new ViewMap<FactosShell>());
        routes.Register(new RouteMap("", View: views.FindByView<FactosShell>()));
    }
}
