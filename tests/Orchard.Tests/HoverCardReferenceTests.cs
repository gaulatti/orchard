using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class HoverCardReferenceTests
{
    private static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
    private static JsonDocument Reference() => JsonDocument.Parse(File.ReadAllBytes(Fixture("HoverCard.json")));
    private static double Number(JsonElement row, string key) => row.GetProperty(key).GetDouble();
    private static BleeckerHoverCardModel.Rect Box(JsonElement row) => new(Number(row, "x"), Number(row, "y"), Number(row, "width"), Number(row, "height"));
    private static BleeckerHoverCardSide Side(JsonElement spec) => spec.TryGetProperty("side", out var side) ? Enum.Parse<BleeckerHoverCardSide>(side.GetString()!, true) : BleeckerHoverCardSide.Bottom;

    [TestMethod]
    public void FrozenSourceBindsAllInputsImagesAndActualBundledDependencies()
    {
        using var document = Reference(); var root = document.RootElement;
        Assert.AreEqual("orchard-hover-card-reference-v1", root.GetProperty("format").GetString());
        Assert.AreEqual("a86059013cd59684ce452849278ea81e734f2066", root.GetProperty("commit").GetString());
        Assert.AreEqual("f8af0dfd5c71926359b269f15d8782bd97fbd3fd18579346f59b28a9bfd0fcb6", root.GetProperty("sourceSha256").GetString());
        Assert.AreEqual(44, root.GetProperty("cases").GetArrayLength()); Assert.AreEqual(4, root.GetProperty("interactions").GetArrayLength()); Assert.AreEqual(16, root.GetProperty("transitions").GetArrayLength());
        CollectionAssert.AreEqual(new[] { "clsx", "react", "react-dom", "scheduler", "tailwind-merge" }, root.GetProperty("modules").EnumerateArray().Select(r => r.GetProperty("name").GetString()!).ToArray());
        foreach (var (file, field) in new[] { ("CaptureHoverCard.cjs", "scriptSha256"), ("HoverCardClient.tsx", "clientSha256") })
            Assert.AreEqual(root.GetProperty(field).GetString(), Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture(file)))));
        foreach (var row in root.GetProperty("cases").EnumerateArray())
        {
            var bytes = File.ReadAllBytes(Fixture("HoverCard/" + row.GetProperty("file").GetString())); var spec = row.GetProperty("spec");
            Assert.AreEqual(row.GetProperty("pngSha256").GetString(), Convert.ToHexStringLower(SHA256.HashData(bytes)));
            Assert.AreEqual(spec.TryGetProperty("viewportWidth", out var width) ? width.GetDouble() : 800, BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4)));
            Assert.AreEqual(spec.TryGetProperty("viewportHeight", out var height) ? height.GetDouble() : 600, BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4)));
        }
    }

    [TestMethod]
    public void ProductionPlacementMatchesEveryOpenClosedRtlAndOverflowState()
    {
        using var document = Reference();
        foreach (var row in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var m = row.GetProperty("measurement"); var surface = Box(m.GetProperty("surface")); var trigger = Box(m.GetProperty("wrapper"));
            var actual = BleeckerHoverCardModel.Place(trigger, surface.Width, surface.Height, Side(row.GetProperty("spec")), m.GetProperty("hovered").GetBoolean());
            Assert.AreEqual(surface.X, actual.X, .000001, row.GetProperty("id").GetString()); Assert.AreEqual(surface.Y, actual.Y, .000001, row.GetProperty("id").GetString());
            Assert.AreEqual(Box(m.GetProperty("trigger")), trigger); Assert.IsTrue(m.GetProperty("parentIsOriginalWrapper").GetBoolean());
        }
    }

    [TestMethod]
    public void ProductionOpacityClockMatchesSourceWhileTranslationAndHitTestingSnap()
    {
        using var document = Reference();
        foreach (var row in document.RootElement.GetProperty("transitions").EnumerateArray())
        {
            var reduced = row.GetProperty("reduced").GetBoolean(); var entering = row.GetProperty("kind").GetString() == "enter";
            var model = new BleeckerHoverCardModel(); if (!entering) model.SetHovered(true, -1000, false); model.SetHovered(entering, 0, !reduced);
            var definitions = row.GetProperty("definitions"); Assert.AreEqual(1, definitions.GetArrayLength()); Assert.AreEqual("opacity", definitions[0].GetProperty("property").GetString());
            Assert.AreEqual(reduced ? .01 : 220, Number(definitions[0].GetProperty("timing"), "duration"), .000001);
            var after = row.GetProperty("after"); var target = Box(after.GetProperty("surface"));
            foreach (var sample in row.GetProperty("samples").EnumerateArray())
            {
                Assert.AreEqual(Number(sample, "opacity"), model.OpacityAt(Number(sample, "milliseconds")), .000002);
                Assert.AreEqual(target.X, Number(sample, "x"), .000001); Assert.AreEqual(target.Y, Number(sample, "y"), .000001);
                Assert.AreEqual("none", sample.GetProperty("transform").GetString()); Assert.AreEqual(entering ? "auto" : "none", sample.GetProperty("pointerEvents").GetString());
                Assert.AreEqual(entering, model.HitTestVisible);
            }
        }
    }

    [TestMethod]
    public void SourceKeyboardFocusOriginalEditsAndLiteralGapRetainTheirObservedBehavior()
    {
        using var document = Reference();
        foreach (var row in document.RootElement.GetProperty("interactions").EnumerateArray())
        {
            var samples = row.GetProperty("samples").EnumerateArray().ToArray();
            if (row.GetProperty("id").GetString() == "keyboard-edit-and-hover")
            {
                var byLabel = samples.ToDictionary(s => s.GetProperty("label").GetString()!);
                Assert.AreEqual(1, byLabel["keyboard-trigger"].GetProperty("clicks").GetInt32());
                Assert.AreEqual(0, Number(byLabel["keyboard-trigger"].GetProperty("measurement"), "opacity"));
                foreach (var label in new[] { "closed-content-tab", "closed-escape", "trigger-hover", "hovered-escape", "outside" }) Assert.AreEqual("editor", byLabel[label].GetProperty("measurement").GetProperty("focused").GetString());
                Assert.AreEqual(1, Number(byLabel["hovered-escape"].GetProperty("measurement"), "opacity"));
                var retained = row.GetProperty("retained"); Assert.IsTrue(retained.GetProperty("original").GetBoolean()); Assert.AreEqual("Caller edit", retained.GetProperty("text").GetString()); Assert.AreEqual(2, retained.GetProperty("start").GetInt32()); Assert.AreEqual(5, retained.GetProperty("end").GetInt32());
            }
            else CollectionAssert.AreEqual(new[] { true, true, false, false }, samples.Select(s => s.GetProperty("measurement").GetProperty("hovered").GetBoolean()).ToArray());
        }
    }

    [TestMethod]
    public void SourcePaintFlowAndTooltipMetadataRemainPresentInBothVisualStates()
    {
        using var document = Reference();
        foreach (var row in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var m = row.GetProperty("measurement"); var paint = m.GetProperty("paint"); var custom = row.GetProperty("id").GetString() == "custom-surface";
            Assert.AreEqual(custom ? 384 : 288, Number(m.GetProperty("surface"), "width")); Assert.AreEqual(custom ? 0 : 12, Number(paint, "radius")); Assert.AreEqual(1, Number(paint, "borderWidth"));
            CollectionAssert.AreEqual(Enumerable.Repeat(custom ? 24d : 16d, 4).ToArray(), paint.GetProperty("padding").EnumerateArray().Select(v => v.GetDouble()).ToArray());
            Assert.AreEqual("tooltip", m.GetProperty("role").GetString()); Assert.AreEqual(JsonValueKind.Null, m.GetProperty("ariaExpanded").ValueKind); Assert.AreEqual(JsonValueKind.Null, m.GetProperty("ariaDescribedBy").ValueKind);
            Assert.AreEqual("visible", m.GetProperty("visibility").GetString()); Assert.AreEqual(16, Number(paint, "fontSize")); Assert.AreEqual(24, Number(paint, "lineHeight"));
        }
    }
}
