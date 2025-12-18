using Factos.Abstractions;
using Factos.Server.Settings.Apps;

namespace Factos.Server.Settings;

/// <summary>
/// Represents the configuration settings for Factos network communication and application testing.
/// </summary>
/// <remarks>This class encapsulates options such as connection timeouts, network endpoints, supported protocols,
/// and the set of applications to be tested. It is typically used to configure Factos services or clients prior to
/// initialization.</remarks>
public class FactosSettings
{
    /// <summary>
    /// Gets or sets the connection timeout in seconds.
    /// </summary>
    public int ConnectionTimeout { get; set; } = 1200;

    /// <summary>
    /// Gets or sets the TCP port number used for network communication.
    /// </summary>
    public int TcpPort { get; set; } = Constants.DEFAULT_TCP_PORT;

    /// <summary>
    /// Gets or sets the HTTP URI endpoint used for network requests.
    /// </summary>
    public string HttpUri { get; set; } = Constants.DEFAULT_HTTP_URI;

    /// <summary>
    /// Gets or sets the WebSockets URI endpoint used for network requests.
    /// </summary>
    public string WebSocketsUri { get; set; } = Constants.DEFAULT_WEBSOCKETS_URI;

    /// <summary>
    /// Gets or sets the collection of applications to be tested.
    /// </summary>
    public IList<TestedApp> TestedApps { get; set; } = [];
}
