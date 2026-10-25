using System.Text;
using System.Text.Json;

namespace Orchard;

// ECMAScript default casing from the pinned producer browser. This does not
// change CSS language-tailored casing used by other components.
internal static class BleeckerCommandSpotlightCasing
{
    private sealed record Data(Dictionary<int, string> Upper, Dictionary<int, string> Lower,
        (int First, int Last)[] EffectiveCased, (int First, int Last)[] Ignorable);
    private static readonly Lazy<Data> mappings = new(Read);
    private readonly record struct Scalar(int Value, int Start, int Length);
    internal static string Upper(string value) => Convert(value, false);
    internal static string Lower(string value) => Convert(value, true);
    private static string Convert(string value, bool lower)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.All(c => c <= 0x7f)) return lower ? value.ToLowerInvariant() : value.ToUpperInvariant();
        var data = mappings.Value; var scalars = new List<Scalar>();
        for (var i = 0; i < value.Length;)
        {
            var start = i; var scalar = (int)value[i++];
            if (char.IsHighSurrogate((char)scalar) && i < value.Length && char.IsLowSurrogate(value[i]))
                scalar = char.ConvertToUtf32((char)scalar, value[i++]);
            scalars.Add(new(scalar, start, i - start));
        }
        var result = new StringBuilder(value.Length); var map = lower ? data.Lower : data.Upper;
        for (var i = 0; i < scalars.Count; i++)
        {
            var scalar = scalars[i];
            if (lower && scalar.Value == 0x3a3)
            {
                var before = i - 1; while (before >= 0 && Contains(data.Ignorable, scalars[before].Value)) before--;
                var after = i + 1; while (after < scalars.Count && Contains(data.Ignorable, scalars[after].Value)) after++;
                if (before >= 0 && Contains(data.EffectiveCased, scalars[before].Value)
                    && (after == scalars.Count || !Contains(data.EffectiveCased, scalars[after].Value)))
                { result.Append('\u03c2'); continue; }
            }
            if (map.TryGetValue(scalar.Value, out var mapped)) result.Append(mapped);
            else result.Append(value.AsSpan(scalar.Start, scalar.Length)); // Preserve lone UTF-16 units.
        }
        return result.ToString();
    }
    private static bool Contains((int First, int Last)[] ranges, int scalar)
    {
        var left = 0; var right = ranges.Length - 1;
        while (left <= right)
        {
            var middle = left + (right - left) / 2; var range = ranges[middle];
            if (scalar < range.First) right = middle - 1;
            else if (scalar > range.Last) left = middle + 1;
            else return true;
        }
        return false;
    }
    private static Data Read()
    {
        using var stream = typeof(BleeckerCommandSpotlightCasing).Assembly.GetManifestResourceStream("Orchard.Text.CommandSpotlightCasing.json")
            ?? throw new InvalidOperationException("The pinned Command Spotlight case data is missing.");
        using var document = JsonDocument.Parse(stream); var root = document.RootElement;
        if (root.GetProperty("browser").GetString() != "155.0.4283.62" || root.GetProperty("scalars").GetInt32() != 1112064)
            throw new InvalidOperationException("The Command Spotlight case data does not match the pinned producer.");
        Dictionary<int, string> Map(string name) => root.GetProperty(name).EnumerateArray().ToDictionary(v => v[0].GetInt32(), v => v[1].GetString()!);
        (int First, int Last)[] Ranges(string name) => root.GetProperty(name).EnumerateArray().Select(v => (v[0].GetInt32(), v[1].GetInt32())).ToArray();
        return new(Map("upper"), Map("lower"), Ranges("effectiveCasedRanges"), Ranges("caseIgnorableRanges"));
    }
}
