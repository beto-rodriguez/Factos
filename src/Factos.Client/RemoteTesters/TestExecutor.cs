namespace Factos.RemoteTesters;

public abstract class TestExecutor
{
    public abstract Task<ExecutionResponse> Execute();
}
