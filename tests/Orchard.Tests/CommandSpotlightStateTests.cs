using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class CommandSpotlightStateTests
{
    private sealed class ActionData(string id, string title, string? description = null, string? group = null, string[]? keywords = null, string? href = null, bool disabled = false)
    {
        internal string Id = id, Title = title;
        internal string? Description = description, Group = group, Href = href;
        internal bool Disabled = disabled;
        internal string[] Keywords = keywords ?? [];
        internal BleeckerCommandSpotlightData Describe() => new(Id, Title, Description, Group, Keywords, Href, Disabled);
    }
    private static ActionData Read(JsonElement a)
    {
        string? Optional(string key) => a.TryGetProperty(key, out var value) ? value.GetString() : null;
        return new(a.GetProperty("id").GetString()!, a.GetProperty("title").GetString()!, Optional("description"), Optional("group"), a.TryGetProperty("keywords", out var words) ? words.EnumerateArray().Select(w => w.GetString()!).ToArray() : [], Optional("href"), a.TryGetProperty("disabled", out var disabled) && disabled.GetBoolean());
    }

    [TestMethod]
    public void NativeModelMatchesEveryFrozenSourceFilterAndGroupCase()
    {
        using var reference = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "CommandSpotlight.json")));
        var r = reference.RootElement;
        foreach (var c in r.GetProperty("cases").EnumerateArray().Where(c => c.GetProperty("measurement").GetProperty("dialog").ValueKind == JsonValueKind.Object))
        {
            var spec = c.GetProperty("spec"); var source = spec.TryGetProperty("actions", out var supplied) ? supplied : r.GetProperty("defaultActions"); var actions = source.EnumerateArray().Select(Read).ToArray();
            var model = new BleeckerCommandSpotlightState<ActionData>(a => a.Describe()); model.SetActions(actions, true);
            var query = c.GetProperty("query"); if (query.ValueKind != JsonValueKind.Null) model.SetQuery(query.GetString()!);
            var actual = c.GetProperty("measurement").GetProperty("dialog");
            CollectionAssert.AreEqual(actual.GetProperty("groups").EnumerateArray().Select(g => g.GetProperty("label").GetProperty("text").GetString()).ToArray(), model.Groups.Select(g => g.Name).ToArray());
            for (var i = 0; i < model.Groups.Count; i++) CollectionAssert.AreEqual(actual.GetProperty("groups")[i].GetProperty("rows").EnumerateArray().Select(row => row.GetProperty("title").GetProperty("text").GetString()).ToArray(), model.Groups[i].Actions.Select(a => a.Title).ToArray());
            if (c.GetProperty("interaction").GetString() != "row-hover") Assert.AreEqual(actual.GetProperty("groups").EnumerateArray().SelectMany(g => g.GetProperty("rows").EnumerateArray()).Where(row => row.GetProperty("selected").GetBoolean()).Select(row => row.GetProperty("title").GetProperty("text").GetString()).SingleOrDefault(), model.EnterTarget?.Title);
        }
    }

    [TestMethod]
    public void ControlledCloseCanBeRejectedAndStillClearsTheQuery()
    {
        var a = new ActionData("original", "Original command"); var m = new BleeckerCommandSpotlightState<ActionData>(a => a.Describe()); m.SetActions([a], true); m.SetQuery("original");
        var requests = new List<bool>(); m.Request(false, true, requests.Add); Assert.IsTrue(m.Effective(true)); Assert.AreEqual("", m.Query); Assert.AreSame(a, m.EnterTarget); CollectionAssert.AreEqual(new[] { false }, requests);
        m.Request(true, null, requests.Add); Assert.IsTrue(m.Effective(null)); m.Request(false, null, requests.Add); Assert.IsFalse(m.Effective(null));
    }

    [TestMethod]
    public void KeyboardUsesOriginalOrderAndPreservesTheOriginalAcrossReorder()
    {
        ActionData a = new("a", "Alpha", group: "Navigation"), b = new("b", "Beta", group: "Tools"), c = new("c", "Gamma", group: "Navigation"), d = new("disabled", "Disabled", disabled: true);
        var m = new BleeckerCommandSpotlightState<ActionData>(a => a.Describe()); m.SetActions([a, b, c, d], true); m.Move(1); Assert.AreSame(b, m.EnterTarget); m.Move(1); Assert.AreSame(c, m.EnterTarget); m.Move(1); Assert.AreSame(a, m.EnterTarget); m.Move(-1); Assert.AreSame(c, m.EnterTarget);
        m.SetActions([d, c, b, a], true); Assert.AreSame(c, m.EnterTarget); m.Hover(d); Assert.AreSame(c, m.EnterTarget); m.SetQuery("Beta"); Assert.AreSame(b, m.EnterTarget); m.SetActions([], true); Assert.IsNull(m.SelectedId);
    }

    [TestMethod]
    public void SourceMemoBoundaryRetainsOriginalObjectsUntilQueryOrCollectionChanges()
    {
        var a = new ActionData("original", "Original", group: "Before"); var m = new BleeckerCommandSpotlightState<ActionData>(a => a.Describe()); m.SetActions([a], true);
        a.Title = "Mutated"; a.Group = "After"; a.Disabled = true;
        Assert.AreSame(a, m.EnterTarget); Assert.AreEqual("Before", m.Groups[0].Name); Assert.IsTrue(m.EnterTarget!.Disabled);
        m.SetQuery("Mutated"); Assert.AreEqual("After", m.Groups[0].Name); Assert.IsNull(m.SelectedId);
        a.Disabled = false; m.SetActions([a], true); Assert.AreSame(a, m.EnterTarget);
    }

    [TestMethod]
    public void SearchKeepsWholePhrasesUnicodeAndEcmaWhitespace()
    {
        var a = new ActionData("unicode", "Straße 東京 é العربية", keywords: ["İstanbul", "ΣΟΣ"]); var m = new BleeckerCommandSpotlightState<ActionData>(a => a.Describe()); m.SetActions([a], true);
        foreach (var query in new[] { "i\u0307stanbul", "σος", "東京", "\uFEFFStraße\uFEFF" }) { m.SetQuery(query); Assert.AreSame(a, m.EnterTarget, query); }
        m.SetQuery("strasse"); Assert.AreEqual(0, m.Filtered.Count); m.SetQuery("Straße العربية"); Assert.AreEqual(0, m.Filtered.Count); m.SetQuery("\u0085"); Assert.AreEqual(0, m.Filtered.Count);
        Assert.ThrowsExactly<ArgumentException>(() => m.SetActions([a, a], true));
    }

    [TestMethod]
    public async Task DispatchPreservesOriginalIdentityOrderingAndClosesBeforeAsyncResolution()
    {
        var original = new object(); var events = new List<string>(); var pending = new TaskCompletionSource();
        BleeckerCommandSpotlightInvocation.Run(original, value => { Assert.AreSame(original, value); events.Add("outer"); }, () => () => { events.Add("own"); return pending.Task; }, () => () => events.Add("wrong href"), () => events.Add("close"), _ => events.Add("error"));
        CollectionAssert.AreEqual(new[] { "outer", "own", "close" }, events); pending.SetResult(); await pending.Task;
        events.Clear(); BleeckerCommandSpotlightInvocation.Run(original, null, () => () => throw new InvalidOperationException("original failure"), () => null, () => events.Add("close"), error => { Assert.AreEqual("original failure", error.Message); events.Add("error"); }); CollectionAssert.AreEqual(new[] { "error", "close" }, events);
        var observed = new TaskCompletionSource<Exception>(); BleeckerCommandSpotlightInvocation.Run(original, null, () => () => Task.FromException(new InvalidOperationException("async original")), () => null, () => { }, error => observed.SetResult(error)); Assert.AreEqual("async original", (await observed.Task).Message);
    }

    [TestMethod]
    public void OuterFailureStopsOwnCallbackNavigationAndClose()
    {
        var events = new List<string>();
        Assert.ThrowsExactly<InvalidOperationException>(() => BleeckerCommandSpotlightInvocation.Run(new object(), _ => throw new InvalidOperationException(), () => () => { events.Add("own"); return null; }, () => () => events.Add("navigate"), () => events.Add("close"), _ => events.Add("caught"))); Assert.AreEqual(0, events.Count);
        BleeckerCommandSpotlightInvocation.Run(new object(), _ => events.Add("outer"), () => null, () => () => events.Add("navigate original"), () => events.Add("close"), _ => events.Add("error")); CollectionAssert.AreEqual(new[] { "outer", "navigate original", "close" }, events);
    }

    [TestMethod]
    public void OuterMutationSelectsTheCurrentOwnCallbackOrHrefBeforeClosing()
    {
        var original = new object(); var events = new List<string>(); Func<Task?>? callback = null; var href = "/old";
        BleeckerCommandSpotlightInvocation.Run(original, _ => { events.Add("outer"); callback = () => { events.Add("new own"); return null; }; }, () => callback, () => () => events.Add(href), () => events.Add("close"), _ => events.Add("error"));
        CollectionAssert.AreEqual(new[] { "outer", "new own", "close" }, events);
        events.Clear(); BleeckerCommandSpotlightInvocation.Run(original, _ => { callback = null; href = "/current-original"; }, () => callback, () => () => events.Add(href), () => events.Add("close"), _ => events.Add("error"));
        CollectionAssert.AreEqual(new[] { "/current-original", "close" }, events);
    }
}
