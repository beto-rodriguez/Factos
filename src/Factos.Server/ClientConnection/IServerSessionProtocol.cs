using Factos.Abstractions.Dto;

namespace Factos.Server.ClientConnection;

internal interface IServerSessionProtocol
{
    string Id { get; }

    Task Start(CancellationToken cancellationToken);

    Task Finish(CancellationToken cancellationToken);

    /// <summary>
    /// Creates the results session for the next client app; call it before the app starts,
    /// so the per-app test filter is ready when the client connects.
    /// </summary>
    void PrepareClient(string clientName, string[] testUids);

    /// <summary>
    /// Ends the current client session with a failed node, unless the client already signed off,
    /// e.g. when the app process exited before reporting its results.
    /// </summary>
    void AbortClient(string clientName, string reason);

    IAsyncEnumerable<TestNodeDto> RequestClient(string clientName, CancellationToken cancellationToken);

    Task CloseClient(string clientName, CancellationToken cancellationToken);
}
