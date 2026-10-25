using System.Security.Cryptography;
using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class AuthShellLayoutTests
{
    private static string FilePath(string file) => Path.Combine(AppContext.BaseDirectory, "Fixtures", file);
    private static JsonDocument Read() => JsonDocument.Parse(File.ReadAllBytes(FilePath("AuthShellLayout.json")));
    private static double Number(JsonElement e, string name, double fallback = 0) => e.TryGetProperty(name, out var v) ? v.GetDouble() : fallback;
    private static void Hash(string file, string? expected) => Assert.AreEqual(expected, Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(FilePath(file)))));
    [TestMethod]
    public void SourceExtensionRetainsOriginalProducerAndAllCapturedPngs()
    {
        Hash("AuthShellLayout.json", "33e1c67660f4ff29b82372b3e4e696b2f3bd8b16061b3aa57c05ca0abcfe8cbd"); using var doc = Read(); var r = doc.RootElement;
        Assert.AreEqual(132, r.GetProperty("cases").GetArrayLength()); Assert.AreEqual(8, r.GetProperty("histories").GetArrayLength());
        foreach (var (file, field) in new[] { ("CaptureAuthShellLayout.cjs", "scriptSha256"), ("AuthShellLayoutClient.tsx", "clientSha256"), ("AuthShellLayout/source.css", "cssSha256"), ("AuthShellLayout/bundle.js", "bundleSha256") }) Hash(file, r.GetProperty(field).GetString());
        foreach (var field in new[] { "dependencies", "cssInputs" }) foreach (var item in r.GetProperty(field).EnumerateArray()) Hash("AuthShellLayout/Source/" + item.GetProperty("path").GetString(), item.GetProperty("sha256").GetString());
        foreach (var c in r.GetProperty("cases").EnumerateArray()) { Hash("AuthShellLayout/" + c.GetProperty("file").GetString(), c.GetProperty("pngSha256").GetString()); foreach (var s in c.GetProperty("samples").EnumerateArray()) Hash("AuthShellLayout/" + s.GetProperty("file").GetString(), s.GetProperty("sha256").GetString()); }
    }
    [TestMethod]
    public void ActualAccessBoxesKeepPaddingWhenGridTracksCollapse()
    {
        using var doc = Read(); foreach (var c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            var s = c.GetProperty("spec"); var m = c.GetProperty("measurement"); var style = s.TryGetProperty("style", out var st) ? st : JsonSerializer.SerializeToElement(new { });
            var outer = Number(style, "padding") + Number(style, "borderWidth"); var width = Number(m.GetProperty("main").GetProperty("box"), "width") - outer * 2; var rem = Number(s, "rootFont", 16) / 16;
            var side = s.GetProperty("side").GetString() == "start" ? BleeckerAuthShellSide.Start : BleeckerAuthShellSide.End; var grid = s.GetProperty("layout").GetString() == "split";
            var tracks = BleeckerAuthShellModel.Columns(width, Number(s, "width"), rem, grid, side); var access = m.GetProperty("access").GetProperty("box"); var rtl = s.TryGetProperty("rtl", out var direction) && direction.GetBoolean();
            Assert.AreEqual(tracks.AccessWidth, Number(access, "width"), 1d / 64, c.GetProperty("id").GetString()); Assert.AreEqual(outer + (rtl ? width - tracks.AccessX - tracks.AccessWidth : tracks.AccessX), Number(access, "x"), 1d / 64);
        }
    }
    [TestMethod]
    public void CenteredBlocksAndSplitRowsRespectTheirDifferentHeightContracts()
    {
        using var doc = Read(); foreach (var c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            var s = c.GetProperty("spec"); var m = c.GetProperty("measurement"); var style = s.TryGetProperty("style", out var st) ? st : JsonSerializer.SerializeToElement(new { }); var outer = Number(style, "padding") + Number(style, "borderWidth");
            var actual = BleeckerAuthShellModel.RegionHeight(760, Math.Max(0, Number(m.GetProperty("main").GetProperty("box"), "height") - outer * 2), s.GetProperty("layout").GetString() == "split");
            Assert.AreEqual(Number(m.GetProperty("access").GetProperty("box"), "height"), actual);
        }
        Assert.AreEqual(1348, BleeckerAuthShellModel.RegionHeight(1348, 500, true)); Assert.AreEqual(1348, BleeckerAuthShellModel.RegionHeight(1348, 500, false));
    }
    [TestMethod]
    public void ResizingAndStyleChangesKeepOriginalCallerEditorState()
    {
        using var doc = Read(); foreach (var h in doc.RootElement.GetProperty("histories").EnumerateArray())
        {
            var id = h.GetProperty("id").GetString(); if (id == "viewport-breakpoint-independent-of-container") continue;
            var property = id == "container-resize-retains-editors" ? "sameEditors" : id == "height-and-insets-retain-editors" ? "sameAccess" : "sameContent"; Assert.IsTrue(h.GetProperty(property).GetBoolean());
            var expected = id == "container-resize-retains-editors" ? "Typed narrow content" : id == "height-and-insets-retain-editors" ? "Typed styled content" : "Typed direction content";
            foreach (var s in h.GetProperty("steps").EnumerateArray()) Assert.AreEqual(expected, s.GetProperty("value").GetProperty("caller").GetProperty("content").GetProperty("value").GetString());
        }
    }
    [TestMethod]
    public void InvalidRegionHeightsFailExplicitly()
    {
        foreach (var value in new[] { -1d, double.NaN, double.PositiveInfinity }) { Assert.Throws<ArgumentOutOfRangeException>(() => BleeckerAuthShellModel.RegionHeight(value, 760, false)); Assert.Throws<ArgumentOutOfRangeException>(() => BleeckerAuthShellModel.RegionHeight(760, value, true)); }
    }
}
