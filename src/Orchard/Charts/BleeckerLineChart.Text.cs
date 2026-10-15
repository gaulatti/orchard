using System.Collections;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Orchard;

public sealed partial class BleeckerLineChart
{
    private static string ChartReactText(object? value)
    {
        if (value is null or bool) return "";
        if (value is IList values) return string.Concat(values.Cast<object?>().Select(ChartReactText));
        return value is not string && BleeckerChartData.Number(value) is double number ? BleeckerChartScale.TickLabel(number) : BleeckerChartData.Label(value);
    }
    private static IEnumerable<string> ChartReactFragments(object? value)
    {
        if (value is IList values) { foreach (var item in values) foreach (var fragment in ChartReactFragments(item)) yield return fragment; }
        else if (value is not null and not bool) yield return ChartReactText(value);
    }
    private static string ChartVisibleText(string value) => Regex.Replace(value, @"[ \t\r\n\f]+", " ").Trim(' ');
    private double ChartFragmentsWidth(IReadOnlyList<string> fragments)
    {
        if (fragments.Count == 0) return 0;
        var family = FontFamily.Source; var marker = family.IndexOf('#');
        if (marker >= 0 && Uri.TryCreate(family[..marker], UriKind.Absolute, out var uri))
        {
            // Use the same documented loose-font path as the package and the
            // Dashboard Section's intrinsic text measurement.
            if (uri.Scheme == "ms-appx" && uri.Host.Length == 0)
            {
                uri = new Uri(System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, Uri.UnescapeDataString(uri.AbsolutePath).TrimStart('/'))));
                family = uri.AbsoluteUri + family[marker..];
            }
            using var fonts = new Microsoft.Graphics.Canvas.Text.CanvasFontSet(uri);
            if (fonts.Fonts.Count == 0) throw new InvalidOperationException("Chart cannot measure the requested bundled font.");
        }
        using var format = new Microsoft.Graphics.Canvas.Text.CanvasTextFormat { FontFamily = family, FontSize = (float)(16 * tooltipSettings.TextScaleFactor), Direction = FlowDirection == FlowDirection.RightToLeft ? Microsoft.Graphics.Canvas.Text.CanvasTextDirection.RightToLeftThenTopToBottom : Microsoft.Graphics.Canvas.Text.CanvasTextDirection.LeftToRightThenTopToBottom };
        using var layout = new Microsoft.Graphics.Canvas.Text.CanvasTextLayout(Microsoft.Graphics.Canvas.CanvasDevice.GetSharedDevice(), string.Concat(fragments), format, float.MaxValue, float.MaxValue);
        layout.SetPairKerning(0, fragments.Sum(fragment => fragment.Length), true);
        var width = 0d; var start = 0;
        foreach (var fragment in fragments)
        {
            // Shape with full context, then allocate each React text node in
            // browser layout units. Separate measurements lose boundary kerning.
            width += Math.Ceiling(layout.GetCharacterRegions(start, fragment.Length).Sum(region => region.LayoutBounds.Width) * 64) / 64;
            start += fragment.Length;
        }
        return width;
    }
}
