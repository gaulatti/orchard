namespace Orchard;

internal static class BleeckerOtpModel
{
    internal readonly record struct Edit(string? RequestedValue, int? FocusIndex);
    private static bool Space(char value) => value is '\uFEFF' || value != '\u0085' && char.IsWhiteSpace(value);
    private static string Notify(IEnumerable<string> digits) => new(string.Concat(digits).Where(c => !Space(c)).ToArray());
    internal static string Display(string value, int length, int index)
    {
        Validate(value, length); Index(length, index);
        return index >= value.Length || Space(value[index]) ? "" : value[index].ToString();
    }
    internal static void Validate(string value, int length)
    {
        ArgumentNullException.ThrowIfNull(value); ArgumentOutOfRangeException.ThrowIfNegative(length);
    }
    private static void Index(int length, int index) { if (index < 0 || index >= length) throw new ArgumentOutOfRangeException(nameof(index)); }
    private static string[] Digits(string value, int length, int through)
    {
        var count = Math.Max(Math.Min(value.Length, length), through + 1); var result = new string[count];
        for (var i = 0; i < count; i++) result[i] = i < Math.Min(value.Length, length) ? value[i].ToString() : "";
        return result;
    }
    internal static Edit Change(string value, int length, int index, string text)
    {
        Validate(value, length); Index(length, index); ArgumentNullException.ThrowIfNull(text);
        var clean = text.LastOrDefault(c => c is >= '0' and <= '9'); var digits = Digits(value, length, index);
        digits[index] = clean == default ? "" : clean.ToString();
        return new(Notify(digits), clean != default && index < length - 1 ? index + 1 : null);
    }
    internal static Edit Backspace(string value, int length, int index)
    {
        Validate(value, length); Index(length, index);
        if (index < Math.Min(value.Length, length)) { var digits = Digits(value, length, index); digits[index] = " "; return new(Notify(digits), null); }
        if (index == 0) return new(null, null);
        var previous = Digits(value, length, index - 1); previous[index - 1] = " "; return new(Notify(previous), index - 1);
    }
    internal static Edit Paste(string text, int length)
    {
        Validate(text, length); var value = new string(text.Where(c => c is >= '0' and <= '9').Take(length).ToArray());
        return new(value, length == 0 ? null : Math.Min(value.Length, length - 1));
    }
}
