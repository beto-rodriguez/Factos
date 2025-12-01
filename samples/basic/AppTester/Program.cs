using Factos.Server;
using Microsoft.Testing.Extensions;
using Microsoft.Testing.Platform.Builder;

var testsBuilder = await TestApplication.CreateBuilderAsync(args);

testsBuilder
    .AddFactos(settings => settings
        // the root path for the next added test apps
        .SetRoot("../../../../")

#if DEBUG
        // when in debug this is an example to test multiple apps
        .TestAndroidApp("MAUITests", "com.companyname.mauitests", publishArgs: "-f net10.0-android")
        .TestBlazorApp("BlazorTests", port: 5080)
        .TestWindowsApp("WPFTests", "WPFTests.exe")
        .TestWindowsApp("MAUITests", "MAUITests.exe", publishArgs: "-f net10.0-windows10.0.19041.0")
        .TestWindowsApp("WinUITests", "WinUITests.exe", publishArgs:
            "-r win-x64 -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true " +
            "-p:PublishTrimmed=false -p:PublishSingleFile=false -p:UseSrc=false")
#endif

        .TestWindowsApp("WPFTests", "WPFTests.exe", enabled: args.Contains("--wpf"))
        )

    // optional, add TRX if needed
    .AddTrxReportProvider();


using ITestApplication testApp = await testsBuilder.BuildAsync();
return await testApp.RunAsync();
