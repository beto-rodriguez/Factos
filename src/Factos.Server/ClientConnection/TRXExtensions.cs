using Factos.Abstractions.Dto;
using Microsoft.Testing.Extensions.TrxReport.Abstractions;
using Microsoft.Testing.Platform.Extensions.Messages;

namespace Factos.Server.ClientConnection;

public static class TRXExtensions
{
    public static void FillTrxProperties(this TestNode testNode, TestNodeDto dto)
    {
        testNode.Properties.Add(new TrxFullyQualifiedTypeNameProperty(dto.Uid));

        var errors = dto.Properties
            .Where(x => x is ErrorTestNodeStatePropertyDto or FailedTestNodeStatePropertyDto)
            .Aggregate(string.Empty, (a, b) => a + ((TestNodePropertyDto)b).Explanation);

        if (!string.IsNullOrEmpty(errors))
            testNode.Properties.Add(new TrxExceptionProperty("Exception", errors));
    }
}