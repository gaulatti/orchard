using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Orchard.Tests;
[TestClass]
public sealed class AdminShellContractTests
{
    private static JsonDocument Read() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "AdminShell.json")));
    private static string Hash(string file) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", file))));
    [TestMethod]
    public void FrozenCollectorClientAndCompleteReferenceFramesAreBound()
    {
        using var document = Read(); var root = document.RootElement;
        Assert.AreEqual("a86059013cd59684ce452849278ea81e734f2066", root.GetProperty("commit").GetString());
        Assert.AreEqual("535f760672696f04aa108eb79bf537055358641f121ae24dde0a0c98cb29c864", root.GetProperty("sourceSha256").GetString());
        Assert.AreEqual(312, root.GetProperty("cases").GetArrayLength());
        Assert.AreEqual(root.GetProperty("scriptSha256").GetString(), Hash("CaptureAdminShell.cjs"));
        Assert.AreEqual(root.GetProperty("clientSha256").GetString(), Hash("AdminShellClient.tsx"));
        var pngs = 0; foreach (var row in root.GetProperty("cases").EnumerateArray())
        {
            if (row.TryGetProperty("file", out var file)) { Assert.AreEqual(row.GetProperty("pngSha256").GetString(), Hash("AdminShell/" + file.GetString())); pngs++; }
            if (row.TryGetProperty("scrolledFile", out var scrolled)) { Assert.AreEqual(row.GetProperty("scrolledPngSha256").GetString(), Hash("AdminShell/" + scrolled.GetString())); pngs++; }
        }
        Assert.AreEqual(88, pngs);
        foreach (var font in root.GetProperty("fonts").EnumerateArray()) Assert.AreEqual(font.GetProperty("sha256").GetString(), Hash(font.GetProperty("name").GetString()!));
    }
    [TestMethod]
    public void ResponsivePaddingUsesSourceViewportBoundaries()
    {
        using var document = Read(); foreach (var row in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var spec = row.GetProperty("spec"); if (spec.TryGetProperty("contentPadding", out _)) continue;
            var spacing = BleeckerAdminShellModel.ContentSpacing(spec.GetProperty("viewport").GetProperty("width").GetDouble());
            var padding = row.GetProperty("measurement").GetProperty("paint").GetProperty("content").GetProperty("padding");
            Assert.AreEqual(spacing.Horizontal + "px", padding[0].GetString()); Assert.AreEqual(spacing.Vertical + "px", padding[1].GetString());
        }
    }
    [TestMethod]
    public void SourceMainGrowsOverItsFooterAndScrollsOnlyWhenBounded()
    {
        using var document = Read(); foreach (var row in document.RootElement.GetProperty("cases").EnumerateArray().Where(row => row.GetProperty("id").GetString()!.StartsWith("matrix-", StringComparison.Ordinal) || row.GetProperty("id").GetString() is "bounded-short" or "bounded-long" or "minimum"))
        {
            var spec = row.GetProperty("spec"); var m = row.GetProperty("measurement"); var header = spec.TryGetProperty("headerHeight", out var h) ? h.GetDouble() : 0;
            var rail = spec.TryGetProperty("sidebarHeight", out var s) ? s.GetDouble() : 0; var footer = spec.TryGetProperty("footerHeight", out var f) ? f.GetDouble() + 41 : 0;
            var spacing = BleeckerAdminShellModel.ContentSpacing(spec.GetProperty("viewport").GetProperty("width").GetDouble()); var content = spec.GetProperty("bodyHeight").GetDouble() + 2 * spacing.Vertical;
            var minimum = spec.TryGetProperty("minimum", out var min) ? min.GetDouble() : spec.GetProperty("viewport").GetProperty("height").GetDouble();
            var shell = BleeckerAdminShellModel.ShellHeight(minimum, header, rail, content + footer, spec.TryGetProperty("height", out var height) ? height.GetDouble() : null);
            Assert.AreEqual(m.GetProperty("shell").GetProperty("height").GetDouble(), shell, .00001, row.GetProperty("id").GetString());
            Assert.AreEqual(m.GetProperty("content").GetProperty("height").GetDouble(), BleeckerAdminShellModel.ContentHeight(content, footer, shell - header), .00001);
        }
    }
    [TestMethod]
    public void RealSourceUpdatesRetainEditedOriginalInputAndAction()
    {
        using var document = Read(); var interactions = document.RootElement.GetProperty("interactions"); Assert.AreEqual(2, interactions.GetArrayLength());
        foreach (var row in interactions.EnumerateArray()) { Assert.IsTrue(row.GetProperty("inputRetained").GetBoolean()); Assert.AreEqual("Edited admin input", row.GetProperty("value").GetString()); Assert.AreEqual("body", row.GetProperty("events")[0].GetString()); }
    }
}
