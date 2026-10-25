using System.Security.Cryptography;
using System.Text.Json;
namespace Orchard.Tests;

[TestClass]
public sealed class PanelLayoutContractTests
{
    private static JsonDocument Read() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "PanelLayout.json")));
    [TestMethod]
    public void OriginalSourceCollectorClientAndEveryCompletePngAreBound()
    {
        using var document = Read(); var root = document.RootElement;
        Assert.AreEqual("fb7bb7fa4505a3572509d87e3004191e7be8167163abd158850a7f07885c024a", root.GetProperty("sourceSha256").GetString());
        Assert.AreEqual(86, root.GetProperty("cases").GetArrayLength());
        foreach (var (file, key) in new[] { ("CapturePanelLayout.cjs", "scriptSha256"), ("PanelLayoutClient.tsx", "clientSha256") })
            Assert.AreEqual(root.GetProperty(key).GetString(), Hash(Path.Combine(AppContext.BaseDirectory, "Fixtures", file)));
        var pngs = 0;
        foreach (var row in root.GetProperty("cases").EnumerateArray()) if (row.TryGetProperty("file", out var file))
        { Assert.AreEqual(row.GetProperty("pngSha256").GetString(), Hash(Path.Combine(AppContext.BaseDirectory, "Fixtures", "PanelLayout", file.GetString()!))); pngs++; }
        Assert.AreEqual(34, pngs);
    }
    [TestMethod]
    public void FixedAndGrowingTracksMatchOriginalHorizontalAndVerticalWrappers()
    {
        using var document = Read();
        foreach (var row in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var spec = row.GetProperty("spec"); if (spec.GetProperty("items").EnumerateArray().Any(item => item.TryGetProperty("items", out _))) continue;
            var column = spec.TryGetProperty("column", out var vertical) && vertical.GetBoolean();
            var padding = spec.TryGetProperty("padding", out var padded) && padded.GetBoolean() ? 16 : 0;
            var items = spec.GetProperty("items").EnumerateArray().ToArray();
            var natural = items.Select(item => item.TryGetProperty("hidden", out var hidden) && hidden.GetBoolean() ? 0 : item.TryGetProperty(column ? "height" : "width", out var size) ? size.GetDouble() : 0).ToArray();
            var grows = items.Select(item => item.TryGetProperty("grow", out var grow) && grow.GetBoolean()).ToArray();
            var tracks = BleeckerPanelFlowModel.Tracks(spec.GetProperty(column ? "height" : "width").GetDouble() - padding, natural, grows);
            var rendered = row.GetProperty("measurement").GetProperty("items").EnumerateArray().ToArray();
            for (var index = 0; index < tracks.Length; index++) Assert.AreEqual(rendered[index].GetProperty("wrapper").GetProperty(column ? "height" : "width").GetDouble(), tracks[index], .001, row.GetProperty("id").GetString());
        }
    }
    [TestMethod]
    public void IntrinsicParentsAndInvalidConstraintsHaveExplicitResults()
    {
        CollectionAssert.AreEqual(new[] { 80d, 120d }, BleeckerPanelFlowModel.Tracks(double.PositiveInfinity, new[] { 80d, 120d }, new[] { true, false }));
        CollectionAssert.AreEqual(new[] { 80d, 0d }, BleeckerPanelFlowModel.Tracks(40, new[] { 80d, 120d }, new[] { false, true }));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BleeckerPanelFlowModel.Tracks(double.NaN, [], []));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BleeckerPanelFlowModel.Tracks(10, [2], []));
    }
    private static string Hash(string file) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
}
