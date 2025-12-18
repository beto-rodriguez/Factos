using Factos.Abstractions;
using Factos.Abstractions.Dto;
using System.Text.Json;

namespace Factos.Protocols;

[Obsolete]
public class HTTPProtocolHandler : IProtocolHandler
{
    static string? cachedResults;

    public async Task Execute(AppController controller)
    {
        var received = false;

        while (!received)
        {
            string? serialized;

            if (cachedResults is not null)
            {
                serialized = cachedResults;
            }
            else
            {
                var nodes = new List<TestNodeDto>();

                await foreach (var node in controller.TestExecutor.Execute())
                    nodes.Add(node);

                serialized = JsonSerializer.Serialize(
                    nodes,
                    JsonGenerationContext.Default.ListTestNodeDto);

                cachedResults = serialized;
            }

            // now send them to the server at the /nodes endpoint
            var httpClient = new HttpClient();
            var content = new StringContent(serialized, System.Text.Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(controller.Settings.HttpServerUri + "/nodes", content);
            //response.EnsureSuccessStatusCode();

            received = response.IsSuccessStatusCode;
            if (!received)
                await Task.Delay(2000);
        }
    }
}
