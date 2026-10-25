using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;

namespace Orchard.Tests;

[TestClass]
public sealed class ThemeToggleContractTests
{
    private static string Fixture(string file) => Path.Combine(AppContext.BaseDirectory, "Fixtures", file);
    private static JsonDocument Read() => JsonDocument.Parse(File.ReadAllBytes(Fixture("ThemeToggle.json")));
    private static BleeckerThemeMode Mode(string text) => Enum.Parse<BleeckerThemeMode>(text, true);

    [TestMethod]
    public void OriginalComponentCollectorAndEveryCompletePngRemainBound()
    {
        using var document = Read(); var root = document.RootElement;
        Assert.AreEqual("57e3f4181d2b50fc0aa7dcd56205ef62e94955d4067d5e20c58254c34bd638d9", root.GetProperty("sourceSha256").GetString());
        Assert.AreEqual("a86059013cd59684ce452849278ea81e734f2066", root.GetProperty("commit").GetString());
        foreach (var (file, key) in new[] { ("CaptureThemeToggle.cjs", "scriptSha256"), ("ThemeToggleClient.tsx", "clientSha256") })
            Assert.AreEqual(root.GetProperty(key).GetString(), Hash(Fixture(file)));
        var rows = root.GetProperty("cases").EnumerateArray().ToArray(); Assert.AreEqual(134, rows.Length);
        Assert.AreEqual(134, rows.Select(row => row.GetProperty("file").GetString()).Distinct().Count());
        foreach (var row in rows) Assert.AreEqual(row.GetProperty("pngSha256").GetString(), Hash(Fixture(Path.Combine("ThemeToggle", row.GetProperty("file").GetString()!))));
    }

    [TestMethod]
    public void LabelsGlyphsAndIconPaintMatchTheRealProviderModes()
    {
        using var document = Read();
        foreach (var row in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var spec = row.GetProperty("spec"); var observed = row.GetProperty("measurement"); var mode = Mode(observed.GetProperty("mode").GetString()!);
            Assert.AreEqual(spec.TryGetProperty("name", out var name) ? name.GetString() : BleeckerThemeToggleModel.Label(mode), observed.GetProperty("label").GetString());
            Assert.AreEqual(spec.TryGetProperty("title", out var title) ? title.GetString() : BleeckerThemeToggleModel.Label(mode), observed.GetProperty("title").GetString());
            Assert.AreEqual(spec.GetProperty("state").GetString() == "pressed" ? 18 * .97 : 18, observed.GetProperty("icon").GetProperty("width").GetDouble(), .0001);
            Assert.AreEqual("1.5px", observed.GetProperty("strokeWidth").GetString());
            var paths = XElement.Parse("<Root>" + BleeckerThemeToggleModel.Glyph(mode) + "</Root>").Elements().ToArray();
            var source = observed.GetProperty("paths").EnumerateArray().ToArray(); Assert.AreEqual(source.Length, paths.Length);
            if (mode == BleeckerThemeMode.Dark) Assert.AreEqual(source[0].GetProperty("attributes").GetProperty("d").GetString(), (string?)paths[0].Attribute("Data"));
            if (mode == BleeckerThemeMode.Light)
            {
                for (var index = 1; index < paths.Length; index++) Assert.AreEqual(source[index].GetProperty("attributes").GetProperty("d").GetString(), (string?)paths[index].Attribute("Data"));
                Assert.AreEqual("4", source[0].GetProperty("attributes").GetProperty("r").GetString());
                Assert.AreEqual("12", source[0].GetProperty("attributes").GetProperty("cx").GetString());
                Assert.AreEqual("12", source[0].GetProperty("attributes").GetProperty("cy").GetString());
            }
            var rgba = observed.GetProperty("strokeRgba").EnumerateArray().Select(value => value.GetInt32()).ToArray();
            CollectionAssert.AreEqual(row.GetProperty("scheme").GetString() == "Light" ? new[] { 74, 85, 101, 255 } : new[] { 209, 213, 220, 255 }, rgba);
        }
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BleeckerThemeToggleModel.Next((BleeckerThemeMode)99));
    }

    [TestMethod]
    public void PhysicalSourceClickSpaceAndEnterKeepTheOriginalCycle()
    {
        using var document = Read(); var rows = document.RootElement.GetProperty("interactions").EnumerateArray().ToArray(); Assert.AreEqual(12, rows.Length);
        foreach (var row in rows)
        {
            var mode = Mode(row.GetProperty("mode").GetString()!); var overridden = row.GetProperty("overrideClick").GetBoolean(); var index = 0;
            foreach (var sample in row.GetProperty("samples").EnumerateArray())
            {
                index++; if (!overridden) mode = BleeckerThemeToggleModel.Next(mode);
                Assert.AreEqual(mode, Mode(sample.GetProperty("mode").GetString()!));
                Assert.AreEqual(BleeckerThemeToggleModel.Label(mode), sample.GetProperty("label").GetString());
                Assert.AreEqual(overridden ? index : 0, sample.GetProperty("overrideClicks").GetInt32());
                if (!overridden) Assert.AreEqual(mode.ToString().ToLowerInvariant(), sample.GetProperty("stored").GetString());
                var resolved = mode == BleeckerThemeMode.System ? row.GetProperty("scheme").GetString()! : mode.ToString();
                Assert.AreEqual(resolved.ToLowerInvariant(), sample.GetProperty("resolved").GetString());
            }
        }
    }
    private static string Hash(string file) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
}
