using System.Collections;
using System.Globalization;
using System.Text;
using System.Numerics;
using System.Text.RegularExpressions;

namespace Orchard;

internal static class BleeckerChartData
{
    // The frozen renderer's es-toolkit get first preserves an exact record key,
    // then resolves dotted/bracketed paths. The series model itself never parses it.
    internal static object? Read(IReadOnlyDictionary<string, object?> record, string key)
    {
        if (key == "__proto__") return null;
        if (record.TryGetValue(key, out var direct)) return direct;
        var path = Path(key); if (path.Count == 0) return null;
        object? current = record;
        foreach (var name in path)
        {
            if (name == "__proto__") return null;
            if (current is IReadOnlyDictionary<string, object?> dictionary) current = dictionary.TryGetValue(name, out var value) ? value : null;
            else if (current is IList list && int.TryParse(name, NumberStyles.None, CultureInfo.InvariantCulture, out var index) && index >= 0 && index < list.Count) current = list[index];
            else return null;
            if (current is null) return null;
        }
        return current;
    }
    // es-toolkit's locked toPath grammar, including quoted bracket keys and
    // escaped characters. Leading dots address an explicit empty-string key.
    private static IReadOnlyList<string> Path(string value)
    {
        var parts = new List<string>(); var key = new StringBuilder(); var quote = '\0'; var bracket = false; var index = 0;
        if (value.StartsWith('.')) { parts.Add(""); index++; }
        for (; index < value.Length; index++)
        {
            var c = value[index];
            if (quote != '\0')
            {
                if (c == '\\' && index + 1 < value.Length) key.Append(value[++index]);
                else if (c == quote) quote = '\0'; else key.Append(c);
            }
            else if (bracket)
            {
                if (c is '\'' or '\"') quote = c;
                else if (c == ']') { bracket = false; parts.Add(key.ToString()); key.Clear(); }
                else key.Append(c);
            }
            else if (c == '[') { bracket = true; if (key.Length > 0) { parts.Add(key.ToString()); key.Clear(); } }
            else if (c == '.') { if (key.Length > 0) { parts.Add(key.ToString()); key.Clear(); } }
            else key.Append(c);
        }
        if (key.Length > 0) parts.Add(key.ToString()); return parts;
    }
    internal static double? Number(object? value)
    {
        if (value is null || value is bool) return null;
        if (value is double number) return double.IsFinite(number) ? number : null;
        if (value is float single) return float.IsFinite(single) ? single : null;
        if (value is byte or sbyte or short or ushort or int or uint or long or ulong or decimal) return Convert.ToDouble(value, CultureInfo.InvariantCulture);
        if (value is string text)
        {
            var start = 0; var end = text.Length; while (start < end && Whitespace(text[start])) start++; while (end > start && Whitespace(text[end - 1])) end--;
            text = text[start..end]; if (text.Length == 0) return 0;
            if (text.Length > 2 && text[0] == '0' && text[1] is 'x' or 'X' or 'b' or 'B' or 'o' or 'O')
            {
                var radix = char.ToLowerInvariant(text[1]) switch { 'x' => 16, 'b' => 2, _ => 8 }; var integer = BigInteger.Zero;
                foreach (var c in text[2..])
                {
                    var digit = c is >= '0' and <= '9' ? c - '0' : c is >= 'a' and <= 'f' ? c - 'a' + 10 : c is >= 'A' and <= 'F' ? c - 'A' + 10 : -1;
                    if (digit < 0 || digit >= radix) return null; integer = integer * radix + digit;
                }
                var parsedRadix = (double)integer; return double.IsFinite(parsedRadix) ? parsedRadix : null;
            }
            if (Regex.IsMatch(text, @"^[+-]?(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)(?:[eE][+-]?[0-9]+)?$") && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) && double.IsFinite(parsed)) return parsed;
        }
        return null;
    }
    // ECMAScript WhiteSpace/LineTerminator includes BOM, but excludes U+0085.
    private static bool Whitespace(char value) => value == '\uFEFF' || value != '\u0085' && char.IsWhiteSpace(value);
    internal static string Label(object? value) => value is null ? "" : Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
}
