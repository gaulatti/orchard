using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Orchard.Tests;

[TestClass]
public sealed class PaginationMotionTests
{
    private static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
    private static JsonDocument Read() => JsonDocument.Parse(File.ReadAllBytes(Fixture("PaginationMotion.json")));

    [TestMethod]
    public void ProductionColorAndOpacityClocksMatchTheOriginalBrowserTimelines()
    {
        using var reference = Read(); var samples = 0;
        foreach (var row in reference.RootElement.GetProperty("transitions").EnumerateArray())
        {
            var reduced = row.GetProperty("reduced").GetBoolean(); var context = $"{row.GetProperty("scheme")}/{row.GetProperty("role")}/{row.GetProperty("kind")}/{reduced}";
            var animation = new BleeckerPaginationAnimation(); animation.Retarget(From(row.GetProperty("before")), 0, false); animation.Retarget(From(row.GetProperty("after")), 0, !reduced);
            foreach (var sample in row.GetProperty("samples").EnumerateArray())
            {
                var time = sample.GetProperty("milliseconds").GetDouble(); if (reduced && time == 0) continue;
                var frame = animation.At(time);
                foreach (var (name, actualColor) in new[] { ("foreground", frame.Foreground), ("background", frame.Background), ("border", frame.Border) })
                {
                    var wanted = Colour(sample.GetProperty(name).GetString()!); var expected = wanted.Encode(); var actual = actualColor.Encode(); var label = context + "/" + name + "/" + time;
                    Assert.AreEqual(expected.R, actual.R, label + "/R"); Assert.AreEqual(expected.G, actual.G, label + "/G"); Assert.AreEqual(expected.B, actual.B, label + "/B");
                    if (wanted.Legacy) Assert.AreEqual(Math.Round(expected.Alpha * 255), Math.Round(actual.Alpha * 255), label + "/alpha");
                    else Assert.AreEqual(expected.Alpha, actual.Alpha, .000001, label + "/alpha");
                }
                Assert.AreEqual(double.Parse(sample.GetProperty("opacity").GetString()!, CultureInfo.InvariantCulture), frame.Opacity, .000001, context + "/opacity/" + time); samples++;
            }
        }
        Assert.AreEqual(352, samples);
    }

    [TestMethod]
    public void NativeStateMappingKeepsEachSourcesInterpolationSpace()
    {
        using var reference = Read(); var states = 0;
        foreach (var row in reference.RootElement.GetProperty("transitions").EnumerateArray()) foreach (var before in new[] { true, false })
        {
            var role = row.GetProperty("role").GetString(); var kind = row.GetProperty("kind").GetString();
            var hovered = before ? kind is "leave" or "press" or "release" : kind is "hover" or "press" or "release";
            var selected = role == "selected" || (role == "page" && (before ? kind == "deselect" : kind == "select"));
            var spaces = BleeckerPaginationAnimation.ColorSpaces(selected, hovered, role == "previous"); var state = row.GetProperty(before ? "before" : "after");
            Assert.AreEqual(Colour(state.GetProperty("background").GetString()!).Legacy, spaces.BackgroundLegacy);
            Assert.AreEqual(Colour(state.GetProperty("border").GetString()!).Legacy, spaces.BorderLegacy); states++;
        }
        Assert.AreEqual(128, states);
    }

    [TestMethod]
    public void SourceReferenceAndImmediateScaleRemainBound()
    {
        using var reference = Read(); var root = reference.RootElement; Assert.AreEqual(64, root.GetProperty("transitions").GetArrayLength());
        Assert.AreEqual(190, root.GetProperty("sourceColorAndOpacityDuration").GetDouble()); Assert.IsFalse(root.GetProperty("sourceScaleAnimated").GetBoolean());
        foreach (var (name, field) in new[] { ("Pagination.json", "referenceSha256"), ("CapturePaginationMotion.cjs", "scriptSha256"), ("PaginationClient.tsx", "clientSha256") })
            Assert.AreEqual(root.GetProperty(field).GetString(), Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture(name)))));
        foreach (var row in root.GetProperty("transitions").EnumerateArray().Where(row => row.GetProperty("kind").GetString() is "press" or "release"))
        {
            Assert.IsFalse(row.GetProperty("definitions").EnumerateArray().Any(definition => definition.GetProperty("property").GetString() is "transform" or "scale"));
            foreach (var sample in row.GetProperty("samples").EnumerateArray()) Assert.AreEqual(row.GetProperty("after").GetProperty("scale").GetString(), sample.GetProperty("scale").GetString());
        }
    }

    private static BleeckerPaginationAnimation.Frame From(JsonElement value) => new(NativeColour(value.GetProperty("foreground").GetString()!), NativeColour(value.GetProperty("background").GetString()!), NativeColour(value.GetProperty("border").GetString()!), double.Parse(value.GetProperty("opacity").GetString()!, CultureInfo.InvariantCulture));
    private static BleeckerCssColor NativeColour(string css) { var color = Colour(css); return BleeckerCssColor.FromRgba(color.Encode(), color.Legacy); }
    private static BleeckerCssColor Colour(string css)
    {
        var values = Regex.Matches(css, @"[-+]?(?:\d*\.)?\d+(?:[eE][-+]?\d+)?").Select(match => double.Parse(match.Value, CultureInfo.InvariantCulture)).ToArray();
        if (css.StartsWith("oklab", StringComparison.Ordinal)) { var alpha = values.Length == 4 ? values[3] : 1; return new(false, values[0] * alpha, values[1] * alpha, values[2] * alpha, alpha); }
        if (!css.StartsWith("rgb", StringComparison.Ordinal)) throw new InvalidOperationException("Unsupported pinned source color: " + css);
        return BleeckerCssColor.FromRgba(((byte)values[0], (byte)values[1], (byte)values[2], values.Length == 4 ? values[3] : 1), true);
    }
}
