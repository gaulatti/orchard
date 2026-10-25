using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Orchard.Tests;

[TestClass]
public sealed class TabsMotionTests
{
    [TestMethod]
    public void ProductionPaintClocksMatchAllFrozenBrowserTimelines()
    {
        using var document = Read(); var samples = 0;
        Assert.AreEqual(96, document.RootElement.GetProperty("transitions").GetArrayLength());
        foreach (var row in document.RootElement.GetProperty("transitions").EnumerateArray())
        {
            var reduced = row.GetProperty("reduced").GetBoolean();
            var context = $"{row.GetProperty("variant").GetString()}/{row.GetProperty("scheme").GetString()}/{row.GetProperty("kind").GetString()}";
            var animation = new BleeckerTabAnimation();
            animation.Retarget(From(row.GetProperty("before")), 0, false);
            animation.Retarget(From(row.GetProperty("after")), 0, !reduced);
            foreach (var definition in row.GetProperty("definitions").EnumerateArray())
                if (!reduced)
                {
                    Assert.AreEqual(190, definition.GetProperty("timing").GetProperty("duration").GetDouble(), context);
                    Assert.AreEqual("cubic-bezier(0.22, 1, 0.36, 1)", definition.GetProperty("timing").GetProperty("easing").GetString(), context);
                }
            foreach (var sample in row.GetProperty("samples").EnumerateArray())
            {
                var time = sample.GetProperty("milliseconds").GetDouble();
                // The source's reduced-motion floor can exist at time zero;
                // native reduced motion snaps before its first presented frame.
                if (reduced && time == 0) continue;
                var frame = animation.At(time);
                foreach (var (name, value) in new[] { ("foreground", frame.Foreground), ("background", frame.Background), ("border", frame.Border) })
                {
                    var wanted = Colour(sample.GetProperty(name).GetString()!); var expected = wanted.Encode(); var actual = value.Encode();
                    Assert.AreEqual(expected.R, actual.R, context + "/" + name + "/R/" + time);
                    Assert.AreEqual(expected.G, actual.G, context + "/" + name + "/G/" + time);
                    Assert.AreEqual(expected.B, actual.B, context + "/" + name + "/B/" + time);
                    // Legacy rgb() serialization quantizes alpha to a byte;
                    // oklab() retains six decimal places in the captured source.
                    if (wanted.Legacy) Assert.AreEqual(Math.Round(expected.Alpha * 255), Math.Round(actual.Alpha * 255), context + "/" + name + "/alpha/" + time);
                    else Assert.AreEqual(expected.Alpha, actual.Alpha, .000001, context + "/" + name + "/alpha/" + time);
                }
                var shadow = Shadow(sample.GetProperty("shadow").GetString()!);
                Assert.AreEqual(shadow.Y, frame.Shadow, .00001, context + "/shadow/y/" + time);
                Assert.AreEqual(shadow.Blur, 3 * frame.Shadow, .00001, context + "/shadow/blur/" + time);
                samples++;
            }
        }
        Assert.AreEqual(528, samples);
    }

    [TestMethod]
    public void NativeStateMappingRetainsEachSourcesInterpolationSpace()
    {
        using var document = Read(); var states = 0;
        foreach (var row in document.RootElement.GetProperty("transitions").EnumerateArray())
        {
            var variant = Enum.Parse<BleeckerTabsVariant>(row.GetProperty("variant").GetString()!, true);
            var kind = row.GetProperty("kind").GetString();
            foreach (var before in new[] { true, false })
            {
                var state = row.GetProperty(before ? "before" : "after");
                var hovered = before ? kind is "leave" or "press" or "release" : kind is "hover" or "select" or "press" or "release";
                var spaces = BleeckerTabAnimation.ColorSpaces(variant, state.GetProperty("active").GetBoolean(), hovered);
                Assert.AreEqual(Colour(state.GetProperty("background").GetString()!).Legacy, spaces.BackgroundLegacy, $"{variant}/{kind}/background/{before}");
                Assert.AreEqual(Colour(state.GetProperty("border").GetString()!).Legacy, spaces.BorderLegacy, $"{variant}/{kind}/border/{before}");
                states++;
            }
        }
        Assert.AreEqual(192, states);
    }

    [TestMethod]
    public void PressDisplacementIsImmediateAndUsesTheIndividualTranslateProperty()
    {
        using var document = Read(); var presses = 0;
        foreach (var row in document.RootElement.GetProperty("transitions").EnumerateArray().Where(row => row.GetProperty("kind").GetString() is "press" or "release"))
        {
            var pressed = row.GetProperty("kind").GetString() == "press";
            foreach (var sample in row.GetProperty("samples").EnumerateArray())
            {
                Assert.AreEqual(pressed ? "0px 1px" : "0px", sample.GetProperty("translate").GetString());
                Assert.AreEqual("none", sample.GetProperty("transform").GetString());
            }
            Assert.IsFalse(row.GetProperty("definitions").EnumerateArray().Any(definition => definition.GetProperty("property").GetString() is "translate" or "transform"));
            presses++;
        }
        Assert.AreEqual(24, presses);
    }

    [TestMethod]
    public void MotionReferenceRetainsOriginalSourceAndCollectorProvenance()
    {
        using var document = Read(); var root = document.RootElement;
        Assert.AreEqual("812294042eecf36f03ccfe890bac6f1542a6573c1fd8c24b42e963bb560c6827", root.GetProperty("sourceSha256").GetString());
        foreach (var (file, field) in new[] { ("Tabs.json", "normalReferenceSha256"), ("CaptureTabsMotion.cjs", "scriptSha256"), ("TabsMotionClient.tsx", "clientSha256") })
            Assert.AreEqual(root.GetProperty(field).GetString(), Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", file)))));
    }

    private static JsonDocument Read() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "TabsMotion.json")));
    private static BleeckerTabAnimation.Frame From(JsonElement value) => new(NativeColour(value.GetProperty("foreground").GetString()!),
        NativeColour(value.GetProperty("background").GetString()!), NativeColour(value.GetProperty("border").GetString()!), Shadow(value.GetProperty("shadow").GetString()!).Y);
    private static BleeckerCssColor NativeColour(string css) { var colour = Colour(css); return BleeckerCssColor.FromRgba(colour.Encode(), colour.Legacy); }
    private static BleeckerCssColor Colour(string css)
    {
        var values = Regex.Matches(css, @"[-+]?(?:\d*\.)?\d+(?:[eE][-+]?\d+)?").Select(match => double.Parse(match.Value, CultureInfo.InvariantCulture)).ToArray();
        if (css.StartsWith("oklab", StringComparison.Ordinal)) { var alpha = values.Length == 4 ? values[3] : 1; return new(false, values[0] * alpha, values[1] * alpha, values[2] * alpha, alpha); }
        return BleeckerCssColor.FromRgba(((byte)values[0], (byte)values[1], (byte)values[2], values.Length == 4 ? values[3] : 1), true);
    }
    private static (double Y, double Blur) Shadow(string css)
    {
        if (css == "none") return (0, 0);
        var match = Regex.Matches(css, @"rgba?\([^)]+\)\s+[-.\d]+px\s+([-.\d]+)px\s+([-.\d]+)px\s+[-.\d]+px").Last();
        return (double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture), double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture));
    }
}
