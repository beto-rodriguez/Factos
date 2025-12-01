using Factos.Server.Settings;
using Microsoft.Testing.Platform.Builder;

namespace Factos.Server;

public static class FactosExtensions
{
    private static string _currentRoot = string.Empty;
    const string DEFAULT_OUT_PATH = "bin/factos";

#if DEBUG
    public static string Config =>"Debug";
#else
    public static string Config => "Release";
#endif

    public static ITestApplicationBuilder AddFactos(
        this ITestApplicationBuilder builder,
        Action<FactosSettings> settingsBuilder,
        Func<FactosSettings>? settingsFactory = null)
    {
        settingsFactory ??= () => new FactosSettings();
        var settings = settingsFactory();
        settingsBuilder(settings);

        builder.TestHost.AddTestHostApplicationLifetime(
            serviceProvider => new ProtocolosLifeTime(serviceProvider, settings));

        builder.RegisterTestFramework(
            (serviceProvider) => new FactosCapabilities(),
            (capabilities, serviceProvider) => new FactosFramework(serviceProvider, settings));

        return builder;
    }

    public static ITestApplicationBuilder AddFactos(
        this ITestApplicationBuilder builder, Action<FactosSettings> settingsBuilder) =>
            AddFactos(builder, settingsBuilder, null);

    /// <summary>
    /// Sets the the default root that will be appended to the next added test app.
    /// to clear use an empty string.
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="rootPath"></param>
    /// <returns></returns>
    public static FactosSettings SetRoot(this FactosSettings settings, string rootPath)
    {
        _currentRoot = rootPath;
        return settings;
    }

    public static FactosSettings TestApp(this FactosSettings settings, bool enabled, TestApp app)
    {
        if (enabled)
            settings.TestedApps.Add(app);

        return settings;
    }

    public static FactosSettings TestWindowsApp(
        this FactosSettings settings, string path, string fileName, string? displayName = null, string publishArgs = "", bool enabled = true, string outPath = DEFAULT_OUT_PATH) =>
            TestApp(settings, enabled, new TestApp
            {
                Name = displayName ?? path + "/" + fileName,
                StartCommands = [
                    $"dotnet restore {_currentRoot}{path}",
                    $"dotnet publish {_currentRoot}{path} -c {Config} -o {_currentRoot}{path}/{outPath} {publishArgs}",
                    $"{_currentRoot}{path}/{outPath}/{fileName} &"
                ],
            });

    public static FactosSettings TestAndroidApp(
        this FactosSettings settings, string path, string appName, string? displayName = null, string publishArgs = "", bool enabled = true, string outPath = DEFAULT_OUT_PATH) =>
            TestApp(settings, enabled, new TestApp
            {
                Name = displayName ?? appName,
                StartCommands = [
                    $"dotnet restore {_currentRoot}{path}",
                    $"dotnet publish {_currentRoot}{path} -c {Config} -o {_currentRoot}{path}/{outPath} {publishArgs}",
                    // Start the first Android emulator installed
                    "dotnet run Scripts/start-android-emulator.cs",
                    // Install the app on the emulator
                    $"adb install -r {_currentRoot}{path}/{outPath}/{appName}-Signed.apk",
                    // Launch the app
                    $"adb shell monkey -p {appName} -c android.intent.category.LAUNCHER 1"
                ],
            });

    public static FactosSettings TestBlazorApp(
        this FactosSettings settings, string path, string displayName = "Blazor app", string outPath = DEFAULT_OUT_PATH, bool enabled = true, int port = 5080) =>
            TestApp(settings, enabled, new TestApp
            {
                Name = displayName,
                StartCommands = [
                    $"dotnet restore {_currentRoot}{path}",
                    $"dotnet publish {_currentRoot}{path} -c {Config} -o {_currentRoot}{path}/{outPath}",
                    "dotnet tool install --global dotnet-serve",
                    $"dotnet serve -d {_currentRoot}{path}/{outPath}/wwwroot -p {port} &",
                    $"dotnet run Scripts/start-chrome.cs -- --at http://localhost:{port} &"
                ],
            });
}
