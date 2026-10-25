using System.Security.Cryptography;
using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class MediaLibraryReferenceTests
{
    private static string Fixture(string file) => Path.Combine(AppContext.BaseDirectory, "Fixtures", file);
    private static JsonDocument Reference() => JsonDocument.Parse(File.ReadAllBytes(Fixture("MediaLibrary.json")));
    private static JsonElement History(JsonElement r, string id, string theme) => r.GetProperty("histories").EnumerateArray().Single(h => h.GetProperty("id").GetString() == id && h.GetProperty("scheme").GetString() == theme);
    private static JsonElement Step(JsonElement r, string id, string theme, int index = 0) => History(r, id, theme).GetProperty("steps")[index].GetProperty("value");
    private static JsonElement Case(JsonElement r, string id, string theme) => r.GetProperty("cases").EnumerateArray().Single(c => c.GetProperty("id").GetString() == id && c.GetProperty("scheme").GetString() == theme).GetProperty("measurement");
    private static string? Text(JsonElement e, string key) => e.GetProperty(key).GetString();
    private static void Hash(string file, string? expected) => Assert.AreEqual(expected, Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture(file)))));

    [TestMethod]
    public void FrozenSourceKeepsOriginalProducerAssetsCssAndAllSettledSamples()
    {
        Hash("MediaLibrary.json", "11cbfaa6488702b925f04e498647c59dccce1a3229366c1ed530cd34f56257c5");
        using var reference = Reference(); var r = reference.RootElement;
        Assert.AreEqual("a86059013cd59684ce452849278ea81e734f2066", Text(r, "commit"));
        Assert.AreEqual(100, r.GetProperty("cases").GetArrayLength()); Assert.AreEqual(50, r.GetProperty("histories").GetArrayLength());
        Assert.AreEqual(122, r.GetProperty("cssInputs").GetArrayLength()); Assert.AreEqual(1699, r.GetProperty("bundledFiles").GetArrayLength());
        foreach (var (file, field) in new[] { ("CaptureMediaLibrary.cjs", "scriptSha256"), ("MediaLibraryClient.tsx", "clientSha256"), ("MediaLibrary/source.css", "cssSha256"), ("MediaLibrary/bundle.js", "bundleSha256") }) Hash(file, Text(r, field));
        foreach (var field in new[] { "dependencies", "cssInputs" })
            foreach (var input in r.GetProperty(field).EnumerateArray()) Hash("MediaLibrary/Source/" + Text(input, "path"), Text(input, "sha256"));
        foreach (var asset in r.GetProperty("assets").EnumerateArray()) Hash("MediaLibrary/" + Text(asset, "file"), Text(asset, "sha256"));
        foreach (var c in r.GetProperty("cases").EnumerateArray())
        {
            Assert.AreEqual(2, c.GetProperty("stableFrames").GetInt32());
            var samples = c.GetProperty("samples"); Assert.AreEqual(Text(samples[samples.GetArrayLength() - 1], "sha256"), Text(samples[samples.GetArrayLength() - 2], "sha256"));
            Hash("MediaLibrary/" + Text(c, "file"), Text(c, "pngSha256"));
            foreach (var sample in samples.EnumerateArray()) Hash("MediaLibrary/" + Text(sample, "file"), Text(sample, "sha256"));
        }
        Assert.IsFalse(r.GetProperty("nativeImplemented").GetBoolean()); Assert.IsFalse(r.GetProperty("nativeComponentAccepted").GetBoolean()); Assert.IsFalse(r.GetProperty("systemClipboardModified").GetBoolean());
    }

    [TestMethod]
    public void ControlledSearchRequestsOriginalValuesAndOnlyTheCallerFilters()
    {
        using var reference = Reference(); var r = reference.RootElement;
        foreach (var theme in new[] { "Light", "Dark" })
        {
            var rejected = Step(r, "controlled-search-rejected", theme); Assert.AreEqual("", Text(rejected.GetProperty("root").GetProperty("search"), "value"));
            Assert.AreEqual("Original requested search", Text(rejected.GetProperty("events")[0], "value"));
            var accepted = Step(r, "controlled-search-accepted", theme); Assert.AreEqual("Original accepted search", Text(accepted.GetProperty("root").GetProperty("search"), "value"));
            var cleared = Step(r, "controlled-search-accepted", theme, 1); Assert.AreEqual("", Text(cleared.GetProperty("root").GetProperty("search"), "value")); Assert.AreEqual("", Text(cleared.GetProperty("events")[1], "value"));
            Assert.AreEqual(6, Step(r, "source-does-not-filter", theme).GetProperty("root").GetProperty("cards").GetArrayLength());
            Assert.AreEqual(1, Step(r, "caller-filters-and-clears", theme).GetProperty("root").GetProperty("cards").GetArrayLength()); Assert.AreEqual(6, Step(r, "caller-filters-and-clears", theme, 1).GetProperty("root").GetProperty("cards").GetArrayLength());
        }
    }

    [TestMethod]
    public void CopyAndDeleteCrossCallerBoundariesWithOriginalStringsAndWithoutCollectionMutation()
    {
        using var reference = Reference(); var r = reference.RootElement; var original = r.GetProperty("defaultItems")[0];
        foreach (var theme in new[] { "Light", "Dark" })
        {
            var copy = Step(r, "copy-callback-originals", theme, 1); var events = copy.GetProperty("events");
            Assert.AreEqual(Text(original, "filename"), Text(events[0], "text")); Assert.AreEqual(Text(original, "url"), Text(events[1], "text")); Assert.AreEqual(0, copy.GetProperty("clipboard").GetArrayLength());
            var clipboard = Step(r, "clipboard-boundary-originals", theme, 1); Assert.AreEqual(0, clipboard.GetProperty("events").GetArrayLength()); Assert.AreEqual(Text(original, "url"), Text(clipboard.GetProperty("clipboard")[1], "text"));
            var deletion = Step(r, "delete-keyboard-original-id", theme); Assert.AreEqual("portrait", Text(deletion.GetProperty("events")[0], "id")); Assert.AreEqual(6, deletion.GetProperty("root").GetProperty("cards").GetArrayLength()); Assert.AreEqual(JsonValueKind.Null, deletion.GetProperty("dialog").ValueKind);
            // Preserve the source defect as evidence. The user explicitly requires working pointer Delete in WinUI.
            var pointer = Step(r, "delete-pointer-routing", theme, 1); Assert.AreEqual(0, pointer.GetProperty("events").GetArrayLength()); Assert.AreEqual(Text(original, "filename"), Text(pointer.GetProperty("dialog").GetProperty("filename"), "text"));
        }
    }

    [TestMethod]
    public void PreviewRetainsTheSelectedOriginalAcrossRemovalReplacementAndMutation()
    {
        using var reference = Reference(); var r = reference.RootElement;
        foreach (var theme in new[] { "Light", "Dark" })
        {
            foreach (var id in new[] { "preview-survives-removal", "preview-keeps-replaced-original" }) Assert.AreEqual("fictional-coast.svg", Text(Step(r, id, theme).GetProperty("dialog").GetProperty("filename"), "text"));
            Assert.AreEqual(0, Step(r, "preview-survives-removal", theme).GetProperty("root").GetProperty("cards").GetArrayLength());
            Assert.AreEqual("Open Replacement caller filename", Text(Step(r, "preview-keeps-replaced-original", theme).GetProperty("root").GetProperty("cards")[0].GetProperty("open"), "name"));
            Assert.AreEqual("Mutated original filename", Text(Step(r, "original-object-mutation-updates-preview", theme).GetProperty("dialog").GetProperty("filename"), "text"));
            var cleared = Step(r, "live-preview-metadata-clears", theme).GetProperty("dialog"); foreach (var key in new[] { "url", "alt", "created", "image" }) Assert.AreEqual(JsonValueKind.Null, cleared.GetProperty(key).ValueKind);
        }
    }

    [TestMethod]
    public void TilesPreferThumbnailsButPreviewUsesOnlyTheOriginalFullUrlWithoutFailureFallback()
    {
        using var reference = Reference(); var r = reference.RootElement;
        foreach (var theme in new[] { "Light", "Dark" })
        {
            var tile = Case(r, "thumbnail-and-url", theme).GetProperty("root").GetProperty("cards")[0]; Assert.AreEqual(Text(r.GetProperty("defaultItems")[0], "thumbnailUrl"), Text(tile.GetProperty("image"), "src"));
            var preview = Case(r, "preview-default", theme).GetProperty("dialog"); Assert.AreEqual(Text(r.GetProperty("defaultItems")[0], "url"), Text(preview.GetProperty("image"), "src")); Assert.AreEqual(Text(preview.GetProperty("url"), "href"), Text(preview.GetProperty("image"), "src"));
            Assert.AreEqual(JsonValueKind.Null, Case(r, "preview-thumbnail-only", theme).GetProperty("dialog").GetProperty("image").ValueKind);
            Assert.AreEqual(0, Case(r, "broken-thumbnail", theme).GetProperty("root").GetProperty("cards")[0].GetProperty("image").GetProperty("naturalWidth").GetInt32()); Assert.AreEqual(0, Case(r, "preview-broken-full", theme).GetProperty("dialog").GetProperty("image").GetProperty("naturalWidth").GetInt32());
            Assert.AreEqual("No preview", Text(Case(r, "no-preview", theme).GetProperty("root").GetProperty("cards")[0].GetProperty("placeholder"), "text"));
            Assert.AreEqual("Media Preview", Text(Case(r, "preview-empty-filename", theme).GetProperty("dialog").GetProperty("title"), "text"));
        }
    }

    [TestMethod]
    public void CallerEditorsKeyedTilesAndPreviewPresenceSurviveLiveUpdates()
    {
        using var reference = Reference(); var r = reference.RootElement;
        foreach (var theme in new[] { "Light", "Dark" })
        {
            var slots = History(r, "caller-slots-retained", theme); Assert.IsTrue(slots.GetProperty("sameActionNode").GetBoolean()); Assert.IsTrue(slots.GetProperty("sameFilterNode").GetBoolean()); Assert.AreEqual("Retained original filter edit", Text(Step(r, "caller-slots-retained", theme).GetProperty("root"), "filterEditor"));
            Assert.IsTrue(History(r, "keyed-tiles-retained-through-reorder", theme).GetProperty("sameTileNode").GetBoolean()); Assert.IsTrue(History(r, "responsive-reflow-retains-tiles", theme).GetProperty("sameTileNode").GetBoolean());
            Assert.IsTrue(History(r, "rapid-close-reopen-retains-presence", theme).GetProperty("sameDialogNode").GetBoolean());
            Assert.AreEqual(JsonValueKind.Null, Step(r, "unmount-remount-clears-preview", theme, 1).GetProperty("dialog").ValueKind);
        }
    }

    [TestMethod]
    public void ModalDismissalRestoresTheOpenerAndTheOriginalUrlKeepsItsSecurityAttributes()
    {
        using var reference = Reference(); var r = reference.RootElement;
        foreach (var theme in new[] { "Light", "Dark" })
        {
            foreach (var id in new[] { "close-escape", "close-backdrop", "close-button" })
            {
                Assert.IsTrue(Step(r, id, theme).GetProperty("rootInert").GetBoolean()); var closed = Step(r, id, theme, 1); Assert.IsFalse(closed.GetProperty("rootInert").GetBoolean()); Assert.AreEqual("", Text(closed, "bodyInlineOverflow")); Assert.AreEqual("Open fictional-coast.svg", Text(closed, "focus")); Assert.AreEqual(JsonValueKind.Null, closed.GetProperty("dialog").ValueKind);
            }
            var url = Case(r, "preview-default", theme).GetProperty("dialog").GetProperty("url"); Assert.AreEqual("_blank", Text(url, "target")); Assert.AreEqual("noreferrer noopener", Text(url, "rel"));
            CollectionAssert.AreEqual(new[] { "Close", "Copy filename", Text(r.GetProperty("defaultItems")[0], "url"), "Copy URL", "Close" }, History(r, "modal-tab-cycle", theme).GetProperty("tabOrder").EnumerateArray().Select(e => e.GetString()).ToArray());
        }
    }

    [TestMethod]
    public void ResponsiveColumnsFixedPixelTypographyAndLocalDatesKeepTheOriginalContract()
    {
        using var reference = Reference(); var r = reference.RootElement;
        foreach (var theme in new[] { "Light", "Dark" })
        {
            foreach (var (id, columns) in new[] { ("width-767", 2), ("width-768", 4), ("width-1023", 4), ("width-1024", 5), ("narrow-host", 4) }) Assert.AreEqual(columns, Text(Case(r, id, theme).GetProperty("root").GetProperty("grid").GetProperty("paint"), "gridColumns")!.Split(' ').Length);
            var scaled = Case(r, "font200", theme).GetProperty("root"); Assert.AreEqual("10px", Text(scaled.GetProperty("header").GetProperty("kicker").GetProperty("paint"), "fontSize")); Assert.AreEqual("64px", Text(scaled.GetProperty("header").GetProperty("title").GetProperty("paint"), "fontSize")); Assert.AreEqual("12px", Text(scaled.GetProperty("cards")[0].GetProperty("overlay").GetProperty("filename").GetProperty("paint"), "fontSize"));
            Assert.AreEqual("3/10/2026", Text(Case(r, "preview-url-only", theme).GetProperty("dialog").GetProperty("created").GetProperty("value"), "text")); Assert.AreEqual("Invalid Date", Text(Case(r, "preview-invalid-date", theme).GetProperty("dialog").GetProperty("created").GetProperty("value"), "text"));
            Assert.AreEqual(theme == "Dark" ? "rgb(224, 172, 105)" : "rgb(193, 129, 77)", Text(scaled.GetProperty("header").GetProperty("kicker").GetProperty("paint"), "color"));
        }
    }
}
