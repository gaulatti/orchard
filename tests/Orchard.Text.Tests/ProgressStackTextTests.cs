using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Orchard;

namespace Orchard.Text.Tests;

// These tests create no Window, change no input state and run the actual
// pinned Win2D shaping/break analysis used by the native ProgressStack.
[TestClass]
public sealed partial class ProgressStackTextTests
{
    private static BleeckerGlyphWidthRenderer Shape(string text,bool rtl,bool semibold)
    {
        var file=Path.Combine(AppContext.BaseDirectory,"Fixtures",semibold?"EncodeSans-SemiBold.ttf":"EncodeSans-Regular.ttf");
        using var format=new CanvasTextFormat
        {
            FontFamily=new Uri(file).AbsoluteUri+"#Encode Sans",FontSize=12,
            FontWeight=new(){Weight=(ushort)(semibold?600:400)},
            WordWrapping=CanvasWordWrapping.NoWrap,LocaleName="en-US",
            Direction=rtl?CanvasTextDirection.RightToLeftThenTopToBottom:CanvasTextDirection.LeftToRightThenTopToBottom
        };
        using var layout=new CanvasTextLayout(CanvasDevice.GetSharedDevice(),text,format,float.MaxValue,float.MaxValue);
        layout.SetPairKerning(0,text.Length,true);
        var index=0;foreach(var run in BleeckerReferenceFontRuns.Split(text,cjkPunctuation:true))
        {
            if(run.Family is not null)layout.SetFontFamily(index,run.Text.Length,run.Family);index+=run.Text.Length;
        }
        var renderer=new BleeckerGlyphWidthRenderer(true,true);layout.DrawToTextRenderer(renderer,Vector2.Zero);return renderer;
    }

    private static BleeckerInlineText Inline(string text,bool rtl,bool semibold)
    {
        var paragraph=Shape(BleeckerInlineText.Paint(text),rtl,semibold);
        return new(text,BleeckerInlineBreaks.Analyze(text,rtl,"en-US"),value=>Shape(value,rtl,semibold).InlineWidth,paragraph.InlineSpanWidth);
    }

    [TestMethod]
    public void NativeUnicodeIntrinsicWidthsAndWrappedLinesMatchAllFrozenBrowserCases()
    {
        using var document=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","ProgressStackText.json")));
        var rows=document.RootElement.GetProperty("cases");Assert.AreEqual(72,rows.GetArrayLength());
        foreach(var font in document.RootElement.GetProperty("fonts").EnumerateArray())
            Assert.AreEqual(font.GetProperty("sha256").GetString(),Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures",font.GetProperty("name").GetString()+".ttf")))));
        foreach(var row in rows.EnumerateArray())
        {
            var spec=row.GetProperty("spec");var rtl=spec.TryGetProperty("rtl",out var direction)&&direction.GetBoolean();
            var id=$"{row.GetProperty("id").GetString()}/{row.GetProperty("scheme").GetString()}/{row.GetProperty("motion").GetString()}";
            foreach(var item in row.GetProperty("measurement").GetProperty("items").EnumerateArray())
            foreach(var role in new[]{"label","value"})
            {
                var field=item.GetProperty(role);var original=field.GetProperty("text").GetString()!;
                var inline=Inline(original,rtl,role=="value");var intrinsic=field.GetProperty("intrinsic");
                Assert.AreEqual(intrinsic.GetProperty("maximum").GetDouble(),inline.MaximumWidth,$"{id}/{role}/maximum");
                Assert.AreEqual(intrinsic.GetProperty("minimum").GetDouble(),inline.MinimumWidth,$"{id}/{role}/minimum");
                var expected=field.GetProperty("graphemes").EnumerateArray().GroupBy(grapheme=>grapheme.GetProperty("line").GetInt32()).OrderBy(group=>group.Key)
                    .Select(group=>string.Concat(group.Select(grapheme=>grapheme.GetProperty("text").GetString())).Trim(' ')).ToArray();
                for(var i=0;i<expected.Length;i++)
                {
                    var hyphen=i<expected.Length-1&&expected[i].EndsWith('\u00ad');expected[i]=BleeckerInlineText.Paint(expected[i])+(hyphen?"\u2010":"");
                }
                var actual=inline.Wrap(field.GetProperty("box").GetProperty("width").GetDouble()).ToArray();
                CollectionAssert.AreEqual(expected,actual,$"{id}/{role}/lines expected=[{string.Join(" | ",expected)}] actual=[{string.Join(" | ",actual)}]");
            }
        }
    }

    [TestMethod]
    public void NativeBreaksRespectGraphemesJoinersAndJapanesePunctuation()
    {
        foreach(var text in new[]{"👩🏽‍💻","🏳️‍🌈","👨‍👩‍👧","e\u0301","Review\u2060queue","Review\u00a0queue"})
            CollectionAssert.AreEqual(new[]{text.Length},BleeckerInlineBreaks.Analyze(text,false,"en-US").ToArray(),text);
        CollectionAssert.AreEqual(new[]{4,5},BleeckerInlineBreaks.Analyze("「日」、本",false,"en-US").ToArray());
        CollectionAssert.AreEqual(new[]{1,2,3},BleeckerInlineBreaks.Analyze("ワーク",false,"en-US").ToArray());
    }
}
