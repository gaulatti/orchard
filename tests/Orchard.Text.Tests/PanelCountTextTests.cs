using System.Numerics;
using System.Text.Json;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Orchard;

namespace Orchard.Text.Tests;

[TestClass]
public sealed class PanelCountTextTests
{
    private static double Width(string value)
    {
        var file = Path.Combine(AppContext.BaseDirectory,"Fixtures","EncodeSans-SemiBold.ttf");
        using var format = new CanvasTextFormat { FontFamily=new Uri(file).AbsoluteUri+"#Encode Sans",FontSize=11,FontWeight=new(){Weight=600},WordWrapping=CanvasWordWrapping.NoWrap,LocaleName="en-US" };
        using var layout = new CanvasTextLayout(CanvasDevice.GetSharedDevice(),value,format,float.MaxValue,float.MaxValue);
        using var typography = BleeckerTextTypography.Tabular();
        layout.SetTypography(0,value.Length,typography);
        layout.SetPairKerning(0,value.Length,true); layout.SetCharacterSpacing(0,value.Length,0,0,0);
        var renderer=new BleeckerGlyphWidthRenderer(false,true);layout.DrawToTextRenderer(renderer,Vector2.Zero);return renderer.Width;
    }
    [TestMethod]
    public void NativePanelTabularCountsMatchFrozenBrowserWidths()
    {
        using var reference=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","Panel.json")));
        foreach(var count in reference.RootElement.GetProperty("cases").EnumerateArray().Select(row=>row.GetProperty("measurement").GetProperty("count")).Where(count=>count.ValueKind!=JsonValueKind.Null).DistinctBy(count=>count.GetProperty("value").GetString()))
        {
            var text=count.GetProperty("value").GetString()!;
            Assert.AreEqual(count.GetProperty("naturalWidth").GetDouble()-18,Width(text),text);
        }
    }
}
