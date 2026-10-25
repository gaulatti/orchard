namespace Orchard;

// The original objects stay authoritative. Only filtering, group membership
// and selectable IDs are memoized, at the producer's array/query boundary.
internal sealed class BleeckerCommandSpotlightState<T>(Func<T, BleeckerCommandSpotlightData> describe) where T : class
{
    private IReadOnlyList<T> actions = [];
    private string[] selectable = [];
    private bool internalOpen;
    internal string Query { get; private set; } = "";
    internal string? SelectedId { get; private set; }
    internal IReadOnlyList<T> Filtered { get; private set; } = [];
    internal IReadOnlyList<(string Name, IReadOnlyList<T> Actions)> Groups { get; private set; } = [];
    internal bool Effective(bool? controlled) => controlled ?? internalOpen;
    internal void Request(bool next, bool? controlled, Action<bool> notify)
    {
        if (controlled is null) internalOpen = next;
        notify(next);
        if (!next) SetQuery("");
        Reconcile(Effective(controlled));
    }
    internal void SetActions(IReadOnlyList<T> value, bool open)
    {
        ArgumentNullException.ThrowIfNull(value);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var action in value)
        {
            ArgumentNullException.ThrowIfNull(action);
            var data = describe(action);
            if (data.Id is null || data.Title is null || data.Keywords.Any(k => k is null)) throw new ArgumentException("Spotlight actions require non-null IDs, titles and keyword entries.");
            if (!ids.Add(data.Id)) throw new ArgumentException("Spotlight action IDs must be unique.");
        }
        actions = value; Recompute(); Reconcile(open);
    }
    internal void SetQuery(string value)
    {
        ArgumentNullException.ThrowIfNull(value); if (value == Query) return;
        Query = value; Recompute(); Reconcile(true);
    }
    internal void Reconcile(bool open)
    {
        if (!open) return;
        if (!string.IsNullOrEmpty(SelectedId) && selectable.Contains(SelectedId, StringComparer.Ordinal)) return;
        SelectedId = selectable.FirstOrDefault();
    }
    internal void Move(int direction)
    {
        if (direction is not (-1 or 1)) throw new ArgumentOutOfRangeException(nameof(direction));
        if (selectable.Length == 0) { SelectedId = null; return; }
        var index = string.IsNullOrEmpty(SelectedId) ? -1 : Array.IndexOf(selectable, SelectedId);
        SelectedId = selectable[index < 0 ? 0 : (index + direction + selectable.Length) % selectable.Length];
    }
    internal void Hover(T action)
    {
        if (Filtered.Any(a => ReferenceEquals(a, action)) && !describe(action).Disabled) SelectedId = describe(action).Id;
    }
    internal T? EnterTarget => string.IsNullOrEmpty(SelectedId) ? null : Filtered.FirstOrDefault(a => describe(a).Id == SelectedId);
    private void Recompute()
    {
        var query = Normalize(Query);
        Filtered = query.Length == 0 ? actions : actions.Where(a => Normalize(Haystack(describe(a))).Contains(query, StringComparison.Ordinal)).ToArray();
        selectable = Filtered.Where(a => !describe(a).Disabled).Select(a => describe(a).Id).ToArray();
        var groups = new List<(string Name, List<T> Actions)>();
        var groupIndices = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var action in Filtered)
        {
            var name = Trim(describe(action).Group ?? ""); if (name.Length == 0) name = "Actions";
            if (!groupIndices.TryGetValue(name, out var index))
            { groupIndices.Add(name, groups.Count); groups.Add((name, [action])); }
            else groups[index].Actions.Add(action);
        }
        Groups = groups.Select(g => (g.Name, (IReadOnlyList<T>)g.Actions)).ToArray();
    }
    private static string Haystack(BleeckerCommandSpotlightData a) => string.Join(" ", new[] { a.Title, a.Description ?? "", a.Group ?? "" }.Concat(a.Keywords).Append(a.Href ?? ""));
    private static bool White(char c) => c is '\u0009' or '\u000A' or '\u000B' or '\u000C' or '\u000D' or '\u0020' or '\u00A0' or '\u1680' or '\u2028' or '\u2029' or '\u202F' or '\u205F' or '\u3000' or '\uFEFF' || c is >= '\u2000' and <= '\u200A';
    private static string Trim(string s) { var start = 0; var end = s.Length; while (start < end && White(s[start])) start++; while (end > start && White(s[end - 1])) end--; return s[start..end]; }
    internal static string Normalize(string value) => Lower(Trim(value));
    internal static string Lower(string value) => BleeckerCommandSpotlightCasing.Lower(value);
}

internal sealed record BleeckerCommandSpotlightData(string Id, string Title, string? Description, string? Group, IReadOnlyList<string> Keywords, string? Href, bool Disabled);

internal static class BleeckerCommandSpotlightInvocation
{
    internal static void Run<T>(T original, Action<T>? outer, Func<Func<Task?>?> selectOwn, Func<Action?> selectNavigation, Action close, Action<Exception> failed)
    {
        outer?.Invoke(original); // The producer deliberately leaves this outside its own-action catch.
        var own = selectOwn();
        if (own is not null)
        {
            try { if (own() is { } task) _ = Observe(task, failed); }
            catch (Exception error) { failed(error); }
        }
        else selectNavigation()?.Invoke();
        close();
    }
    private static async Task Observe(Task task, Action<Exception> failed)
    {
        try { await task; } catch (Exception error) { failed(error); }
    }
}
