using System.Text;
using System.Text.Json;

namespace Factos.Abstractions;

public static class OutputTransform
{
    public static string SummarizeResults(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Array)
            return json;

        int total = 0, failed = 0, succeeded = 0, skipped = 0;
        var failedDetails = new List<(string Name, string? Reason)>();

        foreach (var item in root.EnumerateArray())
        {
            total++;
            var displayName = item.TryGetProperty("DisplayName", out var dn) ? dn.GetString() ?? string.Empty : string.Empty;

            string? stateType = null;
            string? explanation = null;
            if (item.TryGetProperty("Properties", out var props) && props.ValueKind == JsonValueKind.Array)
            {
                foreach (var p in props.EnumerateArray())
                {
                    if (p.TryGetProperty("$type", out var t)) stateType = t.GetString();
                    if (p.TryGetProperty("Explanation", out var e) && e.ValueKind != JsonValueKind.Null) explanation = e.GetString();
                    break; // first state entry is enough
                }
            }

            var type = (stateType ?? string.Empty).ToLowerInvariant();
            if (type.Contains("pass"))
            {
                succeeded++;
            }
            else if (type.Contains("error") || type.Contains("fail"))
            {
                failed++;
                failedDetails.Add((displayName, explanation));
            }
            else if (type.Contains("skip") || type.Contains("ignore"))
            {
                skipped++;
            }
            else
            {
                // Unknown -> count as skipped to keep totals consistent
                skipped++;
            }
        }

        var status = failed > 0 ? "Failed!" : $"Success!";

        var sb = new StringBuilder();
        sb.AppendLine($"Test run summary: {status}");
        sb.AppendLine($"  total: {total}");
        sb.AppendLine($"  failed: {failed}");
        sb.AppendLine($"  succeeded: {succeeded}");
        sb.AppendLine($"  skipped: {skipped}");

        if (failedDetails.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Failed tests:");
            foreach (var (name, reason) in failedDetails)
            {
                sb.AppendLine($"  - {name}");
                if (!string.IsNullOrWhiteSpace(reason))
                {
                    sb.AppendLine($"    {reason}");
                }
            }
        }

        return sb.ToString().Trim();
    }
}