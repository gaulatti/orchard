using System.Collections;
using System.Globalization;

namespace Orchard;

// Recharts 3.8.1 computeFunnelTrapezoids and Trapezoid's settled path.
internal static class BleeckerFunnelGeometry
{
    internal sealed record Entry(int Index, double X, double Y, double Upper, double Lower, double Height, object? Value)
    {
        internal double CenterX => X + Upper / 2;
        internal double CenterY => Y + Height / 2;
        internal double LabelWidth => (Upper + Lower) / 2;
        internal string? Path
        {
            get
            {
                // Invalid SVG coordinates and zero-area source shapes paint no
                // ink. Keep their record/inspection entry without inventing a path.
                if (!new[] { X, Y, Upper, Lower, Height }.All(double.IsFinite) || Upper == 0 && Lower == 0 || Height == 0) return null;
                var gap = (Upper - Lower) / 2;
                return $"M{N(X)},{N(Y)}L{N(X + Upper)},{N(Y)}L{N(X + Upper - gap)},{N(Y + Height)}L{N(X + Upper - gap - Lower)},{N(Y + Height)}L{N(X)},{N(Y)}Z";
            }
        }
    }
    private static string N(double value) => (Math.Floor(value * 10000 + .5) / 10000).ToString("G17", CultureInfo.InvariantCulture);
    private static bool Numeric(object? value) => value is not null and not string and not bool and not IList && BleeckerChartData.Number(value) is not null;
    private static double Coerce(object? value)
    {
        if (value is null) return 0;
        if (value is bool flag) return flag ? 1 : 0;
        if (value is IList array) return Coerce(string.Join(",", array.Cast<object?>().Select(item => item is null ? "" : BleeckerChartData.Label(item))));
        if (value is string text && text.Trim().Length == 0) return 0;
        return BleeckerChartData.Number(value) ?? double.NaN;
    }
    internal static Entry[] Compute(IReadOnlyList<IReadOnlyDictionary<string, object?>> data, double width, double height)
    {
        if (data.Count == 0) return [];
        var values = data.Select(row => BleeckerChartData.TryRead(row, "value", out var value) ? value : 0d).ToArray();
        var maximum = values.Max(value => Numeric(value) ? BleeckerChartData.Number(value)!.Value : 0);
        var realWidth = width - 10; var rowHeight = (height - 10) / data.Count;
        var entries = new Entry[data.Count];
        for (var i = 0; i < data.Count; i++)
        {
            var raw = values[i]; var value = raw; object? next = double.NaN;
            if (i != data.Count - 1)
            {
                var following = values[i + 1];
                if (Numeric(following)) next = following;
                else if (following is IList range)
                {
                    if (range.Count > 0 && Numeric(range[0])) value = range[0];
                    if (range.Count > 1 && Numeric(range[1])) next = range[1];
                }
            }
            else if (raw is IList range && range.Count == 2)
            {
                if (Numeric(range[0])) value = range[0];
                if (Numeric(range[1])) next = range[1];
            }
            else next = 0d;
            var number = Coerce(value);
            entries[i] = new(i, (maximum - number) * realWidth / (2 * maximum) + 5, rowHeight * i + 5,
                number / maximum * realWidth, Coerce(next) / maximum * realWidth, rowHeight, raw);
        }
        return entries;
    }
}
