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
        var builder = WebApplication.CreateBuilder();

        // avoid noise from asp logs
        builder.Logging.ClearProviders();

        // Add SignalR services
        builder.Services.AddSignalR();

        // receive from any origin
        builder.Services.AddCors(options =>
            options.AddPolicy("AllowAll",
                policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

        var webApp = builder.Build();

        webApp.UseCors("AllowAll");
        webApp.MapHub<TestHub>("/test");

        // Bind to all interfaces, not the literal "localhost" host portion of
        // settings.WebSocketsUri. Android emulators reach the host machine via
        // the special address 10.0.2.2 — when Kestrel listens on
        // http://localhost:7008 only it binds 127.0.0.1 / ::1 and the request
        // arriving from the emulator (with Host: 10.0.2.2:7008) cannot be
        // routed. Substituting "+" makes Kestrel listen on every interface so
        // both same-host clients and emulator/simulator clients can connect.
        webApp.Urls.Add(settings.WebSocketsUri.Replace("localhost", "+"));

        // StartAsync returns once Kestrel is listening and throws if it cannot bind
        // (e.g. the port is taken). The former fire-and-forget app.Run swallowed that
        // error, the client never connected and the run waited until it timed out.
        await webApp.StartAsync(cancellationToken);
        app = webApp;

        await deviceWritter.Dimmed(
            $"WebSockets server listening on {settings.WebSocketsUri}/test'.", cancellationToken);
    }

    public async Task Finish(CancellationToken cancellationToken)
    {
        if (app is null) return;
        await app.StopAsync(cancellationToken);
        await app.DisposeAsync();
        app = null;

        await deviceWritter.Dimmed("WebSockets server stopped", cancellationToken);
    }

    public void PrepareClient(string clientName, string[] testUids) =>
        TestHub.CurrentSession = new ClientSession(clientName, testUids);

    public void AbortClient(string clientName, string reason) =>
        TestHub.CurrentSession?.Abort(reason);

    public async IAsyncEnumerable<TestNodeDto> RequestClient(
        string clientName, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var session = TestHub.CurrentSession ?? throw new InvalidOperationException(
            $"{nameof(PrepareClient)} must be called before requesting results from '{clientName}'.");

        // the client starts sending nodes as soon as it connects, see TestHub.OnConnectedAsync
        while (true)
        {
            TestNodeDto? node = null;
            var isCancelled = false;

            try
            {
                if (!await session.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                    yield break; // the client signed off, or the session was aborted

                if (!session.Reader.TryRead(out node))
                    continue;
            }
            catch (OperationCanceledException)
            {
                isCancelled = true;
            }

            if (isCancelled)
            {
                await deviceWritter.Red(
                    $"Timed out (or cancelled) while waiting for test results from '{clientName}'.", CancellationToken.None);

                // surface it as a failed node, so the run fails instead of hanging or passing
                yield return ClientSession.CreateFailedNode(
                    ClientSession.TIMEOUT_NODE_UID,
                    "Factos session timed out",
                    $"The client app '{clientName}' did not finish the tests before the timeout or the run was cancelled.");

                yield break;
            }

            yield return node!;
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
        internal const string SESSION_KEY = "factos-session";

        // The session of the app being tested; FactosFramework prepares it before
        // the app starts so the hub can forward the per-app filter on connection.
        internal static ClientSession? CurrentSession { get; set; }

        // Each connection keeps the session that was current when it connected, so a
        // late disconnect from the previous app cannot end the next app's session.
        ClientSession? Session =>
            Context?.Items.TryGetValue(SESSION_KEY, out var session) == true
                ? session as ClientSession
                : CurrentSession;

        public Task TestNodeGenerated(TestNodeDto nodeDto)
        {
            Session?.Publish(nodeDto);
            return Task.CompletedTask;
        }

        public Task AllTestsCompleted()
        {
            Session?.CompleteGracefully();
            return Task.CompletedTask;
        }

        public override Task OnConnectedAsync()
        {
            var session = CurrentSession;
            if (session is not null)
                Context.Items[SESSION_KEY] = session;

            // Empty = run all.
            _ = Clients.Caller.SendAsync("RunTests", session?.TestUids ?? []);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            // A transport drop (client SIGSEGV, simulator killed, etc.) without the
            // AllTestsCompleted sign-off is surfaced as a failed node so MTP marks the
            // run failed (exit code != 0); after a graceful sign-off this is a no-op.
            var reason = exception?.Message ?? "the client disconnected before reporting completion";

            Session?.Abort(
                "The client app disconnected before sending AllTestsCompleted; " +
                "the remaining tests did not run. " +
                $"Reason: {reason}");

            return base.OnDisconnectedAsync(exception);
        }
    }
}
