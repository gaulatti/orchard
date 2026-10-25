using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class ScrollAreaReferenceTests
{
    private static string Fixture(string path) => Path.Combine(AppContext.BaseDirectory, "Fixtures", path);
    private static JsonDocument Reference() => JsonDocument.Parse(File.ReadAllBytes(Fixture("ScrollArea.json")));
    private static double N(JsonElement row, string name, double fallback) => row.TryGetProperty(name, out var value) ? value.GetDouble() : fallback;
    private static bool F(JsonElement row, string name) => row.TryGetProperty(name, out var value) && value.GetBoolean();
    private static BleeckerScrollAreaOrientation Orientation(JsonElement spec) => spec.TryGetProperty("orientation", out var value) ? Enum.Parse<BleeckerScrollAreaOrientation>(value.GetString()!, true) : BleeckerScrollAreaOrientation.Vertical;
    private static double Maximum(JsonElement spec)
    {
        var has = spec.TryGetProperty("styleMaxHeight", out var value); if (!has) has = spec.TryGetProperty("maxHeight", out value);
        if (!has) return 240; if (value.ValueKind == JsonValueKind.Null) return double.PositiveInfinity;
        var text = value.GetString()!;
        if (text.EndsWith("px", StringComparison.Ordinal)) return double.Parse(text[..^2], CultureInfo.InvariantCulture);
        if (text.EndsWith("rem", StringComparison.Ordinal)) return double.Parse(text[..^3], CultureInfo.InvariantCulture) * 16;
        if (text.EndsWith('%')) return double.Parse(text[..^1], CultureInfo.InvariantCulture) / 100 * N(spec, "parentHeight", 0);
        throw new FormatException("Unsupported frozen fixture length.");
    }
    private static BleeckerScrollAreaModel.Metrics Measure(JsonElement spec)
    {
        var custom = F(spec, "custom"); var padding = custom ? new BleeckerScrollAreaModel.Insets(8, 8, 8, 8) : default; var border = custom ? new BleeckerScrollAreaModel.Insets(1, 1, 1, 1) : default;
        double? contentWidth = F(spec, "empty") ? 0 : spec.TryGetProperty("contentWidth", out var width) && width.ValueKind == JsonValueKind.Null ? null : N(spec, "contentWidth", 520);
        var orientation = spec.TryGetProperty("styleOverflow", out _) ? BleeckerScrollAreaOrientation.Both : Orientation(spec);
        return BleeckerScrollAreaModel.Measure(N(spec, "width", 256), contentWidth, F(spec, "empty") ? 0 : N(spec, "contentHeight", 480), Maximum(spec), orientation, F(spec, "rtl"), padding, border, spec.TryGetProperty("height", out var height) ? height.GetDouble() : null);
    }

    [TestMethod]
    public void SourceReferenceBindsVisibleScrollbarLaunchAndAllFrozenInputsAndImages()
    {
        using var document = Reference(); var root = document.RootElement;
        Assert.AreEqual("orchard-scroll-area-reference-v1", root.GetProperty("format").GetString()); Assert.AreEqual("a86059013cd59684ce452849278ea81e734f2066", root.GetProperty("commit").GetString());
        Assert.AreEqual("20c254f224ec44aa8e965674435f40e7f5fc05ce4bdd8abe3c4dc443df5cd78f", root.GetProperty("sourceSha256").GetString());
        CollectionAssert.AreEqual(new[] { "--hide-scrollbars" }, root.GetProperty("ignoredDefaultArgs").EnumerateArray().Select(v => v.GetString()!).ToArray());
        Assert.AreEqual(94, root.GetProperty("cases").GetArrayLength()); Assert.AreEqual(12, root.GetProperty("paints").GetArrayLength()); Assert.AreEqual(16, root.GetProperty("interactions").GetArrayLength());
        foreach (var (file, field) in new[] { ("CaptureScrollArea.cjs", "scriptSha256"), ("ScrollAreaClient.tsx", "clientSha256") }) Assert.AreEqual(root.GetProperty(field).GetString(), Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture(file)))));
        foreach (var row in root.GetProperty("cases").EnumerateArray().Concat(root.GetProperty("paints").EnumerateArray()))
        {
            var bytes = File.ReadAllBytes(Fixture("ScrollArea/" + row.GetProperty("file").GetString())); Assert.AreEqual(row.GetProperty("pngSha256").GetString(), Convert.ToHexStringLower(SHA256.HashData(bytes)));
            Assert.AreEqual(800, BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4))); Assert.AreEqual(700, BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4)));
        }
    }

    [TestMethod]
    public void ProductionViewportModelMatchesAllAxesGuttersMaximumPrecedenceAndBoxGeometry()
    {
        using var document = Reference();
        foreach (var row in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var spec = row.GetProperty("spec"); var m = row.GetProperty("measurement"); var actual = Measure(spec); var id = row.GetProperty("scheme").GetString() + "/" + row.GetProperty("id").GetString();
            Assert.AreEqual(N(m.GetProperty("area"), "width", 0), actual.Width, .000001, id); Assert.AreEqual(N(m.GetProperty("area"), "height", 0), actual.Height, .000001, id);
            Assert.AreEqual(N(m.GetProperty("client"), "width", 0), actual.DomClientWidth, .000001, id); Assert.AreEqual(N(m.GetProperty("client"), "height", 0), actual.DomClientHeight, .000001, id);
            Assert.AreEqual(N(m.GetProperty("client"), "left", 0), actual.ClientLeft, .000001, id); Assert.AreEqual(N(m.GetProperty("client"), "top", 0), actual.ClientTop, .000001, id);
            Assert.AreEqual(N(m.GetProperty("scroll"), "width", 0), actual.DomExtentWidth, .000001, id); Assert.AreEqual(N(m.GetProperty("scroll"), "height", 0), actual.DomExtentHeight, .000001, id);
            if (!F(spec, "empty"))
            {
                var padding = F(spec, "custom") ? new BleeckerScrollAreaModel.Insets(8, 8, 8, 8) : default; var origin = BleeckerScrollAreaModel.ContentOrigin(actual, padding, F(spec, "rtl")); var content = m.GetProperty("content");
                Assert.AreEqual(N(content, "width", 0), actual.ContentWidth, .000001, id); Assert.AreEqual(N(content, "height", 0), actual.ContentHeight, .000001, id);
                Assert.AreEqual(N(content, "x", 0), N(m.GetProperty("area"), "x", 0) + origin.X, .000001, id); Assert.AreEqual(N(content, "y", 0), N(m.GetProperty("area"), "y", 0) + origin.Y, .000001, id);
            }
        }
    }

    [TestMethod]
    public void ProgrammaticHiddenAxesRetainBothRangesAndRtlNegativeOrigin()
    {
        using var document = Reference();
        foreach (var row in document.RootElement.GetProperty("interactions").EnumerateArray().Where(r => !r.GetProperty("id").GetString()!.StartsWith("thumb", StringComparison.Ordinal)))
        {
            var orientation = Enum.Parse<BleeckerScrollAreaOrientation>(row.GetProperty("orientation").GetString()!, true); var rtl = row.GetProperty("rtl").GetBoolean(); var metrics = BleeckerScrollAreaModel.Measure(256, 520, 480, 240, orientation, rtl);
            var samples = row.GetProperty("samples").EnumerateArray().ToDictionary(s => s.GetProperty("label").GetString()!);
            foreach (var (label, x, y) in new[] { ("programmatic-positive", 80d, 120d), ("programmatic-negative", -80d, 120d), ("end-positive", 1e6, 1e6), ("end-negative", -1e6, 1e6) })
            {
                var expected = samples[label].GetProperty("measurement").GetProperty("scroll"); var actual = BleeckerScrollAreaModel.Clamp(metrics, x, y, rtl); Assert.AreEqual(N(expected, "left", 0), actual.Left, .000001); Assert.AreEqual(N(expected, "top", 0), actual.Top, .000001);
            }
            var wheel = samples["vertical-wheel"].GetProperty("measurement").GetProperty("scroll"); Assert.AreEqual(BleeckerScrollAreaModel.UserVertical(orientation) ? 120 : 0, N(wheel, "top", 0));
            var arrow = samples["arrow-down"].GetProperty("measurement").GetProperty("scroll"); Assert.AreEqual(BleeckerScrollAreaModel.UserVertical(orientation) ? 40 : 0, N(arrow, "top", 0));
        }
    }

    [TestMethod]
    public void SourceOriginalEditingNotificationsAndCallerMetadataSurviveLayoutChanges()
    {
        using var document = Reference();
        foreach (var row in document.RootElement.GetProperty("interactions").EnumerateArray().Where(r => r.TryGetProperty("retained", out _)))
        {
            var retained = row.GetProperty("retained"); Assert.IsTrue(retained.GetProperty("original").GetBoolean()); Assert.AreEqual("Caller edit", retained.GetProperty("text").GetString()); Assert.AreEqual(2, retained.GetProperty("start").GetInt32()); Assert.AreEqual(5, retained.GetProperty("end").GetInt32()); Assert.IsTrue(row.GetProperty("scrollCallbackObserved").GetBoolean());
            var last = row.GetProperty("samples").EnumerateArray().Last().GetProperty("measurement"); Assert.AreEqual("editor", last.GetProperty("focused").GetString()); Assert.AreEqual(0, N(last.GetProperty("scroll"), "left", 0)); Assert.AreEqual(0, N(last.GetProperty("scroll"), "top", 0));
            Assert.AreEqual("region", last.GetProperty("role").GetString()); Assert.AreEqual("Fictional scroll area", last.GetProperty("name").GetString()); Assert.AreEqual("original-scroll-area", last.GetProperty("marker").GetString()); Assert.AreEqual(0, last.GetProperty("tabIndex").GetInt32());
        }
    }

    [TestMethod]
    public void SourceScrollbarRecipeAndDragRemainDistinctFromAComponentMotionPolicy()
    {
        using var document = Reference(); var root = document.RootElement;
        foreach (var row in root.GetProperty("cases").EnumerateArray())
        {
            var m = row.GetProperty("measurement"); Assert.AreEqual("6px", m.GetProperty("scrollbar").GetProperty("width").GetString()); Assert.AreEqual("auto", m.GetProperty("scrollbar").GetProperty("height").GetString()); Assert.AreEqual("rgba(0, 0, 0, 0)", m.GetProperty("track").GetProperty("background").GetString());
            Assert.AreEqual("auto", m.GetProperty("scrollBehavior").GetString()); Assert.AreEqual("border-box", m.GetProperty("boxSizing").GetString());
        }
        foreach (var row in root.GetProperty("interactions").EnumerateArray().Where(r => r.GetProperty("id").GetString()!.StartsWith("thumb", StringComparison.Ordinal)))
        {
            var after = row.GetProperty("after").GetProperty("scroll"); var vertical = row.GetProperty("orientation").GetString() == "vertical"; Assert.AreEqual(vertical ? 120 : 0, N(after, "top", 0)); Assert.AreEqual(vertical ? 0 : 121, N(after, "left", 0));
        }
        foreach (var row in root.GetProperty("motionStyles").EnumerateArray()) Assert.AreEqual("auto", row.GetProperty("scrollBehavior").GetString());
    }
}
