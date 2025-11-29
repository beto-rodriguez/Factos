using Factos.Abstractions;

namespace Factos;

public readonly struct ControllerSettings
{
    public static ControllerSettings Default { get; } = new()
    {
        TcpPort = Constants.DEFAULT_TCP_PORT,
        HttpServerUri = Constants.DEFAULT_HTTP_URI,
        IsAndroid = false,
        Protocol = ProtocolType.Tcp
    };

    public int TcpPort { get; init; }
    public string HttpServerUri { get; init; }
    public bool IsAndroid { get; init; }
    public ProtocolType Protocol { get; init; }
}
