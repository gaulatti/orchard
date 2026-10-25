using System.Security.Cryptography;
using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class DetailLayoutContractTests
{
    private static JsonDocument Read() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "DetailLayout.json")));
    [TestMethod]
    public void SourceClientCollectorAndAllOriginalPngsRemainVerifiable()
    {
        using var document = Read(); var root = document.RootElement;
        Assert.AreEqual("98be08a0c16be98b8846f43afb4edca93e143d4f5857cf77511c92e2874c836d", root.GetProperty("sourceSha256").GetString());
        Assert.AreEqual(888, root.GetProperty("cases").GetArrayLength());
        foreach (var (file, field) in new[] { ("CaptureDetailLayout.cjs", "scriptSha256"), ("DetailLayoutClient.tsx", "clientSha256") })
            Assert.AreEqual(root.GetProperty(field).GetString(), Hash(Path.Combine(AppContext.BaseDirectory, "Fixtures", file)));
        var count = 0;
        foreach (var row in root.GetProperty("cases").EnumerateArray()) if (row.TryGetProperty("file", out var file))
        { Assert.AreEqual(row.GetProperty("pngSha256").GetString(), Hash(Path.Combine(AppContext.BaseDirectory, "Fixtures", "DetailLayout", file.GetString()!))); count++; }
        Assert.AreEqual(48, count);
    }
    [TestMethod]
    public void TracksAndStickyClampingMatchEveryFrozenSourceLayout()
    {
        using var document = Read();
        foreach (var row in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var spec = row.GetProperty("spec"); var m = row.GetProperty("measurement"); var layout = m.GetProperty("layout");
            var ratio = spec.TryGetProperty("ratio", out var r) ? Enum.Parse<BleeckerDetailLayoutRatio>(r.GetString()!, true) : BleeckerDetailLayoutRatio.Primary;
            var side = spec.TryGetProperty("side", out var s) ? Enum.Parse<BleeckerDetailLayoutSide>(s.GetString()!, true) : BleeckerDetailLayoutSide.End;
            var rail = m.GetProperty("rail"); var present = rail.ValueKind != JsonValueKind.Null;
            var viewport = spec.GetProperty("viewport").GetDouble(); var width = spec.GetProperty("width").GetDouble();
            var tracks = BleeckerDetailLayoutModel.Columns(width, viewport, present, ratio, side);
            var rtl = spec.TryGetProperty("rtl", out var direction) && direction.GetBoolean();
            var originX = layout.GetProperty("x").GetDouble(); var originY = layout.GetProperty("y").GetDouble();
            var main = m.GetProperty("main"); var mainX = rtl ? width - tracks.MainX - tracks.MainWidth : tracks.MainX;
            Assert.AreEqual(tracks.Gap, m.GetProperty("gap").GetDouble());
            Assert.AreEqual(originX + mainX, main.GetProperty("x").GetDouble(), .001, row.GetProperty("id").GetString());
            Assert.AreEqual(tracks.MainWidth, main.GetProperty("width").GetDouble(), .001, row.GetProperty("id").GetString());
            if (!present) continue;
            var railX = rtl ? width - tracks.RailX - tracks.RailWidth : tracks.RailX;
            Assert.AreEqual(originX + railX, rail.GetProperty("x").GetDouble(), .001, row.GetProperty("id").GetString());
            Assert.AreEqual(tracks.RailWidth, rail.GetProperty("width").GetDouble(), .001, row.GetProperty("id").GetString());
            var normalY = tracks.Columns ? 0 : main.GetProperty("height").GetDouble() + tracks.Gap;
            var scroll = m.GetProperty("scrollViewport"); var viewportTop = scroll.ValueKind == JsonValueKind.Null ? 0 : scroll.GetProperty("y").GetDouble();
            var sticky = !spec.TryGetProperty("sticky", out var enabled) || enabled.GetBoolean();
            var delta = BleeckerDetailLayoutModel.StickyOffset(originY + normalY - viewportTop, layout.GetProperty("height").GetDouble(), rail.GetProperty("height").GetDouble(), sticky && viewport >= 1024);
            Assert.AreEqual(originY + normalY + delta, rail.GetProperty("y").GetDouble(), .001, row.GetProperty("id").GetString());
        }
    }
    [TestMethod]
    public void SourceKeepsInputsAndDocumentTabOrderWhenTheRailMovesVisually()
    {
        using var document = Read();
        foreach (var row in document.RootElement.GetProperty("interactions").EnumerateArray())
        {
            Assert.IsTrue(row.GetProperty("mainThenRailTabOrder").GetBoolean()); var retained = row.GetProperty("retained");
            Assert.IsTrue(retained.GetProperty("main").GetBoolean()); Assert.IsTrue(retained.GetProperty("rail").GetBoolean());
            Assert.AreEqual("Changed main", retained.GetProperty("mainValue").GetString()); Assert.AreEqual("Changed rail", retained.GetProperty("railValue").GetString());
        }
    }
    [TestMethod]
    public void UnknownRecipesAndInvalidGeometryFailExplicitly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BleeckerDetailLayoutModel.Columns(500, 1024, true, (BleeckerDetailLayoutRatio)99, BleeckerDetailLayoutSide.End));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BleeckerDetailLayoutModel.Columns(500, 1024, true, BleeckerDetailLayoutRatio.Primary, (BleeckerDetailLayoutSide)99));
        foreach (var width in new[] { -1, double.NaN, double.PositiveInfinity }) Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BleeckerDetailLayoutModel.Columns(width, 1024, false, BleeckerDetailLayoutRatio.Primary, BleeckerDetailLayoutSide.End));
    }
    private static string Hash(string file) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
}
