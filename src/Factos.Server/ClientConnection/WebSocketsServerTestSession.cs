using Factos.Abstractions.Dto;
using Factos.Server.Settings;
using Microsoft.AspNetCore.SignalR;
using System.Runtime.CompilerServices;

namespace Factos.Server.ClientConnection;

internal sealed class WebSocketsServerTestSession(
    DeviceWritter serviceProvider,
    FactosSettings factosSettings)
        : IServerSessionProtocol
{
    readonly FactosSettings settings = factosSettings;
    readonly DeviceWritter deviceWritter = serviceProvider;
    WebApplication? app;

    public string Id => nameof(WebSocketsServerTestSession);

    public async Task Start(CancellationToken cancellationToken)
    {
        string[] args = [];

        _ = Task.Run(() =>
        {
            var builder = WebApplication.CreateBuilder(args);

            // avoid noise from asp logs
            builder.Logging.ClearProviders();

            // Add SignalR services
            builder.Services.AddSignalR();

            // receive from any origin
            builder.Services.AddCors(options =>
                options.AddPolicy("AllowAll",
                    policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

            app = builder.Build();

            app.UseCors("AllowAll");
            app.MapHub<TestHub>("/test");

            app.Run(settings.WebSocketsUri);
        }, cancellationToken);

        await deviceWritter.Dimmed(
            $"WebSockets server listening on {settings.WebSocketsUri}/test'.", cancellationToken);
    }

    public async Task Finish(CancellationToken cancellationToken)
    {
        if (app is null) return;
        await app.StopAsync(cancellationToken);

        await deviceWritter.Dimmed("WebSockets server stopped", cancellationToken);
    }

    public async IAsyncEnumerable<TestNodeDto> RequestClient(
        string clientName, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // this will run as soon as the client connects
        // see TestHub.OnConnectedAsync

        while (true)
        {
            var tcs = new TaskCompletionSource<TestNodeDto>();

            void OnTestNodeGenerated(TestNodeDto nodeDto)
            {
                tcs.TrySetResult(nodeDto);
                TestHub.OnTestNodeGenerated -= OnTestNodeGenerated;
            }

            void OnAllTestsCompleted()
            {
                tcs.TrySetCanceled(cancellationToken);
                TestHub.OnAllTestsCompleted -= OnAllTestsCompleted;
            }

            await using var cancellationRegistration = cancellationToken.Register(async () =>
            {
                await deviceWritter.Red(
                    $"Cancellation requested while waiting for test node from client '{clientName}'.", CancellationToken.None);

                tcs.TrySetCanceled();
                TestHub.OnTestNodeGenerated -= OnTestNodeGenerated;
            });

            TestHub.OnTestNodeGenerated += OnTestNodeGenerated;
            TestHub.OnAllTestsCompleted += OnAllTestsCompleted;

            TestNodeDto node;

            try
            {
                node = await tcs.Task.ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                await deviceWritter.Red(
                    $"Error receiving test node from client '{clientName}': {ex.Message}", cancellationToken);

                break;
            }

            yield return node;
        }
    }

    public Task CloseClient(string clientName, CancellationToken cancellationToken)
    {
        // send a message to the client to quit
        // Clients.All.SendAsync("Quit");
        return Task.CompletedTask;
    }

    public class TestHub : Hub
    {
        public static event Action<TestNodeDto>? OnTestNodeGenerated;
        public static event Action? OnAllTestsCompleted;

        public async Task TestNodeGenerated(TestNodeDto nodeDto)
        {
            OnTestNodeGenerated?.Invoke(nodeDto);
        }

        public async Task AllTestsCompleted()
        {
            OnAllTestsCompleted?.Invoke();
        }

        public override Task OnConnectedAsync()
        {
            var connectionId = Context.ConnectionId;
            Clients.Client(connectionId).SendAsync("RunTests");
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            return base.OnDisconnectedAsync(exception);
        }
    }
}
