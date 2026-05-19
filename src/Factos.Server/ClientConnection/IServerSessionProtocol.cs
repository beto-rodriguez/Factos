using Factos.Abstractions.Dto;

namespace Factos.Server.ClientConnection;

internal interface IServerSessionProtocol
{
    string Id { get; }

    Task Start(CancellationToken cancellationToken);

    Task Finish(CancellationToken cancellationToken);

    IAsyncEnumerable<TestNodeDto> RequestClient(string clientName, string[] testUids, CancellationToken cancellationToken);

    Task CloseClient(string clientName, CancellationToken cancellationToken);
}
