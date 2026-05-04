using Factos.Server.Tests;

var failures = 0;

failures += await Run(
    nameof(TestHubDisconnectTests.OnDisconnected_after_AllTestsCompleted_does_not_emit_abort_node),
    TestHubDisconnectTests.OnDisconnected_after_AllTestsCompleted_does_not_emit_abort_node);

failures += await Run(
    nameof(TestHubDisconnectTests.OnDisconnected_without_AllTestsCompleted_emits_abort_node_carrying_the_disconnect_reason),
    TestHubDisconnectTests.OnDisconnected_without_AllTestsCompleted_emits_abort_node_carrying_the_disconnect_reason);

failures += await Run(
    nameof(TestHubDisconnectTests.OnDisconnected_without_AllTestsCompleted_uses_default_reason_when_exception_is_null),
    TestHubDisconnectTests.OnDisconnected_without_AllTestsCompleted_uses_default_reason_when_exception_is_null);

Console.WriteLine();
Console.WriteLine(failures == 0 ? "All tests passed." : $"{failures} test(s) failed.");
return failures;

static async Task<int> Run(string name, Func<Task> test)
{
    try
    {
        await test();
        Console.WriteLine($"PASS  {name}");
        return 0;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"FAIL  {name}");
        Console.WriteLine($"      {ex.Message}");
        return 1;
    }
}
