namespace Orchard;

// Event-driven lifetime shared by the native renderer and portable source
// regressions. The scheduler supplies actual frame/timeout events; this model
// deliberately does not replace requestAnimationFrame with a guessed delay.
internal sealed class BleeckerSonnerLifetime
{
    private double observedAt;
    internal bool IsMounted { get; private set; } = true;
    internal bool IsVisible { get; private set; }
    internal bool CanInvokeAction => IsMounted;
    internal double? RemoveAt { get; private set; }

    internal void AdvanceTo(double now)
    {
        if (!double.IsFinite(now) || now < observedAt)
            throw new ArgumentOutOfRangeException(nameof(now));
        observedAt = now;
        if (RemoveAt is double deadline && now >= deadline) Remove();
    }

    internal void Frame(double now)
    {
        AdvanceTo(now);
        // The pinned source does not cancel its initial frame on dismissal.
        // A late first frame can restore opacity during the retained interval.
        if (IsMounted) IsVisible = true;
    }

    internal void Dismiss(double now)
    {
        AdvanceTo(now);
        if (!IsMounted) return;
        IsVisible = false;
        var next = now + 300;
        RemoveAt = RemoveAt is double previous ? Math.Min(previous, next) : next;
    }

    internal void Remove()
    {
        IsMounted = false;
        IsVisible = false;
        RemoveAt = null;
    }
}
