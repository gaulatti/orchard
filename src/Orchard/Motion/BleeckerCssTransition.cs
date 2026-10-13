namespace Orchard;

// Presentation clock for one CSS transition property. The caller supplies time
// so native rendering and deterministic browser-timeline regressions agree.
internal sealed class BleeckerCssTransition<T>(Func<T, T, double, T> interpolate, Func<double, double>? timing = null)
{
    private readonly Func<double, double> easing = timing ?? BleeckerColorMix.Premium;
    private bool initialized;
    private T from = default!, to = default!, reversingStart = default!;
    private double start, duration, reversingFactor = 1;

    internal bool IsRunning(double time) => initialized && duration > 0 && time - start < duration;
    internal T ValueAt(double time) => !IsRunning(time) ? to :
        interpolate(from, to, easing((time - start) / duration));

    internal void Retarget(T destination, double time, double fullDuration, bool animate)
    {
        if (!initialized || !animate)
        {
            initialized = true;
            from = to = reversingStart = destination;
            start = time; duration = 0; reversingFactor = 1;
            return;
        }
        if (EqualityComparer<T>.Default.Equals(destination, to)) return;
        var current = ValueAt(time);
        var reversing = IsRunning(time) && EqualityComparer<T>.Default.Equals(destination, reversingStart);
        var factor = reversing ? Math.Clamp(Math.Abs(
            easing((time - start) / duration) * reversingFactor + 1 - reversingFactor), 0, 1) : 1;
        reversingStart = reversing ? to : current;
        from = current; to = destination; start = time;
        duration = fullDuration * factor; reversingFactor = factor;
    }
}
