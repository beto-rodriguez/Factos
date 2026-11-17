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
    nameof(TestHubDisconnectTests.Results_sent_while_the_reader_is_busy_are_not_lost),
    TestHubDisconnectTests.Results_sent_while_the_reader_is_busy_are_not_lost);

failures += await Run(
    nameof(TestHubDisconnectTests.Late_disconnect_of_the_previous_app_does_not_end_the_next_session),
    TestHubDisconnectTests.Late_disconnect_of_the_previous_app_does_not_end_the_next_session);

failures += await Run(
    nameof(TestHubDisconnectTests.Timeout_ends_the_session_with_a_failed_node),
    TestHubDisconnectTests.Timeout_ends_the_session_with_a_failed_node);

failures += await Run(
    nameof(TestHubDisconnectTests.AbortClient_ends_the_session_unless_the_client_signed_off),
    TestHubDisconnectTests.AbortClient_ends_the_session_unless_the_client_signed_off);

failures += await Run(
    nameof(TestHubDisconnectTests.Background_process_with_non_zero_exit_reports_instead_of_exiting),
    TestHubDisconnectTests.Background_process_with_non_zero_exit_reports_instead_of_exiting);

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
