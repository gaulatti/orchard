using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;
using Orchard;

namespace Orchard.Tests;
[TestClass]
public sealed class EmptyReferenceTests
{
    private static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
    [TestMethod]
    public void EmptyPaletteAndSurfaceShadowsKeepTheSourceRecipes()
    {
        Assert.AreEqual(2,BleeckerTokens.SurfaceShadows.Count);
        Assert.AreEqual((0f,1f,2f,0f,.025f,(byte)26,(byte)55,(byte)77),BleeckerTokens.SurfaceShadows[0]);
        Assert.AreEqual((0f,10f,30f,-18f,.18f,(byte)26,(byte)55,(byte)77),BleeckerTokens.SurfaceShadows[1]);
        XNamespace ns="http://schemas.microsoft.com/winfx/2006/xaml/presentation",x="http://schemas.microsoft.com/winfx/2006/xaml";
        var palette=XDocument.Load(Fixture("Library/Themes/EmptyPalette.g.xaml"));
        foreach(var theme in new[]{"Light","Default","HighContrast"})
        {
            var colors=palette.Descendants(ns+"ResourceDictionary").Single(element=>(string?)element.Attribute(x+"Key")==theme);
            Assert.AreEqual(7,colors.Elements(ns+"SolidColorBrush").Count());
            var fill=colors.Elements(ns+"SolidColorBrush").Single(element=>(string?)element.Attribute(x+"Key")=="EmptyBackground");
            Assert.AreEqual(theme=="Light"?"0.25":theme=="Default"?"0.025":"1",(string?)fill.Attribute("Opacity"));
            if(theme=="HighContrast")foreach(var brush in colors.Elements(ns+"SolidColorBrush")){StringAssert.StartsWith((string)brush.Attribute("Color")!,"{ThemeResource SystemColor");Assert.AreEqual("1",(string?)brush.Attribute("Opacity"));}
        }
    }
    [TestMethod]
    public void SourceEmptyDistinguishesNullishIconsTruthyActionsAndWhitespace()
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(Fixture("Empty.json")));
        var root = document.RootElement;
        Assert.AreEqual("d11e150b9241e508fa81b769e645469db54838bb96530f640ee2cea978f85209", root.GetProperty("sourceSha256").GetString());
        Assert.AreEqual(120, root.GetProperty("cases").GetArrayLength());
        foreach (var scheme in new[] { "Light", "Dark" }) foreach (var motion in new[] { "reduce", "no-preference" })
        {
            var cases = root.GetProperty("cases").EnumerateArray().Where(row => row.GetProperty("scheme").GetString() == scheme && row.GetProperty("motion").GetString() == motion).ToDictionary(row => row.GetProperty("id").GetString()!, row => row);
            Assert.AreEqual(30, cases.Count);
            JsonElement Measure(string id) => cases[id].GetProperty("measurement");
            Assert.AreEqual(230d, Measure("defaults").GetProperty("root").GetProperty("box").GetProperty("height").GetDouble());
            Assert.AreEqual(56d, Measure("defaults").GetProperty("icon").GetProperty("box").GetProperty("width").GetDouble());
            Assert.AreEqual("600", Measure("defaults").GetProperty("title").GetProperty("fontWeight").GetString());
            Assert.AreEqual(cases["defaults"].GetProperty("pngSha256").GetString(), cases["icon-null"].GetProperty("pngSha256").GetString());
            foreach (var id in new[] { "icon-false", "icon-true" }) Assert.AreEqual(0, Measure(id).GetProperty("iconChildren").GetArrayLength());
            Assert.AreEqual("0", Measure("icon-zero").GetProperty("icon").GetProperty("text").GetString());
            foreach (var id in new[] { "action-zero", "action-nan", "action-false" }) Assert.AreEqual(JsonValueKind.Null, Measure(id).GetProperty("action").ValueKind);
            Assert.AreEqual(0d, Measure("action-true").GetProperty("action").GetProperty("box").GetProperty("height").GetDouble());
            Assert.AreEqual(254d, Measure("action-true").GetProperty("root").GetProperty("box").GetProperty("height").GetDouble());
            Assert.AreEqual(0d, Measure("space-description").GetProperty("description").GetProperty("box").GetProperty("height").GetDouble());
            Assert.AreEqual(30d, Measure("space-description").GetProperty("body").GetProperty("box").GetProperty("height").GetDouble());
            Assert.AreEqual(206d, Measure("empty-title").GetProperty("root").GetProperty("box").GetProperty("height").GetDouble());
            Assert.AreEqual(66d, Measure("width-0").GetProperty("root").GetProperty("box").GetProperty("width").GetDouble());
            Assert.IsGreaterThan(66d, Measure("width-0").GetProperty("body").GetProperty("box").GetProperty("width").GetDouble());
        }
        foreach (var row in root.GetProperty("interactions").EnumerateArray())
        {
            Assert.IsTrue(row.GetProperty("retained").GetBoolean()); Assert.AreEqual("Typed state", row.GetProperty("value").GetString());
        }
    }
    [TestMethod]
    public void SourceEmptyRetainsExactCollectorFontAndRasterProvenance()
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(Fixture("Empty.json")));
        var root = document.RootElement;
        foreach (var (file, field) in new[] { ("CaptureEmpty.cjs", "scriptSha256"), ("EmptyClient.tsx", "clientSha256") })
            Assert.AreEqual(root.GetProperty(field).GetString(), Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture(file)))));
        foreach (var row in root.GetProperty("cases").EnumerateArray())
            Assert.AreEqual(row.GetProperty("pngSha256").GetString(), Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture(Path.Combine("Empty", row.GetProperty("file").GetString()!))))));
        Assert.AreEqual("155.0.4283.62", root.GetProperty("browser").GetString());
        Assert.AreEqual(3, root.GetProperty("fonts").GetArrayLength());
        Assert.AreEqual(1, root.GetProperty("fonts").EnumerateArray().Count(font => font.GetProperty("weight").GetInt32() == 600));
    }
}
