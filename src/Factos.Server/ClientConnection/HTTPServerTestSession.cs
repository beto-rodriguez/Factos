using Factos.Abstractions.Dto;
using Factos.Server.Settings;
using Microsoft.Testing.Platform.Extensions.Messages;
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
    public event Action<TestNodeDto[]>? NodesReceived;

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
            app.MapPost(endPoint, (TestNodeDto[] nodes) =>
            {
                Console.WriteLine("Signal received from WASM!");
                NodesReceived?.Invoke(nodes);
                return Results.Ok();
            });

            app.Run(settings.HttpUri);
        }, cancellationToken);

        await deviceWritter.Dimmed(
            $"Http session started. Listening for requests on {settings.HttpUri}{endPoint}'.",
            cancellationToken);
    }

    public async Task Finish(CancellationToken cancellationToken)
    {
        if (app is null) return;
        await app.StopAsync(cancellationToken);

        await deviceWritter.Dimmed("HTTP server stopped", cancellationToken);
    }

    public async Task<NodesResponse> RequestClient(string clientName, ExecuteRequestContext context)
    {
        var nodes = await NodesListener();
        var results = new List<TestNode>();

        foreach (var nodeDto in nodes)
        {
            var testNode = new TestNode
            {
                DisplayName = nodeDto.DisplayName,
                Uid = nodeDto.Uid,
                Properties = nodeDto.Properties.AsPropertyBagResult()
            };

            testNode.FillTrxProperties(nodeDto);
            results.Add(testNode);
        }

        return new() { Nodes = results, Sender = this };
    }

    public Task CloseClient(string clientName, CancellationToken cancellationToken)
    {
        // not supported in HTTP
        return Task.CompletedTask;
    }

    private Task<TestNodeDto[]> NodesListener()
    {
        var tcs = new TaskCompletionSource<TestNodeDto[]>();

        void OnNodesReceived(TestNodeDto[] nodes)
        {
            NodesReceived -= OnNodesReceived;

            tcs.SetResult(nodes);
        }

        NodesReceived += OnNodesReceived;

        return tcs.Task;
    }
}
