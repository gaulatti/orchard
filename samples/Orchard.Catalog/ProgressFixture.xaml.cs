using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class ProgressFixture : UserControl
{
    public ProgressFixture()
    {
        InitializeComponent();
        foreach (var size in Enum.GetValues<BleeckerProgressSize>())
        foreach (var variant in Enum.GetValues<BleeckerProgressVariant>())
            Matrix.Children.Add(new BleeckerProgress { ProgressSize = size, Variant = variant, Value = 65, ShowLabel = true });
    }

    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        UpdateLayout();
        await Task.Delay(100);
        foreach (var control in Matrix.Children.OfType<BleeckerProgress>())
        {
            var track = Find<Border>(control, "PART_Track");
            var fill = Find<Border>(control, "PART_Fill");
            var label = Find<TextBlock>(control, "PART_Label");
            var height = control.ProgressSize switch { BleeckerProgressSize.Small => 6, BleeckerProgressSize.Medium => 8, _ => 12 };
            Require(track.ActualHeight == height && fill.ActualHeight == height && Math.Abs(fill.ActualWidth - track.ActualWidth * .65) < 1,
                "Progress size or proportional fill differs from the reference.");
            Require(label.Text == "65%" && label.MinWidth == 40 && label.Margin.Left == 12 && label.LineHeight == 16 &&
                control.FontFamily.Source.Contains("EncodeSans-Medium.ttf"), "Progress label lost percentage, spacing or typography.");
            var key = control.Variant switch { BleeckerProgressVariant.Warning => scheme == BleeckerColorScheme.Dark ? "accentGold" : "sunset",
                BleeckerProgressVariant.Destructive => "terracotta", _ => scheme == BleeckerColorScheme.Dark ? "accentBlue" : "sea" };
            Require(Hex(fill.Background) == BleeckerTokens.Colors(scheme)[key], "Progress fill variant differs from the theme.");
        }
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(Plain);
        var range = (IRangeValueProvider)peer.GetPattern(PatternInterface.RangeValue);
        Require(peer.GetAutomationControlType() == AutomationControlType.ProgressBar && peer.GetName() == "Upload" &&
            range.Value == 65 && range.Maximum == 100 && range.Minimum == 0 && range.IsReadOnly && !Plain.IsTabStop,
            "Progress lost its native read-only range semantics.");
        var rejected = false;
        try { range.SetValue(12); }
        catch (InvalidOperationException) { rejected = true; }
        Require(rejected && Plain.Value == 65, "Progress accepted a UIA write or changed consumer progress.");
        Require(Disabled.Opacity == 1, "Disabled progress acquired Windows dimming.");
        try
        {
            Plain.ShowLabel = true;
            Plain.Max = 200;
            foreach (var (value, percentage) in new[] { (-50d, 0d), (50d, 25d), (145d, 72.5d), (250d, 100d) })
            {
                Plain.Value = value;
                await SettleAsync(() => Math.Abs(Find<Border>(Plain, "PART_Fill").ActualWidth -
                    Find<Border>(Plain, "PART_Track").ActualWidth * percentage / 100) < 1);
                Require(range.Value == value && range.Maximum == 200 &&
                    Find<TextBlock>(Plain, "PART_Label").Text == $"{Math.Floor(percentage + .5)}%",
                    "Progress normalization changed consumer values or rounded its label incorrectly.");
            }
        }
        finally { Plain.Value = 65; Plain.Max = 100; Plain.ShowLabel = false; }
        await SettleAsync(() => Math.Abs(Find<Border>(Plain, "PART_Fill").ActualWidth -
            Find<Border>(Plain, "PART_Track").ActualWidth * .65) < 1);
        await MainWindow.CaptureElementAsync(this, Path.Combine(folder, $"progress-{scheme}.png"));
    }

    private async Task SettleAsync(Func<bool> condition)
    {
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        while (!condition() && elapsed.Elapsed < TimeSpan.FromSeconds(1)) { UpdateLayout(); await Task.Delay(16); }
        Require(condition(), "Progress did not reach its destination width.");
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
        throw new InvalidOperationException($"Missing progress template part {name}.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
