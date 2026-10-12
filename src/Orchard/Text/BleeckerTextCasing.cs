using System.Globalization;
using System.Text;

namespace Orchard;

internal static class BleeckerTextCasing
{
    // .NET's invariant casing supplies simple mappings. CSS uppercase also
    // needs Unicode's unconditional mappings that expand into multiple scalars.
    private static readonly Lazy<IReadOnlyDictionary<int, string>> special = new(ReadSpecialMappings);

    internal static string Uppercase(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.All(character => character <= 0x7f)) return value.ToUpperInvariant();
        var output = new StringBuilder(value.Length);
        foreach (var scalar in value.EnumerateRunes())
            output.Append(special.Value.TryGetValue(scalar.Value, out var mapped) ? mapped : Rune.ToUpperInvariant(scalar).ToString());
        return output.ToString();
    }

    private static IReadOnlyDictionary<int, string> ReadSpecialMappings()
    {
        using var stream = typeof(BleeckerTextCasing).Assembly.GetManifestResourceStream("Orchard.Text.SpecialCasing-17.0.0.txt")
            ?? throw new InvalidOperationException("The pinned Unicode special-casing resource is missing.");
        using var reader = new StreamReader(stream);
        var result = new Dictionary<int, string>();
        while (reader.ReadLine() is { } line)
        {
            var data = line.Split('#')[0].Trim();
            if (data.Length == 0) continue;
            var fields = data.Split(';').Select(field => field.Trim()).ToArray();
            if (fields.Length < 5) throw new InvalidOperationException("The pinned Unicode special-casing resource is malformed.");
            if (fields[4].Length != 0) continue; // Context and locale casing are audited separately.
            var code = int.Parse(fields[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var mapped = string.Concat(fields[3].Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(hex =>
                char.ConvertFromUtf32(int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture))));
            result.Add(code, mapped);
        }
        return result;
    }
}
