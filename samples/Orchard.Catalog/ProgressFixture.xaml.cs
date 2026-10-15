using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;

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
        await MainWindow.AwaitPresentationAsync(this);
        foreach (var control in Matrix.Children.OfType<BleeckerProgress>())
        {
            var track = Find<Border>(control, "PART_Track");
            var fill = Find<Border>(control, "PART_Fill");
            var label = Find<TextBlock>(control, "PART_Label");
            var height = control.ProgressSize switch { BleeckerProgressSize.Small => 6, BleeckerProgressSize.Medium => 8, _ => 12 };
            Require(track.ActualHeight == height && fill.ActualHeight == height && !control.UseLayoutRounding &&
                Math.Abs(fill.ActualWidth - track.ActualWidth * .65) < .001,
                "Progress size or proportional fill differs from the reference.");
            Require(track.CornerRadius.TopLeft == height / 2d && fill.CornerRadius.TopLeft == height / 2d,
                "Progress pill radius differs from half its height.");
            var bitmap = new RenderTargetBitmap();
            await bitmap.RenderAsync(fill);
            var pixels = (await bitmap.GetPixelsAsync()).ToArray();
            var x = bitmap.PixelWidth / 4;
            Require(bitmap.PixelHeight > 0 && pixels[(x * 4) + 3] >= 250 &&
                pixels[((bitmap.PixelHeight - 1) * bitmap.PixelWidth + x) * 4 + 3] >= 250,
                "Progress fill has elliptical edges instead of a straight pill body.");
            Require(label.Text == "65%" && label.MinWidth == 40 && label.Margin.Left == 12 && label.LineHeight == 16 &&
                control.FontFamily.Source.Contains("EncodeSans-Medium.ttf"), "Progress label lost percentage, spacing or typography.");
            var labelSlot = LayoutInformation.GetLayoutSlot(label);
            Require(control.ActualWidth == 320 && control.ActualHeight == 16 && track.ActualWidth == 268 &&
                labelSlot.Width - label.Margin.Left - label.Margin.Right == 40 &&
                label.TransformToVisual(control).TransformPoint(new Windows.Foundation.Point()).X == 280,
                $"Progress row allocation differs from the rendered 320px reference: row={control.ActualWidth:R}x{control.ActualHeight:R}, " +
                $"track={track.ActualWidth:R}, labelSlot={labelSlot}, textWidth={label.ActualWidth:R}, " +
                $"labelX={label.TransformToVisual(control).TransformPoint(new Windows.Foundation.Point()).X:R}.");
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
        await VerifyLayoutLifecycleAsync();
        var measurements = Matrix.Children.OfType<BleeckerProgress>().Select(control =>
        {
            var track = Find<Border>(control, "PART_Track");
            var fill = Find<Border>(control, "PART_Fill");
            var label = Find<TextBlock>(control, "PART_Label");
            return new
            {
                size = control.ProgressSize.ToString(), variant = control.Variant.ToString(),
                control.ActualWidth, control.ActualHeight, control.UseLayoutRounding,
                track = new { track.ActualWidth, track.ActualHeight, color = Hex(track.Background), opacity = track.Background.Opacity },
                fill = new { fill.ActualWidth, fill.ActualHeight, color = Hex(fill.Background) },
                label = new { label.Text, label.ActualWidth, label.ActualHeight, label.FontSize, label.BaselineOffset,
                    allocatedWidth = LayoutInformation.GetLayoutSlot(label).Width - label.Margin.Left - label.Margin.Right,
                    font = label.FontFamily.Source, weight = label.FontWeight.Weight, color = Hex(label.Foreground),
                    x = label.TransformToVisual(control).TransformPoint(new Windows.Foundation.Point()).X }
            };
        });
        await File.WriteAllTextAsync(Path.Combine(folder, $"progress-measurements-{scheme}.json"),
            System.Text.Json.JsonSerializer.Serialize(new { scheme, displayScale = XamlRoot.RasterizationScale,
                textScale = new Windows.UI.ViewManagement.UISettings().TextScaleFactor, measurements },
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        await MainWindow.CaptureElementAsync(this, Path.Combine(folder, $"progress-{scheme}.png"));
    }

    private async Task VerifyLayoutLifecycleAsync()
    {
        var originalWidth = Plain.Width;
        var originalTemplate = Plain.Template;
        try
        {
            Plain.ShowLabel = true;
            foreach (var width in new[] { 200d, 360d })
            {
                Plain.Width = width;
                UpdateLayout();
                await SettleAsync(() => Plain.ActualWidth == width && Math.Abs(Find<Border>(Plain, "PART_Fill").ActualWidth -
                    Find<Border>(Plain, "PART_Track").ActualWidth * .65) < 1);
                Require(Find<Border>(Plain, "PART_Track").ActualWidth == width - 52 && Plain.Value == 65 && Plain.Max == 100,
                    "Progress resize changed its label allocation or consumer range.");
            }
            var previousTrack = Find<Border>(Plain, "PART_Track");
            Plain.Template = null;
            Plain.ApplyTemplate();
            Plain.Template = originalTemplate;
            Plain.ApplyTemplate();
            UpdateLayout();
            Require(!ReferenceEquals(previousTrack, Find<Border>(Plain, "PART_Track")), "Progress did not replace its template visuals.");
            await SettleAsync(() => Math.Abs(Find<Border>(Plain, "PART_Fill").ActualWidth -
                Find<Border>(Plain, "PART_Track").ActualWidth * .65) < 1);
            Require(Find<TextBlock>(Plain, "PART_Label").Text == "65%", "Progress template replacement lost its label.");
        }
        finally { Plain.Template = originalTemplate; Plain.Width = originalWidth; Plain.ShowLabel = false; UpdateLayout(); }
        await SettleAsync(() => Plain.ActualWidth == originalWidth && Math.Abs(Find<Border>(Plain, "PART_Fill").ActualWidth -
            Find<Border>(Plain, "PART_Track").ActualWidth * .65) < 1);
    }

    private async Task SettleAsync(Func<bool> condition)
    {
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        while (!condition() && elapsed.Elapsed < TimeSpan.FromSeconds(1)) { UpdateLayout(); if (condition()) break; await Task.Delay(16); }
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
