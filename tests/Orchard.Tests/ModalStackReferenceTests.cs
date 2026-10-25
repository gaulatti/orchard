using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class ModalStackReferenceTests
{
    private sealed class Layer(string id) { internal string Id { get; } = id; }
    private sealed record Target(string Id, Layer? Owner, bool Branch = false);
    private static JsonDocument Reference() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "AlertDialog.json")));
    private static bool Owns(Layer layer, Target target) => ReferenceEquals(layer, target.Owner);

    [TestMethod]
    public void SequentialAndSimultaneousMountMatchOriginalLogicalOrderAcrossPortals()
    {
        using var d = Reference();
        foreach (var scheme in new[] { "Light", "Dark" }) foreach (var simultaneous in new[] { false, true })
        {
            var id = simultaneous ? "simultaneous-original-modal-ancestry" : "nested-original-modal-drawer-and-popup";
            var row = d.RootElement.GetProperty("interactions").EnumerateArray().Single(r => r.GetProperty("scheme").GetString() == scheme && r.GetProperty("id").GetString() == id);
            var stack = new BleeckerModalStackModel<Layer, Target>(); var drawer = new Layer("drawer"); var modal = new Layer("modal"); var alert = new Layer("alert");
            if (simultaneous)
            {
                // Original simultaneous React effects register child-first.
                stack.Add(alert, [drawer, modal], null); stack.Add(modal, [drawer], null); stack.Add(drawer, [], null);
            }
            else { stack.Add(drawer, [], null); stack.Add(modal, [drawer], null); stack.Add(alert, [drawer, modal], null); }
            CollectionAssert.AreEqual(new[] { drawer, modal, alert }, stack.Entries.Select(e => e.Layer).ToArray());
            var layers = row.GetProperty("samples")[0].GetProperty("layers");
            var expected = simultaneous ? layers.EnumerateArray().Reverse().ToArray() : layers.EnumerateArray().ToArray();
            for (var i = 0; i < 3; i++) { Assert.AreEqual(expected[i].GetProperty("zIndex").GetString(), stack.Entries[i].OverlayZIndex.ToString()); Assert.AreEqual(!expected[i].GetProperty("inert").GetBoolean(), stack.Entries[i].Interactive); }
            Assert.AreSame(alert, stack.Active!.Layer); Assert.AreEqual(53, stack.Active.BranchZIndex);
        }
    }

    [TestMethod]
    public void SequentialDismissalReturnsTheOriginalRestorationTargetsWithoutClosingParents()
    {
        using var d = Reference(); foreach (var scheme in new[] { "Light", "Dark" })
        {
            var samples = d.RootElement.GetProperty("interactions").EnumerateArray().Single(r => r.GetProperty("scheme").GetString() == scheme && r.GetProperty("id").GetString() == "nested-original-modal-drawer-and-popup").GetProperty("samples");
            var drawer = new Layer("drawer"); var modal = new Layer("modal"); var alert = new Layer("alert"); var stack = new BleeckerModalStackModel<Layer, Target>();
            stack.Add(drawer, [], new("opener", null)); stack.Add(modal, [drawer], new("middle-opener", drawer)); stack.Add(alert, [drawer, modal], new("alert-opener", modal));
            foreach (var (layer, sample) in new[] { (alert, 1), (modal, 4), (drawer, 5) })
            {
                var removed = stack.Remove(layer, Owns); Assert.IsTrue(removed.WasActive); Assert.AreEqual(samples[sample].GetProperty("focused").GetString(), removed.PreviousFocus!.Id);
                var active = samples[sample].GetProperty("layers").EnumerateArray().SingleOrDefault(r => !r.GetProperty("inert").GetBoolean());
                if (removed.Active is null) Assert.AreEqual(JsonValueKind.Undefined, active.ValueKind);
                else Assert.AreEqual(active.GetProperty("zIndex").GetString(), removed.Active.OverlayZIndex.ToString());
            }
            Assert.AreEqual(0, stack.Entries.Count);
        }
    }

    [TestMethod]
    public void AncestorFirstTeardownRewiresFocusAndKeepsTheLiveChildActive()
    {
        using var d = Reference(); foreach (var scheme in new[] { "Light", "Dark" })
        {
            var samples = d.RootElement.GetProperty("interactions").EnumerateArray().Single(r => r.GetProperty("scheme").GetString() == scheme && r.GetProperty("id").GetString() == "ancestor-closes-before-live-child").GetProperty("samples");
            var drawer = new Layer("drawer"); var modal = new Layer("modal"); var alert = new Layer("alert"); var stack = new BleeckerModalStackModel<Layer, Target>(); var opener = new Target("opener", null);
            stack.Add(drawer, [], opener); stack.Add(modal, [drawer], new("middle-opener", drawer)); stack.Add(alert, [drawer, modal], new("alert-opener", modal));
            var removed = stack.Remove(drawer, Owns); Assert.IsFalse(removed.WasActive); Assert.AreSame(alert, removed.Active!.Layer);
            var active = samples[1].GetProperty("layers").EnumerateArray().Single(r => !r.GetProperty("inert").GetBoolean()); Assert.AreEqual(active.GetProperty("zIndex").GetString(), removed.Active.OverlayZIndex.ToString());
            Assert.IsFalse(stack.Remove(modal, Owns).WasActive); var last = stack.Remove(alert, Owns); Assert.IsTrue(last.WasActive); Assert.AreSame(opener, last.PreviousFocus); Assert.AreEqual(samples[2].GetProperty("focused").GetString(), last.PreviousFocus!.Id); Assert.IsNull(last.Active);
        }
    }

    [TestMethod]
    public void BranchOwnershipAndIndependentRootGroupsSurviveInactiveRemoval()
    {
        var stack = new BleeckerModalStackModel<Layer, Target>(); var first = new Layer("first"); var child = new Layer("child"); var last = new Layer("last"); var lastChild = new Layer("last-child"); var opener = new Target("opener", null);
        stack.Add(first, [], opener); stack.Add(child, [first], new("portaled-first-branch", first, true)); stack.Add(last, [], new("portaled-child-branch", child, true)); stack.Add(lastChild, [last], new("last-action", last));
        CollectionAssert.AreEqual(new[] { first, child, last, lastChild }, stack.Entries.Select(e => e.Layer).ToArray());
        Assert.IsFalse(stack.Remove(first, Owns).WasActive); Assert.IsFalse(stack.Remove(child, Owns).WasActive); Assert.AreSame(opener, stack.Entries.Single(e => ReferenceEquals(e.Layer, last)).PreviousFocus);
        Assert.AreSame(lastChild, stack.Active!.Layer); Assert.IsTrue(stack.Remove(lastChild, Owns).WasActive); Assert.AreSame(opener, stack.Remove(last, Owns).PreviousFocus);
    }
}
