using Factos.Abstractions;
using System.Text.Json;

namespace Factos.Server.Settings;

public class FactosSettings
{
    public int ConnectionTimeout { get; set; } = 180;
    public int TcpPort { get; set; } = Constants.DEFAULT_TCP_PORT;
    public string HttpUri { get; set; } = Constants.DEFAULT_HTTP_URI;
    public List<TestApp> TestedApps { get; set; } = [];
    public ProtocolType Protocols { get; set; } = ProtocolType.Http | ProtocolType.Tcp;
    public static JsonSerializerOptions JsonOptions { get; } =
        new() { PropertyNameCaseInsensitive = true };
}
