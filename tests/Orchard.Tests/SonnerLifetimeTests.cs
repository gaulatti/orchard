using System.Security.Cryptography;
using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class SonnerLifetimeTests
{
    private static string Fixture(string file) => Path.Combine(AppContext.BaseDirectory, "Fixtures", file);
    private static JsonDocument Reference() => JsonDocument.Parse(File.ReadAllBytes(Fixture("Sonner.json")));
    private static JsonElement History(JsonElement root, string id, string scheme) => root.GetProperty("histories").EnumerateArray().Single(h => h.GetProperty("id").GetString() == id && h.GetProperty("scheme").GetString() == scheme);
    private static void Matches(BleeckerSonnerLifetime lifetime, JsonElement history, string label)
    {
        var step = history.GetProperty("steps").EnumerateArray().Single(s => s.GetProperty("label").GetString() == label);
        var items = step.GetProperty("value").GetProperty("hosts").EnumerateArray().SelectMany(h => h.GetProperty("items").EnumerateArray()).ToArray();
        Assert.AreEqual(items.Length == 1, lifetime.IsMounted, label);
        Assert.AreEqual(items.Length == 1 && items[0].GetProperty("visible").GetBoolean(), lifetime.IsVisible, label);
    }

    [TestMethod]
    public void FrozenReferenceBindsOriginalSourceScriptsAndEveryUnmodifiedScreenshot()
    {
        using var reference = Reference(); var root = reference.RootElement;
        Assert.AreEqual("a86059013cd59684ce452849278ea81e734f2066", root.GetProperty("commit").GetString());
        Assert.AreEqual("9abd2f6c1f907708e499170c6368e20f8400afb0c0c5dc07f60fb4635289154d", root.GetProperty("dependencies")[0].GetProperty("sha256").GetString());
        foreach (var (file, field) in new[] { ("CaptureSonner.cjs", "scriptSha256"), ("SonnerClient.tsx", "clientSha256") })
            Assert.AreEqual(root.GetProperty(field).GetString(), Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture(file)))));
        Assert.AreEqual(70, root.GetProperty("cases").GetArrayLength());
        Assert.AreEqual(38, root.GetProperty("histories").GetArrayLength());
        foreach (var row in root.GetProperty("cases").EnumerateArray())
            Assert.AreEqual(row.GetProperty("pngSha256").GetString(), Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture("Sonner/" + row.GetProperty("file").GetString())))));
        foreach (var input in root.GetProperty("dependencies").EnumerateArray())
            Assert.AreEqual(input.GetProperty("sha256").GetString(), Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture("Sonner/Source/" + input.GetProperty("path").GetString())))));
    }

    [TestMethod]
    public void TimeoutHidesAtTheSourceDeadlineAndRetainsTheItemForExactlyThreeHundredMilliseconds()
    {
        using var reference = Reference();
        foreach (var scheme in new[] { "Light", "Dark" })
        {
            var history = History(reference.RootElement, "default-timeout", scheme); var lifetime = new BleeckerSonnerLifetime();
            Matches(lifetime, history, "added"); lifetime.Frame(16); Matches(lifetime, history, "frame");
            lifetime.AdvanceTo(3999); Matches(lifetime, history, "before-timeout"); lifetime.Dismiss(4000); Matches(lifetime, history, "hidden-at-4000");
            lifetime.AdvanceTo(4299); Matches(lifetime, history, "retained-299"); lifetime.AdvanceTo(4300); Matches(lifetime, history, "removed-300");
            Assert.IsFalse(lifetime.CanInvokeAction); lifetime.Frame(4301); Assert.IsFalse(lifetime.IsMounted); Assert.IsFalse(lifetime.IsVisible);
        }
    }

    [TestMethod]
    public void ImperativeDismissalRemovesImmediatelyWhileTheDismissButtonRetainsItsItem()
    {
        using var reference = Reference();
        foreach (var scheme in new[] { "Light", "Dark" })
        {
            var button = History(reference.RootElement, "button-dismiss", scheme); var lifetime = new BleeckerSonnerLifetime();
            lifetime.Frame(16); Matches(lifetime, button, "before"); lifetime.Dismiss(16); Matches(lifetime, button, "hidden");
            lifetime.AdvanceTo(315); Matches(lifetime, button, "retained-299"); lifetime.AdvanceTo(316); Matches(lifetime, button, "removed-300");
            var api = History(reference.RootElement, "api-dismiss", scheme); lifetime = new BleeckerSonnerLifetime(); lifetime.Frame(16); Matches(lifetime, api, "before");
            lifetime.Remove(); Matches(lifetime, api, "removed-immediately");
        }
    }

    [TestMethod]
    public void AnUncancelledFirstFrameCanRestoreVisibilityAfterEarlyTimeoutOrButtonDismissal()
    {
        using var reference = Reference();
        foreach (var scheme in new[] { "Light", "Dark" })
        {
            var timeout = History(reference.RootElement, "short-timeout-before-frame", scheme); var lifetime = new BleeckerSonnerLifetime();
            Matches(lifetime, timeout, "added"); lifetime.Dismiss(1); Matches(lifetime, timeout, "timed-out"); lifetime.Frame(17); Matches(lifetime, timeout, "late-frame");
            lifetime.AdvanceTo(301); Matches(lifetime, timeout, "removed");
            var button = History(reference.RootElement, "button-before-frame", scheme); lifetime = new BleeckerSonnerLifetime();
            lifetime.Dismiss(0); Matches(lifetime, button, "hidden"); lifetime.Frame(16); Matches(lifetime, button, "late-frame"); lifetime.AdvanceTo(300); Matches(lifetime, button, "removed");
        }
    }

    [TestMethod]
    public void RetainedSourceActionsRemainCallableUntilRemovalEvenWhenTheItemIsTransparent()
    {
        using var reference = Reference();
        foreach (var scheme in new[] { "Light", "Dark" })
        {
            var history = History(reference.RootElement, "retained-action", scheme); var lifetime = new BleeckerSonnerLifetime();
            lifetime.Frame(16); lifetime.Dismiss(16); Matches(lifetime, history, "hidden"); Assert.IsTrue(lifetime.CanInvokeAction);
            Matches(lifetime, history, "action-while-retained");
            var step = history.GetProperty("steps").EnumerateArray().Single(s => s.GetProperty("label").GetString() == "action-while-retained");
            Assert.AreEqual(1, step.GetProperty("value").GetProperty("events").GetArrayLength());
            lifetime.AdvanceTo(316); Matches(lifetime, history, "removed"); Assert.IsFalse(lifetime.CanInvokeAction);
        }
    }
}
