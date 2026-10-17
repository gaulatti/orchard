using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;

namespace Orchard.Tests;
[TestClass]
public sealed class IconBadgeContractTests
{
    private static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
    [TestMethod]
    public void FrozenSourceCoversSizesVariantsScalarOverflowAndPassiveSemantics()
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(Fixture("IconBadge.json")));
        var root = document.RootElement;
        Assert.AreEqual("a86059013cd59684ce452849278ea81e734f2066", root.GetProperty("sourceCommit").GetString());
        Assert.AreEqual("45701e66553a6c9b57e79dc0ec339adf6c6583abf75432fced09261bc7fefd0a", root.GetProperty("sourceSha256").GetString());
        Assert.AreEqual("155.0.4283.62", root.GetProperty("browser").GetString());
        var cases = root.GetProperty("cases").EnumerateArray().ToArray(); Assert.AreEqual(136, cases.Length);
        foreach (var scheme in new[] { "Light", "Dark" }) foreach (var motion in new[] { "reduce", "no-preference" })
        {
            var group = cases.Where(row => row.GetProperty("scheme").GetString() == scheme && row.GetProperty("motion").GetString() == motion).ToDictionary(row => row.GetProperty("id").GetString()!);
            Assert.AreEqual(34, group.Count);
            foreach (var size in new[] { "md", "lg" }) foreach (var variant in new[] { "primary", "subtle", "outlined" })
            {
                var measure = group[size + "-" + variant].GetProperty("measurement");
                Assert.AreEqual(size == "md" ? 56 : 64, measure.GetProperty("box").GetProperty("width").GetDouble());
                Assert.AreEqual(-1, measure.GetProperty("tabIndex").GetInt32());
                Assert.AreEqual("10px", measure.GetProperty("radius").GetString());
                Assert.AreEqual(0, measure.GetProperty("animations").GetInt32());
            }
            Assert.AreEqual(100, group["large-child"].GetProperty("measurement").GetProperty("children")[0].GetProperty("box").GetProperty("width").GetDouble());
            Assert.AreEqual(40, group["narrow-flex"].GetProperty("measurement").GetProperty("box").GetProperty("width").GetDouble());
            Assert.AreEqual(64, group["narrow-block"].GetProperty("measurement").GetProperty("box").GetProperty("width").GetDouble());
            foreach (var id in new[] { "boolean-true", "boolean-false", "empty" }) Assert.AreEqual("", group[id].GetProperty("measurement").GetProperty("text").GetString());
            Assert.AreEqual("0", group["number--0"].GetProperty("measurement").GetProperty("text").GetString());
        }
        var updates = root.GetProperty("interactions"); Assert.AreEqual(12, updates.GetArrayLength());
        foreach (var update in updates.EnumerateArray()) { Assert.IsTrue(update.GetProperty("childRetained").GetBoolean()); Assert.AreEqual("Retained", update.GetProperty("value").GetString()); }
    }
    [TestMethod]
    public void SourceCollectorsAndAllRasterBytesRemainReproducible()
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(Fixture("IconBadge.json")));
        foreach (var (file, field) in new[] { ("CaptureIconBadge.cjs", "scriptSha256"), ("IconBadgeClient.tsx", "clientSha256") })
            Assert.AreEqual(document.RootElement.GetProperty(field).GetString(), Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture(file)))));
        foreach (var row in document.RootElement.GetProperty("cases").EnumerateArray())
            Assert.AreEqual(row.GetProperty("pngSha256").GetString(), Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture(Path.Combine("IconBadge", row.GetProperty("file").GetString()!))))));
    }
    [TestMethod]
    public void IconBadgePalettesKeepExactFractionalAlphaAndSystemContrastColors()
    {
        XNamespace ns = "http://schemas.microsoft.com/winfx/2006/xaml/presentation", x = "http://schemas.microsoft.com/winfx/2006/xaml";
        Assert.IsTrue(XDocument.Load(Fixture("Library/Themes/Generic.xaml")).Descendants(ns + "ResourceDictionary").Any(element => (string?)element.Attribute("Source") == "ms-appx:///Orchard/Themes/IconBadges/Primary.g.xaml"), "The default style must resolve its palette during XAML startup.");
        foreach (var variant in new[] { "Primary", "Subtle", "Outlined" })
        {
            var dictionary = XDocument.Load(Fixture($"Library/Themes/IconBadges/{variant}.g.xaml"));
            foreach (var theme in new[] { "Light", "Default", "HighContrast" })
            {
                var colors = dictionary.Descendants(ns + "ResourceDictionary").Single(element => (string?)element.Attribute(x + "Key") == theme);
                Assert.AreEqual(3, colors.Elements(ns + "SolidColorBrush").Count());
                if (theme == "HighContrast") foreach (var brush in colors.Elements(ns + "SolidColorBrush"))
                {
                    StringAssert.StartsWith((string)brush.Attribute("Color")!, "{ThemeResource SystemColor"); Assert.AreEqual("1", (string?)brush.Attribute("Opacity"));
                }
                if (variant == "Subtle" && theme != "HighContrast")
                {
                    var fill = colors.Elements(ns + "SolidColorBrush").Single(element => (string?)element.Attribute(x + "Key") == "IconBadgeBackground");
                    Assert.AreEqual(theme == "Light" ? "0.06" : "0.1", (string?)fill.Attribute("Opacity"));
                }
            }
        }
    }
}
