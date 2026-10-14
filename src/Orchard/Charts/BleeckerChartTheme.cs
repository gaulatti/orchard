using System.Collections.ObjectModel;

namespace Orchard;

// Frozen chart-theme.ts metadata. Resource keys preserve scoped live theme
// references; these values do not resolve or cache a global RGB palette.
internal static class BleeckerChartTheme
{
    internal static IReadOnlyDictionary<string,string> Colors { get; } = new ReadOnlyDictionary<string,string>(new Dictionary<string,string>
    {
        ["sea"]="BleeckerSeaBrush",["desert"]="BleeckerDesertBrush",["terracotta"]="BleeckerTerracottaBrush",
        ["sunset"]="BleeckerSunsetBrush",["dusk"]="BleeckerDuskBrush",["accentGold"]="BleeckerAccentGoldBrush",
        ["accentBlue"]="BleeckerAccentBlueBrush",["textPrimary"]="BleeckerTextPrimaryBrush",["textSecondary"]="BleeckerTextSecondaryBrush",
        ["background"]="BleeckerBackgroundBrush",["card"]="BleeckerCardBrush",["border"]="BleeckerBorderBrush"
    });
    internal static IReadOnlyList<string> Palette { get; } = Array.AsReadOnly(new[]
    {
        "BleeckerSeaBrush","BleeckerDesertBrush","BleeckerTerracottaBrush","BleeckerSunsetBrush","BleeckerDuskBrush",
        "BleeckerAccentGoldBrush","BleeckerAccentBlueBrush","BleeckerAccentOxbloodBrush","BleeckerAccentBronzeBrush","BleeckerAccentYellowBrush"
    });
    internal static BleeckerChartGridStyle Grid { get; } = new("BleeckerBorderBrush",Array.AsReadOnly(new[]{3d,3d}));
    internal static BleeckerChartAxisStyle Axis { get; } = new("BleeckerBorderBrush","BleeckerTextSecondaryBrush",12);
    internal static BleeckerChartTooltipStyle Tooltip { get; } = new("BleeckerCardBrush","BleeckerBorderBrush",1,12,
        0,8,32,0,0,0,.08,8,"BleeckerTextPrimaryBrush","BleeckerTextSecondaryBrush",600);

    // JavaScript array lookup returns undefined for non-integer, negative
    // nonzero or nonfinite remainders. Negative zero indexes the first entry.
    internal static string? PickColor(double index)
    {
        var remainder=index%Palette.Count;
        return !double.IsFinite(remainder)||remainder<0||remainder!=Math.Truncate(remainder)
            ? null : Palette[(int)remainder];
    }
}

internal readonly record struct BleeckerChartGridStyle(string StrokeResourceKey,IReadOnlyList<double> StrokeDashes);
internal readonly record struct BleeckerChartAxisStyle(string StrokeResourceKey,string TickForegroundResourceKey,double TickFontSize);
internal readonly record struct BleeckerChartTooltipStyle(string BackgroundResourceKey,string BorderResourceKey,double BorderThickness,double CornerRadius,
    double ShadowX,double ShadowY,double ShadowBlurCssPixels,byte ShadowRed,byte ShadowGreen,byte ShadowBlue,double ShadowOpacity,
    double BackdropBlurCssPixels,string ItemForegroundResourceKey,string LabelForegroundResourceKey,int LabelFontWeight);
