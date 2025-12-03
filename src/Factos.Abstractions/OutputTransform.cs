using Factos.Abstractions.Dto;
using Factos.RemoteTesters;

namespace Factos.Abstractions;

public static class OutputTransform
{
    public static string SummarizeResults(ExecutionResponse response)
    {
        int execured = 0, passed = 0, failed = 0, skipped = 0;

        foreach (var result in response.Results ?? [])
        {
            execured++;

            foreach (var prop in result.Properties)
            {
                if (prop is PassedTestNodeStatePropertyDto)
                    passed++;
                else if (prop is FailedTestNodeStatePropertyDto)
                    failed++;
                else if (prop is SkippedTestNodeStatePropertyDto)
                    skipped++;
            }
        }

        return
            $"""
            Execution Summary:
                ✔️ {passed} Passed
                ❌ {failed} Failed
                ▶️ {skipped} Skipped
            """;
    }
}