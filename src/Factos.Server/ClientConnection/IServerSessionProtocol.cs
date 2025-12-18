using Factos.Abstractions.Dto;

namespace Factos.Server.ClientConnection;

internal interface IServerSessionProtocol
{
    string Id { get; }

    Task Start(CancellationToken cancellationToken);

    Task Finish(CancellationToken cancellationToken);

    IAsyncEnumerable<TestNodeDto> RequestClient(string clientName, CancellationToken cancellationToken);

    Task CloseClient(string clientName, CancellationToken cancellationToken);
}
