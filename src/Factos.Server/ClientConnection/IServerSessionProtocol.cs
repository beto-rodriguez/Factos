using Microsoft.Testing.Platform.Extensions.TestFramework;

namespace Factos.Server.ClientConnection;

internal interface IServerSessionProtocol
{
    string Id { get; }

    Task Start(CancellationToken cancellationToken);

    Task Finish(CancellationToken cancellationToken);

    Task<NodesResponse> RequestClient(string clientName, ExecuteRequestContext context);

    Task CloseClient(string clientName, CancellationToken cancellationToken);
}
