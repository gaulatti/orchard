namespace Orchard;

// Recharts 3.8.1's default adaptive five-tick numeric axis. Empty data has no
// ticks; the source's [0,auto] domain expands to include negative data.
internal static class BleeckerChartScale
{
    internal static double[] Ticks(IEnumerable<double> values)
    {
        var finite = values.Where(double.IsFinite).ToArray();
        if (finite.Length == 0) return [];
        var min = Math.Min(0, finite.Min()); var max = Math.Max(0, finite.Max());
        if (min == max) return [0, 1, 2, 3, 4];
        var rough = (max - min) / 4;
        if (!double.IsFinite(rough)) throw new ArgumentOutOfRangeException(nameof(values), "Chart domain exceeds the finite numeric range.");
        var digits = Math.Floor(Math.Log10(Math.Abs(rough))) + 1; var magnitude = Math.Pow(10, digits);
        var ratioScale = digits == 1 ? .1 : .05; var initial = Math.Ceiling(rough / magnitude / ratioScale);
        for (var correction = 0; ; correction++)
        {
            var step = (initial + correction) * ratioScale * magnitude;
            var below = (int)Math.Ceiling(-min / step); var above = (int)Math.Ceiling(max / step);
            var count = below + above + 1;
            if (count > 5) continue;
            if (count < 5) { if (max > 0) above += 5 - count; else below += 5 - count; }
            return Enumerable.Range(-below, below + above + 1).Select(index => index * step).ToArray();
        }
    }
    internal static double Position(double value, double min, double max, double start, double end) => start + (value - min) / (max - min) * (end - start);
}
