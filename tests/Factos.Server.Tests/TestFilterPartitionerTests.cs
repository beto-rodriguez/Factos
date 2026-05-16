using Factos.Server.ClientConnection;

namespace Factos.Server.Tests;

// Locks in the MTP filter -> per-app UID translation. MTP returns
// TestNodeUidListFilter UIDs that carry the `[appName]` prefix attached by
// MTPResultsMapper during discovery; the partitioner has to strip it and tell
// us whether to skip an app entirely (no UIDs in this app's slice).
internal static class TestFilterPartitionerTests
{
    public static Task Null_filter_runs_every_app_with_empty_uid_slice()
    {
        var (skip, appUids) = TestFilterPartitioner.PartitionForApp(null, "AvaloniaTests");

        AssertFalse(skip, "null filter means 'no filter' -> never skip");
        AssertEqual(0, appUids.Length, "null filter -> empty uid slice (sentinel for run-all on client)");
        return Task.CompletedTask;
    }

    public static Task Strips_prefix_for_matching_app()
    {
        var selected = new[] { "[AvaloniaTests]UnitTests.Foo", "[AvaloniaTests]UnitTests.Bar" };

        var (skip, appUids) = TestFilterPartitioner.PartitionForApp(selected, "AvaloniaTests");

        AssertFalse(skip, "matches exist -> do not skip");
        AssertSequenceEqual(new[] { "UnitTests.Foo", "UnitTests.Bar" }, appUids, "prefix stripped");
        return Task.CompletedTask;
    }

    public static Task Partitions_uids_across_apps()
    {
        var selected = new[]
        {
            "[AvaloniaTests]UnitTests.Foo",
            "[WPFTests]UnitTests.Bar",
            "[AvaloniaTests]UnitTests.Baz",
        };

        var (skipA, uidsA) = TestFilterPartitioner.PartitionForApp(selected, "AvaloniaTests");
        var (skipW, uidsW) = TestFilterPartitioner.PartitionForApp(selected, "WPFTests");

        AssertFalse(skipA, "Avalonia has matches");
        AssertSequenceEqual(new[] { "UnitTests.Foo", "UnitTests.Baz" }, uidsA, "Avalonia slice");

        AssertFalse(skipW, "WPF has matches");
        AssertSequenceEqual(new[] { "UnitTests.Bar" }, uidsW, "WPF slice");
        return Task.CompletedTask;
    }

    public static Task Signals_skip_when_filter_has_no_uids_for_app()
    {
        var selected = new[] { "[AvaloniaTests]UnitTests.Foo" };

        var (skip, appUids) = TestFilterPartitioner.PartitionForApp(selected, "WPFTests");

        AssertTrue(skip, "no uids for this app -> caller must skip launch (avoid wasted app start)");
        AssertEqual(0, appUids.Length, "empty slice when skipping");
        return Task.CompletedTask;
    }

    public static Task Ignores_uids_without_app_prefix()
    {
        // Defensive: anything that doesn't carry the `[appName]` prefix can't
        // belong to a known app. Drop it rather than guess.
        var selected = new[] { "UnitTests.Foo", "[AvaloniaTests]UnitTests.Bar" };

        var (skip, appUids) = TestFilterPartitioner.PartitionForApp(selected, "AvaloniaTests");

        AssertFalse(skip, "the one prefixed uid still matches");
        AssertSequenceEqual(new[] { "UnitTests.Bar" }, appUids, "non-prefixed entry dropped");
        return Task.CompletedTask;
    }

    public static Task Does_not_match_other_app_whose_name_is_a_prefix_substring()
    {
        // `[Foo]` must not match against a UID prefixed with `[FooBar]`, otherwise
        // selecting a test from app "FooBar" would leak into app "Foo".
        var selected = new[] { "[FooBar]UnitTests.X" };

        var (skip, appUids) = TestFilterPartitioner.PartitionForApp(selected, "Foo");

        AssertTrue(skip, "[Foo] prefix must not partial-match [FooBar]");
        AssertEqual(0, appUids.Length, "no leak across app names");
        return Task.CompletedTask;
    }

    static void AssertTrue(bool value, string message)
    {
        if (!value) throw new Exception($"Expected true: {message}");
    }

    static void AssertFalse(bool value, string message)
    {
        if (value) throw new Exception($"Expected false: {message}");
    }

    static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception($"Expected '{expected}' but got '{actual}': {message}");
    }

    static void AssertSequenceEqual<T>(T[] expected, T[] actual, string message)
    {
        if (expected.Length != actual.Length)
            throw new Exception($"Expected length {expected.Length} but got {actual.Length}: {message}");

        for (int i = 0; i < expected.Length; i++)
        {
            if (!EqualityComparer<T>.Default.Equals(expected[i], actual[i]))
                throw new Exception($"At index {i}: expected '{expected[i]}' but got '{actual[i]}': {message}");
        }
    }
}
