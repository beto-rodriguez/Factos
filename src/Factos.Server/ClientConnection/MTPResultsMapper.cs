using Factos.Abstractions.Dto;
using Microsoft.Testing.Platform.Extensions.Messages;

namespace Factos.Server.ClientConnection;

internal static class MTPResultsMapper
{
    private static int skipped = 0;
    private static int passed = 0;
    private static int failed = 0;

    public static IProperty AsMTP(this PropertyDto dto) =>
        dto switch
        {
            TestMethodIdentifierPropertyDto mi => new TestMethodIdentifierProperty(
                mi.AssemblyFullName, mi.Namespace, mi.TypeName, mi.MethodName, mi.MethodArity, mi.ParameterTypeFullNames, mi.ReturnTypeFullName),
            DiscoveredTestNodeStatePropertyDto dt => new DiscoveredTestNodeStateProperty(dt.Explanation),
            SkippedTestNodeStatePropertyDto st => new SkippedTestNodeStateProperty(st.Explanation),
            PassedTestNodeStatePropertyDto pt => new PassedTestNodeStateProperty(pt.Explanation),
            FailedTestNodeStatePropertyDto ft => new FailedTestNodeStateProperty(ft.Explanation!),
            ErrorTestNodeStatePropertyDto et => new ErrorTestNodeStateProperty(et.Explanation!),
            _ => throw new NotSupportedException("Unknown type")
        };

    public static PropertyBag AsPropertyBagResult(this IEnumerable<PropertyDto> dtos)
    {
        var bag = new PropertyBag();

        foreach (var dto in dtos)
        {
            var prop = dto.AsMTP();
            bag.Add(prop);

            switch (prop)
            {
                case SkippedTestNodeStateProperty:
                    skipped++;
                    break;

                case PassedTestNodeStateProperty:
                    passed++;
                    break;

                case FailedTestNodeStateProperty:
                    failed++;
                    break;

                case ErrorTestNodeStateProperty:
                    failed++;
                    break;

                default:
                    break;
            }
        }

        return bag;
    }

    public static async Task LogCount(
        string appName, DeviceWritter deviceWritter, CancellationToken cancellationToken)
    {
        var s = skipped;
        var p = passed;
        var f = failed;

        await deviceWritter.Normal($"Tests results for {appName}", cancellationToken);
        await deviceWritter.Dimmed($"Skipped   {s}", cancellationToken);
        await deviceWritter.Green($" Passed   {p}", cancellationToken);
        await deviceWritter.Red(  $" Failed   {f}", cancellationToken);

        skipped = 0;
        passed = 0;
        failed = 0;
    }
}
