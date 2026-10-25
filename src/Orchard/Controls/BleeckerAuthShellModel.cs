namespace Orchard;

public enum BleeckerAuthShellLayout { Centered, Split }
public enum BleeckerAuthShellSide { Start, End }

internal static class BleeckerAuthShellModel
{
    internal readonly record struct Tracks(double AccessX, double AccessWidth, double AsideX, double AsideWidth, bool AsideVisible);
    internal readonly record struct Spacing(double Horizontal, double Vertical, double ContentVertical, double Aside, double MaximumContent);
    internal static Spacing Insets(double viewport, double rem)
    {
        Validate(viewport, nameof(viewport)); if (!double.IsFinite(rem) || rem <= 0) throw new ArgumentOutOfRangeException(nameof(rem));
        return new((viewport < 640 ? 20 : viewport < 1024 ? 32 : viewport < 1280 ? 48 : 64) * rem, (viewport < 640 ? 24 : 32) * rem, (viewport < 640 ? 40 : 56) * rem, (viewport < 1280 ? 48 : 64) * rem, 448 * rem);
    }
    internal static Tracks Columns(double width, double viewport, double rem, bool hasAside, BleeckerAuthShellSide side)
    {
        Validate(width, nameof(width)); var paddingWidth = Insets(viewport, rem).Horizontal * 2; if (!Enum.IsDefined(side)) throw new ArgumentOutOfRangeException(nameof(side));
        if (!hasAside || viewport < 1024) return new(0, Math.Max(width, paddingWidth), 0, 0, false);
        var minimum = 384 * rem; double first, second;
        if (minimum >= width * 5 / 12) { first = side == BleeckerAuthShellSide.Start ? minimum : Math.Max(0, width - minimum); second = side == BleeckerAuthShellSide.Start ? Math.Max(0, width - minimum) : minimum; }
        else
        {
            // The source grid floors its first logical track to a 1/64px unit;
            // the second track receives the remaining available width.
            first = Math.Floor(width * (side == BleeckerAuthShellSide.Start ? 5 : 7) / 12 * 64) / 64; second = width - first;
        }
        // A section's border box keeps its padding even when the grid assigns
        // its track zero width. The next track still starts at its grid edge.
        return side == BleeckerAuthShellSide.Start ? new(first, Math.Max(second, paddingWidth), 0, first, true) : new(0, Math.Max(first, paddingWidth), first, second, true);
    }
    internal static double RegionHeight(double natural, double arranged, bool grid)
    {
        Validate(natural, nameof(natural)); Validate(arranged, nameof(arranged));
        return grid ? Math.Max(natural, arranged) : natural;
    }
    private static void Validate(double value, string name) { if (!double.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(name); }
}
