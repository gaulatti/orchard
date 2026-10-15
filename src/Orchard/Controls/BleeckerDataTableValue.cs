using System.Collections;
using System.Globalization;

namespace Orchard;

// The pinned React input coerces its unknown filter value to a DOM string.
// Global filtering uses Boolean(value), including truthy empty arrays/objects.
internal static class BleeckerDataTableValue
{
    internal static bool Truthy(object? value)=>value switch
    {
        null=>false,bool flag=>flag,string text=>text.Length>0,
        double number=>number!=0&&!double.IsNaN(number),float number=>number!=0&&!float.IsNaN(number),
        byte number=>number!=0,sbyte number=>number!=0,short number=>number!=0,ushort number=>number!=0,
        int number=>number!=0,uint number=>number!=0,long number=>number!=0,ulong number=>number!=0,decimal number=>number!=0,
        _=>true
    };
    internal static string InputText(object? value)=>String(value,new HashSet<object>(ReferenceEqualityComparer.Instance));
    private static string String(object? value,HashSet<object> path)
    {
        if(value is null)return "";
        if(value is string text)return text;
        if(value is bool flag)return flag?"true":"false";
        if(value is char character)return character.ToString();
        if(value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal)return Number(Convert.ToDouble(value,CultureInfo.InvariantCulture));
        if(value is IEnumerable array && value is not IDictionary)
        {
            if(!path.Add(value))throw new ArgumentException("A cyclic filter array cannot be rendered as input text.");
            try{return string.Join(',',array.Cast<object?>().Select(item=>String(item,path)));}finally{path.Remove(value);}
        }
        return "[object Object]";
    }
    private static string Number(double value)
    {
        if(double.IsNaN(value))return "NaN";if(double.IsPositiveInfinity(value))return "Infinity";if(double.IsNegativeInfinity(value))return "-Infinity";if(value==0)return "0";
        var text=value.ToString("R",CultureInfo.InvariantCulture).ToLowerInvariant();var at=text.IndexOf('e');if(at<0)return text;
        var exponent=int.Parse(text[(at+1)..],CultureInfo.InvariantCulture);var mantissa=text[..at];
        if(exponent < -6 || exponent >=21)return mantissa+"e"+(exponent>=0?"+":"")+exponent.ToString(CultureInfo.InvariantCulture);
        var negative=mantissa.StartsWith('-');var digits=(negative?mantissa[1..]:mantissa).Replace(".","");var point=1+exponent;
        return(negative?"-":"")+(point<=0?"0."+new string('0',-point)+digits:point>=digits.Length?digits+new string('0',point-digits.Length):digits[..point]+"."+digits[point..]);
    }
}
