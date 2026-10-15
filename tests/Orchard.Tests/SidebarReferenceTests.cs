using System.Text.Json;

namespace Orchard.Tests;

// These checks protect the browser source contract. They do not accept a native
// Sidebar; its implementation and observable Windows boundary remain pending.
[TestClass]
public sealed class SidebarReferenceTests
{
    [TestMethod]
    public void FrozenRuntimeAndAllSourceCasesRetainTheirInputAndIdentityEvidence()
    {
        using var source = new Reference();
        var root = source.Root;
        Assert.AreEqual("orchard-sidebar-reference-v1", Text(root, "format"));
        Assert.AreEqual("2c06422943eedda2232a04c087b8f8562f614829f2b77229ecc7941488894856", Text(root, "sourceSha256"));
        Assert.AreEqual("a86059013cd59684ce452849278ea81e734f2066", Text(root, "sourceCommit"));
        Assert.AreEqual(64, Text(root, "fullCaptureSha256").Length);
        var cases = root.GetProperty("cases").EnumerateArray().ToArray();
        Assert.HasCount(82, cases);
        Assert.AreEqual(82, cases.Select(c => Text(c, "id") + "/" + Text(c, "scheme")).Distinct().Count());
        foreach (var (name, version) in new[] { ("react", "19.2.4"), ("react-dom", "19.2.4"), ("lucide-react", "0.563.0"), ("@radix-ui/react-collapsible", "1.1.14") })
        {
            var dependency = root.GetProperty("bundledRuntime").EnumerateArray().Single(p => Text(p, "name") == name);
            Assert.AreEqual(version, Text(dependency, "version"));
            StringAssert.StartsWith(Text(dependency, "integrity"), "sha512-");
        }
        var inputCount = 0;
        foreach (var sample in cases)
        {
            Assert.AreEqual(0, sample.GetProperty("errors").GetArrayLength(), Text(sample, "id"));
            Assert.AreEqual("ASIDE", Text(sample.GetProperty("normal").GetProperty("surface"), "tag"));
            Assert.AreEqual("NAV", Text(sample.GetProperty("normal").GetProperty("nav"), "tag"));
            foreach (var state in new[] { sample.GetProperty("normal") }.Concat(sample.GetProperty("states").EnumerateArray().Select(s => s.GetProperty("state"))))
            {
                foreach (var receipt in state.GetProperty("inputEvents").EnumerateArray()) { Assert.IsTrue(receipt.GetProperty("trusted").GetBoolean()); inputCount++; }
                foreach (var callback in state.GetProperty("events").EnumerateArray().Where(e => Text(e, "kind") != "slot")) Assert.IsTrue(callback.GetProperty("same").GetBoolean());
                foreach (var render in state.GetProperty("renderCalls").EnumerateArray())
                    foreach (var identity in new[] { "same", "iconSame", "badgeSame", "labelSame" }) Assert.IsTrue(render.GetProperty(identity).GetBoolean(), identity);
                AssertAllStyleReferences(state, source.Root.GetProperty("styles").GetArrayLength());
            }
        }
        Assert.IsGreaterThan(100, inputCount);
    }

    [TestMethod]
    public void SourceGeometryPreservesSlotsChildIndentAndIndependentScrollArea()
    {
        using var source = new Reference();
        foreach (var theme in new[] { "Light", "Dark" })
        {
            var normal = source.Case("default", theme).GetProperty("normal");
            Box(normal.GetProperty("surface"), 0, 0, 256, 480);
            Box(normal.GetProperty("slots")[0], 12, 20, 231, 45);
            Box(normal.GetProperty("nav"), 12, 85, 231, 334);
            Box(normal.GetProperty("slots")[1], 12, 419, 231, 41);
            var rows = normal.GetProperty("rows");
            Assert.AreEqual(5, rows.GetArrayLength());
            Box(rows[0], 12, 85, 231, 40); Box(rows[1], 12, 129, 231, 40);
            Box(rows[2], 49, 173, 194, 40); Box(rows[3], 49, 213, 194, 40);
            Box(rows[4], 12, 257, 231, 40);
            Assert.AreEqual("14px", Text(source.Style(rows[0].GetProperty("spans")[1]), "fontSize"));
            Assert.AreEqual("500", Text(source.Style(rows[0].GetProperty("spans")[1]), "fontWeight"));
            Assert.AreEqual("7px", Text(source.Style(rows[0]), "borderRadius"));
            StringAssert.Contains(Text(source.Style(rows[0]), "boxShadow"), "inset");
            Assert.AreEqual("none", Text(source.Style(rows[1]), "boxShadow"));
            var collapsed = source.Case("collapsed", theme).GetProperty("normal");
            Box(collapsed.GetProperty("surface"), 0, 0, 64, 480);
            Box(collapsed.GetProperty("nav"), 8, 85, 47, 334);
            Assert.AreEqual(2, collapsed.GetProperty("rows").GetArrayLength());
            foreach (var row in collapsed.GetProperty("rows").EnumerateArray())
            {
                Assert.AreEqual("", Text(row, "text"));
                Assert.IsFalse(row.GetProperty("attributes").TryGetProperty("aria-label", out _));
            }
            var dense = source.Case("dense", theme);
            Assert.AreEqual(1536, dense.GetProperty("normal").GetProperty("nav").GetProperty("scrollHeight").GetInt32());
            Assert.IsGreaterThan(0d, State(dense, "scroll-last").GetProperty("nav").GetProperty("scrollTop").GetDouble());
            Assert.IsTrue(State(dense, "scroll-last").GetProperty("rows")[34].GetProperty("focused").GetBoolean());
            var shortSurface = source.Case("short", theme).GetProperty("normal");
            Box(shortSurface.GetProperty("nav"), 12, 85, 231, 0);
            Box(shortSurface.GetProperty("slots")[1], 12, 85, 231, 41);
            Box(source.Case("constrained", theme).GetProperty("normal").GetProperty("surface"), 0, 0, 180, 480);
            Box(source.Case("custom-width", theme).GetProperty("normal").GetProperty("surface"), 0, 0, 320, 480);
        }
    }

    [TestMethod]
    public void DisabledLinksRemainKeyboardActiveWhileGroupsIgnoreTheDisabledField()
    {
        using var source = new Reference();
        foreach (var theme in new[] { "Light", "Dark" })
        {
            var sample = source.Case("disabled-link", theme);
            var disabled = sample.GetProperty("normal").GetProperty("rows")[0];
            Assert.AreEqual("none", Text(source.Style(disabled), "pointerEvents"));
            Assert.AreEqual("0.5", Text(source.Style(disabled), "opacity"));
            Assert.IsFalse(disabled.GetProperty("attributes").TryGetProperty("disabled", out _));
            Assert.IsFalse(disabled.GetProperty("attributes").TryGetProperty("aria-disabled", out _));
            Assert.IsTrue(sample.GetProperty("states")[0].GetProperty("state").GetProperty("rows")[0].GetProperty("focused").GetBoolean());
            var keyboard = State(sample, "link-Enter");
            Assert.AreEqual(1, keyboard.GetProperty("events").GetArrayLength());
            Assert.AreEqual("disabled", Text(keyboard.GetProperty("events")[0], "id"));
            Assert.AreEqual("#disabled", Text(keyboard.GetProperty("events")[0], "href"));
            Assert.IsTrue(keyboard.GetProperty("inputEvents").EnumerateArray().Any(e => Text(e, "type") == "keydown" && Text(e, "key") == "Enter" && Text(e, "href") == "#disabled"));
            var pointer = State(sample, "link-pointer");
            Assert.AreEqual(1, pointer.GetProperty("events").GetArrayLength());
            var click = pointer.GetProperty("inputEvents").EnumerateArray().Last(e => Text(e, "type") == "click");
            Assert.AreNotEqual("A", Text(click, "tag"));
            Assert.IsGreaterThan(0d, click.GetProperty("x").GetDouble());
            var group = source.Case("disabled-group", theme);
            foreach (var (action, expanded) in new[] { ("group-click", "true"), ("group-Enter", "false"), ("group-Space", "true") })
            {
                var state = State(group, action);
                Assert.AreEqual(expanded, Expanded(state).Single());
                Assert.AreEqual(0, state.GetProperty("events").GetArrayLength());
            }
        }
    }

    [TestMethod]
    public void ExpansionFollowsMountedKeysAcrossActiveChangesReorderAndLeafGroupChanges()
    {
        using var source = new Reference();
        foreach (var theme in new[] { "Light", "Dark" })
        {
            foreach (var (id, expanded) in new[] { ("state-retention", "true"), ("state-closed", "false") })
            {
                var sample = source.Case(id, theme);
                foreach (var action in new[] { "active-change", "expand", "same-id-replacement" }) Assert.AreEqual(expanded, Expanded(State(sample, action)).Single(), id + "/" + action);
                Assert.HasCount(0, Expanded(State(sample, "collapse")));
                Assert.HasCount(0, Expanded(State(sample, "remove")));
                Assert.AreEqual("true", Expanded(State(sample, "readd")).Single());
            }
            foreach (var (id, expanded) in new[] { ("state-shape-open", "true"), ("state-shape-closed", "false") })
            {
                var sample = source.Case(id, theme);
                Assert.AreEqual(expanded, Expanded(State(sample, "to-group")).Single());
                Assert.HasCount(0, Expanded(State(sample, "to-leaf")));
                Assert.AreEqual(expanded, Expanded(State(sample, "to-group-inactive")).Single());
            }
            var reorder = source.Case("state-reorder", theme);
            foreach (var action in new[] { "reorder", "active-change" }) CollectionAssert.AreEqual(new[] { "false", "true" }, Expanded(State(reorder, action)));
            CollectionAssert.AreEqual(new[] { "true", "false" }, Expanded(State(reorder, "reorder-back")));
            Assert.AreEqual("false", Expanded(source.Case("deep-active", theme).GetProperty("normal")).Single());
            foreach (var (id, count) in new[] { ("empty", 0), ("collapsed-no-route", 0), ("collapsed-group-route", 1), ("empty-group", 1) }) Assert.AreEqual(count, source.Case(id, theme).GetProperty("normal").GetProperty("rows").GetArrayLength(), id);
        }
    }

    [TestMethod]
    public void ConsumerLinkContentCallbacksAndSlotTruthinessRemainSourceOwned()
    {
        using var source = new Reference();
        foreach (var theme in new[] { "Light", "Dark" })
        {
            foreach (var id in new[] { "custom-link", "custom-link-collapsed" })
            {
                var sample = source.Case(id, theme);
                Assert.IsGreaterThan(0, sample.GetProperty("normal").GetProperty("renderCalls").GetArrayLength());
                var events = State(sample, "link-Enter").GetProperty("events");
                Assert.AreEqual(2, events.GetArrayLength());
                Assert.AreEqual("custom", Text(events[0], "kind")); Assert.AreEqual("item", Text(events[1], "kind"));
                Assert.AreEqual("overview", Text(events[1], "id")); Assert.AreEqual("#overview", Text(events[1], "href"));
                Assert.AreEqual("about:blank", Text(State(sample, "link-pointer"), "url"));
            }
            var zero = source.Case("zero-content", theme).GetProperty("normal");
            CollectionAssert.AreEqual(new[] { "0", "0" }, zero.GetProperty("anonymous").EnumerateArray().Select(e => e.GetString()).ToArray());
            Assert.AreEqual(0, zero.GetProperty("slots").GetArrayLength());
            Box(zero.GetProperty("nav"), 12, 44, 231, 392);
            Assert.AreEqual("0Zero0", Text(zero.GetProperty("rows")[0], "text"));
            var truthy = source.Case("true-content", theme).GetProperty("normal");
            Assert.AreEqual(21d, truthy.GetProperty("slots")[0].GetProperty("box").GetProperty("height").GetDouble());
            Assert.AreEqual(17d, truthy.GetProperty("slots")[1].GetProperty("box").GetProperty("height").GetDouble());
            foreach (var id in new[] { "false-content", "empty-string-slots" }) Assert.AreEqual(0, source.Case(id, theme).GetProperty("normal").GetProperty("slots").GetArrayLength());
            var buttons = source.Case("slot-buttons", theme);
            Assert.AreEqual("header", Text(State(buttons, "header-click").GetProperty("events").EnumerateArray().Last(), "slot"));
            Assert.AreEqual("footer", Text(State(buttons, "footer-Space").GetProperty("events").EnumerateArray().Last(), "slot"));
        }
    }

    [TestMethod]
    public void MotionUsesMeasuredIndividualTransformsAndRealWidthTransitionFrames()
    {
        using var source = new Reference();
        foreach (var theme in new[] { "Light", "Dark" })
        {
            var sample = source.Case("motion", theme); var normal = sample.GetProperty("normal");
            var surfaceStyle = source.Style(normal.GetProperty("surface"));
            Assert.AreEqual("width, padding", Text(surfaceStyle, "transitionProperty"));
            Assert.AreEqual("0.2s", Text(surfaceStyle, "transitionDuration"));
            Assert.AreEqual("cubic-bezier(0.4, 0, 0.2, 1)", Text(surfaceStyle, "transitionTimingFunction"));
            Assert.AreEqual("0.19s", Text(source.Style(normal.GetProperty("rows")[0]), "transitionDuration"));
            var chevron = normal.GetProperty("rows")[1].GetProperty("svg")[1];
            Assert.AreEqual("0.22s", Text(source.Style(chevron), "transitionDuration"));
            Assert.AreEqual("90deg", Text(source.Style(chevron), "rotate"));
            var hover = State(sample, "link-hover").GetProperty("rows")[0];
            var pressed = State(sample, "link-pressed").GetProperty("rows")[0];
            Assert.AreEqual("0px 1px", Text(source.Style(pressed), "translate"));
            Assert.AreEqual("none", Text(source.Style(pressed), "transform"));
            Assert.AreEqual(hover.GetProperty("box").GetProperty("y").GetDouble() + 1, pressed.GetProperty("box").GetProperty("y").GetDouble());
            var frames = sample.GetProperty("motionFrames").EnumerateArray().ToArray();
            Assert.IsTrue(frames.Any(f => f.GetProperty("width").GetDouble() is > 64 and < 256));
            Assert.AreEqual(64d, frames[^1].GetProperty("width").GetDouble());
            Assert.AreEqual(8d, frames[^1].GetProperty("paddingLeft").GetDouble());
            Assert.IsTrue(frames.SelectMany(f => f.GetProperty("animations").EnumerateArray()).Any(a => Text(a, "property") == "width" && a.GetProperty("duration").GetDouble() == 200));
        }
    }

    private static string Text(JsonElement value, string key) => value.GetProperty(key).GetString() ?? "";
    private static JsonElement State(JsonElement sample, string action) => sample.GetProperty("states").EnumerateArray().Single(s => Text(s, "action") == action).GetProperty("state");
    private static string[] Expanded(JsonElement state) => state.GetProperty("rows").EnumerateArray().Where(r => Text(r, "tag") == "BUTTON").Select(r => Text(r.GetProperty("attributes"), "aria-expanded")).ToArray();
    private static void Box(JsonElement element, double x, double y, double width, double height)
    {
        var box = element.GetProperty("box");
        Assert.AreEqual(x, box.GetProperty("x").GetDouble()); Assert.AreEqual(y, box.GetProperty("y").GetDouble());
        Assert.AreEqual(width, box.GetProperty("width").GetDouble()); Assert.AreEqual(height, box.GetProperty("height").GetDouble());
    }
    private static void AssertAllStyleReferences(JsonElement value, int count)
    {
        if (value.ValueKind == JsonValueKind.Array) { foreach (var item in value.EnumerateArray()) AssertAllStyleReferences(item, count); }
        else if (value.ValueKind == JsonValueKind.Object)
            foreach (var property in value.EnumerateObject())
                if (property.Name == "styleIndex") Assert.IsTrue(property.Value.GetInt32() >= 0 && property.Value.GetInt32() < count);
                else AssertAllStyleReferences(property.Value, count);
    }
    private sealed class Reference : IDisposable
    {
        private readonly JsonDocument document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "SidebarLayout.json")));
        internal JsonElement Root => document.RootElement;
        internal JsonElement Case(string id, string theme) => Root.GetProperty("cases").EnumerateArray().Single(c => Text(c, "id") == id && Text(c, "scheme") == theme);
        internal JsonElement Style(JsonElement element) => Root.GetProperty("styles")[element.GetProperty("styleIndex").GetInt32()];
        public void Dispose() => document.Dispose();
    }
}
