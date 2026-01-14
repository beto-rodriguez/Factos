namespace Factos.Abstractions.Dto;

public class TestNodeDto
{
    public const string DISCOVERED_NODES = "discovered-nodes";
    public const string EXECUTED_NODES = "executed-nodes";
    public string Uid { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public PropertyDto[] Properties { get; set; } = [];
    public TestNodeDto[]? Children { get; set; }
}
