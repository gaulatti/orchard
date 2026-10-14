namespace Orchard.Catalog;

internal readonly record struct BauhausMotionFrame(double Milliseconds, double RenderingMilliseconds, double? AnimationMilliseconds, double Outer, double Inner);
internal sealed record BauhausMotionMeasurement(bool Passed, double ExpectedDelta, double ExpectedFrameDelta, double OuterDelta, double InnerDelta,
    double MaximumFrameError, double ObserverSpan, double FrameSpan, bool Monotonic);

// RenderingTime identifies the frame whose transform values are being sampled.
// Stopwatch measures callback delivery, which may include queued older frames.
// The animation clock is retained for diagnosis and never supplies the oracle.
internal static class BauhausMotionTiming
{
    internal static BauhausMotionMeasurement Measure(IReadOnlyList<BauhausMotionFrame> frames, bool enabled)
    {
        if (frames.Count < 2) throw new ArgumentException("Bauhaus timing requires at least two real frames.", nameof(frames));
        var first = frames[0]; var last = frames[^1];
        var observerSpan = last.Milliseconds - first.Milliseconds;
        var frameSpan = last.RenderingMilliseconds - first.RenderingMilliseconds;
        var expected = enabled ? 18 * frameSpan / 1000 : 0;
        var outer = Delta(last.Outer, first.Outer); var inner = Delta(first.Inner, last.Inner);
        var monotonic = true; var maximumError = 0d;
        for (var index = 0; index < frames.Count; index++)
        {
            var frame = frames[index];
            if (!double.IsFinite(frame.Milliseconds) || !double.IsFinite(frame.RenderingMilliseconds) || !double.IsFinite(frame.Outer) || !double.IsFinite(frame.Inner)) monotonic = false;
            if (index > 0 && (frame.Milliseconds < frames[index-1].Milliseconds || frame.RenderingMilliseconds < frames[index-1].RenderingMilliseconds)) monotonic = false;
            var frameExpected = enabled ? 18 * (frame.RenderingMilliseconds - first.RenderingMilliseconds) / 1000 : 0;
            maximumError = Math.Max(maximumError, enabled ? Math.Max(Math.Abs(Delta(frame.Outer, first.Outer) - frameExpected), Math.Abs(Delta(first.Inner, frame.Inner) - frameExpected)) : Math.Max(Math.Abs(frame.Outer), Math.Abs(frame.Inner)));
        }
        var passed = monotonic && observerSpan >= 320 && frameSpan >= 320 &&
            (enabled ? maximumError < 1 && outer > 0 && inner > 0 : frames.All(frame => frame.Outer == 0 && frame.Inner == 0));
        return new(passed, 18 * observerSpan / 1000, expected, outer, inner, maximumError, observerSpan, frameSpan, monotonic);
    }
    private static double Delta(double end, double start) => ((end - start) % 360 + 360) % 360;
}
