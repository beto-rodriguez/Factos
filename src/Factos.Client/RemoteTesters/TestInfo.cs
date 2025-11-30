using Factos.Abstractions.Dto;

namespace Factos.RemoteTesters;

public class TestInfo(
    string uid, string displayName, TestMethodIdentifierPropertyDto id, Func<Task> invoker, string? skipReason, bool expectFail)
{
    public TestMethodIdentifierPropertyDto Identifier { get; set; } = id;
    public string Uid { get; } = uid;
    public string DisplayName { get; } = displayName;
    public Func<Task> Invoker { get; } = invoker;
    public string? SkipReason { get; } = skipReason;
    public bool ExpectFail { get; } = expectFail;
}
