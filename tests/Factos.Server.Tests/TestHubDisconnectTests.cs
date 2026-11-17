using System.Security.Claims;
using Factos.Abstractions.Dto;
using Factos.Server.ClientConnection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Testing.Platform.Extensions.OutputDevice;
using Microsoft.Testing.Platform.OutputDevice;

namespace Factos.Server.Tests;

// Locks in the contract between the SignalR hub and the results stream of each
// tested app:
// - a graceful sign-off and a transport drop are told apart (pre-fix, a crashed
//   client's partial result set was reported as a passing run),
// - no message is lost while the reader is busy,
// - a late disconnect from one app never ends the next app's session,
// - a timeout or a dead app process ends the session with a failed node.
//
// Bespoke test runner instead of xunit/MTP: Factos.Server pins
// Microsoft.Testing.Platform 2.0.2, which is incompatible with the MTP version
// xunit.v3 currently builds against. A standalone runner sidesteps the
// dependency conflict entirely and keeps this project a tiny self-contained
// console app.
internal static class TestHubDisconnectTests
{
    public static async Task OnDisconnected_after_AllTestsCompleted_does_not_emit_abort_node()
    {
        var session = NewSession(out var sut);
        var hub = new WebSocketsServerTestSession.TestHub();

        await hub.TestNodeGenerated(PassedNode("t1"));

        // Graceful sign-off.
        await hub.AllTestsCompleted();

        // Transport drop following a graceful sign-off must NOT publish a
        // synthetic abort node, otherwise every successful run would gain
        // a phantom failure at the end.
        await hub.OnDisconnectedAsync(exception: null);

        var nodes = await ReadAll(sut, "app");

        AssertEqual(1, nodes.Count, "only the real node is reported");
        AssertEqual("t1", nodes[0].Uid, "the real node");
        AssertEqual(true, session.CompletedGracefully, "graceful flag");
    }

    public static async Task OnDisconnected_without_AllTestsCompleted_emits_abort_node_carrying_the_disconnect_reason()
    {
        NewSession(out var sut);
        var hub = new WebSocketsServerTestSession.TestHub();

        await hub.OnDisconnectedAsync(new InvalidOperationException("connection reset by peer"));

        var nodes = await ReadAll(sut, "app");

        AssertEqual(1, nodes.Count, "abort node must be emitted when client disconnects without graceful sign-off");
        var captured = nodes[0];
        AssertEqual(ClientSession.ABORTED_NODE_UID, captured.Uid, "abort node Uid");
        AssertEqual("Factos session aborted", captured.DisplayName, "abort node DisplayName");

        var failed = AssertSingleProperty<FailedTestNodeStatePropertyDto>(captured.Properties);
        AssertNotNull(failed.Explanation, "abort node Explanation must not be null");
        AssertContains("AllTestsCompleted", failed.Explanation!, "Explanation must reference the missing RPC");
        AssertContains("connection reset by peer", failed.Explanation!, "Explanation must include the disconnect reason");
    }

    public static async Task OnDisconnected_without_AllTestsCompleted_uses_default_reason_when_exception_is_null()
    {
        NewSession(out var sut);
        var hub = new WebSocketsServerTestSession.TestHub();

        await hub.OnDisconnectedAsync(exception: null);

        var nodes = await ReadAll(sut, "app");

        AssertEqual(1, nodes.Count, "abort node must still be emitted when exception is null");
        var failed = AssertSingleProperty<FailedTestNodeStatePropertyDto>(nodes[0].Properties);
        AssertContains("disconnected before reporting completion", failed.Explanation!, "default reason");
    }

    public static async Task Results_sent_while_the_reader_is_busy_are_not_lost()
    {
        NewSession(out var sut);
        var hub = new WebSocketsServerTestSession.TestHub();

        // Pre-fix, the server re-subscribed to static events between nodes, so a
        // node (or the final sign-off) arriving in that gap was dropped and the run
        // hung. Send everything before the reader even starts.
        const int count = 200;
        for (var i = 0; i < count; i++)
            await hub.TestNodeGenerated(PassedNode($"t{i}"));
        await hub.AllTestsCompleted();

        var received = new List<TestNodeDto>();
        await foreach (var node in sut.RequestClient("app", CancellationToken.None))
        {
            received.Add(node);
            await Task.Delay(1); // a slow consumer
        }

        AssertEqual(count, received.Count, "every node is delivered");
        AssertEqual("t0", received[0].Uid, "order is kept (first)");
        AssertEqual($"t{count - 1}", received[^1].Uid, "order is kept (last)");
    }

    public static async Task Late_disconnect_of_the_previous_app_does_not_end_the_next_session()
    {
        var first = NewSession(out var sut, "first");

        // the first app's connection is bound to its own session
        var firstHub = new WebSocketsServerTestSession.TestHub { Context = new FakeCallerContext(first) };
        await firstHub.AllTestsCompleted();

        // the runner moves on to the next app before the first one's disconnect arrives
        sut.PrepareClient("second", []);
        await firstHub.OnDisconnectedAsync(exception: null);

        var secondHub = new WebSocketsServerTestSession.TestHub();
        await secondHub.TestNodeGenerated(PassedNode("second-t1"));
        await secondHub.AllTestsCompleted();

        var nodes = await ReadAll(sut, "second");

        AssertEqual(1, nodes.Count, "the second app's results are not cut short");
        AssertEqual("second-t1", nodes[0].Uid, "the second app's node");
    }

    public static async Task Timeout_ends_the_session_with_a_failed_node()
    {
        NewSession(out var sut);

        // the client never sends anything
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        var nodes = new List<TestNodeDto>();
        await foreach (var node in sut.RequestClient("app", cts.Token))
            nodes.Add(node);

        AssertEqual(1, nodes.Count, "a single timeout node");
        AssertEqual(ClientSession.TIMEOUT_NODE_UID, nodes[0].Uid, "timeout node Uid");
        AssertSingleProperty<FailedTestNodeStatePropertyDto>(nodes[0].Properties);
    }

    public static async Task AbortClient_ends_the_session_unless_the_client_signed_off()
    {
        NewSession(out var sut);
        sut.AbortClient("app", "The app process exited before the tests finished.");

        var nodes = await ReadAll(sut, "app");
        AssertEqual(1, nodes.Count, "abort node");
        AssertEqual(ClientSession.ABORTED_NODE_UID, nodes[0].Uid, "abort node Uid");
        var failed = AssertSingleProperty<FailedTestNodeStatePropertyDto>(nodes[0].Properties);
        AssertContains("app process exited", failed.Explanation!, "abort reason");

        // an app that exits with an error after signing off does not fail the run
        NewSession(out sut);
        await new WebSocketsServerTestSession.TestHub().AllTestsCompleted();
        sut.AbortClient("app", "exited with 0xC000027B on shutdown");

        nodes = await ReadAll(sut, "app");
        AssertEqual(0, nodes.Count, "no abort node after a graceful sign-off");
    }

    public static async Task Background_process_with_non_zero_exit_reports_instead_of_exiting()
    {
        var failed = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        // `dotnet <unknown command>` exits with a non-zero code on every OS. Pre-fix,
        // any positive background exit code called Environment.Exit(1), and a negative
        // one (Windows NTSTATUS crash codes) was logged as a clean exit.
        _ = new ProcessHandler(
            "dotnet factos-command-that-does-not-exist --bg",
            NewWriter(),
            CancellationToken.None,
            reason => failed.TrySetResult(reason));

        string reason;
        try
        {
            reason = await failed.Task.WaitAsync(TimeSpan.FromSeconds(60));
        }
        catch (TimeoutException)
        {
            throw new Exception("the failure callback was not called");
        }

        AssertContains("Exit Code:", reason, "the reason carries the exit code");
    }

    static ClientSession NewSession(out WebSocketsServerTestSession sut, string clientName = "app")
    {
        sut = new WebSocketsServerTestSession(NewWriter(), new());
        sut.PrepareClient(clientName, []);
        return WebSocketsServerTestSession.TestHub.CurrentSession!;
    }

    static async Task<List<TestNodeDto>> ReadAll(WebSocketsServerTestSession sut, string clientName)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var nodes = new List<TestNodeDto>();
        await foreach (var node in sut.RequestClient(clientName, cts.Token))
            nodes.Add(node);

        if (nodes.Any(n => n.Uid == ClientSession.TIMEOUT_NODE_UID))
            throw new Exception("the session did not end, the reader timed out");

        return nodes;
    }

    static DeviceWritter NewWriter() => new(new NullProducer(), new NullOutputDevice());

    static TestNodeDto PassedNode(string uid) =>
        new() { Uid = uid, DisplayName = uid, Properties = [new PassedTestNodeStatePropertyDto()] };

    sealed class NullProducer : IOutputDeviceDataProducer
    {
        public string Uid => nameof(NullProducer);
        public string Version => "1.0.0";
        public string DisplayName => nameof(NullProducer);
        public string Description => nameof(NullProducer);
        public Task<bool> IsEnabledAsync() => Task.FromResult(true);
    }

    sealed class NullOutputDevice : IOutputDevice
    {
        public Task DisplayAsync(IOutputDeviceDataProducer producer, IOutputDeviceData data, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    sealed class FakeCallerContext : HubCallerContext
    {
        public FakeCallerContext(ClientSession session) =>
            Items[WebSocketsServerTestSession.TestHub.SESSION_KEY] = session;

        public override string ConnectionId => "fake";
        public override string? UserIdentifier => null;
        public override ClaimsPrincipal? User => null;
        public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();
        public override IFeatureCollection Features { get; } = new FeatureCollection();
        public override CancellationToken ConnectionAborted => CancellationToken.None;
        public override void Abort() { }
    }

    static void AssertNotNull(object? value, string message)
    {
        if (value is null) throw new Exception($"Expected non-null: {message}");
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
