namespace Factos.Abstractions.Dto;

public class TestMethodIdentifierPropertyDto : PropertyDto
{
    public required string AssemblyFullName { get; set; }

    public required string Namespace { get; set; }

    public required string TypeName { get; set; }

    public required string MethodName { get; set; }

    public required int MethodArity { get; set; }

    public required string[] ParameterTypeFullNames { get; set; }

    public required string ReturnTypeFullName { get; set; }
}
