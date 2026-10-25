using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Orchard;

internal static class BleeckerFeedItemModel
{
    internal static string Trim(string value)
    {
        static bool Space(char c) => c is >= '\u0009' and <= '\u000d' or ' ' or '\u00a0' or '\u1680' or >= '\u2000' and <= '\u200a' or '\u2028' or '\u2029' or '\u202f' or '\u205f' or '\u3000' or '\ufeff';
        var start=0;var end=value.Length;while(start<end&&Space(value[start]))start++;while(end>start&&Space(value[end-1]))end--;return value[start..end];
    }
    internal static string Category(string value) => Lowercase(Trim(value));
    // ECMAScript's default lowercase includes dotted-I expansion and the
    // context-sensitive final sigma that invariant simple casing omits.
    private static string Lowercase(string value)
    {
        var runes=value.EnumerateRunes().ToArray();var result=new StringBuilder(value.Length);
        static bool Cased(Rune r)=>Rune.ToLowerInvariant(r)!=Rune.ToUpperInvariant(r);
        static bool Ignorable(Rune r)=>Rune.GetUnicodeCategory(r) is UnicodeCategory.NonSpacingMark or UnicodeCategory.EnclosingMark or UnicodeCategory.Format or UnicodeCategory.ModifierLetter or UnicodeCategory.ModifierSymbol || r.Value is 0x27 or 0x2e or 0x3a or 0xb7 or 0x387 or 0x5f4 or 0x2018 or 0x2019 or 0x2024 or 0x2027;
        for(var i=0;i<runes.Length;i++)
        {
            var r=runes[i];if(r.Value==0x130){result.Append("i\u0307");continue;}
            if(r.Value==0x3a3){var before=i-1;while(before>=0&&Ignorable(runes[before]))before--;var after=i+1;while(after<runes.Length&&Ignorable(runes[after]))after++;result.Append(before>=0&&Cased(runes[before])&&(after==runes.Length||!Cased(runes[after]))?'\u03c2':'\u03c3');continue;}
            result.Append(Rune.ToLowerInvariant(r).ToString());
        }
        return result.ToString();
    }
    internal static string Fallback(string author,string? init) => !string.IsNullOrEmpty(init)?init:BleeckerTextCasing.Uppercase(Trim(author) is {Length:>0} trimmed?trimmed[..1]:"?");
    internal static string Relevance(double value) => BleeckerNumberText.Format(double.IsFinite(value)?Math.Floor(value)+(value-Math.Floor(value)>=.5?1:0):value);
    internal static string SourceLabel(string source)
    {
        if(!Uri.TryCreate(source,UriKind.Absolute,out var uri))return "Source article";
        var colon=source.IndexOf(':');if(colon>=0&&!source[(colon+1)..].StartsWith("//",StringComparison.Ordinal)&&uri.Scheme is not ("http" or "https" or "ftp" or "file" or "ws" or "wss"))return "";
        return uri.HostNameType==UriHostNameType.IPv6?"["+uri.IdnHost.Trim('[',']')+"]":uri.IdnHost.ToLowerInvariant();
    }
    internal readonly record struct Timestamp(string Label,string Iso);
    internal static Timestamp FormatTimestamp(object value,DateTimeOffset now,CultureInfo culture,TimeZoneInfo zone)
    {
        DateTimeOffset date;
        if(value is DateTimeOffset offset)date=offset;
        else if(value is string text)
        {
            // ISO date-only strings are UTC; an ISO local date-time uses the
            // current local zone, matching the source's Date constructor.
            var styles=Regex.IsMatch(text,@"^\d{4}-\d\d-\d\d$")?DateTimeStyles.AssumeUniversal:DateTimeStyles.None;
            if(!DateTimeOffset.TryParse(text,CultureInfo.InvariantCulture,styles,out date))return new("Unknown time",text);
            if(Regex.IsMatch(text,@"^\d{4}-\d\d-\d\dT\d\d:\d\d(?::\d\d(?:\.\d+)?)?$"))date=new DateTimeOffset(date.DateTime,zone.GetUtcOffset(date.DateTime));
        }
        else throw new ArgumentException("FeedItem PostedAt requires a DateTimeOffset or timestamp string.",nameof(value));
        var local=TimeZoneInfo.ConvertTime(date,zone);var diff=now-date;string label;
        if(diff>=TimeSpan.Zero&&diff<=TimeSpan.FromHours(12))label=local.ToString(culture.DateTimeFormat.ShortTimePattern.Replace("HH","H",StringComparison.Ordinal).Replace("hh","h",StringComparison.Ordinal),culture);
        else
        {
            var pattern=culture.DateTimeFormat.LongDatePattern.Replace("dddd, ","",StringComparison.Ordinal).Replace("dddd ","",StringComparison.Ordinal).Replace("MMMM","MMM",StringComparison.Ordinal);
            pattern=Regex.Replace(pattern,@"'[^']*'|d+",match=>match.Value is "d" or "dd"?"dd":match.Value);label=local.ToString(pattern,culture);
        }
        return new(label,date.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'",CultureInfo.InvariantCulture));
    }
}
