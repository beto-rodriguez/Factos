using Microsoft.Testing.Platform.Capabilities.TestFramework;

namespace Factos.Server;

internal sealed class FactosCapabilities : ITestFrameworkCapabilities
{
    public TrxCapability TrxCapability { get; } = new();

    public IReadOnlyCollection<ITestFrameworkCapability> Capabilities => [TrxCapability];
}
