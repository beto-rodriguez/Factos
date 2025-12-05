using Factos.Abstractions;
using Factos.Server;
using Factos.Server.Settings;
using Factos.Server.Settings.Apps;
using Microsoft.Testing.Extensions;
using Microsoft.Testing.Platform.Builder;

var testsBuilder = await TestApplication.CreateBuilderAsync(args);

var root = "../../../../";

#if !DEBUG
// we are using the release mode in CI pipelines
// we adjust the path, in this case, the relative path to the samples
root = "samples/basic/";
#endif

var settings = new FactosSettings
{
    TestedApps = [

        // example app without test groups (runs always)
        // new DesktopApp
        // {
        //     ProjectPath = $"{root}WPFTests",
        //     ExecutableName = "WPFTests.exe"
        // },

        // when test groups are defined, the app will only run if the group is specified in the CLI.
        // the next command will run tests for browser and windows apps:
        // dotnet test --test-groups browser windows

        // == wpf example ==
        new DesktopApp
        {
            ProjectPath = $"{root}WPFTests",
            ExecutableName = "WPFTests.exe",
            TestGroups = ["windows", "wpf"]
        },

        // == winui example ==
        new DesktopApp
        {
            ProjectPath = $"{root}WinUITests",
            ExecutableName = "WinUITests.exe",
            PublishArgs =
                "-c Release -r win-x64 -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true " +
                "-p:PublishTrimmed=false -p:PublishSingleFile=false -p:UseSrc=false",
            TestGroups = ["windows", "winui"]
        },

        // == winforms example ==
        new DesktopApp
        {
            ProjectPath = $"{root}WinFormsTests",
            ExecutableName = "WinFormsTests.exe",
            TestGroups = ["windows", "winforms"]
        },

        // == etoforms example ==
        new DesktopApp
        {
            ProjectPath = $"{root}EtoFormsTests",
            ExecutableName = "EtoFormsTests.exe",
            TestGroups = ["windows", "etoforms"]
        },

        // == avalonia example ==
        new DesktopApp
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

         // == uno example ==
        new DesktopApp
        {
            ProjectPath = $"{root}UnoTests/UnoTests",
            ExecutableName = "UnoTests.exe",
            PublishArgs = "-c Release -f net10.0-desktop",
            TestGroups = ["windows", "uno", "uno-windows"]
        },
        new AndroidApp
        {
            ProjectPath = $"{root}UnoTests/UnoTests",
            AppName = "com.companyname.UnoTests",
            PublishArgs = "-c Release -f net10.0-android",
            TestGroups = ["android", "uno", "uno-android"]
        },
        new ReactiveCircusActionApp // uses Reactive Circus Action runner for Android CI
        {
            ProjectPath = $"{root}UnoTests/UnoTests",
            AppName = "com.companyname.UnoTests",
            PublishArgs = "-c Release -f net10.0-android",
            TestGroups = ["uno-android-ci"]
        },
        new BrowserApp
        {
            ProjectPath = $"{root}UnoTests/UnoTests",
            PublishArgs = "-c Release -f net10.0-browserwasm",
            TestGroups = ["browser", "uno-wasm"]
        },
        new BrowserApp
        {
            ProjectPath = $"{root}UnoTests/UnoTests",
            HeadlessChrome = true, // use headless Chrome for CI
            PublishArgs = "-c Release -f net10.0-browserwasm",
            TestGroups = ["browser", "uno-wasm-ci"]
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
        new DesktopApp
        {
            ProjectPath = $"{root}MAUITests",
            ExecutableName = "MAUITests.exe",
            PublishArgs = "-c Release -f net10.0-windows10.0.19041.0",
            TestGroups = ["windows", "maui", "maui-windows"]
        },
        TestApp.FromCommands(
            config: (
                projectPath: $"{root}MAUITests",
                outputPath: "bin/Release/net10.0-maccatalyst",
                ["maccatalyst", "maui", "maui-maccatalyst"]),
            commands: app => [
                $"""
                dotnet build {app.ProjectPath}
                    -o {app.ProjectPath}/{app.OutputPath}
                    -c Release
                    -f net10.0-maccatalyst
                    -r maccatalyst-x64
                    -p:BuildMacCatalystApp=true
                """,
                $"open {app.ProjectPath}/{app.OutputPath}/MAUITests.app"
            ]
        ),
        TestApp.FromCommands(
            config: (
                projectPath: $"{root}MAUITests",
                outputPath: "bin/Release/net10.0-ios",
                ["maccatalyst", "maui", "maui-ios"]),
            commands: app => [
                $"{Constants.TASK_COMMAND} cd-at-project",
                $"dotnet run -f net10.0-ios -c Debug &", // maybe trimming is too agressive?
                $"{Constants.TASK_COMMAND} cd-pop"
            ]
        ),
        // new DesktopApp
        // {
        //     ProjectPath = $"{root}MAUITests",
        //     ExecutableName = "MAUITests.app",
        //     PublishArgs = "-c Release -f net10.0-maccatalyst -p:BuildMacCatalystApp=true",
        //     // it seems that the default output path is not working for mac catalyst
        //     // lets use the explicit path produced by the publish command
        //     OutputPath = "bin/Release/net10.0-maccatalyst",
        //     TestGroups = ["maccatalyst", "maui", "maui-maccatalyst"]
        // },
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
