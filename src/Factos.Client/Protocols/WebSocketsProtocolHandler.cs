using Factos.Abstractions;
using Factos.Abstractions.Dto;
using Factos.RemoteTesters;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Factos.Protocols;

public class WebSocketsProtocolHandler : IProtocolHandler
{
    public async Task Execute(AppController controller)
    {
        var tcs = new TaskCompletionSource<object>();

        var uri = controller.Settings.WebSocketsServerUri;

        // Detect Android with a runtime filesystem probe instead of going
        // through controller.GetIsAndroid() / OperatingSystem.IsAndroid().
        //
        // Both indirections fail here:
        //   1) OperatingSystem.IsAndroid() and RuntimeInformation.RuntimeIdentifier
        //      are intrinsified by the JIT/AOT to a compile-time constant based on
        //      the *calling assembly's* TargetFramework. Factos.Client targets
        //      net10.0 (no -android variant), so the intrinsic folds to false even
        //      when running on a real Android device.
        //   2) The virtual override on per-platform subclasses (e.g.
        //      Factos.Uno.UnoAppController, compiled for net10.0-android with
        //      #if ANDROID return true) is devirtualized by the trimmer/JIT at this
        //      call site because `controller` is declared as the base AppController
        //      type and the trimmer treats the base implementation as the only
        //      reachable one.
        //
        // /system/bin/app_process exists on every Android device and is world-
        // readable, so File.Exists is a reliable real-runtime check that no
        // intrinsic or trimmer pass can fold away.
#if NET6_0_OR_GREATER
        var isAndroid = System.IO.File.Exists("/system/bin/app_process");
#else
        var isAndroid = false;
#endif
        if (isAndroid)
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

        connection.On<string[]>("RunTests", async testUids =>
        {
            var sh = SourceGeneratedTestExecutor.StreamHandler;

            sh.CancellationTokenSource.Token.Register(async () =>
            {
                // this is called on unhandled exceptions, useful to catch ui thread exceptions
                var test = new TestNodeDto
                {
                    Uid = sh.LastKnownTestUid,
                    DisplayName = sh.LastKnownTestDisplayName,
                    Properties = [
                        new FailedTestNodeStatePropertyDto
                        {
                            Explanation = $"Unhandled exception.\n{sh.CancelationReason}"
                        }
                    ]
                };

                controller.LogMessage($"{test.Uid} sent.");
                await connection.InvokeAsync("TestNodeGenerated", test);
            });

            await foreach (var test in controller.TestExecutor.Execute(testUids))
            {
                controller.LogMessage($"{test.Uid} sent.");
                await connection.InvokeAsync("TestNodeGenerated", test);
            }

            await connection.InvokeAsync("AllTestsCompleted");

            await controller.InvokeOnUIThread(async () =>
            {
                controller.QuitApp();
            }, null!);

            tcs.SetResult(new());
        });

        await connection.StartAsync();
        await tcs.Task;
    }
}
