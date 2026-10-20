using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.UI;

namespace Orchard.Catalog;

public sealed partial class ContrastFixture
{
    // Fixture-only paint at animation precedence. Never SetValue/ClearValue on
    // the live Foreground/Background: those operations can sever expressions.
    internal static async Task<InkSample> ReadCalibratedInkAsync(NativeInputSession input,
        TextBlock text, Panel backdrop, string folder, string name)
    {
        Require(!NativeContrastSession.Read().Enabled, "Calibrate before the actual Windows contrast transition.");
        Require(text.Foreground is SolidColorBrush { Opacity: 1 } brush && brush.Color.A == 255,
            "Calibration requires an opaque semantic foreground producer.");
        Require(IsDescendantOf(text, backdrop), "Calibration backdrop must actually contain the live glyphs.");
        var originalForeground = text.Foreground;
        var originalBackground = backdrop.Background;
        var foregroundLocal = text.ReadLocalValue(TextBlock.ForegroundProperty);
        var backgroundLocal = backdrop.ReadLocalValue(Panel.BackgroundProperty);
        var foregroundBinding = text.GetBindingExpression(TextBlock.ForegroundProperty);
        var backgroundBinding = backdrop.GetBindingExpression(Panel.BackgroundProperty);
        var foregroundBase = text.GetAnimationBaseValue(TextBlock.ForegroundProperty);
        var backgroundBase = backdrop.GetAnimationBaseValue(Panel.BackgroundProperty);
        Require(ReferenceEquals(foregroundBase, originalForeground) && ReferenceEquals(backgroundBase, originalBackground),
            "An existing paint animation makes the calibration producer ambiguous.");
        await PresentedAsync();
        var baseline = await ReadScreenInkAsync(input, text, folder, name + "-baseline");
        var baselineGeometry = GeometryWithoutPaint(baseline.Geometry);
        var black = Color.FromArgb(255, 0, 0, 0); var white = Color.FromArgb(255, 255, 255, 255);
        var onWhite = await PaintAsync(black, white, "black-on-white");
        var onBlack = await PaintAsync(white, black, "white-on-black");
        var original = ((SolidColorBrush)originalForeground).Color;
        var mask = Enumerable.Range(0, baseline.Width * baseline.Height).Where(index =>
            Exact(onWhite.Pixels, index, black) && Exact(onBlack.Pixels, index, white) &&
            Exact(baseline.Pixels, index, original)).ToArray();
        Require(mask.Length > 0, "Opposite-paint calibration established no fully covered glyph samples.");
        await SaveInkRasterAsync(baseline.Width, baseline.Height, baseline.Pixels,
            System.IO.Path.Combine(folder, $"contrast-ink-{name}-before.png"));
        await File.WriteAllTextAsync(System.IO.Path.Combine(folder, $"contrast-ink-{name}-calibration.json"),
            JsonSerializer.Serialize(new { method = "same-live-text-opposite-opaque-paints-at-animation-precedence",
                baseline.Width, baseline.Height, geometry = baseline.Geometry, pixels = mask,
                fullCoverageMeaning = "Exact8bit endpoints in both opposite-paint captures; actual Windows-state exact checks remain mandatory.",
                restoredPaintSources = true, restoredOriginalRaster = true }));
        return new InkSample(baseline.Width, baseline.Height, mask, baseline.Geometry);

        async Task<InkRaster> PaintAsync(Color foreground, Color background, string phase)
        {
            var paint = new Storyboard();
            Override(paint, text, "Foreground", new SolidColorBrush(foreground));
            Override(paint, backdrop, "Background", new SolidColorBrush(background));
            try
            {
                paint.Begin(); await PresentedAsync();
                Require(text.Foreground is SolidColorBrush ink && ink.Color == foreground && ink.Opacity == 1 &&
                    backdrop.Background is SolidColorBrush paper && paper.Color == background && paper.Opacity == 1,
                    "Calibration animation did not establish the requested opaque paints.");
                var first = await ReadScreenInkAsync(input, text, folder, name + "-" + phase + "-first");
                await PresentedAsync();
                var second = await ReadScreenInkAsync(input, text, folder, name + "-" + phase + "-second");
                Require(first.Width == baseline.Width && first.Height == baseline.Height &&
                    second.Width == baseline.Width && second.Height == baseline.Height &&
                    GeometryWithoutPaint(first.Geometry) == baselineGeometry && GeometryWithoutPaint(second.Geometry) == baselineGeometry,
                    "Calibration moved or reconfigured the actual glyphs.");
                Require(first.Pixels.SequenceEqual(second.Pixels), "Calibration paint was not stable across presented captures.");
                await SaveInkRasterAsync(first.Width, first.Height, first.Pixels,
                    System.IO.Path.Combine(folder, $"contrast-ink-{name}-{phase}.png"));
                return first;
            }
            finally
            {
                // Stop releases only animation precedence. Base expressions,
                // including ThemeResource/TemplateBinding, were never replaced.
                paint.Stop(); await PresentedAsync();
                Require(ReferenceEquals(text.Foreground, originalForeground) && ReferenceEquals(backdrop.Background, originalBackground) &&
                    ReferenceEquals(text.ReadLocalValue(TextBlock.ForegroundProperty), foregroundLocal) &&
                    ReferenceEquals(backdrop.ReadLocalValue(Panel.BackgroundProperty), backgroundLocal) &&
                    ReferenceEquals(text.GetBindingExpression(TextBlock.ForegroundProperty), foregroundBinding) &&
                    ReferenceEquals(backdrop.GetBindingExpression(Panel.BackgroundProperty), backgroundBinding) &&
                    ReferenceEquals(text.GetAnimationBaseValue(TextBlock.ForegroundProperty), foregroundBase) &&
                    ReferenceEquals(backdrop.GetAnimationBaseValue(Panel.BackgroundProperty), backgroundBase),
                    "Calibration did not restore the original live property sources.");
                var restored = await ReadScreenInkAsync(input, text, folder, name + "-" + phase + "-restored");
                Require(restored.Width == baseline.Width && restored.Height == baseline.Height &&
                    GeometryWithoutPaint(restored.Geometry) == baselineGeometry && restored.Pixels.SequenceEqual(baseline.Pixels),
                    "Calibration did not restore the exact original composed glyph raster.");
            }
        }
    }
    private static void Override(Storyboard storyboard, DependencyObject target, string property, object value)
    {
        var animation = new ObjectAnimationUsingKeyFrames { Duration = TimeSpan.FromSeconds(1), RepeatBehavior = RepeatBehavior.Forever };
        animation.KeyFrames.Add(new DiscreteObjectKeyFrame { KeyTime = TimeSpan.Zero, Value = value });
        Storyboard.SetTarget(animation, target); Storyboard.SetTargetProperty(animation, property); storyboard.Children.Add(animation);
    }
    private static async Task PresentedAsync()
    {
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var frames = 0;
        void Frame(object? sender, object args) { if (++frames == 2) ready.TrySetResult(); }
        CompositionTarget.Rendering += Frame;
        try { await ready.Task.WaitAsync(TimeSpan.FromSeconds(2)); } finally { CompositionTarget.Rendering -= Frame; }
    }
    private static bool Exact(byte[] pixels, int index, Color color) => pixels[index * 4] == color.B &&
        pixels[index * 4 + 1] == color.G && pixels[index * 4 + 2] == color.R && pixels[index * 4 + 3] == 255;
    private static string GeometryWithoutPaint(object geometry) => JsonSerializer.Serialize(
        JsonSerializer.SerializeToElement(geometry).EnumerateObject().Where(item => item.Name != "foreground")
            .ToDictionary(item => item.Name, item => item.Value));
    private static bool IsDescendantOf(DependencyObject child, DependencyObject parent)
    {
        for (var current = VisualTreeHelper.GetParent(child); current is not null; current = VisualTreeHelper.GetParent(current))
            if (ReferenceEquals(current, parent)) return true;
        return false;
    }
}
