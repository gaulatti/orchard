using System.Globalization;
using System.Text.RegularExpressions;

namespace Orchard;

// Recharts 3.8.1 computePieSectors and Sector's unrounded-corner path.
internal static class BleeckerPieGeometry
{
    internal readonly record struct Sector(double Start, double End, double Percent);
    internal readonly record struct Point(double X, double Y);
    internal static double Number(object? value) => value is null or string or bool ? 0 : BleeckerChartData.Number(value) ?? 0;
    internal static Sector[] Sectors(IReadOnlyList<object?> values)
    {
        var sum = values.Sum(Number); if (!double.IsFinite(sum)) throw new ArgumentOutOfRangeException(nameof(values), "Pie total exceeds the finite numeric range.");
        if (sum <= 0) return [];
        var padding = values.Count <= 1 ? 0 : 2;
        var available = 360 - values.Count(value => !Zero(value)) * padding;
        var result = new Sector[values.Count]; var end = 0d;
        for (var i = 0; i < result.Length; i++) { var start = end + (i == 0 || Zero(values[i]) ? 0 : padding); var percent = Number(values[i]) / sum; end = start + percent * available; result[i] = new(start, end, percent); }
        return result;
    }
    private static bool Zero(object? value) => value is not null and not bool and not string && BleeckerChartData.Number(value) == 0;
    internal static Point Polar(double cx, double cy, double radius, double angle) => new(cx + radius * Math.Cos(-angle * Math.PI / 180), cy + radius * Math.Sin(-angle * Math.PI / 180));
    internal static double Radius(object value, double maximum)
    {
        var percent = value is string text && text.EndsWith('%');
        if (!percent) return BleeckerChartData.Number(value) ?? 0;
        var match = Regex.Match((string)value, @"^[\s\uFEFF]*([+-]?(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)(?:[eE][+-]?[0-9]+)?)");
        return match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number / 100 * maximum : 0;
    }
    internal static string? Path(double cx, double cy, double inner, double outer, Sector sector)
    {
        if (outer < inner || sector.Start == sector.End) return null;
        var angle = Math.Sign(sector.End - sector.Start) * Math.Min(Math.Abs(sector.End - sector.Start), 359.999); var end = sector.Start + angle;
        var a = Polar(cx, cy, outer, sector.Start); var b = Polar(cx, cy, outer, end); var large = Math.Abs(angle) > 180 ? 1 : 0; var sweep = sector.Start > end ? 1 : 0;
        var path = $"M{N(a.X)},{N(a.Y)} A{N(outer)},{N(outer)},0,{large},{sweep},{N(b.X)},{N(b.Y)}";
        if (inner > 0) { var c = Polar(cx, cy, inner, end); var d = Polar(cx, cy, inner, sector.Start); path += $" L{N(c.X)},{N(c.Y)} A{N(inner)},{N(inner)},0,{large},{1 - sweep},{N(d.X)},{N(d.Y)} Z"; }
        else path += $" L{N(cx)},{N(cy)} Z";
        return path;
    }
    private static string N(double value) => (Math.Floor(value * 10000 + .5) / 10000).ToString("G17", CultureInfo.InvariantCulture);
}
