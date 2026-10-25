using System.Numerics;
using System.Text.Json;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Orchard;

namespace Orchard.Text.Tests;

[TestClass]
public sealed class NavMenuTextTests
{
    [TestMethod]
    public void SourceTrackingPreservesCursiveConnectionsAndNativeIntrinsicWidths()
    {
        using var reference = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "NavMenu.json")));
        var checkedLinks = 0;
        foreach (var row in reference.RootElement.GetProperty("cases").EnumerateArray().Where(row => row.GetProperty("scheme").GetString() == "Light" && row.GetProperty("id").GetString() is "horizontal" or "unicode"))
        foreach (var link in row.GetProperty("measurement").GetProperty("links").EnumerateArray())
        {
            var text = link.GetProperty("label").GetString()!;
            using var format = new CanvasTextFormat { FontFamily = new Uri(Path.Combine(AppContext.BaseDirectory, "Fixtures", "EncodeSans-Medium.ttf")).AbsoluteUri + "#Encode Sans", FontSize = 16, FontWeight = new() { Weight = 500 }, LocaleName = "en-US", Direction = CanvasTextDirection.LeftToRightThenTopToBottom, WordWrapping = CanvasWordWrapping.NoWrap };
            using var layout = new CanvasTextLayout(CanvasDevice.GetSharedDevice(), text, format, float.MaxValue, float.MaxValue);
            layout.SetPairKerning(0, text.Length, true); BleeckerCssLetterSpacing.Apply(layout, text, format.Direction, -.32f);
            var start = 0; foreach (var run in BleeckerReferenceFontRuns.Split(text)) { if (run.Family is not null) layout.SetFontFamily(start, run.Text.Length, run.Family); start += run.Text.Length; }
            var renderer = new BleeckerGlyphWidthRenderer(false, true); layout.DrawToTextRenderer(renderer, Vector2.Zero);
            Assert.AreEqual(link.GetProperty("naturalWidth").GetDouble(), renderer.InlineWidth, .001, text); checkedLinks++;
        }
        Assert.AreEqual(7, checkedLinks);
    }
}
