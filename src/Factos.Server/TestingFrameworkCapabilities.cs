using Microsoft.Testing.Platform.Capabilities.TestFramework;

namespace Factos.Server;

internal sealed class TestingFrameworkCapabilities : ITestFrameworkCapabilities
{
    public IReadOnlyCollection<ITestFrameworkCapability> Capabilities => [];

#pragma warning disable IDE0060 // Remove unused parameter
    public static TestingFrameworkCapabilities Create(IServiceProvider serviceProvider) =>
        new();
#pragma warning restore IDE0060 // Remove unused parameter
}
