using System.Security.Cryptography;
using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class NavMenuContractTests
{
    private static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
    private static JsonDocument Reference() => JsonDocument.Parse(File.ReadAllBytes(Fixture("NavMenu.json")));
    [TestMethod]
    public void FrozenNavMenuBindsOriginalSourceScriptsAndAllUnmodifiedPngs()
    {
        using var reference = Reference(); var root = reference.RootElement;
        Assert.AreEqual("a86059013cd59684ce452849278ea81e734f2066", root.GetProperty("commit").GetString());
        Assert.AreEqual("1a9b3d9c7c7d43c52d23080c37af477211d37ed457f1c6a8eb2a18269b2f8cc5", root.GetProperty("sourceSha256").GetString());
        foreach (var (file, field) in new[] { ("CaptureNavMenu.cjs", "scriptSha256"), ("NavMenuClient.tsx", "clientSha256") }) Assert.AreEqual(root.GetProperty(field).GetString(), Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture(file)))));
        Assert.AreEqual(42, root.GetProperty("cases").GetArrayLength());
        foreach (var row in root.GetProperty("cases").EnumerateArray()) Assert.AreEqual(row.GetProperty("pngSha256").GetString(), Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture("NavMenu/" + row.GetProperty("file").GetString())))));
        Assert.AreEqual(8, root.GetProperty("interactions").GetArrayLength());
    }
    [TestMethod]
    public void FlexShrinkAndMinimumOverflowReachEveryCapturedHorizontalLinkWidth()
    {
        using var reference = Reference(); var checkedLinks = 0;
        foreach (var row in reference.RootElement.GetProperty("cases").EnumerateArray())
        {
            if (row.GetProperty("spec").GetProperty("direction").GetString() != "horizontal") continue;
            var links = row.GetProperty("measurement").GetProperty("links").EnumerateArray().ToArray();
            var widths = BleeckerNavMenuModel.Widths(links.Select(link => link.GetProperty("naturalWidth").GetDouble()).ToArray(), links.Select(link => link.GetProperty("minimumWidth").GetDouble()).ToArray(), row.GetProperty("measurement").GetProperty("root").GetProperty("width").GetDouble());
            for (var index = 0; index < links.Length; index++) { Assert.AreEqual(links[index].GetProperty("box").GetProperty("width").GetDouble(), widths[index], .001, row.GetProperty("id").GetString()); checkedLinks++; }
        }
        Assert.IsTrue(checkedLinks > 50);
    }
    [TestMethod]
    public void EffectiveGlobalColorTransitionMatchesTheRealPausedBrowserMidpoint()
    {
        static (byte R, byte G, byte B, double Alpha) Parse(string css)
        {
            var values = css[(css.IndexOf('(') + 1)..css.IndexOf(')')].Split(',').Select(value => byte.Parse(value.Trim())).ToArray(); return (values[0], values[1], values[2], 1);
        }
        using var reference = Reference(); var rows = 0;
        foreach (var row in reference.RootElement.GetProperty("normalMotion").EnumerateArray())
        {
            var start = Parse(row.GetProperty("initial").GetProperty("color").GetString()!); var end = Parse(row.GetProperty("hover").GetProperty("color").GetString()!); var midpoint = row.GetProperty("midpoint");
            Assert.AreEqual(220d, midpoint.GetProperty("duration").GetDouble());
            var motion = new BleeckerCssTransition<(double R, double G, double B, double Alpha)>(BleeckerSrgbColorMix.MixPremultiplied, BleeckerCssEasing.Default);
            motion.Retarget(BleeckerSrgbColorMix.Premultiply(start), 0, 220, false); motion.Retarget(BleeckerSrgbColorMix.Premultiply(end), 0, 220, true);
            Assert.AreEqual(Parse(midpoint.GetProperty("color").GetString()!), BleeckerSrgbColorMix.EncodePremultiplied(motion.ValueAt(midpoint.GetProperty("time").GetDouble()))); rows++;
        }
        Assert.AreEqual(2, rows);
    }
}
