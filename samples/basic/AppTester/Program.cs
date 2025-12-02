using Factos.Server;
using Factos.Server.Settings;
using Factos.Server.Settings.Apps;
using Microsoft.Testing.Extensions;
using Microsoft.Testing.Platform.Builder;

var testsBuilder = await TestApplication.CreateBuilderAsync(args);

var root = "../../../../";

#if !DEBUG
// we are using the release mode in CI/CD pipelines
// we adjust the path, in this case, the relative path to the samples
root = "samples/basic/";
#endif

var settings = new FactosSettings
{
    TestedApps = [

        // example app without test groups (runs always)
        new WindowsApp
        {
            ProjectPath = $"{root}WPFTests",
            ExecutableName = "WPFTests.exe"
        },

        // when test group are defined, the app will only run if the group is specified in the CLI
        // the next command will run tests for browser and windows apps:
        // dotnet test --test-groups browser windows

        // == wpf example ==
        new WindowsApp
        {
            ProjectPath = $"{root}WPFTests",
            ExecutableName = "WPFTests.exe",
            TestGroups = ["windows", "wpf"]
        },

        // == winui example ==
        new WindowsApp
        {
            ProjectPath = $"{root}WinUITests",
            ExecutableName = "WinUITests.exe",
            PublishArgs =
                "-c Release -r win-x64 -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true " +
                "-p:PublishTrimmed=false -p:PublishSingleFile=false -p:UseSrc=false",
            TestGroups = ["windows", "winui"]
        },

        // == avalonia example ==
        new WindowsApp
        {
            ProjectPath = $"{root}AvaloniaTests.Desktop",
            ExecutableName = "AvaloniaTests.Desktop.exe",
            TestGroups = ["windows", "avalonia", "avalonia-windows"]
        },
        new AndroidApp
        {
            ProjectPath = $"{root}AvaloniaTests.Android",
            AppName = "com.CompanyName.AvaloniaTest",
            TestGroups = ["android", "avalonia", "avalonia-android"]
        },
        new ReactiveCircusActionApp // uses Reactive Circus Action runner for Android CI
        {
            ProjectPath = $"{root}AvaloniaTests.Android",
            AppName = "com.CompanyName.AvaloniaTest",
            TestGroups = ["avalonia-android-ci"]
        },
        new BrowserApp
        {
            ProjectPath = $"{root}AvaloniaTests.Browser",
            TestGroups = ["browser", "avalonia-wasm"]
        },
        new BrowserApp
        {
            ProjectPath = $"{root}AvaloniaTests.Browser",
            HeadlessChrome = true, // use headless Chrome for CI
            TestGroups = ["browser", "avalonia-wasm-ci"]
        },

        // == blazor wasm example ==
        new BrowserApp
        {
            ProjectPath = $"{root}BlazorTests",
            TestGroups = ["browser", "blazor-wasm"]
        },
        new BrowserApp
        {
            ProjectPath = $"{root}BlazorTests",
            HeadlessChrome = true, // use headless Chrome for CI
            TestGroups = ["blazor-wasm-ci"]
        },

        // == maui example ==
        new WindowsApp
        {
            ProjectPath = $"{root}MAUITests",
            ExecutableName = "MAUITests.exe",
            PublishArgs = "-c Release -f net10.0-windows10.0.19041.0",
            TestGroups = ["windows", "maui", "maui-windows"]
        },
        new AndroidApp
        {
            ProjectPath = $"{root}MAUITests",
            AppName = "com.companyname.mauitests",
            PublishArgs = "-c Release -f net10.0-android",
            TestGroups = ["android", "maui", "maui-android"]
        },
        new ReactiveCircusActionApp // uses Reactive Circus Action runner for Android CI
        {
            ProjectPath = $"{root}MAUITests",
            AppName = "com.companyname.mauitests",
            PublishArgs = "-c Release -f net10.0-android",
            TestGroups = ["maui-android-ci"]
        }
    ]
};

testsBuilder
    .AddFactos(settings)
    .AddTrxReportProvider(); // optional, add TRX if needed

using ITestApplication testApp = await testsBuilder.BuildAsync();

return await testApp.RunAsync();
