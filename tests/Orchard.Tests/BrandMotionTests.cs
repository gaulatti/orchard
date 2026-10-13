using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class BrandMotionTests
{
    [TestMethod]
    public void HoverReversalsMatchTheFrozenBrowserOpacityTimeline()
    {
        using var reference = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "BrandOpacityTransitions.json")));
        var samples = reference.RootElement.GetProperty("samples");
        var transition = new BleeckerCssTransition<double>((from, to, progress) => from + (to - from) * progress, BleeckerCssEasing.Default);
        transition.Retarget(.9, 0, 220, false);
        transition.Retarget(1, 0, 220, true);
        Check("enter80", 80, 0);
        transition.Retarget(.9, 80, 220, true);
        Check("exit40", 120, 80);
        transition.Retarget(1, 120, 220, true);
        Check("reenter25", 145, 120);
        var end = samples.EnumerateArray().Single(sample => sample.GetProperty("name").GetString() == "reenter-end");
        Assert.AreEqual(1, transition.ValueAt(120 + end.GetProperty("time").GetDouble() + .01), 0);
        transition.Retarget(.9, 300, 220, false);
        Assert.IsFalse(transition.IsRunning(300)); Assert.AreEqual(.9, transition.ValueAt(300), 0);

        void Check(string name, double time, double start)
        {
            var expected = samples.EnumerateArray().Single(sample => sample.GetProperty("name").GetString() == name);
            Assert.AreEqual(expected.GetProperty("opacity").GetDouble(), transition.ValueAt(time), .000001);
            var duration = expected.GetProperty("duration").GetDouble();
            Assert.IsTrue(transition.IsRunning(start + duration - .01));
            Assert.IsFalse(transition.IsRunning(start + duration + .01));
        }
    }
}
