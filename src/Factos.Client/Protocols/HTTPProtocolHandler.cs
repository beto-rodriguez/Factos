using Factos.Abstractions;
using System.Text.Json;

namespace Factos.Protocols;

public class HTTPProtocolHandler : IProtocolHandler
{
    static string? cachedResults;

    public async Task<bool> Execute(AppController controller)
    {
        string? serialized;

        if (cachedResults is not null)
        {
            serialized = cachedResults;
        }
        else
        {
            var testResults = await controller.TestExecutor.Execute();

            serialized = JsonSerializer.Serialize(
                testResults,
                JsonGenerationContext.Default.ExecutionResponse);

            cachedResults = serialized;
        }

        // now send them to the server at the /nodes endpoint
        var httpClient = new HttpClient();
        var content = new StringContent(serialized, System.Text.Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync(controller.Settings.HttpServerUri + "/nodes", content);
        response.EnsureSuccessStatusCode();

        return response.IsSuccessStatusCode;
    }
}
