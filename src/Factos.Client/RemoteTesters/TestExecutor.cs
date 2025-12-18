using Factos.Abstractions.Dto;

namespace Factos.RemoteTesters;

public abstract class TestExecutor
{
    internal abstract IAsyncEnumerable<TestNodeDto> Execute();
}
