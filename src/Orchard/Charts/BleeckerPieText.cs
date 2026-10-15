using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

namespace Orchard;

// The frozen browser's platform-font audit selects these Windows faces for
// scripts not covered by the bundled Encode Sans face. Explicit runs avoid
// the different font/scale selected by WinUI's default fallback map.
internal static class BleeckerPieText
{
    internal static double InlineWidth(TextBlock target)
    {
        var runs = target.Inlines.OfType<Run>().ToArray();
        if (!runs.Any(run => run.ReadLocalValue(TextElement.FontFamilyProperty) != DependencyProperty.UnsetValue)) return target.DesiredSize.Width;
        var width = 0d;
        foreach (var run in runs)
        {
            var family = run.ReadLocalValue(TextElement.FontFamilyProperty) == DependencyProperty.UnsetValue ? target.FontFamily : run.FontFamily;
            // Preserve trailing spaces at a font boundary. WinUI otherwise
            // excludes them from a standalone TextBlock's desired width.
            var probe = new TextBlock { Text = run.Text + "\u200B", FontFamily = family, FontSize = target.FontSize, FontWeight = target.FontWeight, UseLayoutRounding = false };
            probe.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
            width += Math.Ceiling(probe.DesiredSize.Width * 64) / 64;
        }
        return width;
    }
    internal static void Set(TextBlock target, string value)
    {
        target.Text = value;
        if (!target.FontFamily.Source.Contains("/Orchard/Assets/Fonts/EncodeSans-", StringComparison.Ordinal)) return;
        var parts = new List<(string? Family, StringBuilder Text)>();
        foreach (var rune in value.EnumerateRunes())
        {
            var scalar = rune.Value;
            var family = scalar is >= 0x4E00 and <= 0x9FFF or >= 0x3400 and <= 0x4DBF ? "Microsoft YaHei" :
                scalar is >= 0x3040 and <= 0x30FF ? "Yu Gothic" :
                scalar is >= 0x0600 and <= 0x06FF or >= 0x0750 and <= 0x077F or >= 0x08A0 and <= 0x08FF ? "Segoe UI" : null;
            if (parts.Count == 0 || parts[^1].Family != family) parts.Add((family, new StringBuilder()));
            parts[^1].Text.Append(rune.ToString());
        }
        if (!parts.Any(part => part.Family is not null)) return;
        target.Inlines.Clear();
        foreach (var part in parts)
        {
            var run = new Run { Text = part.Text.ToString() };
            if (part.Family is not null) run.FontFamily = new FontFamily(part.Family);
            target.Inlines.Add(run);
        }
    }
}
