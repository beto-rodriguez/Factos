using Microsoft.Testing.Platform.Extensions.Messages;

namespace Factos.Server.ClientConnection;

internal class NodesResponse
{ 
    public IEnumerable<TestNode> Nodes { get; init; } = [];

    public IServerSessionProtocol Sender { get; init; } = null!;
}