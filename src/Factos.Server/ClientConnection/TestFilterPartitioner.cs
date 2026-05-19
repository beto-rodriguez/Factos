namespace Factos.Server.ClientConnection;

internal static class TestFilterPartitioner
{
    // MTPResultsMapper.ReadNode prepends `[{appName}]` to every test UID before
    // publishing to MTP. When MTP later returns a TestNodeUidListFilter (e.g. the
    // user picked specific tests in VS), the UIDs still carry that prefix, so we
    // have to slice them back per app before forwarding to the client.
    public static (bool Skip, string[] AppUids) PartitionForApp(string[]? selectedUids, string appName)
    {
        if (selectedUids is null)
            return (false, []);

        var prefix = $"[{appName}]";
        var appUids = selectedUids
            .Where(u => u.StartsWith(prefix, StringComparison.Ordinal))
            .Select(u => u.Substring(prefix.Length))
            .ToArray();

        return appUids.Length == 0 ? (true, []) : (false, appUids);
    }
}
