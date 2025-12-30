namespace Factos.RemoteTesters;

public class TestStreamHandler
{
    public string Reason { get; set; } = string.Empty;
    public CancellationTokenSource CancellationTokenSource { get; } = new();

    public void Cancel(Exception exception)
    {
        // get the full exception message including inner exceptions
        Reason = exception.ToString();
        CancellationTokenSource.Cancel();
    }
}
