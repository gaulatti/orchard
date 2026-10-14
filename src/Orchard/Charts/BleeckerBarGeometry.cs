using System.Collections;

namespace Orchard;

// Recharts 3.8.1's default grouped allocation and [4,4,0,0] rectangle path.
// The category margin is 10%; inter-series spacing is 4px. Large bar widths
// truncate to integer pixels before series offsets are placed.
internal static class BleeckerBarGeometry
{
    internal static (double Offset, double Size) Slot(double band, int count, int index)
    {
        if (count <= 0) return (0, 0);
        var margin = band * .1;
        var gap = band - 2 * margin - (count - 1) * 4 <= 0 ? 0 : 4;
        var size = (band - 2 * margin - (count - 1) * gap) / count;
        if (size > 1) size = Math.Truncate(size);
        return (margin + (size + gap) * index, size);
    }

    internal static IEnumerable<double> DomainValues(object? value)
    {
        if (value is IList list)
        {
            foreach (var child in list) if (BleeckerChartData.Number(child) is double number) yield return number;
        }
        else if (BleeckerChartData.Number(value) is double number) yield return number;
    }

    internal static (double Start, double End)? Values(object? value, double baseline)
    {
        if (value is IList list)
        {
            if (list.Count < 2 || Point(list[0]) is not double start || Point(list[1]) is not double end) return null;
            return (start, end);
        }
        return Point(value) is double number ? (baseline, number) : null;
    }
    private static double? Point(object? value) => value is bool flag ? flag ? 1 : 0 : BleeckerChartData.Number(value);

    internal static string? Rectangle(double x, double y, double width, double height)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y) || !double.IsFinite(width) || !double.IsFinite(height) || width == 0 || height == 0) return null;
        var radius = Math.Min(4, Math.Min(Math.Abs(Round(width)), Math.Abs(Round(height))) / 2);
        var xs = width >= 0 ? 1 : -1; var ys = height >= 0 ? 1 : -1; var sweep = xs == ys ? 1 : 0;
        if (radius <= 0) return $"M{N(x)},{N(y)}h{N(width)}v{N(height)}h{N(-width)}Z";
        return $"M{N(x)},{N(y + ys * radius)}A{N(radius)},{N(radius)},0,0,{sweep},{N(x + xs * radius)},{N(y)}" +
            $"L{N(x + width - xs * radius)},{N(y)}A{N(radius)},{N(radius)},0,0,{sweep},{N(x + width)},{N(y + ys * radius)}" +
            $"L{N(x + width)},{N(y + height)}L{N(x)},{N(y + height)}Z";
    }
    private static double Round(double value) => Math.Floor(value * 10000 + .5) / 10000;
    private static string N(double value) => BleeckerChartScale.TickLabel(Round(value));
}
