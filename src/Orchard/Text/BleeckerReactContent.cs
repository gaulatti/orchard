using System.Globalization;

namespace Orchard;

internal static class BleeckerReactContent
{
    internal static bool Numeric(object? value) => value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;
    internal static bool Present(object? value) => value switch
    {
        null => false, bool flag => flag, string text => text.Length > 0,
        _ when Numeric(value) => Convert.ToDouble(value, CultureInfo.InvariantCulture) is var number && number != 0 && !double.IsNaN(number),
        _ => true
    };
    internal static object? Display(object? value) => value switch
    {
        null or bool => null,
        _ when Numeric(value) => BleeckerNumberText.Format(Convert.ToDouble(value, CultureInfo.InvariantCulture)),
        _ => value
    };
}
