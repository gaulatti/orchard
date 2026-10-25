using Microsoft.Graphics.Canvas.Text;

namespace Orchard;

internal static class BleeckerTextTypography
{
    internal static CanvasTypography Tabular()
    {
        var typography = new CanvasTypography();
        // An explicit DirectWrite typography replaces the default feature set.
        // Keep the kerning that the surrounding text presentation requests.
        typography.AddFeature(CanvasTypographyFeatureName.Kerning, 1);
        typography.AddFeature(CanvasTypographyFeatureName.TabularFigures, 1);
        return typography;
    }
}
