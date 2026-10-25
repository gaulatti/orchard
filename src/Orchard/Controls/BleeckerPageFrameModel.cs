namespace Orchard;

public enum BleeckerPageFrameElement { Div, Article, Main, Section }
public enum BleeckerPageFrameWidth { Reading, Content, Wide, Full }
public enum BleeckerPageFrameGutter { None, Compact, Comfortable, Spacious }
public enum BleeckerPageFrameVerticalSpacing { None, Compact, Comfortable, Spacious }

internal static class BleeckerPageFrameModel
{
    internal readonly record struct Insets(double Horizontal, double Vertical);
    internal static double Maximum(BleeckerPageFrameWidth width) => width switch
    {
        BleeckerPageFrameWidth.Reading => 768, BleeckerPageFrameWidth.Content => 1152,
        BleeckerPageFrameWidth.Wide => 1440, BleeckerPageFrameWidth.Full => double.PositiveInfinity,
        _ => throw new ArgumentOutOfRangeException(nameof(width))
    };
    internal static Insets Padding(double viewport, BleeckerPageFrameGutter gutter, BleeckerPageFrameVerticalSpacing vertical)
    {
        if (!double.IsFinite(viewport) || viewport < 0) throw new ArgumentOutOfRangeException(nameof(viewport));
        var horizontal = gutter switch
        {
            BleeckerPageFrameGutter.None => 0,
            BleeckerPageFrameGutter.Compact => viewport < 640 ? 16 : 20,
            BleeckerPageFrameGutter.Comfortable => viewport < 640 ? 20 : viewport < 1024 ? 32 : 40,
            BleeckerPageFrameGutter.Spacious => viewport < 640 ? 24 : viewport < 1024 ? 40 : 64,
            _ => throw new ArgumentOutOfRangeException(nameof(gutter))
        };
        var spacing = vertical switch
        {
            BleeckerPageFrameVerticalSpacing.None => 0,
            BleeckerPageFrameVerticalSpacing.Compact => viewport < 640 ? 24 : 32,
            BleeckerPageFrameVerticalSpacing.Comfortable => viewport < 640 ? 40 : viewport < 1024 ? 48 : 64,
            BleeckerPageFrameVerticalSpacing.Spacious => viewport < 640 ? 56 : viewport < 1024 ? 64 : 96,
            _ => throw new ArgumentOutOfRangeException(nameof(vertical))
        };
        return new(horizontal, spacing);
    }
}
