using Factos.Abstractions.Dto;
using System.Threading.Channels;

namespace Factos.Server.ClientConnection;

/// <summary>
/// The results stream of one tested app. The SignalR hub writes into it and
/// the test framework reads from it.
/// </summary>
/// <remarks>
/// A channel buffers every message the client sends, so nothing is lost while
/// the reader is busy publishing the previous node; the former design
/// re-subscribed to static events between nodes and dropped whatever arrived in
/// that gap, including the final AllTestsCompleted, which hung the run.
/// </remarks>
internal sealed class ClientSession(string clientName, string[] testUids)
{
    public const string ABORTED_NODE_UID = "factos-session-aborted";
    public const string TIMEOUT_NODE_UID = "factos-session-timeout";

    readonly Channel<TestNodeDto> _channel = Channel.CreateUnbounded<TestNodeDto>();
    readonly object _sync = new();
    bool _isCompleted;

    public string ClientName { get; } = clientName;
    public string[] TestUids { get; } = testUids;
    public ChannelReader<TestNodeDto> Reader => _channel.Reader;

    /// <summary>
    /// Gets whether the client signed off with AllTestsCompleted.
    /// </summary>
    public bool CompletedGracefully { get; private set; }

    public void Publish(TestNodeDto node)
    {
        lock (_sync)
        {
            if (_isCompleted) return;
            _ = _channel.Writer.TryWrite(node);
        }
    }

    public void CompleteGracefully()
    {
        lock (_sync)
        {
            if (_isCompleted) return;
            _isCompleted = true;
            CompletedGracefully = true;
            _ = _channel.Writer.TryComplete();
        }
    }

    /// <summary>
    /// Ends the session with a failed node, unless the client already signed off.
    /// </summary>
    public void Abort(string explanation)
    {
        lock (_sync)
        {
            if (_isCompleted) return;
            _isCompleted = true;
            _ = _channel.Writer.TryWrite(CreateFailedNode(ABORTED_NODE_UID, "Factos session aborted", explanation));
            _ = _channel.Writer.TryComplete();
        }
    }

    public static TestNodeDto CreateFailedNode(string uid, string displayName, string explanation) =>
        new()
        {
            Uid = uid,
            DisplayName = displayName,
            Properties = [new FailedTestNodeStatePropertyDto { Explanation = explanation }]
        };
}
