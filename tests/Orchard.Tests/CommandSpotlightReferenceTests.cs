using System.Security.Cryptography;
using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class CommandSpotlightReferenceTests
{
    private static string Fixture(string file) => Path.Combine(AppContext.BaseDirectory, "Fixtures", file);
    private static JsonDocument Reference() => JsonDocument.Parse(File.ReadAllBytes(Fixture("CommandSpotlight.json")));
    private static JsonElement History(JsonElement r, string id, string theme) => r.GetProperty("histories").EnumerateArray().Single(h => h.GetProperty("id").GetString() == id && h.GetProperty("scheme").GetString() == theme);
    private static JsonElement Step(JsonElement r, string id, string theme, int index = 0) => History(r, id, theme).GetProperty("steps")[index].GetProperty("value");
    private static JsonElement Case(JsonElement r, string id, string theme) => r.GetProperty("cases").EnumerateArray().Single(c => c.GetProperty("id").GetString() == id && c.GetProperty("scheme").GetString() == theme).GetProperty("measurement");
    private static IEnumerable<JsonElement> Rows(JsonElement m) => m.GetProperty("dialog").GetProperty("groups").EnumerateArray().SelectMany(g => g.GetProperty("rows").EnumerateArray());
    private static string? Text(JsonElement e, string key) => e.GetProperty(key).GetString();
    private static string?[] Selected(JsonElement m) => Rows(m).Where(n => n.GetProperty("selected").GetBoolean()).Select(n => Text(n.GetProperty("title"), "text")).ToArray();
    private static void Hash(string file, string? expected) => Assert.AreEqual(expected, Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture(file)))));

    [TestMethod]
    public void FrozenProducerRetainsEveryStyleInputAndSettledOriginalPng()
    {
        Hash("CommandSpotlight.json", "12b264b46566b389eff73d6b21e7f32feb465cdc52f2c86ab8379555a49a075e");
        using var reference = Reference(); var r = reference.RootElement;
        Assert.AreEqual("a86059013cd59684ce452849278ea81e734f2066", Text(r, "commit"));
        Assert.AreEqual(100, r.GetProperty("cases").GetArrayLength()); Assert.AreEqual(76, r.GetProperty("histories").GetArrayLength());
        Assert.AreEqual(122, r.GetProperty("cssInputs").GetArrayLength()); Assert.AreEqual(1698, r.GetProperty("bundledFiles").GetArrayLength());
        foreach (var (file, field) in new[] { ("CaptureCommandSpotlight.cjs", "scriptSha256"), ("CommandSpotlightClient.tsx", "clientSha256"), ("CommandSpotlight/source.css", "cssSha256"), ("CommandSpotlight/bundle.js", "bundleSha256") }) Hash(file, Text(r, field));
        foreach (var field in new[] { "dependencies", "cssInputs" })
            foreach (var input in r.GetProperty(field).EnumerateArray()) Hash("CommandSpotlight/Source/" + Text(input, "path"), Text(input, "sha256"));
        foreach (var c in r.GetProperty("cases").EnumerateArray())
        {
            var samples = c.GetProperty("samples"); Assert.AreEqual(2, c.GetProperty("stableFrames").GetInt32());
            Assert.AreEqual(Text(samples[samples.GetArrayLength() - 1], "sha256"), Text(samples[samples.GetArrayLength() - 2], "sha256"));
            Hash("CommandSpotlight/" + Text(c, "file"), Text(c, "pngSha256"));
            foreach (var sample in samples.EnumerateArray()) Hash("CommandSpotlight/" + Text(sample, "file"), Text(sample, "sha256"));
        }
        Assert.IsFalse(r.GetProperty("nativeImplemented").GetBoolean()); Assert.IsFalse(r.GetProperty("nativeComponentAccepted").GetBoolean()); Assert.IsTrue(r.GetProperty("navigationBoundaryIntercepted").GetBoolean()); Assert.IsFalse(r.GetProperty("externalWindowOpened").GetBoolean());
    }

    [TestMethod]
    public void ControlledRequestsCanBeRejectedAndRejectedCloseStillClearsQuery()
    {
        using var reference = Reference(); var r = reference.RootElement;
        foreach (var theme in new[] { "Light", "Dark" })
        {
            var rejected = Step(r, "controlled-open-rejected", theme); Assert.AreEqual(JsonValueKind.Null, rejected.GetProperty("dialog").ValueKind); Assert.IsTrue(rejected.GetProperty("events")[0].GetProperty("value").GetBoolean());
            Assert.AreEqual("beta", Text(Step(r, "controlled-close-rejected-clears-query", theme).GetProperty("dialog").GetProperty("input"), "value"));
            var close = Step(r, "controlled-close-rejected-clears-query", theme, 1); Assert.AreEqual("", Text(close.GetProperty("dialog").GetProperty("input"), "value")); Assert.IsFalse(close.GetProperty("events")[0].GetProperty("value").GetBoolean()); Assert.AreEqual(6, Rows(close).Count());
            Assert.AreEqual(JsonValueKind.Null, Step(r, "controlled-open-accepted", theme, 1).GetProperty("dialog").ValueKind);
            Assert.AreEqual(0, Step(r, "open-callback-omitted", theme, 1).GetProperty("events").GetArrayLength());
        }
    }

    [TestMethod]
    public void SearchUsesWholeOriginalFieldsAndGroupingPreservesFirstOccurrence()
    {
        using var reference = Reference(); var r = reference.RootElement;
        foreach (var theme in new[] { "Light", "Dark" })
        {
            foreach (var id in new[] { "query-no-match", "query-whole-phrase" }) Assert.AreEqual(0, Rows(Case(r, id, theme)).Count());
            Assert.AreEqual("Original alpha command", Text(Rows(Case(r, "query-keyword", theme)).Single().GetProperty("title"), "text"));
            Assert.AreEqual("Original gamma command", Text(Rows(Case(r, "query-description", theme)).Single().GetProperty("title"), "text"));
            Assert.AreEqual("Straße 東京 é العربية", Text(Rows(Case(r, "query-unicode", theme)).Single().GetProperty("title"), "text"));
            CollectionAssert.AreEqual(new[] { "Navigation", "Tools", "Actions" }, Case(r, "open-default", theme).GetProperty("dialog").GetProperty("groups").EnumerateArray().Select(g => Text(g.GetProperty("label"), "text")).ToArray());
            Assert.AreEqual("Original gamma command", Text(Rows(Case(r, "open-default", theme)).ElementAt(1).GetProperty("title"), "text"));
        }
    }

    [TestMethod]
    public void InputArrowsFollowOriginalOrderWhileFocusedButtonActivationRunsThatButton()
    {
        using var reference = Reference(); var r = reference.RootElement;
        foreach (var theme in new[] { "Light", "Dark" })
        {
            var actual = History(r, "keyboard-original-order-wraps", theme).GetProperty("steps").EnumerateArray().Select(s => Selected(s.GetProperty("value")).Single()).ToArray();
            CollectionAssert.AreEqual(new[] { "Original alpha command", "Original beta command", "Original gamma command", "Original ungrouped command", "Straße 東京 é العربية", "Original alpha command", "Straße 東京 é العربية" }, actual);
            Assert.AreEqual(0, Selected(Step(r, "all-disabled-enter-ignored", theme, 1)).Length); Assert.AreEqual(0, Step(r, "disabled-button-activation-ignored", theme).GetProperty("events").GetArrayLength());
            CollectionAssert.AreEqual(new[] { "Original alpha command" }, Selected(Step(r, "action-button-arrows-are-not-global", theme)));
            Assert.AreEqual("beta", Text(Step(r, "action-button-arrows-are-not-global", theme, 1).GetProperty("events")[0], "id"));
        }
    }

    [TestMethod]
    public void DispatchPreservesOriginalIdentityOrderErrorsAndImmediateClose()
    {
        using var reference = Reference(); var r = reference.RootElement;
        foreach (var theme in new[] { "Light", "Dark" })
        {
            var called = Step(r, "enter-original-callback-order", theme).GetProperty("events"); CollectionAssert.AreEqual(new[] { "open", "outer", "own", "open" }, called.EnumerateArray().Select(e => Text(e, "kind")).ToArray()); Assert.IsTrue(called[1].GetProperty("sameOriginal").GetBoolean()); Assert.IsTrue(called[2].GetProperty("sameOriginal").GetBoolean());
            Assert.AreEqual(0, Step(r, "own-callback-precedes-href", theme).GetProperty("externalOpens").GetArrayLength());
            foreach (var id in new[] { "own-synchronous-failure-closes", "own-asynchronous-failure-closes" }) { var failed = Step(r, id, theme); Assert.AreEqual(JsonValueKind.Null, failed.GetProperty("dialog").ValueKind); Assert.AreEqual(1, failed.GetProperty("errors").GetArrayLength()); }
            var outerFailed = Step(r, "outer-callback-failure-stops-run", theme); Assert.AreEqual(JsonValueKind.Object, outerFailed.GetProperty("dialog").ValueKind); Assert.AreEqual(2, outerFailed.GetProperty("events").GetArrayLength());
            Assert.AreEqual("Original outer callback failure", History(r, "outer-callback-failure-stops-run", theme).GetProperty("pageErrors")[0].GetString());
            Assert.AreEqual(JsonValueKind.Null, Step(r, "pending-action-closes-immediately", theme).GetProperty("dialog").ValueKind);
        }
    }

    [TestMethod]
    public void NavigationBoundariesRetainRawHrefAndExternalWindowAttributes()
    {
        using var reference = Reference(); var r = reference.RootElement;
        foreach (var theme in new[] { "Light", "Dark" })
        {
            var external = Step(r, "external-link-boundary-original", theme).GetProperty("externalOpens")[0]; Assert.AreEqual("https://fixtures.orchard.test/original?label=Raw%20value#view", Text(external, "url")); Assert.AreEqual("_blank", Text(external, "target")); Assert.AreEqual("noopener,noreferrer", Text(external, "features"));
            Assert.AreEqual("/original-route?label=Raw%20value#view", Text(Step(r, "internal-link-boundary-original", theme).GetProperty("events")[1], "href")); Assert.IsTrue(History(r, "internal-link-boundary-original", theme).GetProperty("requests")[0].GetProperty("intercepted").GetBoolean());
        }
    }

    [TestMethod]
    public void RetainedRowsPresenceAndNestedModalOwnershipCrossLifecycleBoundaries()
    {
        using var reference = Reference(); var r = reference.RootElement;
        foreach (var theme in new[] { "Light", "Dark" })
        {
            Assert.IsTrue(History(r, "reorder-retains-row-and-selection", theme).GetProperty("sameRow").GetBoolean()); Assert.IsTrue(History(r, "rapid-close-reopen-retains-panel", theme).GetProperty("sameDialog").GetBoolean()); Assert.IsTrue(History(r, "live-theme-retains-query-selection", theme).GetProperty("sameDialog").GetBoolean());
            Assert.AreEqual("Navigation", Text(Step(r, "in-place-mutation-memo-boundary", theme).GetProperty("dialog").GetProperty("groups")[0].GetProperty("label"), "text")); Assert.AreEqual("Mutated group", Text(Step(r, "in-place-mutation-memo-boundary", theme, 1).GetProperty("dialog").GetProperty("groups")[0].GetProperty("label"), "text"));
            var nested = Step(r, "nested-parent-modal-ownership", theme).GetProperty("allDialogs"); Assert.IsTrue(nested[0].GetProperty("inert").GetBoolean()); Assert.IsFalse(nested[1].GetProperty("inert").GetBoolean()); Assert.IsFalse(Step(r, "nested-parent-modal-ownership", theme, 1).GetProperty("allDialogs")[0].GetProperty("inert").GetBoolean());
            Assert.AreEqual(0, Step(r, "nested-parent-modal-ownership", theme, 2).GetProperty("allDialogs").GetArrayLength());
            Assert.AreEqual("", Text(Step(r, "unmount-remount-clears-query", theme, 1).GetProperty("dialog").GetProperty("input"), "value"));
        }
    }

    [TestMethod]
    public void SourceKeepsNarrowSeparatorDefectAndItsMixedPixelAndRemTypography()
    {
        using var reference = Reference(); var r = reference.RootElement;
        foreach (var theme in new[] { "Light", "Dark" })
        {
            // Frozen source evidence keeps the defect. The explicit WinUI contract hides the whole narrow hint.
            var narrow = Case(r, "closed-width-639", theme).GetProperty("trigger"); Assert.AreEqual("none", Text(narrow.GetProperty("label").GetProperty("paint"), "display")); Assert.AreEqual("none", Text(narrow.GetProperty("kbd").GetProperty("keys")[0].GetProperty("paint"), "display")); Assert.AreEqual("block", Text(narrow.GetProperty("kbd").GetProperty("separators")[0].GetProperty("paint"), "display"));
            Assert.AreEqual("block", Text(Case(r, "closed-width-640", theme).GetProperty("trigger").GetProperty("label").GetProperty("paint"), "display"));
            var scaled = Case(r, "open-font200", theme).GetProperty("dialog"); Assert.AreEqual("11px", Text(scaled.GetProperty("groups")[0].GetProperty("label").GetProperty("paint"), "fontSize")); Assert.AreEqual("28px", Text(scaled.GetProperty("groups")[0].GetProperty("rows")[0].GetProperty("title").GetProperty("paint"), "fontSize")); Assert.AreEqual("32px", Text(scaled.GetProperty("input").GetProperty("paint"), "fontSize"));
        }
    }
}
