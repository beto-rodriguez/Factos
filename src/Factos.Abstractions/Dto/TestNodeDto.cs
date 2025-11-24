namespace Factos.Abstractions.Dto;

public class TestNodeDto
{
    public required string Uid { get; init; }
    public required string DisplayName { get; init; }
    public required PropertyDto[] Properties { get; init; }
}
