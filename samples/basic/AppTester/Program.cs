using Factos.Server;
using Factos.Server.Settings;
using Factos.Server.Settings.Apps;
using Microsoft.Testing.Extensions;
using Microsoft.Testing.Platform.Builder;

var testsBuilder = await TestApplication.CreateBuilderAsync(args);
var testedApps = new List<TestedApp>();

#if DEBUG

// Add the applications to test here
// paths are relative to the AppTester output folder
var root = "../../../..";

testedApps
    .AddManuallyStartedApp();
    //.Add(project: $"{root}/WPFTests");
#else

// We also add applications to test in Release mode
// but we use the release build for CI pipelines
// paths are relative to the repo root
// also, we select only a subset of apps depending on the CI needs
// for example if testing both WPF and WinUI, from the root folder run:
//    dotnet test -c Release --project samples/basic/AppTester --select wpf winui
// when values like [key] are used, they are replaced from command line environment variables:
//    dotnet test -c Release --project samples/basic/AppTester --test-env key=value
var root = "samples/basic";

testedApps

    // == WPF ==
    .Add(project: $"{root}/WPFTests",               uid: "wpf")

    // == WINFORMS ==
    .Add(project: $"{root}/WinFormsTests",          uid: "winforms")

    // == WINUI ==
    .Add(project: $"{root}/WinUITests",             uid: "winui",
        runtimeIdentifier:
            "win-x64",
        msBuildArgs: [
            new("WindowsPackageType", "None"),
            new("WindowsAppSDKSelfContained", "true"),
            new("UseSrc", "false")
        ])

    // == MAUI ==
    .Add(project: $"{root}/MAUITests",              uid: "maui",                targetFramework: "[tfm]")

    // == AVALONIA ==
    .Add(project: $"{root}/AvaloniaTests.Desktop",  uid: "avalonia-desktop")
    .Add(project: $"{root}/AvaloniaTests.Android",  uid: "avalonia-android")
    .Add(project: $"{root}/AvaloniaTests.iOS",      uid: "avalonia-ios")
    .Add(project: $"{root}/AvaloniaTests.Browser",  uid: "avalonia-browser",    appHost: AppHost.HeadlessChrome)

    // == UNO ==
    .Add(project: $"{root}/UnoTests/UnoTests",      uid: "uno",                 targetFramework: "[tfm]")
    .Add(project: $"{root}/UnoTests/UnoTests",      uid: "uno-browser",         targetFramework: "net10.0-browserwasm", appHost: AppHost.HeadlessChrome)

    // == BLAZOR ==
    .Add(project: $"{root}/BlazorTests",            uid: "blazor",         appHost: AppHost.HeadlessChrome)

    // == ETO ==
    .Add(project: $"{root}/EtoFormsTests",           uid: "eto");

#endif

testsBuilder
    .AddFactos(new FactosSettings()
    {
        ConnectionTimeout = 180,
        TestedApps = testedApps
    })
    .AddTrxReportProvider(); // optional, add TRX if needed

using ITestApplication testApp = await testsBuilder.BuildAsync();

return await testApp.RunAsync();