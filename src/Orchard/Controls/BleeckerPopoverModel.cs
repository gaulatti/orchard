namespace Orchard;

public enum BleeckerPopoverSide { Top, Bottom, Left, Right }
public enum BleeckerPopoverAlign { Start, Center, End }

// State and placement for the pinned nonmodal Popover. Native presentation,
// input and focus remain the responsibility of the mounted component.
internal sealed class BleeckerPopoverModel(bool defaultOpen)
{
    private bool history = defaultOpen;
    internal bool Effective(bool? open) => open ?? history;
    internal bool Request(bool? open, bool next)
    {
        if (Effective(open) == next) return false;
        if (open is null) history = next;
        return true;
    }

    internal readonly record struct Rect(double X, double Y, double Width, double Height)
    {
        internal double Right => X + Width;
        internal double Bottom => Y + Height;
    }
    internal readonly record struct Placement(double X, double Y, BleeckerPopoverSide Side, BleeckerPopoverAlign Align);
    private readonly record struct Candidate(Placement Placement, double Main, double First, double Second)
    {
        internal bool Fits => Main <= 0 && First <= 0 && Second <= 0;
        internal double Overflow => Math.Max(0, Main) + Math.Max(0, First) + Math.Max(0, Second);
    }
    private static double Clamp(double value, double minimum, double maximum) => Math.Max(minimum, Math.Min(value, maximum));
    private static BleeckerPopoverSide Opposite(BleeckerPopoverSide side) => side switch
    {
        BleeckerPopoverSide.Top => BleeckerPopoverSide.Bottom, BleeckerPopoverSide.Bottom => BleeckerPopoverSide.Top,
        BleeckerPopoverSide.Left => BleeckerPopoverSide.Right, BleeckerPopoverSide.Right => BleeckerPopoverSide.Left,
        _ => throw new ArgumentOutOfRangeException(nameof(side))
    };

    // Offset, shift-with-contact and best-fit flipping follow the pinned
    // Floating UI middleware order. See licenses/LICENSE-FloatingUI.txt.
    internal static Placement Place(Rect anchor, double width, double height, double viewportWidth, double viewportHeight,
        BleeckerPopoverSide side, BleeckerPopoverAlign align, double offset, bool rtl, double scale = 1, double collisionPadding = 12)
    {
        if (!Enum.IsDefined(side) || !Enum.IsDefined(align)) throw new ArgumentOutOfRangeException(nameof(side));
        foreach (var number in new[] { anchor.X, anchor.Y, anchor.Width, anchor.Height, width, height, viewportWidth, viewportHeight, offset, scale, collisionPadding })
            if (!double.IsFinite(number)) throw new ArgumentOutOfRangeException(nameof(anchor));
        if (anchor.Width < 0 || anchor.Height < 0 || width < 0 || height < 0 || viewportWidth < 0 || viewportHeight < 0 || scale <= 0 || collisionPadding < 0) throw new ArgumentOutOfRangeException(nameof(width));
        var edge = collisionPadding;
        Candidate Evaluate(BleeckerPopoverSide currentSide, BleeckerPopoverAlign currentAlign)
        {
            var vertical = currentSide is BleeckerPopoverSide.Top or BleeckerPopoverSide.Bottom;
            var x = anchor.X + (anchor.Width - width) / 2;
            var y = anchor.Y + (anchor.Height - height) / 2;
            if (currentAlign != BleeckerPopoverAlign.Center)
            {
                if (vertical) x = (currentAlign == BleeckerPopoverAlign.Start) != rtl ? anchor.X : anchor.Right - width;
                else y = currentAlign == BleeckerPopoverAlign.Start ? anchor.Y : anchor.Bottom - height;
            }
            switch (currentSide)
            {
                case BleeckerPopoverSide.Top: y = anchor.Y - height - offset; break;
                case BleeckerPopoverSide.Bottom: y = anchor.Bottom + offset; break;
                case BleeckerPopoverSide.Left: x = anchor.X - width - offset; break;
                case BleeckerPopoverSide.Right: x = anchor.Right + offset; break;
            }
            if (vertical) x = Clamp(Clamp(x, edge, viewportWidth - width - edge), anchor.X - width, anchor.Right);
            else y = Clamp(Clamp(y, edge, viewportHeight - height - edge), anchor.Y - height, anchor.Bottom);
            var top = edge - y; var bottom = y + height - viewportHeight + edge; var left = edge - x; var right = x + width - viewportWidth + edge;
            var main = currentSide switch { BleeckerPopoverSide.Top => top, BleeckerPopoverSide.Bottom => bottom, BleeckerPopoverSide.Left => left, _ => right };
            var firstPositiveSide = vertical ? currentAlign == (rtl ? BleeckerPopoverAlign.End : BleeckerPopoverAlign.Start) : currentAlign == BleeckerPopoverAlign.Start;
            if (vertical ? anchor.Width > width : anchor.Height > height) firstPositiveSide = !firstPositiveSide;
            var first = vertical ? firstPositiveSide ? right : left : firstPositiveSide ? bottom : top;
            var second = vertical ? firstPositiveSide ? left : right : firstPositiveSide ? top : bottom;
            return new(new(x, y, currentSide, currentAlign), main, first, second);
        }
        var opposite = Opposite(side); var candidates = new List<Candidate> { Evaluate(side, align) };
        if (align == BleeckerPopoverAlign.Center) candidates.Add(Evaluate(opposite, align));
        else
        {
            var other = align == BleeckerPopoverAlign.Start ? BleeckerPopoverAlign.End : BleeckerPopoverAlign.Start;
            candidates.Add(Evaluate(side, other)); candidates.Add(Evaluate(opposite, align)); candidates.Add(Evaluate(opposite, other));
        }
        var fitting = candidates.Where(candidate => candidate.Fits).ToArray();
        var mainFitting = candidates.Where(candidate => candidate.Main <= 0).OrderBy(candidate => candidate.First).ToArray();
        var selected = fitting.Length > 0 ? fitting[0] : mainFitting.Length > 0 ? mainFitting[0] : candidates.OrderBy(candidate => candidate.Overflow).First();
        var result = selected.Placement;
        return result with { X = Math.Floor(result.X * scale + .5) / scale, Y = Math.Floor(result.Y * scale + .5) / scale };
    }
}
