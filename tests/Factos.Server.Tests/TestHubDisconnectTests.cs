using System.Reflection;
using Factos.Abstractions.Dto;
using Factos.Server.ClientConnection;

namespace Factos.Server.Tests;

// Locks in the contract that distinguishes a graceful sign-off from a transport
// drop on the SignalR Hub. Pre-fix, both paths fired the same event and a
// crashed client's partial result set was reported as a passing run.
//
// Bespoke test runner instead of xunit/MTP: Factos.Server pins
// Microsoft.Testing.Platform 2.0.2, which is incompatible with the MTP version
// xunit.v3 currently builds against. A standalone runner sidesteps the
// dependency conflict entirely and keeps this project a tiny self-contained
// console app.
internal static class TestHubDisconnectTests
{
    static readonly FieldInfo CompletedGracefullyField = typeof(WebSocketsServerTestSession.TestHub)
        .GetField("_completedGracefully", BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException(
            "_completedGracefully field is gone — the disconnect-detection invariant is no longer enforced.");

    public static async Task OnDisconnected_after_AllTestsCompleted_does_not_emit_abort_node()
    {
        ResetGracefulFlag();

        TestNodeDto? captured = null;
        var completedFiredCount = 0;

        void OnNode(TestNodeDto n) => captured = n;
        void OnCompleted() => completedFiredCount++;

        WebSocketsServerTestSession.TestHub.OnTestNodeGenerated += OnNode;
        WebSocketsServerTestSession.TestHub.OnAllTestsCompleted += OnCompleted;
        try
        {
            var hub = new WebSocketsServerTestSession.TestHub();

            // Graceful sign-off — sets the flag.
            await hub.AllTestsCompleted();

            // Transport drop following a graceful sign-off must NOT publish a
            // synthetic abort node, otherwise every successful run would gain
            // a phantom failure at the end.
            await hub.OnDisconnectedAsync(exception: null);

            AssertNull(captured, "no synthetic abort node should be emitted after graceful AllTestsCompleted");

            // OnAllTestsCompleted firing once or twice along the graceful path
            // is implementation detail (the existing RequestClient subscriber
            // unsubscribes itself on first fire). The contract is just that it
            // fires at least once, so consumers see the graceful sign-off.
            if (completedFiredCount < 1)
                throw new Exception("OnAllTestsCompleted must fire at least once for the graceful path");
        }
        finally
        {
            WebSocketsServerTestSession.TestHub.OnTestNodeGenerated -= OnNode;
            WebSocketsServerTestSession.TestHub.OnAllTestsCompleted -= OnCompleted;
        }
    }

    public static async Task OnDisconnected_without_AllTestsCompleted_emits_abort_node_carrying_the_disconnect_reason()
    {
        ResetGracefulFlag();

        TestNodeDto? captured = null;
        void OnNode(TestNodeDto n) => captured = n;

        WebSocketsServerTestSession.TestHub.OnTestNodeGenerated += OnNode;
        try
        {
            var hub = new WebSocketsServerTestSession.TestHub();

            await hub.OnDisconnectedAsync(new InvalidOperationException("connection reset by peer"));

            AssertNotNull(captured, "abort node must be emitted when client disconnects without graceful sign-off");
            AssertEqual("factos-session-aborted", captured!.Uid, "abort node Uid");
            AssertEqual("Factos session aborted", captured.DisplayName, "abort node DisplayName");

            var failed = AssertSingleProperty<FailedTestNodeStatePropertyDto>(captured.Properties);
            AssertNotNull(failed.Explanation, "abort node Explanation must not be null");
            AssertContains("AllTestsCompleted", failed.Explanation!, "Explanation must reference the missing RPC");
            AssertContains("connection reset by peer", failed.Explanation!, "Explanation must include the disconnect reason");
        }
        finally
        {
            WebSocketsServerTestSession.TestHub.OnTestNodeGenerated -= OnNode;
        }
    }

    public static async Task OnDisconnected_without_AllTestsCompleted_uses_default_reason_when_exception_is_null()
    {
        ResetGracefulFlag();

        TestNodeDto? captured = null;
        void OnNode(TestNodeDto n) => captured = n;

        WebSocketsServerTestSession.TestHub.OnTestNodeGenerated += OnNode;
        try
        {
            var hub = new WebSocketsServerTestSession.TestHub();

            await hub.OnDisconnectedAsync(exception: null);

            AssertNotNull(captured, "abort node must still be emitted when exception is null");
            var failed = AssertSingleProperty<FailedTestNodeStatePropertyDto>(captured!.Properties);
            AssertContains("disconnected before reporting completion", failed.Explanation!, "default reason");
        }
        finally
        {
            WebSocketsServerTestSession.TestHub.OnTestNodeGenerated -= OnNode;
        }
    }

    static void ResetGracefulFlag() => CompletedGracefullyField.SetValue(null, false);

    static void AssertNotNull(object? value, string message)
    {
        if (value is null) throw new Exception($"Expected non-null: {message}");
    }

    static void AssertNull(object? value, string message)
    {
        if (value is not null) throw new Exception($"Expected null but got '{value}': {message}");
    }

    static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception($"Expected '{expected}' but got '{actual}': {message}");
    }

    static void AssertContains(string expected, string actual, string message)
    {
        if (!actual.Contains(expected, StringComparison.Ordinal))
            throw new Exception($"Expected to find '{expected}' in '{actual}': {message}");
    }

    static T AssertSingleProperty<T>(IEnumerable<PropertyDto> properties) where T : PropertyDto
    {
        var list = properties.ToList();
        if (list.Count != 1) throw new Exception($"Expected exactly one property but got {list.Count}");
        if (list[0] is not T t) throw new Exception($"Expected {typeof(T).Name} but got {list[0].GetType().Name}");
        return t;
    }
}
