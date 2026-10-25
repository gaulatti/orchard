namespace Orchard;

public enum BleeckerHoverCardSide { Top, Bottom }

// The pinned CSS hover group has no portal, collision policy or open delay.
// Pointer ownership is supplied by the mounted native component. Translation
// changes immediately; only opacity follows the source transition clock.
internal sealed class BleeckerHoverCardModel
{
    private readonly BleeckerCssTransition<double> opacity = new((from, to, progress) => from + (to - from) * progress);
    internal BleeckerHoverCardModel() => opacity.Retarget(0, 0, 220, false);
    internal bool Hovered { get; private set; }
    internal void SetHovered(bool value, double time, bool motionEnabled)
    {
        Hovered = value;
        opacity.Retarget(value ? 1 : 0, time, motionEnabled ? 220 : .01, true);
    }
    internal double OpacityAt(double time) => opacity.ValueAt(time);
    internal bool IsAnimating(double time) => opacity.IsRunning(time);
    internal bool HitTestVisible => Hovered;
    internal readonly record struct Rect(double X, double Y, double Width, double Height);
    internal static Rect Place(Rect trigger, double width, double height, BleeckerHoverCardSide side, bool hovered)
    {
        if (!Enum.IsDefined(side)) throw new ArgumentOutOfRangeException(nameof(side));
        foreach (var value in new[] { trigger.X, trigger.Y, trigger.Width, trigger.Height, width, height })
            if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(trigger));
        if (trigger.Width < 0 || trigger.Height < 0 || width < 0 || height < 0) throw new ArgumentOutOfRangeException(nameof(width));
        var translation = hovered ? 0 : side == BleeckerHoverCardSide.Top ? -4 : 4;
        return new(trigger.X + (trigger.Width - width) / 2,
            (side == BleeckerHoverCardSide.Top ? trigger.Y - height - 8 : trigger.Y + trigger.Height + 8) + translation,
            width, height);
    }
}
