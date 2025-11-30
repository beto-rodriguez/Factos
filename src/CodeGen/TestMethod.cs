namespace CodeGen;

public struct TestMethod(
    string containingClass,
    string name,
    bool isStatic,
    bool isAsync,
    string assemblyFullName,
    string @namespace,
    string typeName,
    int methodArity,
    string parameterTypeFullNames,
    string returnTypeFullName,
    string uid,
    string displayName,
    string? skipReason,
    bool expectFail)
{
    public string ContainingClass { get; set; } = containingClass;
    public string Name { get; set; } = name;

    public bool IsStatic { get; set; } = isStatic;
    public bool IsAsync { get; set; } = isAsync;

    public string AssemblyFullName { get; set; } = assemblyFullName;
    public string Namespace { get; set; } = @namespace;
    public string TypeName { get; set; } = typeName;
    public int MethodArity { get; set; } = methodArity;
    public string ParameterTypeFullNames { get; set; } = parameterTypeFullNames;
    public string ReturnTypeFullName { get; set; } = returnTypeFullName;

    public string Uid { get; set; } = uid;
    public string DisplayName { get; set; } = displayName;

    public string? SkipReason { get; set; } = skipReason;
    public bool ExpectFail { get; set; } = expectFail;
}