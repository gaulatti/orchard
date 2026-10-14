using System.Collections.ObjectModel;

namespace Orchard;

/// <summary>Frozen chart defaults expressed as scoped XAML theme-resource keys.</summary>
/// <remarks>Use these keys in ThemeResource references on the rendered chart.
/// The metadata does not resolve or cache application-wide brush values.</remarks>
public static class BleeckerChartTheme
{
    public static IReadOnlyDictionary<string,string> Colors { get; } = new ReadOnlyDictionary<string,string>(new Dictionary<string,string>
    {
        ["sea"]="BleeckerSeaBrush",["desert"]="BleeckerDesertBrush",["terracotta"]="BleeckerTerracottaBrush",
        ["sunset"]="BleeckerSunsetBrush",["dusk"]="BleeckerDuskBrush",["accentGold"]="BleeckerAccentGoldBrush",
        ["accentBlue"]="BleeckerAccentBlueBrush",["textPrimary"]="BleeckerTextPrimaryBrush",["textSecondary"]="BleeckerTextSecondaryBrush",
        ["background"]="BleeckerBackgroundBrush",["card"]="BleeckerCardBrush",["border"]="BleeckerBorderBrush"
    });
    public static IReadOnlyList<string> Palette { get; } = Array.AsReadOnly(new[]
    {
        "BleeckerSeaBrush","BleeckerDesertBrush","BleeckerTerracottaBrush","BleeckerSunsetBrush","BleeckerDuskBrush",
        "BleeckerAccentGoldBrush","BleeckerAccentBlueBrush","BleeckerAccentOxbloodBrush","BleeckerAccentBronzeBrush","BleeckerAccentYellowBrush"
    });
    public static BleeckerChartGridStyle Grid { get; } = new("BleeckerBorderBrush",Array.AsReadOnly(new[]{3d,3d}));
    public static BleeckerChartAxisStyle Axis { get; } = new("BleeckerBorderBrush","BleeckerTextSecondaryBrush",12);
    public static BleeckerChartTooltipStyle Tooltip { get; } = new("BleeckerCardBrush","BleeckerBorderBrush",1,12,
        0,8,32,0,0,0,.08,8,"BleeckerTextPrimaryBrush","BleeckerTextSecondaryBrush",600);

    // JavaScript array lookup returns undefined for non-integer, negative
    // nonzero or nonfinite remainders. Negative zero indexes the first entry.
    /// <summary>Returns the palette resource key selected by the frozen source's remainder rule.</summary>
    /// <returns>Null for a fractional, negative nonzero or nonfinite remainder.</returns>
    public static string? PickColor(double index)
    {
        var remainder=index%Palette.Count;
        return !double.IsFinite(remainder)||remainder<0||remainder!=Math.Truncate(remainder)
            ? null : Palette[(int)remainder];
    }
}

public readonly record struct BleeckerChartGridStyle(string StrokeResourceKey,IReadOnlyList<double> StrokeDashes);
public readonly record struct BleeckerChartAxisStyle(string StrokeResourceKey,string TickForegroundResourceKey,double TickFontSize);
public readonly record struct BleeckerChartTooltipStyle(string BackgroundResourceKey,string BorderResourceKey,double BorderThickness,double CornerRadius,
    double ShadowX,double ShadowY,double ShadowBlurCssPixels,byte ShadowRed,byte ShadowGreen,byte ShadowBlue,double ShadowOpacity,
    double BackdropBlurCssPixels,string ItemForegroundResourceKey,string LabelForegroundResourceKey,int LabelFontWeight);
