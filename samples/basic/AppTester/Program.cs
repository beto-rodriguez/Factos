using Factos.Server;
using Factos.Server.Settings;
using Factos.Server.Settings.Apps;
using Microsoft.Testing.Extensions;
using Microsoft.Testing.Platform.Builder;

var testsBuilder = await TestApplication.CreateBuilderAsync(args);

var settings = new FactosSettings
{
    TestedApps = [
        new WindowsApp
        {
            ProjectPath = "../../../../WPFTests",
            ExecutableName = "WPFTests.exe",
            TestGroups = ["windows", "wpf"]
        },
        new WindowsApp
        {
            ProjectPath = "../../../../MAUITests",
            ExecutableName = "MAUITests.exe",
            PublishArgs = "-f net10.0-windows10.0.19041.0",
            TestGroups = ["windows", "maui", "maui-windows" ]
        },
        new WindowsApp
        {
            ProjectPath = "../../../../WinUITests",
            ExecutableName = "WinUITests.exe",
            PublishArgs =
                "-r win-x64 -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true " +
                "-p:PublishTrimmed=false -p:PublishSingleFile=false -p:UseSrc=false",
            TestGroups = ["windows", "winui"]
        },
        //new BlazorWasmApp
        //{
        //    ProjectPath = "../../../../BlazorTests"
        //},
        //new AndroidApp
        //{
        //    ProjectPath = "../../../../MAUITests",
        //    AppName = "com.companyname.mauitests",
        //    PublishArgs = "-f net10.0-android"
        //}
    ]
};


testsBuilder
    .AddFactos(settings)
    //.AddFactos(settings => settings

//#if DEBUG
//        // ==== DEBUG SECTION =============================
//        // the debug examples work when running the AppTester project
//        // or calling dotnet test / dotnet run from the AppTester output folder.

//        // the root path for the next added test apps
//        .SetRoot("../../../../")
//        // when in debug this is an example to test multiple apps
//        .TestAndroidApp("MAUITests", "com.companyname.mauitests", publishArgs: "-f net10.0-android")
//        .TestBlazorApp("BlazorTests", port: 5080)
//        .TestWindowsApp("WPFTests", "WPFTests.exe")
//        .TestWindowsApp("MAUITests", "MAUITests.exe", publishArgs: "-f net10.0-windows10.0.19041.0")
//        .TestWindowsApp("WinUITests", "WinUITests.exe", publishArgs:
//            "-r win-x64 -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true " +
//            "-p:PublishTrimmed=false -p:PublishSingleFile=false -p:UseSrc=false")
//#endif

//        // ==== CI SECTION =============================
//        // for convenience we will run the test from the repo root, so we set
//        // the tests root to samples/basic
//        .SetRoot("samples/basic/")

//        // we are using the "when" condition, the next test will only run
//        // when the --wpf argument is provided to the test runner
//        .TestWindowsApp("WPFTests", "WPFTests.exe", when: "wpf")
//        .TestWindowsApp("MAUITests", "MAUITests.exe", when: "maui-windows", publishArgs: "-f net10.0-windows10.0.19041.0")
//        .TestWindowsApp("WinUITests", "WinUITests.exe",when: "winui", publishArgs:
//            "-r win-x64 -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true " +
//            "-p:PublishTrimmed=false -p:PublishSingleFile=false -p:UseSrc=false")

//        .TestAndroidApp("MAUITests", "com.companyname.mauitests", when: "maui-android", publishArgs: "-f net10.0-android")
        //)

    // optional, add TRX if needed
    .AddTrxReportProvider();

using ITestApplication testApp = await testsBuilder.BuildAsync();
return await testApp.RunAsync();
