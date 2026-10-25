using System.Security.Cryptography;
using System.Text.Json;
namespace Orchard.Tests;
[TestClass]
public sealed class FeedGridContractTests
{
    private static JsonDocument Read() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "FeedGrid.json")));
    [TestMethod]
    public void OriginalSourceCollectorClientAndEveryPngRemainBound()
    {
        using var document = Read(); var root = document.RootElement; Assert.AreEqual("a905ad5186b6579312b1cd233ae1df60bcbdb85147df7d451f53cabd3789d48d", root.GetProperty("sourceSha256").GetString()); Assert.AreEqual(144, root.GetProperty("cases").GetArrayLength());
        foreach (var (file, key) in new[] { ("CaptureFeedGrid.cjs", "scriptSha256"), ("FeedGridClient.tsx", "clientSha256") }) Assert.AreEqual(root.GetProperty(key).GetString(), Hash(Path.Combine(AppContext.BaseDirectory, "Fixtures", file)));
        var pngs = 0; foreach (var row in root.GetProperty("cases").EnumerateArray()) if (row.TryGetProperty("file", out var file)) { Assert.AreEqual(row.GetProperty("pngSha256").GetString(), Hash(Path.Combine(AppContext.BaseDirectory, "Fixtures", "FeedGrid", file.GetString()!))); pngs++; } Assert.AreEqual(22, pngs);
    }
    [TestMethod]
    public void AutoFillTracksAndVariableRowsMatchEveryOriginalSourceItem()
    {
        using var document = Read(); foreach (var row in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var spec = row.GetProperty("spec"); var measured = row.GetProperty("measurement"); var box = measured.GetProperty("grid"); var padding = measured.GetProperty("padding").EnumerateArray().Select(v => v.GetDouble()).ToArray(); var gap = measured.GetProperty("gap").GetDouble();
            var tracks = BleeckerFeedGridModel.Tracks(box.GetProperty("width").GetDouble() - padding[0] - padding[2], spec.TryGetProperty("minimum", out var minimum) ? minimum.GetDouble() : 340, gap);
            Assert.AreEqual(measured.GetProperty("columns").GetArrayLength(), tracks.Length, row.GetProperty("id").GetString());
            var hidden = spec.TryGetProperty("hidden", out var h) ? h.EnumerateArray().Select(v => v.GetInt32()).ToArray() : [];
            var items = measured.GetProperty("items").EnumerateArray().Where(item => !hidden.Contains(item.GetProperty("id").GetInt32())).ToArray(); var heights = new double[(items.Length + tracks.Length - 1) / tracks.Length];
            for (var i = 0; i < items.Length; i++) heights[i / tracks.Length] = Math.Max(heights[i / tracks.Length], 40 + items[i].GetProperty("id").GetInt32() % 3 * 20);
            var rtl = spec.TryGetProperty("rtl", out var direction) && direction.GetBoolean();
            for (var i = 0; i < items.Length; i++)
            {
                var column = i % tracks.Length; var r = i / tracks.Length; var item = items[i].GetProperty("box"); var logical = padding[0] + tracks.Take(column).Sum() + gap * column;
                var x = box.GetProperty("x").GetDouble() + (rtl ? box.GetProperty("width").GetDouble() - logical - tracks[column] : logical);
                Assert.AreEqual(item.GetProperty("width").GetDouble(), tracks[column], .001, row.GetProperty("id").GetString()); Assert.AreEqual(item.GetProperty("x").GetDouble(), x, .001); Assert.AreEqual(item.GetProperty("y").GetDouble(), box.GetProperty("y").GetDouble() + padding[1] + heights.Take(r).Sum() + gap * r, .001);
            }
            Assert.AreEqual(box.GetProperty("height").GetDouble(), padding[1] + padding[3] + heights.Sum() + gap * Math.Max(0, heights.Length - 1), .001);
        }
    }
    [TestMethod]
    public void InvalidNativeSizesFailExplicitly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BleeckerFeedGridModel.Tracks(double.NaN, 340, 12)); Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BleeckerFeedGridModel.Tracks(500, -1, 12)); Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BleeckerFeedGridModel.Tracks(500, 340, double.PositiveInfinity));
    }
    private static string Hash(string file) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
}
