using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;

namespace Orchard.Tests;

// These tests protect measured source behavior. A passing source fixture does
// not demonstrate a native Header implementation or component acceptance.
[TestClass]
public sealed class HeaderReferenceTests
{
    private static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
    private static JsonDocument Reference() => JsonDocument.Parse(File.ReadAllBytes(Fixture("Header.json")));
    private static double Number(JsonElement node, string key, double otherwise) => node.TryGetProperty(key, out var value) ? value.GetDouble() : otherwise;
    private static bool Flag(JsonElement node, string key) => node.TryGetProperty(key, out var value) && value.GetBoolean();
    private static double Dimension(JsonElement measurement, string node, string dimension) => measurement.GetProperty(node).GetProperty(dimension).GetDouble();
    private static string Paint(JsonElement measurement, string node, string field) => measurement.GetProperty("paint").GetProperty(node).GetProperty(field).GetString()!;

    [TestMethod]
    public void SourceHeaderOverlaysAppShellWhileOriginalBodyAndFooterKeepTheirFlow()
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(Fixture("HeaderComposition.json")));
        var root = document.RootElement;
        Assert.AreEqual("orchard-header-composition-reference-v1", root.GetProperty("format").GetString());
        Assert.AreEqual("a86059013cd59684ce452849278ea81e734f2066", root.GetProperty("commit").GetString());
        var inputs = root.GetProperty("dependencies").EnumerateArray().ToDictionary(row => row.GetProperty("path").GetString()!, row => row.GetProperty("sha256").GetString());
        Assert.AreEqual("e6d0c2969a7f042e13cf3261043c74dc8ae9c7caf9c7225db9271ea3d28bd009", inputs["src/layout/header.tsx"]);
        Assert.AreEqual("21cbb676b184bcad28ffb5577b19a1ccc34d18ab420221b08cf936a23919c3e1", inputs["src/layout/app-shell.tsx"]);
        foreach (var (file, field) in new[] { ("CaptureHeaderComposition.cjs", "scriptSha256"), ("HeaderCompositionClient.tsx", "clientSha256") })
            Assert.AreEqual(root.GetProperty(field).GetString(), Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture(file)))));
        var cases = root.GetProperty("cases").EnumerateArray().ToArray(); Assert.AreEqual(6, cases.Length);
        foreach (var scheme in new[] { "Light", "Dark" })
            CollectionAssert.AreEquivalent(new[] { "wide", "narrow", "narrow-open" }, cases.Where(row => row.GetProperty("scheme").GetString() == scheme).Select(row => row.GetProperty("id").GetString()).ToArray());
        foreach (var row in cases)
        {
            var measurement = row.GetProperty("measurement"); var width = row.GetProperty("spec").GetProperty("width").GetInt32();
            Assert.AreEqual(0, Dimension(measurement, "header", "y")); Assert.AreEqual(73, Dimension(measurement, "header", "height"));
            Assert.AreEqual(0, Dimension(measurement, "main", "y")); Assert.AreEqual(868, Dimension(measurement, "main", "height"));
            Assert.AreEqual(72, Dimension(measurement, "content", "y")); Assert.AreEqual(796, Dimension(measurement, "content", "height"));
            Assert.AreEqual(72, Dimension(measurement, "body", "y")); Assert.AreEqual(868, Dimension(measurement, "footer", "y"));
            Assert.IsTrue(measurement.GetProperty("headerOwnsHit").GetBoolean()); Assert.IsTrue(measurement.GetProperty("originalBody").GetBoolean());
            Assert.AreEqual("Original body edited", measurement.GetProperty("text").GetString()); Assert.AreEqual(2, measurement.GetProperty("selectionStart").GetInt32()); Assert.AreEqual(3, measurement.GetProperty("selectionLength").GetInt32());
            var bytes = File.ReadAllBytes(Fixture("HeaderComposition/" + row.GetProperty("file").GetString()));
            Assert.AreEqual(row.GetProperty("pngSha256").GetString(), Convert.ToHexStringLower(SHA256.HashData(bytes)));
            Assert.AreEqual(width, BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4))); Assert.AreEqual(900, BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4)));
        }
    }

    [TestMethod]
    public void FrozenHeaderBindsSourceCaptureDependenciesAndEveryFullClientImage()
    {
        using var reference = Reference(); var root = reference.RootElement;
        Assert.AreEqual("orchard-header-reference-v1", root.GetProperty("format").GetString());
        Assert.AreEqual("a86059013cd59684ce452849278ea81e734f2066", root.GetProperty("commit").GetString());
        Assert.AreEqual("src/layout/header.tsx", root.GetProperty("source").GetString());
        Assert.AreEqual("e6d0c2969a7f042e13cf3261043c74dc8ae9c7caf9c7225db9271ea3d28bd009", root.GetProperty("sourceSha256").GetString());
        foreach (var (file, field) in new[] { ("CaptureHeader.cjs", "scriptSha256"), ("HeaderClient.tsx", "clientSha256") })
            Assert.AreEqual(root.GetProperty(field).GetString(), Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture(file)))));
        var required = new[] { "src/layout/header.tsx", "src/components/brand-lockup.tsx", "src/components/nav-menu.tsx", "src/components/icon-button.tsx", "src/styles/global.css", "src/styles/tokens.generated.css", "stories/layout/Header.stories.tsx", "package-lock.json" };
        var dependencies = root.GetProperty("dependencies").EnumerateArray().Select(row => row.GetProperty("path").GetString()).ToArray();
        foreach (var path in required) CollectionAssert.Contains(dependencies, path);
        CollectionAssert.AreEquivalent(new[] { "react", "react-dom", "clsx", "tailwind-merge", "lucide-react" }, root.GetProperty("modules").EnumerateArray().Select(row => row.GetProperty("name").GetString()).ToArray());
        Assert.AreEqual(4, root.GetProperty("fonts").GetArrayLength());
        Assert.AreEqual("en-US", root.GetProperty("locale").GetString());
        Assert.AreEqual("America/New_York", root.GetProperty("timezone").GetString());
        Assert.AreEqual("reduce", root.GetProperty("motion").GetString());
        var cases = root.GetProperty("cases"); Assert.AreEqual(72, cases.GetArrayLength());
        foreach (var row in cases.EnumerateArray())
        {
            var bytes = File.ReadAllBytes(Fixture("Header/" + row.GetProperty("file").GetString()));
            Assert.AreEqual(row.GetProperty("pngSha256").GetString(), Convert.ToHexStringLower(SHA256.HashData(bytes)));
            CollectionAssert.AreEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, bytes[..8]);
            Assert.AreEqual(row.GetProperty("spec").GetProperty("viewportWidth").GetInt32(), BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4)));
            Assert.AreEqual(900, BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4)));
        }
        foreach (var group in cases.EnumerateArray().GroupBy(row => row.GetProperty("id").GetString()))
            CollectionAssert.AreEquivalent(new[] { "Light", "Dark" }, group.Select(row => row.GetProperty("scheme").GetString()).ToArray());
    }

    [TestMethod]
    public void SourceFixedStaticAndStickyPositionsPreserveTheirOwnFlowAndViewportContracts()
    {
        using var reference = Reference();
        foreach (var row in reference.RootElement.GetProperty("cases").EnumerateArray())
        {
            var spec = row.GetProperty("spec"); var measured = row.GetProperty("measurement");
            var position = spec.TryGetProperty("position", out var value) ? value.GetString() : "fixed";
            var height = Number(spec, "headerHeight", 72) + 1;
            var hostY = Dimension(measured, "host", "y");
            var expectedY = position switch { "fixed" => 0, "sticky" => Math.Min(Math.Max(hostY, 0), hostY + Dimension(measured, "host", "height") - height), _ => hostY };
            Assert.AreEqual(expectedY, Dimension(measured, "header", "y"), .000001, row.GetProperty("id").GetString());
            var nativePosition = Enum.Parse<BleeckerHeaderPosition>(position!, true);
            Assert.AreEqual(expectedY - hostY, BleeckerHeaderModel.Shift(nativePosition, hostY, hostY + Dimension(measured, "host", "height"), height), .000001);
            Assert.AreEqual(height, Dimension(measured, "header", "height"), .000001);
            Assert.AreEqual(position == "fixed" ? spec.GetProperty("viewportWidth").GetDouble() : Dimension(measured, "host", "width"), Dimension(measured, "header", "width"), .000001);
            Assert.AreEqual(Dimension(measured, "host", "x"), Dimension(measured, "header", "x"), .000001);
            Assert.AreEqual(hostY + (position == "fixed" ? 0 : height), Dimension(measured, "following", "y"), .000001);
        }
    }

    [TestMethod]
    public void SourceUsesViewportBreakpointsAndClipsMobileContentWithoutChangingHeaderFlow()
    {
        using var reference = Reference();
        foreach (var row in reference.RootElement.GetProperty("cases").EnumerateArray())
        {
            var spec = row.GetProperty("spec"); var m = row.GetProperty("measurement");
            var viewport = spec.GetProperty("viewportWidth").GetDouble(); var desktop = viewport >= 1024; var open = Flag(spec, "open");
            var maximum = viewport >= 1536 ? 1536 : viewport >= 1280 ? 1280 : viewport >= 1024 ? 1024 : viewport >= 768 ? 768 : viewport >= 640 ? 640 : double.PositiveInfinity;
            var width = Dimension(m, "header", "width"); var container = Flag(spec, "fullWidth") ? width : Math.Min(width, maximum);
            Assert.AreEqual(container, Dimension(m, "container", "width"), .000001);
            Assert.AreEqual(container, BleeckerHeaderModel.Container(viewport, width, Flag(spec, "fullWidth")), .000001);
            Assert.AreEqual(Dimension(m, "header", "x") + (width - container) / 2, Dimension(m, "container", "x"), .000001);
            Assert.AreEqual(desktop ? "grid" : "flex", Paint(m, "row", "display"));
            Assert.AreEqual(desktop ? "none" : "block", Paint(m, "panel", "display"));
            Assert.AreEqual(open ? "384px" : "0px", Paint(m, "panel", "maxHeight"));
            Assert.AreEqual(open ? "1" : "0", Paint(m, "panel", "opacity"));
            Assert.AreEqual(desktop ? 0 : 40, Dimension(m, "toggle", "height"), .000001);
            if (desktop) Assert.AreEqual(0, Dimension(m, "panel", "height"));
            else
            {
                Assert.AreEqual(Dimension(m, "header", "y") + Number(spec, "headerHeight", 72), Dimension(m, "panel", "y"), .000001);
                Assert.AreEqual(open ? Math.Min(384, Dimension(m, "mobileContent", "height") + 1) : 1, Dimension(m, "panel", "height"), .000001);
                Assert.AreEqual(Dimension(m, "panel", "height"), BleeckerHeaderModel.MenuHeight(Dimension(m, "mobileContent", "height") + 1, open ? 384 : 0), .000001);
                Assert.IsTrue(Dimension(m, "mobileContent", "height") > 0, "Closed source children stay mounted behind the max-height/opacity clip.");
            }
        }
    }

    [TestMethod]
    public void SourceRetainsCallerEditsAndOpenStateAcrossDesktopResizeAndItemReordering()
    {
        using var reference = Reference(); var histories = reference.RootElement.GetProperty("interactions");
        Assert.AreEqual(4, histories.GetArrayLength());
        foreach (var history in histories.EnumerateArray())
        {
            var samples = history.GetProperty("samples").EnumerateArray().ToArray();
            CollectionAssert.AreEqual(new[] { "closed", "open-edited", "desktop-hidden", "narrow-restored", "reorder", "item-closes" }, samples.Select(sample => sample.GetProperty("action").GetString()).ToArray());
            foreach (var (sample, index) in samples.Select((sample, index) => (sample, index)))
            {
                var m = sample.GetProperty("measurement"); var input = m.GetProperty("inputs")[1];
                Assert.AreEqual(index == 0 ? "Original" : "Caller text", input.GetProperty("value").GetString());
                CollectionAssert.AreEqual(index == 0 ? new[] { 8, 8 } : new[] { 2, 5 }, input.GetProperty("selection").EnumerateArray().Select(value => value.GetInt32()).ToArray());
                Assert.AreEqual(index == 1, input.GetProperty("focused").GetBoolean());
                Assert.AreEqual(index is 0 or 5 ? "0px" : "384px", Paint(m, "panel", "maxHeight"));
                Assert.AreEqual(index == 2 ? "none" : "block", Paint(m, "panel", "display"));
                Assert.AreEqual("Original", m.GetProperty("inputs")[0].GetProperty("value").GetString());
            }
            Assert.AreEqual(261, Dimension(samples[3].GetProperty("measurement"), "panel", "height"));
            var requests = samples[^1].GetProperty("requests");
            Assert.AreEqual(history.GetProperty("custom").GetBoolean() ? 1 : 0, requests.GetArrayLength());
            if (requests.GetArrayLength() > 0)
            {
                Assert.IsTrue(requests[0].GetProperty("original").GetBoolean());
                Assert.AreEqual("home", requests[0].GetProperty("marker").GetString());
                Assert.AreEqual("#home", requests[0].GetProperty("href").GetString());
            }
        }
    }

    [TestMethod]
    public void SourcePaintPreservesHeaderBlurRulesAndMobileTransitionRecipes()
    {
        using var reference = Reference();
        foreach (var row in reference.RootElement.GetProperty("cases").EnumerateArray())
        {
            var m = row.GetProperty("measurement");
            Assert.AreEqual("blur(8px)", Paint(m, "header", "backdropFilter"));
            Assert.IsTrue(Paint(m, "header", "background").EndsWith("/ 0.95)", StringComparison.Ordinal));
            Assert.AreEqual("1px", m.GetProperty("paint").GetProperty("header").GetProperty("border")[0].GetString());
            Assert.AreEqual("1px", m.GetProperty("paint").GetProperty("panel").GetProperty("topBorder")[0].GetString());
            Assert.AreEqual("max-height, opacity", Paint(m, "panel", "transitionProperty"));
            Assert.IsTrue(Paint(m, "panel", "boxShadow").Contains("0px 24px 64px -20px", StringComparison.Ordinal));
            Assert.IsTrue(Paint(m, "panel", "boxShadow").Contains("0px 4px 14px 0px", StringComparison.Ordinal));
        }
    }
}
