using Microsoft.Testing.Extensions.TrxReport.Abstractions;

namespace Factos.Server;

internal sealed class TrxCapability : ITrxReportCapability
{
    public bool IsTrxEnabled { get; set; }

    public bool IsSupported => true;

    public void Enable() => IsTrxEnabled = true;
}
