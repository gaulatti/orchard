namespace Orchard;

// Shared modal ordering from the pinned useModalLayer contract. This model
// owns no native elements, focus, visibility or background property values.
// The native adapter must supply logical ancestry and ownership, including
// registered portaled branches, rather than infer either from portal order.
internal sealed class BleeckerModalStackModel<TLayer, TFocus> where TLayer : class where TFocus : class
{
    internal sealed class Entry(TLayer layer, TLayer[] ancestors, TFocus? previousFocus, long order)
    {
        internal TLayer Layer { get; } = layer;
        internal IReadOnlyList<TLayer> Ancestors { get; } = Array.AsReadOnly(ancestors);
        internal TFocus? PreviousFocus { get; set; } = previousFocus;
        internal long Order { get; } = order;
        internal int OverlayZIndex { get; set; }
        internal int BranchZIndex => OverlayZIndex + 1;
        internal bool Interactive { get; set; }
    }
    internal readonly record struct Removal(bool WasActive, TFocus? PreviousFocus, Entry? Active);
    private readonly List<Entry> entries = [];
    private readonly IReadOnlyList<Entry> view;
    private long sequence;
    internal BleeckerModalStackModel() => view = entries.AsReadOnly();
    internal IReadOnlyList<Entry> Entries => view;
    internal Entry? Active => entries.Count == 0 ? null : entries[^1];

    internal Entry Add(TLayer layer, IEnumerable<TLayer> ancestors, TFocus? previousFocus)
    {
        ArgumentNullException.ThrowIfNull(layer); ArgumentNullException.ThrowIfNull(ancestors);
        if (entries.Any(e => ReferenceEquals(e.Layer, layer))) throw new InvalidOperationException("The modal layer is already registered.");
        var ancestry = ancestors.ToArray();
        if (ancestry.Any(a => a is null || ReferenceEquals(a, layer)) || ancestry.Distinct(ReferenceEqualityComparer.Instance).Count() != ancestry.Length)
            throw new ArgumentException("Modal ancestry must contain distinct ancestor identities.", nameof(ancestors));
        var entry = new Entry(layer, ancestry, previousFocus, ++sequence); entries.Add(entry); Update(); return entry;
    }

    internal Removal Remove(TLayer layer, Func<TLayer, TFocus, bool> ownsTarget)
    {
        ArgumentNullException.ThrowIfNull(layer); ArgumentNullException.ThrowIfNull(ownsTarget);
        var entry = entries.SingleOrDefault(e => ReferenceEquals(e.Layer, layer)) ?? throw new InvalidOperationException("The modal layer is not registered.");
        var wasActive = ReferenceEquals(Active, entry);
        // Closing an ancestor must not leave a successor restoring focus into
        // a removed surface or one of that surface's registered popup branches.
        foreach (var successor in entries)
            if (successor.PreviousFocus is { } focus && ownsTarget(layer, focus)) successor.PreviousFocus = entry.PreviousFocus;
        entries.Remove(entry); entry.Interactive = false; Update();
        if (entries.Count == 0) sequence = 0;
        return new(wasActive, entry.PreviousFocus, Active);
    }

    private void Update()
    {
        // Snapshot roots before sorting: List.Sort mutates the list during
        // comparison, unlike the source's Array.sort collection readback.
        var roots = new Dictionary<Entry, long>();
        foreach (var entry in entries)
        {
            var root = entry.Ancestors.Select(ancestor => entries.Find(candidate => ReferenceEquals(candidate.Layer, ancestor))).FirstOrDefault(candidate => candidate is not null);
            roots.Add(entry, root?.Order ?? entry.Order);
        }
        entries.Sort((a, b) =>
        {
            var root = roots[a].CompareTo(roots[b]); if (root != 0) return root;
            var depth = a.Ancestors.Count.CompareTo(b.Ancestors.Count); return depth != 0 ? depth : a.Order.CompareTo(b.Order);
        });
        for (var i = 0; i < entries.Count; i++) { entries[i].OverlayZIndex = 50 + i; entries[i].Interactive = i == entries.Count - 1; }
    }
}
