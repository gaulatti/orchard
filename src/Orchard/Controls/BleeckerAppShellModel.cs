namespace Orchard;

internal static class BleeckerAppShellModel
{
    internal readonly record struct Tracks(double MainY,double MainHeight,double ContentY,double ContentHeight,double FooterY);
    internal static Tracks Arrange(double height,double header,double footer,double top,double bottom)
    {
        var main=Math.Max(top+bottom,height-header-footer);
        return new(header,main,header+top,Math.Max(0,main-top-bottom),header+main);
    }
}
