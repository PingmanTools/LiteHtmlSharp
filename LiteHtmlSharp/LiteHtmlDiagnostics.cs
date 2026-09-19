namespace LiteHtmlSharp;

/// <summary>Immutable wrapper lifecycle counters. Global snapshots sum currently live containers;
/// document snapshots describe one container, including its successful loads since creation.</summary>
public readonly record struct LiteHtmlDiagnostics(
    int TotalContainerCount, int NativeDocumentCount, int CustomElementCount,
    long ParseCount, int HasDocument);

/// <summary>Process-wide diagnostics for this loaded LiteHtmlSharp assembly.</summary>
public static class LibInterop
{
    /// <summary>Counts live wrapper resources, not total native allocations or proof of leak freedom.
    /// ParseCount is the sum of successful loads for currently live containers.</summary>
    public static LiteHtmlDiagnostics GetDiagnostics() => DiagnosticRegistry.Snapshot();
}

// Stores only values: diagnostics must never keep a container or document alive.
internal static class DiagnosticRegistry
{
    private static readonly object gate = new();
    private static readonly Dictionary<long, LiteHtmlDiagnostics> entries = new();
    private static long nextId;
    internal static long Register()
    {
        lock (gate)
        {
            var id = ++nextId;
            entries.Add(id, new(1, 1, 0, 0, 0));
            return id;
        }
    }
    internal static void Loaded(long id, int customElements)
    {
        lock (gate)
        {
            var previous = entries[id];
            entries[id] = previous with { HasDocument = 1, CustomElementCount = customElements,
                ParseCount = previous.ParseCount + 1 };
        }
    }
    internal static void Remove(long id) { lock (gate) entries.Remove(id); }
    internal static LiteHtmlDiagnostics Snapshot(long id)
    {
        lock (gate) return entries.GetValueOrDefault(id);
    }
    internal static LiteHtmlDiagnostics Snapshot()
    {
        lock (gate)
        {
            int containers = 0, handles = 0, custom = 0, loaded = 0;
            long parses = 0;
            foreach (var entry in entries.Values)
            {
                containers += entry.TotalContainerCount; handles += entry.NativeDocumentCount;
                custom += entry.CustomElementCount; loaded += entry.HasDocument; parses += entry.ParseCount;
            }
            return new(containers, handles, custom, parses, loaded);
        }
    }
}
