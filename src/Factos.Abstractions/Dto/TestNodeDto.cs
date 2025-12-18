namespace Factos.Abstractions.Dto;

public class TestNodeDto
{
    public const string DISCOVERED_NODES = "discovered-nodes";
    public const string EXECUTED_NODES = "executed-nodes";
    public required string Uid { get; init; }
    public required string DisplayName { get; init; }
    public required PropertyDto[] Properties { get; init; }
    public TestNodeDto[]? Children { get; init; }
}
