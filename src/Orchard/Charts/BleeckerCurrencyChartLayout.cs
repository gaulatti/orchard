using System.Collections;

namespace Orchard;

// The fixed Chart export's Recharts 3.8.1 category and numeric selectors.
// Payload lookup uses the original record, whereas line points use tick indexes.
internal sealed class BleeckerCurrencyChartLayout
{
    private static readonly object Missing = new();
    internal required IReadOnlyList<object?> Records { get; init; }
    internal required object?[] Categories { get; init; }
    internal required double[] Ticks { get; init; }
    internal required BleeckerMonotoneCurve.Point[] Points { get; init; }
    internal double Left => 60;
    internal double Top => 5;
    internal double Right { get; init; }
    internal double Bottom { get; init; }
    internal bool HasCurve => Records.Count >= 2 && Categories.Length > 0 && Ticks.Length > 0 && Right > Left && Bottom > Top;
    internal int InspectionCount => Records.Count == 0 ? 0 : Math.Max(1, Categories.Length);
    internal double X(int index) => index >= Categories.Length ? double.NaN : Categories.Length == 1 ? (Left + Right) / 2 : Left + index * (Right - Left) / (Categories.Length - 1);
    internal double Y(double value) => Ticks.Length == 0 ? double.NaN : BleeckerChartScale.Position(value, Ticks[0], Ticks[^1], Bottom, Top);
    internal object? Category(int index) => index < Categories.Length ? Categories[index] : Missing;
    internal string CategoryText(int index) => StringValue(Category(index));
    internal string AxisText(int index) => ReferenceEquals(Category(index), Missing) ? "undefined" : Category(index) is null ? "null" : StringValue(Category(index));
    internal int PayloadIndex(int index)
    {
        var category = Category(index);
        for (var i = 0; i < Records.Count; i++) if (Same(Read(Records[i], "name", Missing), category)) return i;
        return -1;
    }
    internal BleeckerMonotoneCurve.Point? ActivePoint(int index)
    {
        var payload = PayloadIndex(index);
        if (payload < 0 || Ticks.Length == 0 || Right <= Left || Bottom <= Top) return null;
        // ActivePoints finds the first point whose original payload is selected.
        var record = Records[payload];
        for (var i = 0; i < Records.Count; i++) if (ReferenceEquals(Records[i], record))
            return double.IsFinite(Points[i].X) && double.IsFinite(Points[i].Y) ? Points[i] : null;
        return null;
    }
    internal static object? Read(object? record, string key, object? missing = null)
        => record is IReadOnlyDictionary<string, object?> dictionary && dictionary.TryGetValue(key, out var value) ? value : missing;
    private static bool CategoryValue(object? value) => value is string || value is not bool && BleeckerChartData.Number(value) is not null;
    private static bool Same(object? a, object? b) => a is not string and not bool && b is not string and not bool && BleeckerChartData.Number(a) is double x && BleeckerChartData.Number(b) is double y ? x == y : Equals(a, b);
    internal static string StringValue(object? value)
    {
        if (value is null || ReferenceEquals(value, Missing)) return "";
        if (value is string text) return text;
        if (value is bool boolean) return boolean ? "true" : "false";
        if (value is IList list) return string.Join(",", list.Cast<object?>().Select(StringValue));
        if (BleeckerChartData.Number(value) is double number) return BleeckerChartScale.TickLabel(number);
        return "[object Object]";
    }
    private static double? PointValue(object? value) => value is null ? null : value is bool boolean ? boolean ? 1 : 0 : value is IList ? BleeckerChartData.Number(StringValue(value)) : BleeckerChartData.Number(value);
    internal static BleeckerCurrencyChartLayout Compute(IReadOnlyList<object?> records, double width, double height)
    {
        ArgumentNullException.ThrowIfNull(records);
        if (!double.IsFinite(width) || !double.IsFinite(height) || width < 0 || height < 0) throw new ArgumentOutOfRangeException(nameof(width), "Chart dimensions must be finite and nonnegative.");
        width = Math.Round(width, MidpointRounding.AwayFromZero); height = Math.Round(height, MidpointRounding.AwayFromZero);
        var names = records.Select(record => Read(record, "name", Missing)).ToArray();
        var filtered = names.Where(CategoryValue).ToArray();
        var seen = new HashSet<object?>(); var duplicate = filtered.Any(name => !seen.Add(name is not string && BleeckerChartData.Number(name) is double number ? number : name));
        var categories = duplicate ? names : filtered;
        var ticks = BleeckerChartScale.Ticks(records.SelectMany(record => BleeckerChartData.NumericDomain(Read(record, "total"))));
        var layout = new BleeckerCurrencyChartLayout { Records = records, Categories = categories, Ticks = ticks, Points = new BleeckerMonotoneCurve.Point[records.Count], Right = 60 + Math.Max(0, width - 80), Bottom = 5 + Math.Max(0, height - 40) };
        for (var i = 0; i < records.Count; i++) layout.Points[i] = new(layout.X(i), PointValue(Read(records[i], "total")) is double value ? layout.Y(value) : double.NaN);
        return layout;
    }
}
