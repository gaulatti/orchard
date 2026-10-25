using System.Security.Cryptography;
using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class PanelContractTests
{
    private static JsonDocument Read() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Panel.json")));
    [TestMethod]
    public void OriginalSourceClientCollectorAndEveryPngRemainVerifiable()
    {
        using var document = Read(); var root = document.RootElement;
        Assert.AreEqual("a4a37fedfd21f848895d73837397de962f0dc28596927ef9479c3d4d6f06af38", root.GetProperty("sourceSha256").GetString());
        Assert.AreEqual(266, root.GetProperty("cases").GetArrayLength());
        foreach (var (file, field) in new[] { ("CapturePanel.cjs", "scriptSha256"), ("PanelClient.tsx", "clientSha256") })
            Assert.AreEqual(root.GetProperty(field).GetString(), Hash(Path.Combine(AppContext.BaseDirectory, "Fixtures", file)));
        var count = 0;
        foreach (var row in root.GetProperty("cases").EnumerateArray()) if (row.TryGetProperty("file", out var file))
        { Assert.AreEqual(row.GetProperty("pngSha256").GetString(), Hash(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Panel", file.GetString()!))); count++; }
        Assert.AreEqual(58, count);
    }
    [TestMethod]
    public void HeaderFlexAndSlotFlowMatchEveryFrozenSourceFrame()
    {
        using var document = Read();
        foreach (var row in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var m = row.GetProperty("measurement"); var r = m.GetProperty("row"); var title = m.GetProperty("title"); var count = m.GetProperty("count"); var drag = m.GetProperty("drag");
            var hasCount = count.ValueKind != JsonValueKind.Null; var hasDrag = drag.ValueKind != JsonValueKind.Null;
            var c = hasCount ? count.GetProperty("box") : default;
            double N(JsonElement value, string key) => value.GetProperty(key).GetDouble();
            var model = BleeckerPanelModel.Header(N(r,"width"), N(title,"minimumWidth"), N(title.GetProperty("box"),"height"), hasDrag ? N(drag,"width") : 0, hasDrag ? N(drag,"height") : 0,
                hasCount ? N(c,"width") : 0, hasCount ? N(c,"height") : 0, hasDrag, hasCount);
            var rtl = row.GetProperty("spec").TryGetProperty("rtl", out var direction) && direction.GetBoolean();
            double X(double logical, double width) => N(r,"x") + (rtl ? N(r,"width") - logical - width : logical);
            Assert.AreEqual(N(r,"height"), model.Height, .001, row.GetProperty("id").GetString());
            Assert.AreEqual(N(title.GetProperty("box"),"width"), model.TitleWidth, .001, row.GetProperty("id").GetString());
            Assert.AreEqual(N(title.GetProperty("box"),"x"), X(model.TitleX, model.TitleWidth), .001, row.GetProperty("id").GetString());
            Assert.AreEqual(N(m.GetProperty("dot"),"x"), X(model.DotX,6), .001, row.GetProperty("id").GetString());
            if (hasDrag) Assert.AreEqual(N(drag,"x"), X(model.DragX,N(drag,"width")), .001);
            if (hasCount) Assert.AreEqual(N(c,"x"), X(model.CountX,N(c,"width")), .001);
            var filter = m.GetProperty("filter"); var toolbar = m.GetProperty("toolbar"); var hasFilter = filter.ValueKind != JsonValueKind.Null; var hasToolbar = toolbar.ValueKind != JsonValueKind.Null;
            var headerHeight = BleeckerPanelModel.HeaderHeight(model.Height, hasFilter, hasFilter ? N(filter,"height") - 16 : 0, hasToolbar, hasToolbar ? N(toolbar,"height") - 25 : 0);
            Assert.AreEqual(N(m.GetProperty("header"),"height"),headerHeight,.001);
            Assert.AreEqual(N(m.GetProperty("feed"),"height"),Math.Max(0,N(m.GetProperty("panel"),"height")-headerHeight),.001);
        }
    }
    [TestMethod]
    public void CallerInputAndDocumentOrderSurviveLiveSourceUpdates()
    {
        using var document = Read();
        foreach (var row in document.RootElement.GetProperty("interactions").EnumerateArray())
        {
            Assert.IsTrue(row.GetProperty("filterRetained").GetBoolean()); Assert.IsTrue(row.GetProperty("bodyRetained").GetBoolean());
            Assert.AreEqual("Edited filter",row.GetProperty("filterValue").GetString()); Assert.AreEqual("Edited body",row.GetProperty("bodyValue").GetString());
            Assert.IsTrue(row.GetProperty("tabToToolbar").GetBoolean()); CollectionAssert.AreEqual(new[]{"drag","toolbar"},row.GetProperty("events").EnumerateArray().Select(value=>value.GetString()).ToArray());
            Assert.AreEqual(row.GetProperty("liveScroll").GetProperty("before").GetDouble(),row.GetProperty("liveScroll").GetProperty("after").GetDouble());
        }
        foreach(var row in document.RootElement.GetProperty("keyboard").EnumerateArray())
        { Assert.IsTrue(row.GetProperty("focused").GetBoolean()); Assert.IsTrue(row.GetProperty("pageDown").GetDouble()>0); Assert.AreEqual(0,row.GetProperty("home").GetDouble()); }
    }
    [TestMethod]
    public void BorderTransitionMatchesTheSourceTimelineAndReducedMotionSnaps()
    {
        using var document = Read();
        foreach (var row in document.RootElement.GetProperty("motion").EnumerateArray())
        {
            var motion = new BleeckerCssTransition<BleeckerCssColor>(BleeckerCssColor.Mix,BleeckerCssEasing.Default);
            var start = BleeckerCssColor.FromRgba((64,127,163,1),true); var end = BleeckerCssColor.FromRgba((184,74,80,1),true);
            motion.Retarget(start,0,200,false); motion.Retarget(end,10,200,true); var color = motion.ValueAt(10+row.GetProperty("time").GetDouble()).Encode();
            Assert.AreEqual($"rgb({color.R}, {color.G}, {color.B})",row.GetProperty("left").GetString());
            motion.Retarget(start,12,200,false); Assert.AreEqual(start,motion.ValueAt(12)); Assert.IsFalse(motion.IsRunning(12));
        }
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(()=>BleeckerPanelModel.Header(double.NaN,0,0,0,0,0,0,false,false));
    }
    private static string Hash(string file) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
}
