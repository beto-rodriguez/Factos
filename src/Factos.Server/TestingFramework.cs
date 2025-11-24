using Factos.Server.ClientConnection;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.OutputDevice;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.OutputDevice;

namespace Factos.Server;

internal sealed class TestingFramework 
    : BaseExtension, ITestFramework, IDataProducer, IOutputDeviceDataProducer
{
    readonly DeviceWritter deviceWritter;

    public TestingFramework(IOutputDevice outputDevice)
    {
        deviceWritter = new(this, outputDevice);
    }

    protected override string Id =>
        "FactosTestingFramework";

    Type[] IDataProducer.DataTypesProduced => 
        [typeof(TestNodeUpdateMessage)];

    async Task<CreateTestSessionResult> ITestFramework.CreateTestSessionAsync(CreateTestSessionContext context) =>
        new() { IsSuccess = true };

    async Task ITestFramework.ExecuteRequestAsync(ExecuteRequestContext context)
    {
        try
        {
            var testNodeStream = 
                TcpServerTestSession.Current.RequestTcpClientExecution(context);

            await foreach (var node in testNodeStream)
                await context.MessageBus.PublishAsync(
                    this,
                    new TestNodeUpdateMessage(context.Request.Session.SessionUid, node));

        }
        finally
        {
            context.Complete();
        }
    }

    async Task<CloseTestSessionResult> ITestFramework.CloseTestSessionAsync(CloseTestSessionContext context) =>
        new() { IsSuccess = true };

    Task<bool> Microsoft.Testing.Platform.Extensions.IExtension.IsEnabledAsync() =>
        Task.FromResult(true);
}
