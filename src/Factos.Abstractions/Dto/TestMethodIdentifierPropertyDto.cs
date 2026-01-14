namespace Factos.Abstractions.Dto;

public class TestMethodIdentifierPropertyDto : PropertyDto
{
    public string AssemblyFullName { get; set; } = string.Empty;

    public string Namespace { get; set; } = string.Empty;

    public string TypeName { get; set; } = string.Empty;

    public string MethodName { get; set; } = string.Empty;

    public int MethodArity { get; set; }

    public string[] ParameterTypeFullNames { get; set; } = [];

    public string ReturnTypeFullName { get; set; } = string.Empty;
}
