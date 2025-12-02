using Factos.RemoteTesters;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Factos.Protocols;

public class HTTPProtocolHandler : IProtocolHandler
{
    public async Task<bool> Execute(AppController controller)
    {
        // get the results
        ExecutionResponse testResults = await controller.TestExecutor.Execute();
        var serialized = JsonSerializer.Serialize(
            testResults,
            ExecutionResponseSourceGenerationContext.Default.ExecutionResponse);

        // now send them to the server at the /nodes endpoint
        var httpClient = new HttpClient();
        var content = new StringContent(serialized, System.Text.Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync(controller.Settings.HttpServerUri + "/nodes", content);
        response.EnsureSuccessStatusCode();

        return response.IsSuccessStatusCode;
    }

}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ExecutionResponse))]
internal partial class ExecutionResponseSourceGenerationContext : JsonSerializerContext
{ }
