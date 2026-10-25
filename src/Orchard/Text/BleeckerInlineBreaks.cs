using System.Text;
using Microsoft.Graphics.Canvas.Text;

namespace Orchard;

internal static class BleeckerInlineBreaks
{
    internal static IReadOnlyList<int> Analyze(string text,bool rtl,string language)
    {
        if(text.Length==0)return [];
        // The pinned browser's default CSS auto policy treats CJ as ID.
        // Substitute only for analysis, preserving UTF-16 offsets. All actual
        // shaping and grapheme boundaries still use the original text.
        var analysis=new StringBuilder();
        foreach(var rune in text.EnumerateRunes())analysis.Append(IsConditionalJapaneseStarter(rune.Value)?new string('\u4e00',rune.Utf16SequenceLength):rune.ToString());
        var analyzer=new CanvasTextAnalyzer(analysis.ToString(),rtl?CanvasTextDirection.RightToLeftThenTopToBottom:CanvasTextDirection.LeftToRightThenTopToBottom);
        var points=analyzer.GetBreakpoints(language);
        var result=new List<int>();
        foreach(var end in BleeckerInlineText.GraphemeEnds(text))
        {
            if(end==text.Length){result.Add(end);continue;}
            var after=points[end-1].BreakAfter;var before=points[end].BreakBefore;
            var allowed=after==CanvasLineBreakCondition.MustBreak||before==CanvasLineBreakCondition.MustBreak||
                after!=CanvasLineBreakCondition.CannotBreak&&before!=CanvasLineBreakCondition.CannotBreak&&(after==CanvasLineBreakCondition.CanBreak||before==CanvasLineBreakCondition.CanBreak);
            // CSS's established solidus/letter tailoring keeps dates and paths
            // together. Preserve explicit zero-width-space opportunities.
            if(text[end-1]=='/'&&Rune.IsLetter(Rune.GetRuneAt(text,end)))allowed=false;
            if(allowed)result.Add(end);
        }
        return result;
    }
    // Unicode 17.0 LineBreak.txt, class CJ; not a general CJK punctuation rule.
    private static bool IsConditionalJapaneseStarter(int scalar)=>scalar is
        0x3041 or 0x3043 or 0x3045 or 0x3047 or 0x3049 or 0x3063 or 0x3083 or 0x3085 or 0x3087 or 0x308e or
        >=0x3095 and <=0x3096 or 0x30a1 or 0x30a3 or 0x30a5 or 0x30a7 or 0x30a9 or 0x30c3 or 0x30e3 or 0x30e5 or 0x30e7 or 0x30ee or
        >=0x30f5 and <=0x30f6 or 0x30fc or >=0x31f0 and <=0x31ff or >=0xff67 and <=0xff70 or
        0x1b132 or >=0x1b150 and <=0x1b152 or 0x1b155 or >=0x1b164 and <=0x1b167;
}
