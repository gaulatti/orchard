using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;

namespace Orchard.Tests;

// Frozen browser evidence and the production model; no native popup acceptance
// is inferred from these tests.
[TestClass]
public sealed class PopoverReferenceTests
{
    private static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
    private static JsonDocument Reference() => JsonDocument.Parse(File.ReadAllBytes(Fixture("Popover.json")));
    private static double Number(JsonElement node, string key, double otherwise) => node.TryGetProperty(key, out var value) ? value.GetDouble() : otherwise;
    private static bool Flag(JsonElement node, string key) => node.TryGetProperty(key, out var value) && value.GetBoolean();

    [TestMethod]
    public void FrozenPopoverBindsEverySourceImageAndTheExactCaptureInputs()
    {
        using var document = Reference(); var root = document.RootElement;
        Assert.AreEqual("orchard-popover-reference-v1", root.GetProperty("format").GetString());
        Assert.AreEqual("a86059013cd59684ce452849278ea81e734f2066", root.GetProperty("commit").GetString());
        Assert.AreEqual("045511fc776cb635818eb786f8c7c722de2f167bc1934493617c1ed5ae222c6d", root.GetProperty("sourceSha256").GetString());
        Assert.AreEqual(122, root.GetProperty("cases").GetArrayLength()); Assert.AreEqual(16, root.GetProperty("interactions").GetArrayLength());
        foreach (var (file, field) in new[] { ("CapturePopover.cjs", "scriptSha256"), ("PopoverClient.tsx", "clientSha256") })
            Assert.AreEqual(root.GetProperty(field).GetString(), Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture(file)))));
        var popover = root.GetProperty("modules").EnumerateArray().Single(row => row.GetProperty("name").GetString() == "@radix-ui/react-popover");
        Assert.AreEqual("1.1.23", popover.GetProperty("version").GetString());
        var popper = root.GetProperty("modules").EnumerateArray().Single(row => row.GetProperty("name").GetString() == "@radix-ui/react-popper");
        Assert.AreEqual("1.3.7", popper.GetProperty("version").GetString());
        StringAssert.Contains(popper.GetProperty("lockPath").GetString(), "@radix-ui/react-popover/node_modules/");
        foreach (var row in root.GetProperty("cases").EnumerateArray())
        {
            var spec = row.GetProperty("spec"); var bytes = File.ReadAllBytes(Fixture("Popover/" + row.GetProperty("file").GetString()));
            Assert.AreEqual(row.GetProperty("pngSha256").GetString(), Convert.ToHexStringLower(SHA256.HashData(bytes)));
            Assert.AreEqual(Number(spec, "viewportWidth", 800), BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4)));
            Assert.AreEqual(Number(spec, "viewportHeight", 600), BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4)));
        }
    }

    [TestMethod]
    public void ProductionPlacementMatchesEveryFrozenSideAlignmentCollisionAndOverflow()
    {
        using var document = Reference(); var checkedStates = 0;
        foreach (var row in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var spec = row.GetProperty("spec"); var measurement = row.GetProperty("measurement");
            if (measurement.GetProperty("popup").ValueKind == JsonValueKind.Null) { Assert.AreEqual("closed", row.GetProperty("id").GetString()); continue; }
            var popup = measurement.GetProperty("popup"); var box = popup.GetProperty("box"); var trigger = measurement.GetProperty("trigger");
            var result = BleeckerPopoverModel.Place(new(trigger.GetProperty("x").GetDouble(), trigger.GetProperty("y").GetDouble(), trigger.GetProperty("width").GetDouble(), trigger.GetProperty("height").GetDouble()),
                box.GetProperty("width").GetDouble(), box.GetProperty("height").GetDouble(), Number(spec, "viewportWidth", 800), Number(spec, "viewportHeight", 600),
                spec.TryGetProperty("side", out var side) ? Enum.Parse<BleeckerPopoverSide>(side.GetString()!, true) : BleeckerPopoverSide.Bottom,
                spec.TryGetProperty("align", out var align) ? Enum.Parse<BleeckerPopoverAlign>(align.GetString()!, true) : BleeckerPopoverAlign.Center, Number(spec, "sideOffset", 4), Flag(spec, "rtl"));
            var context = row.GetProperty("scheme").GetString() + "/" + row.GetProperty("id").GetString();
            Assert.AreEqual(box.GetProperty("x").GetDouble(), result.X, .000001, context + "/x"); Assert.AreEqual(box.GetProperty("y").GetDouble(), result.Y, .000001, context + "/y");
            Assert.AreEqual(popup.GetProperty("side").GetString(), result.Side.ToString().ToLowerInvariant(), context + "/side"); Assert.AreEqual(popup.GetProperty("align").GetString(), result.Align.ToString().ToLowerInvariant(), context + "/align"); checkedStates++;
        }
        Assert.AreEqual(120, checkedStates);
    }

    [TestMethod]
    public void SourceOpenHistoryKeepsControlledRequestsSeparateFromInternalState()
    {
        using var document = Reference();
        foreach (var history in document.RootElement.GetProperty("interactions").EnumerateArray().Where(row => row.TryGetProperty("samples", out _)))
        {
            var setup = history.GetProperty("setup"); var model = new BleeckerPopoverModel(Flag(setup, "defaultOpen"));
            bool? open = Flag(setup, "controlled") ? Flag(setup, "open") : null; var requests = new List<bool>();
            foreach (var sample in history.GetProperty("samples").EnumerateArray())
            {
                var label = sample.GetProperty("label").GetString();
                if (label is "trigger" or "escape")
                {
                    var next = label == "trigger" ? !model.Effective(open) : false;
                    if (!(label == "trigger" && (Flag(setup, "prevent") || Flag(setup, "disabled"))) && model.Request(open, next)) { requests.Add(next); if (Flag(setup, "accept")) open = next; }
                }
                if (label == "released") open = null;
                Assert.AreEqual(sample.GetProperty("measurement").GetProperty("expanded").GetBoolean(), model.Effective(open), history.GetProperty("id").GetString() + "/" + label);
                CollectionAssert.AreEqual(sample.GetProperty("requests").EnumerateArray().Select(value => value.GetBoolean()).ToArray(), requests.ToArray());
            }
        }
    }

    [TestMethod]
    public void SourceFocusLoopDismissalAndLiveContentPreserveTheObservedCallerState()
    {
        using var document = Reference();
        foreach (var history in document.RootElement.GetProperty("interactions").EnumerateArray().Where(row => row.GetProperty("id").GetString() == "keyboard-edit-and-dismiss"))
        {
            foreach (var (field, focus) in new[] { ("entered", "editor"), ("loop", "editor"), ("reverseLoop", "last-action"), ("escaped", "trigger"), ("outside", "outside"), ("focusOutside", "outside") })
                Assert.AreEqual(focus, history.GetProperty(field).GetProperty("focused").GetString());
            foreach (var field in new[] { "escaped", "outside", "focusOutside" }) Assert.IsFalse(history.GetProperty(field).GetProperty("expanded").GetBoolean());
            var retained = history.GetProperty("retained"); Assert.IsTrue(retained.GetProperty("original").GetBoolean()); Assert.AreEqual("Caller edit", retained.GetProperty("text").GetString()); Assert.AreEqual(2, retained.GetProperty("start").GetInt32()); Assert.AreEqual(5, retained.GetProperty("end").GetInt32());
            CollectionAssert.AreEqual(new[] { true, false, true, false, true, false }, history.GetProperty("requests").EnumerateArray().Select(value => value.GetBoolean()).ToArray());
        }
    }

    [TestMethod]
    public void SourceSurfaceRetainsItsTokensAndDoesNotClampOversizedCallerContent()
    {
        static double Milliseconds(string text) => text.EndsWith("ms", StringComparison.Ordinal)
            ? double.Parse(text[..^2], System.Globalization.CultureInfo.InvariantCulture)
            : text.EndsWith('s') ? double.Parse(text[..^1], System.Globalization.CultureInfo.InvariantCulture) * 1000 : throw new FormatException("Unsupported source duration.");
        using var document = Reference();
        foreach (var row in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var popup = row.GetProperty("measurement").GetProperty("popup"); if (popup.ValueKind == JsonValueKind.Null) continue;
            var paint = popup.GetProperty("paint"); Assert.AreEqual(224, paint.GetProperty("minimum").GetDouble()); Assert.AreEqual(7, paint.GetProperty("radius").GetDouble()); Assert.AreEqual(1, paint.GetProperty("borderWidth").GetDouble());
            CollectionAssert.AreEqual(new[] { 8d, 8d, 8d, 8d }, paint.GetProperty("padding").EnumerateArray().Select(value => value.GetDouble()).ToArray());
            Assert.AreEqual(220, Milliseconds(popup.GetProperty("motion").GetProperty("enter").GetString()!)); Assert.AreEqual(150, Milliseconds(popup.GetProperty("motion").GetProperty("exit").GetString()!));
            if (row.GetProperty("id").GetString() == "oversized-content") Assert.IsGreaterThan(800, popup.GetProperty("box").GetProperty("width").GetDouble());
        }
        foreach (var style in document.RootElement.GetProperty("motionStyles").EnumerateArray())
        {
            var open = style.GetProperty("normalOpen"); Assert.AreEqual("none", open.GetProperty("animationName").GetString()); Assert.AreEqual(0, open.GetProperty("animationCount").GetInt32()); Assert.AreEqual(0, Milliseconds(open.GetProperty("animationDuration").GetString()!));
        }
    }
}
