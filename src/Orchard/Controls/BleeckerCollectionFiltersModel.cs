using System.Collections.ObjectModel;
using System.Globalization;

namespace Orchard;

public enum BleeckerCollectionFilterType { Select, Boolean, Date }
public enum BleeckerSortOrder { Ascending, Descending }
public sealed record BleeckerSortState(string Field, BleeckerSortOrder Order);

/// <summary>An original boolean or string filter value, without string coercion.</summary>
public readonly record struct BleeckerCollectionFilterValue
{
    public object Value { get; }
    public BleeckerCollectionFilterValue(string value) => Value = value ?? throw new ArgumentNullException(nameof(value));
    public BleeckerCollectionFilterValue(bool value) => Value = value;
    public static implicit operator BleeckerCollectionFilterValue(string value) => new(value);
    public static implicit operator BleeckerCollectionFilterValue(bool value) => new(value);
}

public sealed record BleeckerCollectionFilterOption(string Field, string Label, BleeckerCollectionFilterType Type, IEnumerable<BleeckerSelectOption>? Options = null);
public sealed record BleeckerCollectionSortOption(string Field, string Label);
public readonly record struct BleeckerCollectionFiltersRequestedEventArgs(IReadOnlyDictionary<string, BleeckerCollectionFilterValue> Filters);
public readonly record struct BleeckerCollectionSortRequestedEventArgs(BleeckerSortState Sort);

internal static class BleeckerCollectionFiltersModel
{
    // Object.entries places canonical array indices before other string keys.
    // A stable sort preserves the caller's insertion order for every other name.
    internal static IEnumerable<KeyValuePair<string, BleeckerCollectionFilterValue>> Entries(IReadOnlyDictionary<string, BleeckerCollectionFilterValue> values) => values.OrderBy(pair =>
        uint.TryParse(pair.Key, NumberStyles.None, CultureInfo.InvariantCulture, out var index) && index != uint.MaxValue && index.ToString(CultureInfo.InvariantCulture) == pair.Key ? index : uint.MaxValue);
    internal static void Validate(IReadOnlyDictionary<string, BleeckerCollectionFilterValue> values)
    {
        foreach (var (field, value) in values)
            if (field is null || value.Value is not (bool or string)) throw new ArgumentException("Collection filters require original string keys and boolean/string values.");
    }

    internal static IReadOnlyDictionary<string, BleeckerCollectionFilterValue> Change(IReadOnlyDictionary<string, BleeckerCollectionFilterValue> current, string field, BleeckerCollectionFilterValue? value)
    {
        Validate(current); ArgumentNullException.ThrowIfNull(field);
        var next = new Dictionary<string, BleeckerCollectionFilterValue>(current, StringComparer.Ordinal);
        if (value is null || value.Value.Value is "") next.Remove(field);
        else { if (value.Value.Value is not (bool or string)) throw new ArgumentException("A filter request requires a boolean or string."); next[field] = value.Value; }
        return new ReadOnlyDictionary<string, BleeckerCollectionFilterValue>(Entries(next).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));
    }

    internal static string Display(BleeckerCollectionFilterOption? option, BleeckerCollectionFilterValue value, Func<DateTimeOffset, string> formatDate)
    {
        if (value.Value is bool boolean) return boolean ? "Yes" : "No";
        if (value.Value is not string text) throw new ArgumentException("A filter value requires a boolean or string.");
        if (option?.Type == BleeckerCollectionFilterType.Date)
        {
            var date = ParseDate(text);
            return date is null ? text : formatDate(date.Value);
        }
        return option?.Options?.FirstOrDefault(item => item.Value == text)?.Label ?? text;
    }

    internal static DateTimeOffset? ParseDate(string value)
    {
        if (value.Length == 0) return null;
        var parts = value.Split('-'); if (parts.Length < 3) return null;
        static double Number(string text)
        {
            text = text.Trim(); if (text.Length == 0) return 0;
            try
            {
                if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) return Convert.ToInt64(text[2..], 16);
                if (text.StartsWith("0b", StringComparison.OrdinalIgnoreCase)) return Convert.ToInt64(text[2..], 2);
                if (text.StartsWith("0o", StringComparison.OrdinalIgnoreCase)) return Convert.ToInt64(text[2..], 8);
                return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : double.NaN;
            }
            catch (FormatException) { return double.NaN; }
        }
        var year = Number(parts[0]); var month = Number(parts[1]); var day = Number(parts[2]);
        if (double.IsNaN(year) || year == 0 || double.IsNaN(month) || month == 0 || double.IsNaN(day) || day == 0) return null;
        if (!double.IsFinite(year) || !double.IsFinite(month) || !double.IsFinite(day)) throw new FormatException("Collection filter date is outside the native local-date range.");
        var y = checked((int)Math.Truncate(year)); if (y is >= 0 and <= 99) y += 1900;
        var date = new DateTime(y, 1, 1).AddMonths(checked((int)Math.Truncate(month - 1))).AddDays(Math.Truncate(day) - 1);
        return BleeckerDates.Midnight(date);
    }

    internal static string SerializeDate(DateTimeOffset? date) => date is null ? "" : date.Value.LocalDateTime.Year.ToString(CultureInfo.InvariantCulture) + date.Value.LocalDateTime.ToString("-MM-dd", CultureInfo.InvariantCulture);
    internal static BleeckerSortState Field(BleeckerSortState current, string field) => new(field, current.Order);
    internal static BleeckerSortState Toggle(BleeckerSortState current) => new(current.Field, current.Order == BleeckerSortOrder.Ascending ? BleeckerSortOrder.Descending : BleeckerSortOrder.Ascending);
    internal static int Columns(double viewportWidth) => viewportWidth >= 1024 ? 3 : viewportWidth >= 768 ? 2 : 1;
    internal static double HorizontalPadding(double viewportWidth) => viewportWidth >= 640 ? 20 : 16;
}
