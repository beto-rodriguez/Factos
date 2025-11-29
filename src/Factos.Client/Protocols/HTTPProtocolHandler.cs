namespace Factos.Protocols;

public class HTTPProtocolHandler : IProtocolHandler
{
    public async Task<bool> Execute(AppController controller)
    {
        // get the results
        var testResults = await controller.TestExecutor.Run();

        // now send them to the server at the /nodes endpoint
        var httpClient = new HttpClient();
        var content = new StringContent(testResults, System.Text.Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync(controller.Settings.HttpServerUri + "/nodes", content);

        response.EnsureSuccessStatusCode();

        return response.IsSuccessStatusCode;
    }
}