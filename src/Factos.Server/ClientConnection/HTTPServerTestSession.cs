using Factos.Abstractions.Dto;
using Factos.Server.Settings;
using Microsoft.Testing.Platform.Extensions.TestFramework;

namespace Factos.Server.ClientConnection;

internal sealed class HTTPServerTestSession(
    DeviceWritter serviceProvider,
    FactosSettings factosSettings)
        : IServerSessionProtocol
{
    readonly FactosSettings settings = factosSettings;
    readonly DeviceWritter deviceWritter = serviceProvider;
    WebApplication? app;

    public string Id => nameof(HTTPServerTestSession);
    public event Action<List<TestNodeDto>>? NodesReceived;

    public async Task Start(CancellationToken cancellationToken)
    {
        string[] args = [];
        var endPoint = "/nodes";

        _ = Task.Run(() =>
        {
            var builder = WebApplication.CreateBuilder(args);

            // avoid noise from asp logs
            builder.Logging.ClearProviders();

            // receive from any origin
            builder.Services.AddCors(options =>
                options.AddPolicy("AllowAll",
                    policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

            app = builder.Build();

            app.UseCors("AllowAll");
            app.MapPost(endPoint, async (List<TestNodeDto> nodes) =>
            {
                await deviceWritter.Dimmed("HTTP message received", cancellationToken);
                NodesReceived?.Invoke(nodes);
                return Results.Ok();
            });

            app.Run(settings.HttpUri);
        }, cancellationToken);

        await deviceWritter.Dimmed(
            $"Http server listening on {settings.HttpUri}{endPoint}'.", cancellationToken);
    }

    public async Task Finish(CancellationToken cancellationToken)
    {
        if (app is null) return;
        await app.StopAsync(cancellationToken);

        await deviceWritter.Dimmed("HTTP server stopped", cancellationToken);
    }

    public async IAsyncEnumerable<TestNodeDto> RequestClient(string clientName, ExecuteRequestContext context)
    {
        var nodes = await NodesListener();
        
        foreach (var node in nodes)
            yield return node;
    }

    public Task CloseClient(string clientName, CancellationToken cancellationToken)
    {
        // not supported in HTTP
        return Task.CompletedTask;
    }

    private Task<List<TestNodeDto>> NodesListener()
    {
        var tcs = new TaskCompletionSource<List<TestNodeDto>>();

        void OnNodesReceived(List<TestNodeDto> nodes)
        {
            NodesReceived -= OnNodesReceived;

            tcs.SetResult(nodes);
        }

        NodesReceived += OnNodesReceived;

        return tcs.Task;
    }
}
