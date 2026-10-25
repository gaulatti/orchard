using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Orchard;

// Layout owns the break controls; painting owns only the resulting glyphs.
// Original consumer strings remain in the item model and segment tooltip.
internal sealed class BleeckerInlineText(string text,IReadOnlyList<int> breaks,Func<string,double> measure,Func<int,int,double> measureSpan)
{
    internal static string Normalize(string value)=>Regex.Replace(value,"[ \\t\\r\\n\\f]+"," ").Trim(' ');
    private const string LayoutControls="\u200b\u2060\u00ad";
    internal static string Paint(string value)
    {
        if(value.AsSpan().IndexOfAny(LayoutControls)<0)return value;
        var result=new StringBuilder(value.Length);foreach(var character in value)if(LayoutControls.IndexOf(character)<0)result.Append(character);return result.ToString();
    }
    private readonly string paint=Paint(text);
    private readonly int[] paintOffsets=Offsets(text);
    private static int[] Offsets(string value)
    {
        var result=new int[value.Length+1];for(var i=0;i<value.Length;i++)result[i+1]=result[i]+(LayoutControls.IndexOf(value[i])<0?1:0);return result;
    }
    private double? maximumWidth,minimumWidth;
    private string Slice(int start,int end,bool atBreak)
    {
        var value=text[start..end].Trim(' ');
        var hyphen=atBreak&&value.EndsWith('\u00ad');
        return Paint(value)+(hyphen?"\u2010":"");
    }
    private double Width(int start,int end,bool atBreak)
    {
        while(start<end&&text[start]==' ')start++;
        while(end>start&&text[end-1]==' ')end--;
        // A visible discretionary hyphen changes the glyph sequence and must
        // be shaped as part of the resulting line rather than added by width.
        if(atBreak&&end>start&&text[end-1]=='\u00ad')return measure(Slice(start,end,true));
        var paintStart=paintOffsets[start];var paintEnd=paintOffsets[end];
        return measureSpan(paintStart,paintEnd-paintStart);
    }
    internal double MaximumWidth=>maximumWidth??=measure(paint);
    internal double MinimumWidth
    {
        get
        {
            if(minimumWidth is {} cached)return cached;
            var maximum=0d;var start=0;
            foreach(var end in breaks){maximum=Math.Max(maximum,Width(start,end,end<text.Length));start=end;}
            minimumWidth=maximum;return maximum;
        }
    }
    internal IReadOnlyList<string> Wrap(double width)
    {
        if(text.Length==0)return [];
        var result=new List<string>();var start=0;var last=start;
        foreach(var end in breaks)
        {
            if(last>start&&Width(start,end,end<text.Length)>width)
            {
                result.Add(Slice(start,last,true));start=last;
                while(start<text.Length&&text[start]==' ')start++;
            }
            last=end;
        }
        if(start<text.Length)result.Add(Slice(start,text.Length,false));
        return result;
    }
    // overflow-wrap: break-word keeps the normal word opportunities, then
    // uses grapheme boundaries only when one indivisible word exceeds a line.
    internal IReadOnlyList<string> WrapBreakWords(double width)
    {
        var result=new List<string>();
        foreach(var line in Wrap(width))
        {
            if(measure(line)<=width){result.Add(line);continue;}
            var start=0;var last=0;
            foreach(var end in GraphemeEnds(line))
            {
                if(last>start&&measure(line[start..end])>width){result.Add(line[start..last]);start=last;}
                last=end;
            }
            if(start<line.Length)result.Add(line[start..]);
        }
        return result;
    }
    internal static int[] GraphemeEnds(string value)=>StringInfo.ParseCombiningCharacters(value).Skip(1).Append(value.Length).ToArray();
}
