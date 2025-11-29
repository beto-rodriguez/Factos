namespace Factos.RemoteTesters;

public abstract class TestExecutor
{
    public abstract Task<string> Discover();
    public abstract Task<string> Run();
}
