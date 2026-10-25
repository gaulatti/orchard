using System.Security.Cryptography;
using System.Text.Json;
namespace Orchard.Tests;
[TestClass]
public sealed class FeedColumnContractTests
{
    private static JsonDocument Read() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "FeedColumn.json")));
    [TestMethod]
    public void OriginalSourceCollectorClientFontsAndScreenshotsRemainBound()
    {
        using var document = Read(); var root = document.RootElement; Assert.AreEqual("981a086c1bbf1ccd1f98399cef9bd097992e4e50f2dcd27beae354beac0ac2a3", root.GetProperty("sourceSha256").GetString()); Assert.AreEqual(154, root.GetProperty("cases").GetArrayLength());
        foreach (var (file, key) in new[] { ("CaptureFeedColumn.cjs", "scriptSha256"), ("FeedColumnClient.tsx", "clientSha256") }) Assert.AreEqual(root.GetProperty(key).GetString(), Hash(Path.Combine(AppContext.BaseDirectory, "Fixtures", file)));
        var pngs = 0; foreach (var row in root.GetProperty("cases").EnumerateArray()) if (row.TryGetProperty("file", out var file)) { Assert.AreEqual(row.GetProperty("pngSha256").GetString(), Hash(Path.Combine(AppContext.BaseDirectory, "Fixtures", "FeedColumn", file.GetString()!))); pngs++; } Assert.AreEqual(58, pngs);
        foreach (var font in root.GetProperty("fonts").EnumerateArray()) Assert.AreEqual(font.GetProperty("sha256").GetString(), Hash(Path.Combine(AppContext.BaseDirectory, "Fixtures", font.GetProperty("name").GetString()!)));
    }
    [TestMethod]
    public void HeaderAndScrollingBodyMatchEveryPinnedBrowserState()
    {
        using var document = Read(); foreach (var row in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var m = row.GetProperty("measurement"); var title = m.GetProperty("title"); var count = m.GetProperty("count"); var box = m.GetProperty("row"); var present = count.ValueKind != JsonValueKind.Null;
            var countWidth = present ? count.GetProperty("box").GetProperty("width").GetDouble() : 0;
            var width = BleeckerFeedColumnModel.TitleWidth(box.GetProperty("width").GetDouble(), title.GetProperty("minimumWidth").GetDouble(), countWidth, present);
            Assert.AreEqual(title.GetProperty("box").GetProperty("width").GetDouble(), width, .001, row.GetProperty("id").GetString());
            var height = BleeckerFeedColumnModel.RowHeight(title.GetProperty("box").GetProperty("height").GetDouble(), present ? count.GetProperty("box").GetProperty("height").GetDouble() : 0);
            Assert.AreEqual(box.GetProperty("height").GetDouble(), height, .001);
            var filterHeight = m.GetProperty("header").GetProperty("height").GetDouble() - 49 - height;
            var spec = row.GetProperty("spec"); var expectedFilter = spec.TryGetProperty("filterHeight", out var filter) && filter.GetDouble() > 0 ? filter.GetDouble() : spec.TryGetProperty("filter", out var scalar) && scalar.ValueKind is JsonValueKind.Number or JsonValueKind.String ? 24 : 0;
            Assert.AreEqual(expectedFilter, filterHeight, .001); var header = BleeckerFeedColumnModel.HeaderHeight(height, expectedFilter);
            Assert.AreEqual(m.GetProperty("feed").GetProperty("height").GetDouble(), Math.Max(24, m.GetProperty("column").GetProperty("height").GetDouble() - 2 - header), .001);
        }
    }
    [TestMethod]
    public void OriginalHeaderUpdatesRetainInputsAndScroll()
    {
        using var document = Read(); Assert.AreEqual(2, document.RootElement.GetProperty("interactions").GetArrayLength());
        foreach (var row in document.RootElement.GetProperty("interactions").EnumerateArray()) { Assert.IsTrue(row.GetProperty("filterRetained").GetBoolean()); Assert.IsTrue(row.GetProperty("bodyRetained").GetBoolean()); Assert.AreEqual("Edited filter", row.GetProperty("filterValue").GetString()); Assert.AreEqual("Edited body", row.GetProperty("bodyValue").GetString()); Assert.AreEqual(120, row.GetProperty("beforeOffset").GetInt32()); Assert.AreEqual(120, row.GetProperty("afterOffset").GetInt32()); }
    }
    [TestMethod]
    public void GradientStopsRetainLegacyAlphaUntilOklabMixing()
    {
        var accent = ((byte)44, (byte)87, (byte)132, 1d); var start = BleeckerFeedColumnModel.Gradient(accent, 0); var middle = BleeckerFeedColumnModel.Gradient(accent, 208); var end = BleeckerFeedColumnModel.Gradient(accent, 512); var border = BleeckerFeedColumnModel.Border(accent);
        Assert.AreEqual(.11 + .89 * 224 / 255d, start.Alpha, 1e-12); Assert.AreEqual(224 / 255d, middle.Alpha, 1e-12); Assert.AreEqual(245 / 255d, end.Alpha, 1e-12); Assert.AreEqual(.15 + .85 * 19 / 255d, border.Alpha, 1e-12);
        Assert.AreEqual(start, BleeckerFeedColumnModel.Gradient(accent, -100)); Assert.AreEqual(end, BleeckerFeedColumnModel.Gradient(accent, 1000));
    }
    private static string Hash(string file) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
}
