using System.Globalization;

namespace Orchard;

internal static class BleeckerAppTabModel
{
    internal static string BadgeText(long? count) => count is > 0 ? count > 99 ? "99+" : count.Value.ToString(CultureInfo.InvariantCulture) : "";
    internal static string BadgeStatus(long? count) => count.HasValue ? count.Value.ToString(CultureInfo.InvariantCulture) + " notifications" : "";
    internal static void ValidateValues(IEnumerable<object?> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var seen = new HashSet<object?>();
        foreach (var value in values) if (!seen.Add(value)) throw new ArgumentException("App tab values must be unique.", nameof(values));
    }
    internal static double TrackWidth(int count, double available, double natural)
    {
        if (count < 0 || double.IsNaN(available) || !double.IsFinite(natural) || natural < 0) throw new ArgumentException("Invalid app tab measurement.");
        return count == 0 ? 0 : double.IsPositiveInfinity(available) ? natural : Math.Max(0, available) / count;
    }
    internal static int Navigate(IReadOnlyList<bool> enabled, int current, int direction, bool home = false, bool end = false)
    {
        var candidates = Enumerable.Range(0, enabled.Count).Where(index => enabled[index]).ToArray();
        if (candidates.Length == 0) return -1;
        if (home) return candidates[0]; if (end) return candidates[^1];
        var position = Array.IndexOf(candidates, current);
        return position < 0 ? candidates[0] : candidates[Math.Clamp(position + direction, 0, candidates.Length - 1)];
    }
}
