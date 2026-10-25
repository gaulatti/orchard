using System.Globalization;
using System.Text;
using System.Runtime.InteropServices;

namespace Orchard;

internal static class BleeckerTextCasing
{
    // CSS language casing needs full, contextual mappings (including Greek
    // and Lithuanian), rather than TextInfo's simple per-scalar mappings.
    // Windows exposes this stable ICU common C API from version 1703, below
    // Orchard's 1809 minimum. WinUI's UI thread already initializes COM.
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [DllImport("icuuc.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int u_strToUpper([Out] char[]? destination, int capacity, string source, int length,
        [MarshalAs(UnmanagedType.LPStr)] string locale, ref int error);

    internal static string Uppercase(string value, string language)
    {
        ArgumentNullException.ThrowIfNull(value); ArgumentNullException.ThrowIfNull(language);
        if (value.Length == 0) return "";
        // ASCII only needs language tailoring for Turkic dotted i.
        var primary = language.Split('-', '_')[0];
        if (value.All(c => c <= 0x7f) && !primary.Equals("tr", StringComparison.OrdinalIgnoreCase) && !primary.Equals("az", StringComparison.OrdinalIgnoreCase)) return value.ToUpperInvariant();
        var error = 0; var length = u_strToUpper(null, 0, value, value.Length, language, ref error);
        if (length < 0 || error > 0 && error != 15) throw new InvalidOperationException($"Windows ICU uppercase sizing failed ({error}).");
        var output = new char[checked(length + 1)]; error = 0;
        var written = u_strToUpper(output, output.Length, value, value.Length, language, ref error);
        if (error > 0 || written < 0 || written > length) throw new InvalidOperationException($"Windows ICU uppercase conversion failed ({error}).");
        return Uppercase(new string(output, 0, written));
    }
    // Pin both simple mappings and full unconditional expansions so display
    // does not depend on the OS/runtime's Unicode data version.
    private static readonly Lazy<IReadOnlyDictionary<int, string>> special = new(ReadSpecialMappings);
    private static readonly Lazy<IReadOnlyDictionary<int, string>> simple = new(ReadSimpleMappings);

    internal static string Uppercase(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.All(character => character <= 0x7f)) return value.ToUpperInvariant();
        var output = new StringBuilder(value.Length);
        foreach (var scalar in value.EnumerateRunes())
            output.Append(special.Value.TryGetValue(scalar.Value, out var mapped) || simple.Value.TryGetValue(scalar.Value, out mapped) ? mapped : scalar.ToString());
        return output.ToString();
    }

    private static IReadOnlyDictionary<int, string> ReadSimpleMappings()
    {
        using var stream = typeof(BleeckerTextCasing).Assembly.GetManifestResourceStream("Orchard.Text.Uppercase-17.0.0.txt")
            ?? throw new InvalidOperationException("The pinned Unicode uppercase resource is missing.");
        using var reader = new StreamReader(stream); var result = new Dictionary<int, string>();
        while (reader.ReadLine() is { } line)
        {
            if (line.Length == 0 || line.StartsWith('#')) continue;
            var parts = line.Split(';'); if (parts.Length != 2) throw new InvalidOperationException("The pinned uppercase mapping is malformed.");
            result.Add(int.Parse(parts[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture), char.ConvertFromUtf32(int.Parse(parts[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture)));
        }
        return result;
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
