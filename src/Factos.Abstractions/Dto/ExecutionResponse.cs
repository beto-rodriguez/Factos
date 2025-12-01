using Factos.Abstractions.Dto;

namespace Factos.RemoteTesters;

public class ExecutionResponse
{
    public IEnumerable<TestNodeDto> Discovered { get; set; } = [];
    public IEnumerable<TestNodeDto> Results { get; set; } = [];
}