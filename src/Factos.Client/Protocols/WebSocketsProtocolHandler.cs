using Factos.Abstractions;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Xml.Linq;

namespace Factos.Protocols;

public class WebSocketsProtocolHandler : IProtocolHandler
{
    private bool _isInitialized = false;

    public async Task Execute(AppController controller)
    {
        if (_isInitialized) return; // true;
        _isInitialized = true;

        var tcs = new TaskCompletionSource();

        var uri = controller.Settings.WebSocketsServerUri;

        if (controller.GetIsAndroid())
            uri = uri.Replace("localhost", "10.0.2.2");

        var connection = new HubConnectionBuilder()
            .WithUrl($"{uri}/test")
            .AddJsonProtocol(o =>
            {
                o.PayloadSerializerOptions = new JsonSerializerOptions
                {
                    TypeInfoResolver = JsonGenerationContext.Default
                };
            })
            .Build();

        connection.On("RunTests", async () =>
        {
            await foreach (var test in controller.TestExecutor.Execute())
            {
                controller.LogMessage($"{test.Uid} sent.");

                try
                {
                    await connection.InvokeAsync("TestNodeGenerated", test);
                }
                catch (Exception ex)
                {
                    var a = ex;
                }
            }

            await connection.InvokeAsync("AllTestsCompleted");

            await controller.InvokeOnUIThread(async () =>
            {
                controller.QuitApp();
            });
            
            tcs.SetResult();
        });

        await connection.StartAsync();
        _isInitialized = true;

        await tcs.Task;

        //return true;
    }
}
