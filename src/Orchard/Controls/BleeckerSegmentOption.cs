namespace Orchard;

/// <summary>An immutable, value-keyed option. Values must keep stable equality while in use.</summary>
public sealed record BleeckerSegmentOption
{
    public BleeckerSegmentOption(object? value, string label, bool isDisabled = false)
    {
        ArgumentNullException.ThrowIfNull(label);
        Value = value; Label = label; IsDisabled = isDisabled;
    }
    public object? Value { get; }
    public string Label { get; }
    public bool IsDisabled { get; }
}

internal static class BleeckerSegmentedModel
{
    internal static void Validate(IEnumerable<BleeckerSegmentOption> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var values = new HashSet<object?>();
        foreach (var option in options)
        {
            ArgumentNullException.ThrowIfNull(option);
            if (!values.Add(option.Value)) throw new ArgumentException("Segment values must be unique.", nameof(options));
        }
    }

    // Native text keeps its natural width when possible; longer labels receive
    // the remaining proposal and wrap. Padding is never compressed or scrolled.
    internal static double[] Widths(IReadOnlyList<double> natural, double available)
    {
        if (natural.Any(value => !double.IsFinite(value) || value < 24) || double.IsNaN(available))
            throw new ArgumentException("Invalid segment measurement.");
        var widths = natural.ToArray();
        if (widths.Sum() <= available) return widths;
        var pending = Enumerable.Range(0, widths.Length).ToList();
        var remaining = Math.Max(24 * widths.Length, available);
        while (pending.Count > 0)
        {
            var share = remaining / pending.Count;
            var fixedItems = pending.Where(index => natural[index] <= share).ToArray();
            if (fixedItems.Length == 0) { foreach (var index in pending) widths[index] = share; break; }
            foreach (var index in fixedItems) { widths[index] = natural[index]; remaining -= widths[index]; pending.Remove(index); }
        }
        return widths;
    }

    internal static int Navigate(IReadOnlyList<bool> enabled, int current, int direction, bool home = false, bool end = false)
    {
        var candidates = Enumerable.Range(0, enabled.Count).Where(index => enabled[index]).ToArray();
        if (candidates.Length == 0) return -1;
        if (home) return candidates[0];
        if (end) return candidates[^1];
        var position = Array.IndexOf(candidates, current);
        return position < 0 ? candidates[0] : candidates[Math.Clamp(position + direction, 0, candidates.Length - 1)];
    }
}
