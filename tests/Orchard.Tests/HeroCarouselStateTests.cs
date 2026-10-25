using System.Security.Cryptography;
using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class HeroCarouselStateTests
{
    private static string Fixture(string file) => Path.Combine(AppContext.BaseDirectory, "Fixtures", file);
    private static JsonDocument Reference() => JsonDocument.Parse(File.ReadAllBytes(Fixture("HeroCarousel.json")));
    private static JsonElement History(JsonElement root, string id, string theme) => root.GetProperty("histories").EnumerateArray().Single(h => h.GetProperty("id").GetString() == id && h.GetProperty("scheme").GetString() == theme);
    private static void Matches(BleeckerHeroCarouselState state, JsonElement history, string label)
    {
        var step = history.GetProperty("steps").EnumerateArray().Single(s => s.GetProperty("label").GetString() == label);
        var sections = step.GetProperty("value").GetProperty("sections");
        Assert.AreEqual(state.IsMounted ? 1 : 0, sections.GetArrayLength(), label);
        if (!state.IsMounted) return;
        CollectionAssert.AreEqual(state.HasActiveSlide ? new[] { state.Index } : Array.Empty<int>(), sections[0].GetProperty("activeIndices").EnumerateArray().Select(i => i.GetInt32()).ToArray(), label);
        Assert.AreEqual(state.Count == 0, sections[0].GetProperty("empty").GetBoolean(), label);
    }

    [TestMethod]
    public void FrozenAssetsBindOriginalSourceScriptsImagesAndEveryUnmodifiedScreenshot()
    {
        var bytes = File.ReadAllBytes(Fixture("HeroCarousel.json"));
        Assert.AreEqual("1a16b5f6698507791b3f7a396eafe145a8432cc32f858b0654af698aca261227", Convert.ToHexStringLower(SHA256.HashData(bytes)));
        using var reference = JsonDocument.Parse(bytes); var root = reference.RootElement;
        Assert.AreEqual("a86059013cd59684ce452849278ea81e734f2066", root.GetProperty("commit").GetString());
        Assert.AreEqual(86, root.GetProperty("cases").GetArrayLength()); Assert.AreEqual(54, root.GetProperty("histories").GetArrayLength());
        foreach (var (file, field) in new[] { ("CaptureHeroCarousel.cjs", "scriptSha256"), ("HeroCarouselClient.tsx", "clientSha256"), ("HeroCarousel/source.css", "cssSha256"), ("HeroCarousel/bundle.js", "bundleSha256") })
            Assert.AreEqual(root.GetProperty(field).GetString(), Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture(file)))));
        foreach (var c in root.GetProperty("cases").EnumerateArray())
            Assert.AreEqual(c.GetProperty("pngSha256").GetString(), Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture("HeroCarousel/" + c.GetProperty("file").GetString())))));
        foreach (var input in root.GetProperty("dependencies").EnumerateArray())
            Assert.AreEqual(input.GetProperty("sha256").GetString(), Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture("HeroCarousel/Source/" + input.GetProperty("path").GetString())))));
        foreach (var image in root.GetProperty("assets").EnumerateArray())
            Assert.AreEqual(image.GetProperty("sha256").GetString(), Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture("HeroCarousel/" + image.GetProperty("file").GetString())))));
    }

    [TestMethod]
    public void SourceTicksWrapAndManualNavigationDoesNotRestartTheCurrentTimer()
    {
        using var reference = Reference();
        foreach (var theme in new[] { "Light", "Dark" })
        {
            var history = History(reference.RootElement, "default-interval", theme); var state = new BleeckerHeroCarouselState(3); var generation = state.TimerGeneration;
            Assert.IsTrue(state.ShouldSchedule); Assert.AreEqual(5000d, state.AutoAdvanceMilliseconds); Matches(state, history, "initial"); Matches(state, history, "before");
            Assert.IsTrue(state.Tick(generation)); Matches(state, history, "next"); state.Tick(generation); state.Tick(generation); Matches(state, history, "wrapped");
            state = new BleeckerHeroCarouselState(3); history = History(reference.RootElement, "manual-does-not-restart", theme); generation = state.TimerGeneration;
            state.Next(); Matches(state, history, "manual"); Assert.AreEqual(generation, state.TimerGeneration); state.Tick(generation); Matches(state, history, "original-deadline");
            state = new BleeckerHeroCarouselState(3); history = History(reference.RootElement, "previous-next-wrap", theme); state.Previous(); Matches(state, history, "last"); state.Next(); Matches(state, history, "first");
        }
    }

    [TestMethod]
    public void HoverAndChangedEffectDependenciesRetireThePreviousTimerGeneration()
    {
        using var reference = Reference();
        foreach (var theme in new[] { "Light", "Dark" })
        {
            var state = new BleeckerHeroCarouselState(3); var old = state.TimerGeneration; state.SetHovered(true); Assert.IsFalse(state.ShouldSchedule); Assert.IsFalse(state.Tick(old));
            var history = History(reference.RootElement, "hover-pause-restart", theme); Matches(state, history, "paused"); state.SetHovered(false); Assert.IsTrue(state.ShouldSchedule); Assert.IsFalse(state.Tick(old)); Matches(state, history, "resumed-before"); state.Tick(state.TimerGeneration); Matches(state, history, "resumed-next");
            state = new BleeckerHeroCarouselState(3); old = state.TimerGeneration; state.Configure(2, 5000); history = History(reference.RootElement, "count-change-restarts-clock", theme); Assert.IsFalse(state.Tick(old)); Matches(state, history, "old-deadline"); state.Tick(state.TimerGeneration); Matches(state, history, "new-deadline");
            state = new BleeckerHeroCarouselState(3); old = state.TimerGeneration; state.Configure(3, 100); history = History(reference.RootElement, "interval-change-restarts-clock", theme); Assert.IsFalse(state.Tick(old)); Matches(state, history, "before"); state.Tick(state.TimerGeneration); Matches(state, history, "new-deadline");
        }
    }

    [TestMethod]
    public void SameCountReorderKeepsTheNumericIndexAndOriginalTimerGeneration()
    {
        using var reference = Reference();
        foreach (var theme in new[] { "Light", "Dark" })
        {
            var state = new BleeckerHeroCarouselState(3); state.Select(1); var generation = state.TimerGeneration; state.Configure(3, 5000);
            var history = History(reference.RootElement, "same-count-reorder-keeps-index-and-clock", theme); Assert.AreEqual(generation, state.TimerGeneration); Matches(state, history, "reordered"); state.Tick(generation); Matches(state, history, "old-deadline");
        }
    }

    [TestMethod]
    public void ShrinkSingleAndEmptyUpdatesDoNotSilentlyClampOrResetTheSourceIndex()
    {
        using var reference = Reference();
        foreach (var theme in new[] { "Light", "Dark" })
        {
            var state = new BleeckerHeroCarouselState(3); state.Select(2); state.Configure(2, 5000); var history = History(reference.RootElement, "shrink-preserves-out-of-range-index", theme); Matches(state, history, "all-hidden"); state.Previous(); Matches(state, history, "recovered");
            state = new BleeckerHeroCarouselState(3); state.Select(2); state.Configure(1, 5000); Assert.IsFalse(state.ShouldSchedule); Assert.IsFalse(state.ApplySourceKey("ArrowRight", "BODY")); history = History(reference.RootElement, "single-keeps-out-of-range-index", theme); Matches(state, history, "still-hidden"); state.Configure(3, 5000); Matches(state, history, "restored-third");
            state = new BleeckerHeroCarouselState(3); state.Select(1); state.Configure(0, 5000); history = History(reference.RootElement, "empty-keeps-index", theme); Matches(state, history, "empty"); state.Configure(3, 5000); Matches(state, history, "restored-second");
        }
    }

    [TestMethod]
    public void SourceKeysIgnoreOnlyInputAndTextareaAndCanAffectBothMountedHosts()
    {
        using var reference = Reference();
        foreach (var theme in new[] { "Light", "Dark" })
        {
            var state = new BleeckerHeroCarouselState(3); Assert.IsFalse(state.ApplySourceKey("ArrowRight", "INPUT")); Matches(state, History(reference.RootElement, "ignore-input", theme), "ignored"); Assert.IsFalse(state.ApplySourceKey("ArrowRight", "TEXTAREA")); Matches(state, History(reference.RootElement, "ignore-textarea", theme), "ignored");
            Assert.IsTrue(state.ApplySourceKey("ArrowRight", "DIV")); Matches(state, History(reference.RootElement, "contenteditable-is-not-ignored", theme), "advanced");
            state = new BleeckerHeroCarouselState(3); var other = new BleeckerHeroCarouselState(2); state.ApplySourceKey("ArrowRight", "BUTTON"); other.ApplySourceKey("ArrowRight", "BUTTON");
            var step = History(reference.RootElement, "multiple-hosts-global-key", theme).GetProperty("steps")[0].GetProperty("value").GetProperty("sections");
            Assert.AreEqual(state.Index, step[0].GetProperty("activeIndices")[0].GetInt32()); Assert.AreEqual(other.Index, step[1].GetProperty("activeIndices")[0].GetInt32());
        }
    }

    [TestMethod]
    public void RemovingAnInstanceRevokesStaleTimerAndInputWhileRemountStartsAtZero()
    {
        using var reference = Reference();
        foreach (var theme in new[] { "Light", "Dark" })
        {
            var state = new BleeckerHeroCarouselState(3); state.Select(2); var old = state.TimerGeneration; state.Remove(); Assert.IsFalse(state.ShouldSchedule); Assert.IsFalse(state.Tick(old)); Assert.IsFalse(state.Next()); Assert.IsFalse(state.Select(0)); Assert.IsFalse(state.ApplySourceKey("ArrowRight", "BODY"));
            var history = History(reference.RootElement, "unmount-revokes-clock-and-key", theme); Matches(state, history, "unmounted"); Matches(new BleeckerHeroCarouselState(3), history, "remounted-zero");
        }
    }
}
