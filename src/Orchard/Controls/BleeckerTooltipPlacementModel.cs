namespace Orchard;

internal enum BleeckerTooltipSide { Top, Bottom, Left, Right }

// Pinned Radix/Floating UI geometry: floating coordinates snap to device
// pixels, while arrow middleware uses the integer CSS client dimension.
internal static class BleeckerTooltipPlacementModel
{
    internal readonly record struct Layout(BleeckerTooltipSide Side, double X, double Y, double ArrowOffset, bool ArrowVisible);
    internal static Layout Place(TooltipBounds anchor, double width, double height, double viewportWidth, double viewportHeight, BleeckerTooltipSide preferred, double scale)
    {
        if (!Enum.IsDefined(preferred)) throw new ArgumentOutOfRangeException(nameof(preferred));
        foreach (var value in new[] { width, height, viewportWidth, viewportHeight, scale }) if (!double.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Tooltip dimensions and scale must be finite and positive.");
        foreach (var value in new[] { anchor.Left, anchor.Top, anchor.Right, anchor.Bottom }) if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(anchor));
        if (anchor.Right < anchor.Left || anchor.Bottom < anchor.Top) throw new ArgumentException("Tooltip anchor bounds are inverted.", nameof(anchor));
        const double edge=10,distance=12;
        double Space(BleeckerTooltipSide side)=>side switch {BleeckerTooltipSide.Top=>anchor.Top-edge,BleeckerTooltipSide.Bottom=>viewportHeight-edge-anchor.Bottom,BleeckerTooltipSide.Left=>anchor.Left-edge,_=>viewportWidth-edge-anchor.Right};
        var opposite=preferred switch {BleeckerTooltipSide.Top=>BleeckerTooltipSide.Bottom,BleeckerTooltipSide.Bottom=>BleeckerTooltipSide.Top,BleeckerTooltipSide.Left=>BleeckerTooltipSide.Right,_=>BleeckerTooltipSide.Left};
        var required=(preferred is BleeckerTooltipSide.Top or BleeckerTooltipSide.Bottom?height:width)+distance;
        var side=Space(preferred)<required&&Space(opposite)>Space(preferred)?opposite:preferred;
        var x=side switch {BleeckerTooltipSide.Left=>anchor.Left-width-distance,BleeckerTooltipSide.Right=>anchor.Right+distance,_=>(anchor.Left+anchor.Right-width)/2};
        var y=side switch {BleeckerTooltipSide.Top=>anchor.Top-height-distance,BleeckerTooltipSide.Bottom=>anchor.Bottom+distance,_=>(anchor.Top+anchor.Bottom-height)/2};
        x=Math.Clamp(x,edge,Math.Max(edge,viewportWidth-edge-width));y=Math.Clamp(y,edge,Math.Max(edge,viewportHeight-edge-height));
        var vertical=side is BleeckerTooltipSide.Top or BleeckerTooltipSide.Bottom;var extent=vertical?width:height;var clientExtent=Math.Floor(extent+.5);
        var center=(vertical?(anchor.Left+anchor.Right)/2-x:(anchor.Top+anchor.Bottom)/2-y)+(clientExtent-extent)/2;
        return new(side,Math.Floor(x*scale+.5)/scale,Math.Floor(y*scale+.5)/scale,Math.Clamp(center-5,0,Math.Max(0,clientExtent-10)),center>=5&&center<=clientExtent-5);
    }
}
