using System.Windows.Input;

namespace Orchard;

/// <summary>A consumer-owned tab. Replace a collection entry to update its label or destination.</summary>
public sealed record BleeckerTabItem
{
    public BleeckerTabItem(string id, string label, string? href = null, bool external = false,
        string? panelId = null, bool disabled = false, ICommand? command = null, object? commandParameter = null)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(label);
        Id = id; Label = label; Href = href; External = external; PanelId = panelId;
        Disabled = disabled; Command = command; CommandParameter = commandParameter;
    }
    public string Id { get; }
    public string Label { get; }
    public string? Href { get; }
    public bool External { get; }
    public string? PanelId { get; }
    public bool Disabled { get; }
    public ICommand? Command { get; }
    public object? CommandParameter { get; }
}

public readonly record struct BleeckerTabRequestedEventArgs(string Id);
public readonly record struct BleeckerTabNavigationEventArgs(BleeckerTabItem Item);
public enum BleeckerTabsVariant { Underline, Segmented, Enclosed }

internal static class BleeckerTabsModel
{
    internal static void Validate(IEnumerable<BleeckerTabItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (!ids.Add(item.Id)) throw new ArgumentException("Tab identifiers must be unique.", nameof(items));
        }
    }

    // CSS flex: 1 uses equal tracks, constrained by each nowrap label's minimum.
    internal static double[] Widths(IReadOnlyList<double> minimums, double available, bool stretch)
    {
        var result = minimums.ToArray();
        if (!stretch || result.Length == 0 || available <= result.Sum()) return result;
        var remaining = Enumerable.Range(0, result.Length).ToHashSet();
        var budget = available;
        while (remaining.Count > 0)
        {
            var share = budget / remaining.Count;
            var constrained = remaining.Where(index => minimums[index] > share).ToArray();
            if (constrained.Length == 0)
            {
                // Allocate the last track first, rounding each share to the
                // browser's 1/64px layout unit and retaining the remainder.
                // Reverse distribution keeps the complete strip within its box.
                var count = remaining.Count;
                foreach (var index in remaining.OrderDescending())
                {
                    var width = count == 1 ? budget : Math.Floor(budget / count * 64 + .5) / 64;
                    result[index] = width; budget -= width; count--;
                }
                break;
            }
            foreach (var index in constrained) { budget -= minimums[index]; remaining.Remove(index); }
        }
        return result;
    }
}
