using System.Numerics;
using System.Text.Json;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Orchard;
namespace Orchard.Text.Tests;
[TestClass]
public sealed class SliderTextTests
{
    public TestContext TestContext { get; set; } = null!;
    private static double Width(string value,bool medium,bool tabular)
    {
        var file=Path.Combine(AppContext.BaseDirectory,"Fixtures",medium?"EncodeSans-Medium.ttf":"EncodeSans-Regular.ttf");
        using var format=new CanvasTextFormat{FontFamily=new Uri(file).AbsoluteUri+"#Encode Sans",FontSize=14,FontWeight=new(){Weight=(ushort)(medium?500:400)},WordWrapping=CanvasWordWrapping.NoWrap,LocaleName="en-US"};
        using var layout=new CanvasTextLayout(CanvasDevice.GetSharedDevice(),value,format,float.MaxValue,float.MaxValue);
        using var typography=BleeckerTextTypography.Tabular();if(tabular)layout.SetTypography(0,value.Length,typography);layout.SetPairKerning(0,value.Length,true);layout.SetCharacterSpacing(0,value.Length,0,0,0);
        var renderer=new BleeckerGlyphWidthRenderer(false,true);layout.DrawToTextRenderer(renderer,Vector2.Zero);return renderer.Width;
    }
    [TestMethod]
    public void NativeMediumLabelsAndRegularTabularValuesMatchSourceWidths()
    {
        using var reference=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","Slider.json")));
        foreach(var row in reference.RootElement.GetProperty("cases").EnumerateArray().Where(r=>r.GetProperty("spec").GetProperty("width").GetDouble()==500.375&&r.GetProperty("spec").TryGetProperty("label",out var label)&&label.GetString()!.Length>0))
        {
            var measured=row.GetProperty("measurement");var label=measured.GetProperty("label");Assert.AreEqual(label.GetProperty("box").GetProperty("width").GetDouble(),Width(label.GetProperty("text").GetString()!,true,false),row.GetProperty("id").GetString());
            var display=measured.GetProperty("display");if(display.ValueKind!=JsonValueKind.Null)Assert.AreEqual(display.GetProperty("box").GetProperty("width").GetDouble(),Width(display.GetProperty("text").GetString()!,false,true),row.GetProperty("id").GetString());
        }
    }

    [TestMethod]
    public void NativeMultilingualSliderLabelMatchesSourceShapingWidth()
    {
        using var reference=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","Slider.json")));
        var row=reference.RootElement.GetProperty("cases").EnumerateArray().First(row=>row.GetProperty("id").GetString()=="unicode-label");
        var label=row.GetProperty("measurement").GetProperty("label");var text=label.GetProperty("text").GetString()!;
        var file=Path.Combine(AppContext.BaseDirectory,"Fixtures","EncodeSans-Medium.ttf");
        using var format=new CanvasTextFormat{FontFamily=new Uri(file).AbsoluteUri+"#Encode Sans",FontSize=14,FontWeight=new(){Weight=500},WordWrapping=CanvasWordWrapping.NoWrap,LocaleName="en-US"};
        using var layout=new CanvasTextLayout(CanvasDevice.GetSharedDevice(),text,format,float.MaxValue,float.MaxValue);
        layout.SetPairKerning(0,text.Length,true);layout.SetCharacterSpacing(0,text.Length,0,0,0);
        var start=0;foreach(var run in BleeckerReferenceFontRuns.Split(text)){if(run.Family is not null)layout.SetFontFamily(start,run.Text.Length,run.Family);start+=run.Text.Length;}
        var renderer=new BleeckerGlyphWidthRenderer(true,true);layout.DrawToTextRenderer(renderer,Vector2.Zero);
        TestContext.WriteLine(JsonSerializer.Serialize(new{source=label.GetProperty("box").GetProperty("width").GetDouble(),renderer.Width,renderer.InlineWidth,renderer.WhitespaceJoinedWidth,renderer.Runs}));
        Assert.AreEqual(label.GetProperty("box").GetProperty("width").GetDouble(),renderer.InlineWidth);
    }
}
