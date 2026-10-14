using System.Collections;

namespace Orchard;

// Recharts 3.8.1 computeArea and d3-shape 3.2.0 stackOffsetNone.
internal static class BleeckerAreaGeometry
{
    internal readonly record struct Value(double Lower, double Upper);
    internal static Value[][] Values(IReadOnlyList<IReadOnlyDictionary<string, object?>> data, IReadOnlyList<string> keys, bool stacked, double baseline)
    {
        var result = new Value[keys.Count][];
        for (var s = 0; s < keys.Count; s++)
        {
            result[s] = new Value[data.Count];
            for (var i = 0; i < data.Count; i++)
            {
                var raw = BleeckerChartData.Read(data[i], keys[s]);
                if (stacked)
                {
                    var previous = s == 0 ? new Value(0, 0) : result[s - 1][i];
                    var lower = double.IsNaN(previous.Upper) ? previous.Lower : previous.Upper;
                    result[s][i] = new(lower, lower + Coerce(raw, true));
                }
                else result[s][i] = raw is IList range ? new(range.Count > 0 ? Coerce(range[0], false) : double.NaN, range.Count > 1 ? Coerce(range[1], false) : double.NaN) : new(baseline, Coerce(raw, false));
            }
        }
        // The producer replaces two-number ranges after the entire stack has
        // accumulated. A range therefore does not change later series' sums.
        if (stacked) for (var s = 0; s < keys.Count; s++) for (var i = 0; i < data.Count; i++)
            if (BleeckerChartData.Read(data[i], keys[s]) is IList range && range.Count == 2 && range[0] is not string && range[1] is not string && BleeckerChartData.Number(range[0]) is double lo && BleeckerChartData.Number(range[1]) is double hi) result[s][i] = new(lo, hi);
        return result;
    }
    internal static IEnumerable<double> Domain(IReadOnlyList<IReadOnlyDictionary<string, object?>> data, IReadOnlyList<string> keys, bool stacked)
    {
        if (stacked)
        {
            var values = Values(data, keys, true, 0).SelectMany(series => series).SelectMany(value => new[] { value.Lower, value.Upper }).Where(double.IsFinite).ToArray();
            return values.All(value => value == 0) ? [] : values;
        }
        return keys.SelectMany(key => data.SelectMany(row => BleeckerChartData.Read(row, key) is IList range ? range.Cast<object?>() : new[] { BleeckerChartData.Read(row, key) })).Select(BleeckerChartData.Number).OfType<double>();
    }
    private static double Coerce(object? value, bool stack)
    {
        if (value is null) return stack ? 0 : double.NaN;
        if (value is bool boolean) return boolean ? 1 : 0;
        if (value is IList range) return BleeckerChartData.Number(string.Join(",", range.Cast<object?>().Select(item => item is bool flag ? flag ? "true" : "false" : BleeckerChartData.Label(item)))) ?? double.NaN;
        return BleeckerChartData.Number(value) ?? double.NaN;
    }
    internal static IReadOnlyList<BleeckerMonotoneCurve.Command> Area(IReadOnlyList<BleeckerMonotoneCurve.Point> upper, IReadOnlyList<BleeckerMonotoneCurve.Point> lower)
    {
        var commands = new List<BleeckerMonotoneCurve.Command>(); var start = 0;
        for (var i = 0; i <= upper.Count; i++)
        {
            if (i < upper.Count && double.IsFinite(upper[i].Y) && double.IsFinite(lower[i].Y)) continue;
            if (i > start)
            {
                commands.AddRange(BleeckerMonotoneCurve.Create(upper.Skip(start).Take(i - start)).Where(command => command.Kind != "Z"));
                var reverse = BleeckerMonotoneCurve.Create(lower.Skip(start).Take(i - start).Reverse());
                foreach (var command in reverse.Where(command => command.Kind != "Z")) commands.Add(command.Kind == "M" ? new("L", command.Values) : command);
                commands.Add(new("Z", []));
            }
            start = i + 1;
        }
        return commands;
    }
}
