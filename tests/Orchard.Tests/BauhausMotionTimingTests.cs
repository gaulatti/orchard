using System.Text.Json;
using Orchard.Catalog;

namespace Orchard.Tests;

[TestClass]
public sealed class BauhausMotionTimingTests
{
    private static BauhausMotionFrame[] RecordedFrames()
    {
        using var reference = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "BauhausFrameTiming.json")));
        return reference.RootElement.GetProperty("frames").Deserialize<BauhausMotionFrame[]>()!;
    }
    [TestMethod]
    public void RecordedArm64FramesUseRenderingTimeDespiteQueuedCallbackDelivery()
    {
        var measurement = BauhausMotionTiming.Measure(RecordedFrames(), true);
        Assert.IsTrue(measurement.Passed);
        Assert.AreEqual(395.6032, measurement.FrameSpan, .000001);
        Assert.AreEqual(324.7343, measurement.ObserverSpan, .000001);
        Assert.IsTrue(Math.Abs(measurement.OuterDelta - measurement.ExpectedDelta) > 1, "The captured delivery-clock regression no longer exercises its failing boundary.");
        Assert.AreEqual(.000104230066, measurement.MaximumFrameError, .000000001, "The captured native frame comparison changed.");
    }
    [TestMethod]
    public void IntermediateIncorrectRotationCannotHideBehindCorrectEndpoints()
    {
        var frames = RecordedFrames(); frames[10] = frames[10] with { Outer = frames[10].Outer + 2 };
        Assert.IsFalse(BauhausMotionTiming.Measure(frames, true).Passed);
    }
    [TestMethod]
    public void ReducedMotionRequiresStaticAnglesAcrossTheWholeRealFrameWindow()
    {
        var frames = RecordedFrames().Select(frame => frame with { Outer = 0, Inner = 0, AnimationMilliseconds = null }).ToArray();
        Assert.IsTrue(BauhausMotionTiming.Measure(frames, false).Passed);
        frames[10] = frames[10] with { Inner = -.1 };
        Assert.IsFalse(BauhausMotionTiming.Measure(frames, false).Passed);
    }
    [TestMethod]
    public void TimestampRegressionAndShortFrameWindowsAreRejected()
    {
        var frames = RecordedFrames(); frames[10] = frames[10] with { RenderingMilliseconds = frames[0].RenderingMilliseconds - 1 };
        Assert.IsFalse(BauhausMotionTiming.Measure(frames, true).Passed);
        Assert.IsFalse(BauhausMotionTiming.Measure(RecordedFrames()[..10], true).Passed);
    }
}
