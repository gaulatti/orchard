using System.Numerics;
using System.Text.Json;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Orchard;
namespace Orchard.Text.Tests;
[TestClass]
public sealed class FeedItemTextTests
{
    public TestContext TestContext {get;set;}=null!;
    [TestMethod]
    public void NativeSecondaryNoWrapTimeMatchesDirectionalSourceAllocation()
    {
        using var reference=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","FeedItem.json")));
        foreach(var id in new[]{"matrix-0","rtl"})
        {
            var row=reference.RootElement.GetProperty("cases").EnumerateArray().First(row=>row.GetProperty("id").GetString()==id);var time=row.GetProperty("measurement").GetProperty("time");var text=time.GetProperty("text").GetString()!;
            using var format=new CanvasTextFormat{FontFamily=new Uri(Path.Combine(AppContext.BaseDirectory,"Fixtures","LibreFranklin-Regular.ttf")).AbsoluteUri+"#Libre Franklin",FontSize=11,FontWeight=new(){Weight=400},LocaleName="en-US",Direction=id=="rtl"?CanvasTextDirection.RightToLeftThenTopToBottom:CanvasTextDirection.LeftToRightThenTopToBottom,WordWrapping=CanvasWordWrapping.NoWrap};
            using var layout=new CanvasTextLayout(CanvasDevice.GetSharedDevice(),text,format,float.MaxValue,float.MaxValue);layout.SetPairKerning(0,text.Length,true);layout.SetCharacterSpacing(0,text.Length,0,0,0);var renderer=new BleeckerGlyphWidthRenderer(true,true);layout.DrawToTextRenderer(renderer,Vector2.Zero);
            TestContext.WriteLine(JsonSerializer.Serialize(new{id,source=time.GetProperty("box").GetProperty("width").GetDouble(),renderer.Width,renderer.InlineWidth,renderer.WhitespaceJoinedWidth,renderer.Runs}));
            Assert.AreEqual(time.GetProperty("box").GetProperty("width").GetDouble(),id=="rtl"?renderer.WhitespaceJoinedWidth:renderer.InlineWidth);
        }
    }
}
