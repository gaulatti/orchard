using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace Orchard;

public enum BleeckerMetricFormat { Number, Currency, Percent, Compact }

/// <summary>Uses the Windows ICU number formatter, preserving the source Intl defaults.</summary>
internal static class BleeckerMetricFormatter
{
    internal static string Format(double? value, BleeckerMetricFormat format, string currency, int? decimals, string locale)
    {
        // Source helpers return before constructing Intl for missing/NaN values.
        if (value is null || double.IsNaN(value.Value)) return string.Empty;
        if (!Enum.IsDefined(format)) throw new ArgumentOutOfRangeException(nameof(format));
        if (decimals is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(decimals));
        if (format == BleeckerMetricFormat.Currency && (currency is null || currency.Length != 3 || currency.Any(c => !char.IsAsciiLetter(c))))
            throw new ArgumentException("Currency must contain three ASCII letters.", nameof(currency));
        _ = CultureInfo.GetCultureInfo(locale);
        var status = 0;
        var formatter = unum_open(format switch { BleeckerMetricFormat.Number => 1, BleeckerMetricFormat.Currency => 16, BleeckerMetricFormat.Percent => 3, BleeckerMetricFormat.Compact => 14, _ => throw new ArgumentOutOfRangeException(nameof(format)) }, null, 0, locale, IntPtr.Zero, ref status);
        try
        {
            Check(status);
            if (format == BleeckerMetricFormat.Currency)
            {
                unum_setTextAttribute(formatter, 5, currency.ToUpperInvariant(), 3, ref status);
                Check(status);
            }
            unum_setAttribute(formatter, 11, 6); // UNUM_ROUNDING_MODE = HALFUP.
            if (decimals is { } digits)
            {
                unum_setAttribute(formatter, 16, 0); // Disable significant-digit rounding.
                unum_setAttribute(formatter, 6, digits); // Maximum fraction digits.
                unum_setAttribute(formatter, 7, digits); // Minimum fraction digits.
            }
            else if (format == BleeckerMetricFormat.Number)
            { unum_setAttribute(formatter, 6, 3); unum_setAttribute(formatter, 7, 0); }
            else if (format == BleeckerMetricFormat.Percent)
            { unum_setAttribute(formatter, 6, 1); unum_setAttribute(formatter, 7, 1); }
            var count = unum_formatDouble(formatter, value.Value, null, 0, IntPtr.Zero, ref status);
            if (status == 15) status = 0; // U_BUFFER_OVERFLOW_ERROR from the sizing call.
            Check(status);
            var buffer = new StringBuilder(count + 1);
            unum_formatDouble(formatter, value.Value, buffer, buffer.Capacity, IntPtr.Zero, ref status);
            Check(status);
            return buffer.ToString();
        }
        finally { if (formatter != IntPtr.Zero) unum_close(formatter); }
    }

    private static void Check(int status)
    {
        // ICU negative statuses are successful warnings, including locale data inheritance.
        if (status > 0) throw new InvalidOperationException($"Windows ICU number formatting failed ({status}).");
    }

    // icuin.dll is the Windows i18n library; unlike the combined icu.dll it also
    // exists before Windows 10 1903. ICU's stable C ABI is shared by x64/ARM64.
    [DllImport("icuin.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern IntPtr unum_open(int style, string? pattern, int patternLength, [MarshalAs(UnmanagedType.LPUTF8Str)] string locale, IntPtr parseError, ref int status);
    [DllImport("icuin.dll", CallingConvention = CallingConvention.Cdecl)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern void unum_setAttribute(IntPtr formatter, int attribute, int value);
    [DllImport("icuin.dll", CallingConvention = CallingConvention.Cdecl)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern void unum_close(IntPtr formatter);
    [DllImport("icuin.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern int unum_formatDouble(IntPtr formatter, double value, StringBuilder? buffer, int capacity, IntPtr fieldPosition, ref int status);
    [DllImport("icuin.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern void unum_setTextAttribute(IntPtr formatter, int attribute, string value, int length, ref int status);
}
