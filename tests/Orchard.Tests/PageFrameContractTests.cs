using System.Security.Cryptography;
using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class PageFrameContractTests
{
    private static JsonDocument Read() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "PageFrame.json")));
    [TestMethod]
    public void OriginalSourceClientCollectorAndPngHashesRemainVerifiable()
    {
        using var document = Read(); var root = document.RootElement;
        Assert.AreEqual("dacc54a6df068a390544945b15e38cc5d094cc164ef809311d4712a45acf823b", root.GetProperty("sourceSha256").GetString());
        Assert.AreEqual(792, root.GetProperty("cases").GetArrayLength());
        foreach (var (file, field) in new[] { ("CapturePageFrame.cjs", "scriptSha256"), ("PageFrameClient.tsx", "clientSha256") })
            Assert.AreEqual(root.GetProperty(field).GetString(), Hash(Path.Combine(AppContext.BaseDirectory, "Fixtures", file)));
        var images = 0;
        foreach (var row in root.GetProperty("cases").EnumerateArray()) if (row.TryGetProperty("file", out var file))
        { Assert.AreEqual(row.GetProperty("pngSha256").GetString(), Hash(Path.Combine(AppContext.BaseDirectory, "Fixtures", "PageFrame", file.GetString()!))); images++; }
        Assert.AreEqual(40, images);
    }
    [TestMethod]
    public void WidthPaddingAndCenteringMatchEveryFrozenSourceCase()
    {
        using var document = Read();
        foreach (var row in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var spec = row.GetProperty("spec"); var measured = row.GetProperty("measurement");
            T Option<T>(string name, string fallback) where T : struct, Enum => Enum.Parse<T>(spec.TryGetProperty(name, out var value) ? value.GetString()! : fallback, true);
            var maximum = BleeckerPageFrameModel.Maximum(Option<BleeckerPageFrameWidth>("width", "Content"));
            var padding = BleeckerPageFrameModel.Padding(spec.GetProperty("viewport").GetDouble(), Option<BleeckerPageFrameGutter>("gutter", "Comfortable"), Option<BleeckerPageFrameVerticalSpacing>("verticalSpacing", "Comfortable"));
            var horizontal = padding.Horizontal; var vertical = padding.Vertical; var requested = spec.GetProperty("hostWidth").GetDouble();
            if (spec.TryGetProperty("style", out var style))
            {
                if (style.TryGetProperty("maxWidth", out var maxWidth)) maximum = maxWidth.GetDouble();
                if (style.TryGetProperty("width", out var width)) requested = width.GetDouble();
                if (style.TryGetProperty("padding", out _)) { horizontal = 7; vertical = 3; }
            }
            var frame = measured.GetProperty("frame"); var expectedWidth = Math.Min(requested, maximum);
            var expectedX = 20 + (spec.GetProperty("hostWidth").GetDouble() - expectedWidth) / 2;
            Assert.AreEqual(expectedWidth, frame.GetProperty("width").GetDouble(), .001, row.GetProperty("id").GetString());
            Assert.AreEqual(expectedX, frame.GetProperty("x").GetDouble(), .001);
            CollectionAssert.AreEqual(new[] { horizontal, vertical, horizontal, vertical }, measured.GetProperty("padding").EnumerateArray().Select(value => value.GetDouble()).ToArray());
            var bodyHeight = spec.TryGetProperty("empty", out var empty) && empty.GetBoolean() ? 0 : 40;
            Assert.AreEqual(bodyHeight + vertical * 2, frame.GetProperty("height").GetDouble());
        }
    }
    [TestMethod]
    public void InvalidRecipeValuesFailExplicitly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BleeckerPageFrameModel.Maximum((BleeckerPageFrameWidth)99));
        foreach (var width in new[] { -1, double.NaN, double.PositiveInfinity })
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BleeckerPageFrameModel.Padding(width, BleeckerPageFrameGutter.None, BleeckerPageFrameVerticalSpacing.None));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BleeckerPageFrameModel.Padding(1024, (BleeckerPageFrameGutter)99, BleeckerPageFrameVerticalSpacing.None));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BleeckerPageFrameModel.Padding(1024, BleeckerPageFrameGutter.None, (BleeckerPageFrameVerticalSpacing)99));
    }
    private static string Hash(string file) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
}
