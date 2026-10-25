namespace Orchard;

public enum BleeckerHeaderPosition { Fixed, Sticky, Static }

internal static class BleeckerHeaderModel
{
    internal static double Container(double viewport, double width, bool fullWidth) => fullWidth ? width : BleeckerFooterModel.Container(viewport, width);
    internal static double Shift(BleeckerHeaderPosition position, double normalTop, double parentBottom, double height) => position switch
    {
        BleeckerHeaderPosition.Fixed => -normalTop,
        BleeckerHeaderPosition.Sticky => Math.Clamp(-normalTop, 0, Math.Max(0, parentBottom - normalTop - height)),
        BleeckerHeaderPosition.Static => 0,
        _ => throw new ArgumentOutOfRangeException(nameof(position))
    };
    internal static double MenuHeight(double natural, double maximum) => Math.Max(1, Math.Min(natural, maximum));
}
