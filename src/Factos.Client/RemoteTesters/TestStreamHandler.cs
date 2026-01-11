namespace Factos.RemoteTesters;

public class TestStreamHandler
{
    public string LastKnownTestUid { get; set; } = string.Empty;
    public string LastKnownTestDisplayName { get; set; } = string.Empty;
    public string CancelationReason { get; set; } = string.Empty;
    public CancellationTokenSource CancellationTokenSource { get; } = new();

    public void Cancel(Exception exception)
    {
        // get the full exception message including inner exceptions
        CancelationReason = exception.ToString();
        CancellationTokenSource.Cancel();
    }
}
