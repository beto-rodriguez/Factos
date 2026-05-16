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

            // Bind to all interfaces, not the literal "localhost" host portion of
            // settings.WebSocketsUri. Android emulators reach the host machine via
            // the special address 10.0.2.2 — when Kestrel listens on
            // http://localhost:7008 only it binds 127.0.0.1 / ::1 and the request
            // arriving from the emulator (with Host: 10.0.2.2:7008) cannot be
            // routed. Substituting "+" makes Kestrel listen on every interface so
            // both same-host clients and emulator/simulator clients can connect.
            app.Run(settings.WebSocketsUri.Replace("localhost", "+"));
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
        string clientName, string[] testUids, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // The hub reads this on OnConnectedAsync to forward the per-app filter
        // to the client app over the SignalR RunTests invocation.
        TestHub.PendingTestUids = testUids;

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

        // Set by RequestClient before the per-app launch so OnConnectedAsync
        // can forward the filter on the RunTests invocation. Empty = run all.
        public static string[] PendingTestUids { get; set; } = [];

        // Tracks whether the client signed off via the AllTestsCompleted RPC.
        // Without this, a SignalR transport drop (client SIGSEGV, simulator
        // killed, etc.) used to fire OnAllTestsCompleted just like a graceful
        // exit, so partial result sets were reported as passing runs.
        private static bool _completedGracefully;

        public async Task TestNodeGenerated(TestNodeDto nodeDto)
        {
            OnTestNodeGenerated?.Invoke(nodeDto);
        }

        public async Task AllTestsCompleted()
        {
            _completedGracefully = true;
            OnAllTestsCompleted?.Invoke();
        }

        public override Task OnConnectedAsync()
        {
            _completedGracefully = false;
            var connectionId = Context.ConnectionId;
            Clients.Client(connectionId).SendAsync("RunTests", PendingTestUids);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            if (!_completedGracefully)
            {
                // Surface the abort as a failed test node so MTP marks the run
                // failed (exit code != 0). FactosFramework breaks the result
                // stream on the first failed/error node, so emitting one is
                // enough to terminate the iterator.
                var reason = exception?.Message ?? "the client disconnected before reporting completion";
                OnTestNodeGenerated?.Invoke(new TestNodeDto
                {
                    Uid = "factos-session-aborted",
                    DisplayName = "Factos session aborted",
                    Properties =
                    [
                        new FailedTestNodeStatePropertyDto
                        {
                            Explanation =
                                "The client app disconnected before sending AllTestsCompleted; " +
                                "the remaining tests did not run. " +
                                $"Reason: {reason}"
                        }
                    ]
                });
            }

            OnAllTestsCompleted?.Invoke();
            return base.OnDisconnectedAsync(exception);
        }
    }
}
