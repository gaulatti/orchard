namespace Orchard;

public enum BleeckerDetailLayoutRatio { Balanced, Primary }
public enum BleeckerDetailLayoutSide { Start, End }

internal static class BleeckerDetailLayoutModel
{
    internal readonly record struct Tracks(double MainX, double MainWidth, double RailX, double RailWidth, double Gap, bool Columns);
    internal static Tracks Columns(double width, double viewport, bool rail, BleeckerDetailLayoutRatio ratio, BleeckerDetailLayoutSide side)
    {
        if (!double.IsFinite(width) || width < 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (!double.IsFinite(viewport) || viewport < 0) throw new ArgumentOutOfRangeException(nameof(viewport));
        if (!Enum.IsDefined(ratio)) throw new ArgumentOutOfRangeException(nameof(ratio));
        if (!Enum.IsDefined(side)) throw new ArgumentOutOfRangeException(nameof(side));
        var gap = viewport < 1024 ? 40 : viewport < 1280 ? 48 : 64;
        if (!rail || viewport < 1024) return new(0, width, 0, width, gap, false);
        var available = Math.Max(0, width - gap);
        double main, secondary;
        if (ratio == BleeckerDetailLayoutRatio.Primary && available < 816)
        { secondary = 272; main = Math.Max(0, available - secondary); }
        else
        {
            // The source browser allocates flex shares in 1/64px units,
            // carrying each float remainder into the next logical track.
            var units = (float)(available * 64); var fraction = units / (ratio == BleeckerDetailLayoutRatio.Primary ? 3f : 2f); var carry = 0f;
            double Share(float weight)
            {
                var product = fraction * weight; var share = product + carry;
                var size = MathF.Floor(share + 1f / 8388608f); carry = Math.Max(0, share - size); return size / 64d;
            }
            var firstWeight = ratio == BleeckerDetailLayoutRatio.Primary && side == BleeckerDetailLayoutSide.End ? 2f : 1f;
            var first = Share(firstWeight); var second = Share(ratio == BleeckerDetailLayoutRatio.Primary ? 3f - firstWeight : 1f);
            (main, secondary) = side == BleeckerDetailLayoutSide.Start ? (second, first) : (first, second);
        }
        return side == BleeckerDetailLayoutSide.Start ? new(secondary + gap, main, 0, secondary, gap, true) : new(0, main, main + gap, secondary, gap, true);
    }
    internal static double StickyOffset(double normalTop, double containerHeight, double railHeight, bool enabled) =>
        enabled ? Math.Clamp(32 - normalTop, 0, Math.Max(0, containerHeight - railHeight)) : 0;
}
