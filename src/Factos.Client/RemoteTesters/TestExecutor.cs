namespace Factos.RemoteTesters;

public abstract class TestExecutor
{
    internal abstract Task<ExecutionResponse> Execute();
}
