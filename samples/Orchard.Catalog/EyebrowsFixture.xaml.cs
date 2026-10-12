using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class EyebrowsFixture : UserControl
{
    public EyebrowsFixture() => InitializeComponent();

    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        UpdateLayout();
        await Task.Delay(100);
        var text = Find<TextBlock>(Accent, "PART_Text");
        Require(text.Text == "PRIVATE CLIENT SERVICES" && (string)Accent.Content == "Private client services",
            "Eyebrow did not uppercase its display while retaining consumer content.");
        Require(Find<TextBlock>(Expansion, "PART_Text").Text == "STRASSE · FFI · Ι\u0308\u0301" &&
            (string)Expansion.Content == "Straße · ﬃ · ΐ", "Unicode uppercase expansions did not reach the displayed eyebrow or changed consumer content.");
        Require(text.FontFamily.Source.Contains("EncodeSans-SemiBold.ttf") && text.FontSize == 11 && text.FontWeight.Weight == 600 &&
            text.CharacterSpacing == 120 && text.LineHeight == 16 && Accent.ActualHeight == 16,
            $"Eyebrow typography differs: height={Accent.ActualHeight}.");
        var palette = BleeckerTokens.Colors(scheme);
        Require(Hex(Accent.Foreground) == palette["accentText"] && Hex(Muted.Foreground) == palette["textSecondary"] &&
            Hex(Inverse.Foreground) == "#FFFFFFFF" && Math.Abs(Inverse.Foreground.Opacity - .75) < .000001,
            "Eyebrow tone palette differs from the pinned contract.");
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(Accent);
        Require(peer.GetAutomationControlType() == AutomationControlType.Text && !Accent.IsTabStop,
            "Eyebrow lost its noninteractive text role.");
        Require(Wrapped.ActualHeight > 16 && Disabled.Opacity == 1, "Wrapped or disabled eyebrow appearance differs.");
        var original = (Accent.Content, Accent.Tone, Accent.Rule);
        try
        {
            foreach (var tone in Enum.GetValues<BleeckerEyebrowTone>())
            foreach (var rule in new[] { false, true })
            {
                Accent.Tone = tone;
                Accent.Rule = rule;
                Accent.Content = "Updated section";
                UpdateLayout();
                var line = Find<Border>(Accent, "PART_Rule");
                Require(text.Text == "UPDATED SECTION" && line.Visibility == (rule ? Visibility.Visible : Visibility.Collapsed),
                    "Live eyebrow content or rule did not update.");
                if (rule) Require(line.ActualWidth == 28 && line.ActualHeight == 1 && line.Margin.Right == 12 &&
                    Math.Abs(line.Opacity - .6) < .000001 && ReferenceEquals(line.Background, text.Foreground),
                    "Eyebrow rule lost its dimensions, gap, opacity or current-color relationship.");
                var expected = tone switch { BleeckerEyebrowTone.Accent => palette["accentText"],
                    BleeckerEyebrowTone.Muted => palette["textSecondary"], _ => "#FFFFFFFF" };
                Require(Hex(Accent.Foreground) == expected && Hex(Muted.Foreground) == palette["textSecondary"],
                    "Changing one eyebrow tone retained a stale brush or changed another control.");
            }
            Accent.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 12, 34, 56));
            Accent.Tone = BleeckerEyebrowTone.Muted;
            Require(Hex(Accent.Foreground) == "#FF0C2238", "Tone changes replaced the consumer foreground override.");
            Accent.ClearValue(Control.ForegroundProperty);
        }
        finally
        {
            Accent.Content = original.Content;
            Accent.Tone = original.Tone;
            Accent.Rule = original.Rule;
            Accent.ClearValue(Control.ForegroundProperty);
            UpdateLayout();
        }
        await MainWindow.CaptureElementAsync(this, Path.Combine(folder, $"eyebrows-{scheme}.png"));
    }

    private static string Hex(Brush brush)
    {
        var color = ((SolidColorBrush)brush).Color;
        return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }
    private static T Find<T>(DependencyObject parent, string name) where T : FrameworkElement
    {
        if (parent is T match && match.Name == name) return match;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            try { return Find<T>(VisualTreeHelper.GetChild(parent, index), name); }
            catch (InvalidOperationException) { }
        }
        throw new InvalidOperationException($"Missing eyebrow template part {name}.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
