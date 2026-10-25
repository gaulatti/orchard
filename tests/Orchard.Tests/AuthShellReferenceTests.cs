using System.Security.Cryptography;
using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class AuthShellReferenceTests
{
    private static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
    private static JsonDocument Read() => JsonDocument.Parse(File.ReadAllBytes(Fixture("AuthShell.json")));
    private static string? Text(JsonElement e, string key, string? fallback = null) => e.TryGetProperty(key, out var v) ? v.GetString() : fallback;
    private static double Number(JsonElement e, string key, double fallback = 0) => e.TryGetProperty(key, out var v) ? v.GetDouble() : fallback;
    private static bool Flag(JsonElement e, string key) => e.TryGetProperty(key, out var v) && v.GetBoolean();
    private static bool Truthy(JsonElement s, string key) => Text(s, key) is not ("none" or "zero" or "empty" or "false");
    private static bool Node(JsonElement s, string key) => Truthy(s, key) && Text(s, key) != "empty-array";
    private static JsonElement History(JsonElement r, string id, string theme) => r.GetProperty("histories").EnumerateArray().Single(h => Text(h, "id") == id && Text(h, "scheme") == theme);
    private static JsonElement Step(JsonElement r, string id, string theme, int index = 0) => History(r, id, theme).GetProperty("steps")[index].GetProperty("value");
    private static void Near(double expected, double actual) => Assert.AreEqual(expected, actual, 1d / 64);
    private static void Hash(string file, string? sha) => Assert.AreEqual(sha, Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture(file)))));

    [TestMethod]
    public void FrozenProducerAndEveryOriginalCaptureRemainVerifiable()
    {
        Hash("AuthShell.json", "e40f5749a3a2abe09c77b2b0aaf3a080c896a857af359fec2f9845419aeec0e8");
        using var doc = Read(); var r = doc.RootElement;
        Assert.AreEqual("a86059013cd59684ce452849278ea81e734f2066", Text(r, "commit"));
        Assert.AreEqual(104, r.GetProperty("cases").GetArrayLength()); Assert.AreEqual(20, r.GetProperty("histories").GetArrayLength());
        Assert.AreEqual(122, r.GetProperty("cssInputs").GetArrayLength()); Assert.AreEqual(15, r.GetProperty("bundledFiles").GetArrayLength());
        foreach (var field in new[] { "dependencies", "cssInputs" })
            foreach (var input in r.GetProperty(field).EnumerateArray()) Hash("AuthShell/Source/" + Text(input, "path"), Text(input, "sha256"));
        foreach (var (file, field) in new[] { ("CaptureAuthShell.cjs", "scriptSha256"), ("AuthShellClient.tsx", "clientSha256"), ("AuthShell/source.css", "cssSha256"), ("AuthShell/bundle.js", "bundleSha256") }) Hash(file, Text(r, field));
        foreach (var c in r.GetProperty("cases").EnumerateArray())
        {
            Hash("AuthShell/" + Text(c, "file"), Text(c, "pngSha256")); var samples = c.GetProperty("samples");
            Assert.AreEqual(2, c.GetProperty("stableFrames").GetInt32()); Assert.AreEqual(Text(samples[samples.GetArrayLength() - 1], "sha256"), Text(samples[samples.GetArrayLength() - 2], "sha256"));
            foreach (var sample in samples.EnumerateArray()) Hash("AuthShell/" + Text(sample, "file"), Text(sample, "sha256"));
        }
        Assert.IsFalse(Flag(r, "authenticationImplemented")); Assert.IsFalse(Flag(r, "nativeImplemented")); Assert.IsFalse(Flag(r, "nativeComponentAccepted"));
    }

    [TestMethod]
    public void LogicalSplitTracksUseViewportBreakpointAndRootRelativeMinimum()
    {
        using var doc = Read();
        foreach (var c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            var s = c.GetProperty("spec"); var m = c.GetProperty("measurement"); var w = Number(s, "width"); var f = Number(s, "rootFont", 16) / 16;
            var split = Text(s, "layout") == "split" && Truthy(s, "aside"); var visible = split && w >= 1024; var asideWidth = visible ? Math.Max(384 * f, w * 5 / 12) : 0;
            var accessWidth = w - asideWidth; var logicalX = visible && Text(s, "side") == "start" ? asideWidth : 0; var access = m.GetProperty("access").GetProperty("box");
            Near(accessWidth, Number(access, "width")); Near(Flag(s, "rtl") ? w - logicalX - accessWidth : logicalX, Number(access, "x"));
            var aside = m.GetProperty("aside"); Assert.AreEqual(split, aside.ValueKind != JsonValueKind.Null);
            if (split) { Assert.AreEqual(visible ? "flex" : "none", Text(aside.GetProperty("paint"), "display")); Near(asideWidth, Number(aside.GetProperty("box"), "width")); }
            var px = (w < 640 ? 20 : w < 1024 ? 32 : w < 1280 ? 48 : 64) * f;
            Near(Math.Min(448 * f, Math.Max(0, accessWidth - px * 2)), Number(m.GetProperty("inner").GetProperty("box"), "width"));
        }
    }

    [TestMethod]
    public void TallChildrenGrowTheShellWhileShortChildrenCenterBetweenBrandAndFooter()
    {
        using var doc = Read();
        foreach (var c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            var s = c.GetProperty("spec"); var m = c.GetProperty("measurement"); var w = Number(s, "width"); var f = Number(s, "rootFont", 16) / 16;
            var py = (w < 640 ? 24 : 32) * f; var cy = (w < 640 ? 40 : 56) * f; var bh = Node(s, "brand") ? Number(s, "brandHeight", 40) : 0; var fh = Node(s, "footer") ? Number(s, "footerHeight", 32) : 0;
            var ch = Text(s, "children") == "none" ? 0 : Text(s, "children") == "zero" ? 24 * f : Number(s, "contentHeight", 220);
            var asideHeight = Text(s, "layout") == "split" && Node(s, "aside") && w >= 1024 ? Number(s, "asideHeight", 160) + (w < 1280 ? 96 : 128) * f : 0;
            var height = Math.Max(Number(s, "height", 760), Math.Max(py * 2 + bh + fh + cy * 2 + ch, asideHeight));
            Near(height, Number(m.GetProperty("main").GetProperty("box"), "height"));
            Near(py + bh, Number(m.GetProperty("content").GetProperty("box"), "y"));
            Near(py + bh + (height - py * 2 - bh - fh - ch) / 2, Number(m.GetProperty("contentInner").GetProperty("box"), "y"));
            Near(ch, Number(m.GetProperty("contentInner").GetProperty("box"), "height"));
        }
    }

    [TestMethod]
    public void OptionalSlotsFollowProducerTruthinessAndChildrenZeroStillRenders()
    {
        using var doc = Read();
        foreach (var c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            var s = c.GetProperty("spec"); var m = c.GetProperty("measurement");
            foreach (var slot in new[] { "brand", "footer" }) Assert.AreEqual(Truthy(s, slot), m.GetProperty(slot).ValueKind != JsonValueKind.Null);
            Assert.AreEqual(Text(s, "layout") == "split" && Truthy(s, "aside"), m.GetProperty("aside").ValueKind != JsonValueKind.Null);
            if (Text(s, "children") == "zero") Assert.AreEqual("0", Text(m.GetProperty("contentInner"), "text"));
            if (Text(s, "children") == "none") Assert.AreEqual("", Text(m.GetProperty("contentInner"), "text"));
        }
    }

    [TestMethod]
    public void ThemesAndDecorationsRetainSourceTokensAndLandmarks()
    {
        using var doc = Read();
        foreach (var c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            var m = c.GetProperty("measurement"); var dark = Text(c, "scheme") == "Dark";
            Assert.AreEqual(dark ? "rgb(13, 24, 33)" : "rgb(255, 255, 255)", Text(m.GetProperty("access").GetProperty("paint"), "background"));
            Assert.AreEqual("MAIN", Text(m.GetProperty("main"), "tag")); StringAssert.Contains(Text(c, "accessibility"), "main \"Original access shell\"");
            if (m.GetProperty("aside").ValueKind == JsonValueKind.Null) continue;
            Assert.AreEqual(dark ? "rgb(24, 37, 51)" : "rgb(26, 55, 77)", Text(m.GetProperty("aside").GetProperty("paint"), "background"));
            Assert.AreEqual("Original membership note", Text(m.GetProperty("aside"), "name")); Assert.AreEqual(2, m.GetProperty("circles").GetArrayLength());
            foreach (var circle in m.GetProperty("circles").EnumerateArray()) { Assert.AreEqual("true", Text(circle, "ariaHidden")); Assert.AreEqual("none", Text(circle.GetProperty("paint"), "pointerEvents")); }
        }
    }

    [TestMethod]
    public void ResponsiveHidingRetainsEditorsButRemovingAsideRemountsIt()
    {
        using var doc = Read(); var r = doc.RootElement;
        foreach (var theme in new[] { "Light", "Dark" })
        {
            Assert.IsTrue(Flag(History(r, "retained-editors-layout-side-resize", theme), "sameEditors"));
            foreach (var step in History(r, "retained-editors-layout-side-resize", theme).GetProperty("steps").EnumerateArray())
            { var caller = step.GetProperty("value").GetProperty("caller"); Assert.AreEqual("Typed original content", Text(caller.GetProperty("content"), "value")); Assert.AreEqual("Typed original aside", Text(caller.GetProperty("aside"), "value")); }
            Assert.IsFalse(Flag(Step(r, "retained-editors-layout-side-resize", theme, 2).GetProperty("caller").GetProperty("aside"), "visible"));
            Assert.IsTrue(Flag(History(r, "centered-unmounts-aside-retains-access", theme), "oldAsideDetached")); Assert.IsTrue(Flag(History(r, "centered-unmounts-aside-retains-access", theme), "newAside"));
            Assert.AreEqual("Typed content", Text(Step(r, "centered-unmounts-aside-retains-access", theme, 1).GetProperty("caller").GetProperty("content"), "value"));
            Assert.AreEqual("Original aside", Text(Step(r, "centered-unmounts-aside-retains-access", theme, 1).GetProperty("caller").GetProperty("aside"), "value"));
            Assert.IsTrue(Flag(History(r, "live-theme-retains-slots", theme), "sameAccess"));
        }
    }

    [TestMethod]
    public void VisualSideKeepsDomTabOrderAndCallerActionsRemainLocal()
    {
        using var doc = Read(); var r = doc.RootElement;
        foreach (var theme in new[] { "Light", "Dark" })
        {
            foreach (var side in new[] { "start", "end" }) CollectionAssert.AreEqual(new[] { "brand", "brand", "content", "content", "footer", "footer", "aside", "aside" }, History(r, "tab-order-" + side, theme).GetProperty("order").EnumerateArray().Select(o => Text(o, "slot")).ToArray());
            CollectionAssert.AreEqual(new string?[] { "brand", "brand", "content", "content", "footer", "footer", null }, History(r, "narrow-tab-skips-hidden-aside", theme).GetProperty("order").EnumerateArray().Select(o => o.ValueKind == JsonValueKind.Null ? null : o.GetString()).ToArray());
            var events = Step(r, "caller-actions-and-form-local", theme).GetProperty("events"); Assert.AreEqual(2, events.GetArrayLength()); Assert.AreEqual("click", Text(events[0], "kind")); Assert.AreEqual("content", Text(events[0], "slot")); Assert.AreEqual("submit", Text(events[1], "kind")); Assert.IsTrue(Flag(events[1], "prevented"));
        }
    }

    [TestMethod]
    public void CallerOverridesAndLiveSlotUpdatesStayObservable()
    {
        using var doc = Read(); var r = doc.RootElement;
        foreach (var theme in new[] { "Light", "Dark" })
        {
            var changed = Step(r, "live-label-and-slot-truthiness", theme); Assert.AreEqual("Updated original note", Text(changed.GetProperty("aside"), "name")); Assert.AreEqual(JsonValueKind.Null, changed.GetProperty("brand").ValueKind);
            Assert.AreNotEqual(JsonValueKind.Null, Step(r, "live-label-and-slot-truthiness", theme, 1).GetProperty("footer").ValueKind);
            Assert.AreEqual(JsonValueKind.Null, Step(r, "unmount-remount-resets-editors", theme).GetProperty("main").ValueKind);
            Assert.AreEqual("Original content", Text(Step(r, "unmount-remount-resets-editors", theme, 1).GetProperty("caller").GetProperty("content"), "value"));
            Assert.AreEqual("Typed scaled value", Text(Step(r, "live-root-font-and-direction", theme).GetProperty("caller").GetProperty("content"), "value"));
            foreach (var c in r.GetProperty("cases").EnumerateArray().Where(c => Text(c, "scheme") == theme && Text(c, "id") == "data-overrides")) { var main = c.GetProperty("measurement").GetProperty("main"); Assert.AreEqual("caller-layout", Text(main, "layout")); Assert.AreEqual("caller-side", Text(main, "side")); }
        }
    }
}
