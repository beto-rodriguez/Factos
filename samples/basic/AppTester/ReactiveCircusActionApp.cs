using Factos.Server;
using Factos.Server.Settings.Apps;

public class ReactiveCircusActionApp : AndroidApp
{
    protected override Task StartEmulator(DeviceWritter deviceWritter, CancellationToken cancellationToken)
    {
        // do nothing, the emulator is started externally by Reactive Circus
        return Task.CompletedTask;
    }
}