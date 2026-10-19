using System.Globalization;

namespace Orchard;

// Decimal Number text used by React nodes and DOM string coercion.
internal static class BleeckerNumberText
{
    internal static string Format(double value)
    {
        if(double.IsNaN(value))return "NaN";if(double.IsPositiveInfinity(value))return "Infinity";if(double.IsNegativeInfinity(value))return "-Infinity";if(value==0)return "0";
        var text=value.ToString("R",CultureInfo.InvariantCulture).ToLowerInvariant();var at=text.IndexOf('e');if(at<0)return text;
        var exponent=int.Parse(text[(at+1)..],CultureInfo.InvariantCulture);var mantissa=text[..at];
        if(exponent < -6 || exponent >=21)return mantissa+"e"+(exponent>=0?"+":"")+exponent.ToString(CultureInfo.InvariantCulture);
        var negative=mantissa.StartsWith('-');var digits=(negative?mantissa[1..]:mantissa).Replace(".","");var point=1+exponent;
        return(negative?"-":"")+(point<=0?"0."+new string('0',-point)+digits:point>=digits.Length?digits+new string('0',point-digits.Length):digits[..point]+"."+digits[point..]);
    }
}
