using Factos.Abstractions.Dto;
using Factos.RemoteTesters;

namespace Factos.Abstractions;

public static class OutputTransform
{
    public static string SummarizeResults(ExecutionResponse response)
    {
        int execured = 0, passed = 0, failed = 0, skipped = 0;
        string explanation = string.Empty;

        foreach (var result in response.Results ?? [])
        {
            execured++;

            foreach (var prop in result.Properties)
            {
                if (prop is PassedTestNodeStatePropertyDto)
                    passed++;
                else if (prop is FailedTestNodeStatePropertyDto fProp)
                {
                    failed++;
                    explanation += $"failed [{result.DisplayName}]\n{fProp.Explanation}";
                }
                else if (prop is SkippedTestNodeStatePropertyDto)
                    skipped++;
            }
        }

        return
            $"""
            Execution Summary:
                ▶️ {skipped} Skipped
                ✔️ {passed} Passed
                ❌ {failed} Failed

            {explanation}
            """;
    }
}