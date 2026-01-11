using Factos.Abstractions;

namespace Factos;

public struct ControllerSettings
{
    public static ControllerSettings Default { get; } = new()
    {
        TcpPort = Constants.DEFAULT_TCP_PORT,
        HttpServerUri = Constants.DEFAULT_HTTP_URI,
        WebSocketsServerUri = Constants.DEFAULT_WEBSOCKETS_URI
    };

    public int TcpPort { get; set; }
    public string HttpServerUri { get; set; }
    public string WebSocketsServerUri { get; set; }
}
