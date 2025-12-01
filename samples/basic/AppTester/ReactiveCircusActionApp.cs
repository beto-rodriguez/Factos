using Factos.Server.Settings;

public class ReactiveCircusActionApp : TestApp
{
    public required string AppName { get; set; }

    protected override string[]? GetDefaultCommands() => [
        $"adb install -r {ProjectPath}/{OutputPath}/{AppName}-Signed.apk",
        $"adb shell monkey -p {AppName} -c android.intent.category.LAUNCHER 1"
    ];
}