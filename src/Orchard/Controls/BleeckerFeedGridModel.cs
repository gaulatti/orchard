namespace Orchard;

internal static class BleeckerFeedGridModel
{
    internal static double[] Tracks(double width, double minimum, double gap)
    {
        if (!double.IsFinite(width) || width < 0 || !double.IsFinite(minimum) || minimum < 0 || !double.IsFinite(gap) || gap < 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Feed grid sizes must be finite and nonnegative.");
        var count = Math.Max(1, (int)Math.Floor((width + gap) / (Math.Max(1, Math.Min(width, minimum)) + gap)));
        var fraction = (float)(Math.Max(0, width - gap * (count - 1)) / count);
        return Enumerable.Range(0, count).Select(index => Math.Floor((double)fraction * (index + 1) * 64) / 64 - Math.Floor((double)fraction * index * 64) / 64).ToArray();
    }
}
