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

failures += await Run(
    nameof(TestFilterPartitionerTests.Null_filter_runs_every_app_with_empty_uid_slice),
    TestFilterPartitionerTests.Null_filter_runs_every_app_with_empty_uid_slice);

failures += await Run(
    nameof(TestFilterPartitionerTests.Strips_prefix_for_matching_app),
    TestFilterPartitionerTests.Strips_prefix_for_matching_app);

failures += await Run(
    nameof(TestFilterPartitionerTests.Partitions_uids_across_apps),
    TestFilterPartitionerTests.Partitions_uids_across_apps);

failures += await Run(
    nameof(TestFilterPartitionerTests.Signals_skip_when_filter_has_no_uids_for_app),
    TestFilterPartitionerTests.Signals_skip_when_filter_has_no_uids_for_app);

failures += await Run(
    nameof(TestFilterPartitionerTests.Ignores_uids_without_app_prefix),
    TestFilterPartitionerTests.Ignores_uids_without_app_prefix);

failures += await Run(
    nameof(TestFilterPartitionerTests.Does_not_match_other_app_whose_name_is_a_prefix_substring),
    TestFilterPartitionerTests.Does_not_match_other_app_whose_name_is_a_prefix_substring);

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
