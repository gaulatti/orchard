using System.Globalization;

namespace Orchard;

/// <summary>Consumer-owned ISO local dates, matching the source range contract.</summary>
public sealed record BleeckerDateRange(string? From = null, string? To = null);
public sealed record BleeckerDatePreset(string Label, double Days);
public readonly record struct BleeckerDateRequestedEventArgs(DateTimeOffset? Value);
public readonly record struct BleeckerDateRangeRequestedEventArgs(BleeckerDateRange Value);

internal static class BleeckerDates
{
    // The frozen browser uses a single-precision fractional track followed by
    // cumulative 1/64 CSS layout-unit boundaries, including the final track.
    internal static double[] Columns(double width)
    {
        var track = (double)(float)(width / 7);
        return Enumerable.Range(0, 7).Select(index =>
            (Math.Floor(track * 64 * (index + 1)) - Math.Floor(track * 64 * index)) / 64).ToArray();
    }
    internal static DateTime LocalDate(DateTimeOffset value) => value.LocalDateTime.Date;
    internal static DateTimeOffset Midnight(DateTime date) => new(date.Date, TimeZoneInfo.Local.GetUtcOffset(date.Date));
    internal static DateTimeOffset? Parse(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        var parts = value.Split('-'); if (parts.Length < 3) throw new FormatException("Date range requires numeric year-month-day fields.");
        var year = checked((int)Math.Truncate(Number(parts[0])));
        if (year is >= 0 and <= 99) year += 1900;
        var month = checked((int)Math.Truncate(Number(parts[1]) - 1));
        var day = Math.Truncate(Number(parts[2]));
        return Midnight(new DateTime(year, 1, 1).AddMonths(month).AddDays(day - 1));
    }
    private static double Number(string text)
    {
        text = text.Trim(); if (text.Length == 0) return 0;
        var value = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? Convert.ToInt64(text[2..], 16) :
            text.StartsWith("0b", StringComparison.OrdinalIgnoreCase) ? Convert.ToInt64(text[2..], 2) :
            text.StartsWith("0o", StringComparison.OrdinalIgnoreCase) ? Convert.ToInt64(text[2..], 8) :
            double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
        if (!double.IsFinite(value)) throw new FormatException("Date fields must be finite numbers.");
        return value;
    }
    internal static string? Serialize(DateTimeOffset? value) => value?.LocalDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    internal static string Display(DateTimeOffset value) => value.LocalDateTime.ToString("MMM d, yyyy", CultureInfo.GetCultureInfo("en-US"));
    internal static BleeckerDateRange Preset(DateTimeOffset now, double days)
    {
        var local = now.LocalDateTime;
        if (!double.IsFinite(days)) throw new ArgumentOutOfRangeException(nameof(days));
        var from = new DateTime(local.Year, local.Month, 1).AddDays(Math.Truncate(local.Day - days) - 1);
        return new(from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), local.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }
}
