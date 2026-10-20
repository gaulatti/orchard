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
        await MainWindow.AwaitPresentationAsync(text);
        var baseline = await ReadScreenInkAsync(input, text, folder, name + "-baseline");
        var baselineGeometry = GeometryWithoutPaint(baseline.Geometry);
        // Preserve the first baseline as the acceptance reference. Consecutive
        // untouched observations distinguish settling from paint-induced change.
        var untouched = new List<object>();
        for (var index = 1; index <= 2; index++)
        {
            await MainWindow.AwaitPresentationAsync(text);
            var observation = await ReadScreenInkAsync(input, text, folder, name + "-untouched-" + index);
            untouched.Add(new { index,
                geometryUnchanged = GeometryWithoutPaint(observation.Geometry) == baselineGeometry,
                originalRaster = observation.Width == baseline.Width && observation.Height == baseline.Height &&
                    observation.Pixels.SequenceEqual(baseline.Pixels) });
        }
        await File.WriteAllTextAsync(System.IO.Path.Combine(folder, $"contrast-ink-{name}-untouched.json"),
            JsonSerializer.Serialize(new { diagnosticOnly = true, originalBaselineRetained = true, untouched }));
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
            var unstablePaint = false;
            var paint = new Storyboard();
            Override(paint, text, "Foreground", new SolidColorBrush(foreground));
            Override(paint, backdrop, "Background", new SolidColorBrush(background));
            try
            {
                paint.Begin(); await MainWindow.AwaitPresentationAsync(text);
                Require(text.Foreground is SolidColorBrush ink && ink.Color == foreground && ink.Opacity == 1 &&
                    backdrop.Background is SolidColorBrush paper && paper.Color == background && paper.Opacity == 1,
                    "Calibration animation did not establish the requested opaque paints.");
                var first = await ReadScreenInkAsync(input, text, folder, name + "-" + phase + "-first");
                await MainWindow.AwaitPresentationAsync(text);
                var second = await ReadScreenInkAsync(input, text, folder, name + "-" + phase + "-second");
                Require(first.Width == baseline.Width && first.Height == baseline.Height &&
                    second.Width == baseline.Width && second.Height == baseline.Height &&
                    GeometryWithoutPaint(first.Geometry) == baselineGeometry && GeometryWithoutPaint(second.Geometry) == baselineGeometry,
                    "Calibration moved or reconfigured the actual glyphs.");
                unstablePaint = !first.Pixels.SequenceEqual(second.Pixels);
                Require(!unstablePaint, "Calibration paint was not stable across presented captures.");
                await SaveInkRasterAsync(first.Width, first.Height, first.Pixels,
                    System.IO.Path.Combine(folder, $"contrast-ink-{name}-{phase}.png"));
                return first;
            }
            finally
            {
                // Stop releases only animation precedence. Base expressions,
                // including ThemeResource/TemplateBinding, were never replaced.
                paint.Stop(); await MainWindow.AwaitPresentationAsync(text);
                Require(ReferenceEquals(text.Foreground, originalForeground) && ReferenceEquals(backdrop.Background, originalBackground) &&
                    ReferenceEquals(text.ReadLocalValue(TextBlock.ForegroundProperty), foregroundLocal) &&
                    ReferenceEquals(backdrop.ReadLocalValue(Panel.BackgroundProperty), backgroundLocal) &&
                    ReferenceEquals(text.GetBindingExpression(TextBlock.ForegroundProperty), foregroundBinding) &&
                    ReferenceEquals(backdrop.GetBindingExpression(Panel.BackgroundProperty), backgroundBinding) &&
                    ReferenceEquals(text.GetAnimationBaseValue(TextBlock.ForegroundProperty), foregroundBase) &&
                    ReferenceEquals(backdrop.GetAnimationBaseValue(Panel.BackgroundProperty), backgroundBase),
                    "Calibration did not restore the original live property sources.");
                var restored = await ReadScreenInkAsync(input, text, folder, name + "-" + phase + "-restored");
                var exactRestoration = restored.Width == baseline.Width && restored.Height == baseline.Height &&
                    GeometryWithoutPaint(restored.Geometry) == baselineGeometry && restored.Pixels.SequenceEqual(baseline.Pixels);
                if (!exactRestoration || unstablePaint)
                {
                    try { await DiagnoseInterventionsAsync(); }
                    catch (Exception diagnosticError)
                    {
                        var primaryFailure = !exactRestoration
                            ? "Calibration did not restore the exact original composed glyph raster."
                            : "Calibration paint was not stable across presented captures.";
                        throw new InvalidOperationException(primaryFailure + " Failure diagnostics also failed.", diagnosticError);
                    }
                }
                Require(exactRestoration, "Calibration did not restore the exact original composed glyph raster.");
            }
        }
        async Task DiagnoseInterventionsAsync()
        {
            // Run only after failure. These observations cannot replace the
            // baseline, retry calibration, or turn the failed check into a pass.
            var observations = new List<object>();
            for (var index = 1; index <= 2; index++)
            {
                await MainWindow.AwaitPresentationAsync(text);
                var stopped = await ReadScreenInkAsync(input, text, folder, name + "-diagnostic-stopped-" + index);
                observations.Add(new { intervention = "none", index,
                    geometryUnchanged = GeometryWithoutPaint(stopped.Geometry) == baselineGeometry,
                    restoredOriginalRaster = stopped.Width == baseline.Width && stopped.Height == baseline.Height &&
                        stopped.Pixels.SequenceEqual(baseline.Pixels) });
            }
            foreach (var intervention in new[] { "foreground-only", "backdrop-only" })
            {
                var before = await ReadScreenInkAsync(input, text, folder, name + "-diagnostic-" + intervention + "-before");
                var diagnostic = new Storyboard();
                if (intervention == "foreground-only") Override(diagnostic, text, "Foreground", new SolidColorBrush(black));
                else Override(diagnostic, backdrop, "Background", new SolidColorBrush(white));
                try
                {
                    diagnostic.Begin(); await MainWindow.AwaitPresentationAsync(text);
                    await ReadScreenInkAsync(input, text, folder, name + "-diagnostic-" + intervention + "-painted");
                }
                finally
                {
                    diagnostic.Stop(); await MainWindow.AwaitPresentationAsync(text);
                }
                var after = await ReadScreenInkAsync(input, text, folder, name + "-diagnostic-" + intervention + "-restored");
                var identityRestored = ReferenceEquals(text.Foreground, originalForeground) && ReferenceEquals(backdrop.Background, originalBackground) &&
                    ReferenceEquals(text.ReadLocalValue(TextBlock.ForegroundProperty), foregroundLocal) &&
                    ReferenceEquals(backdrop.ReadLocalValue(Panel.BackgroundProperty), backgroundLocal) &&
                    ReferenceEquals(text.GetBindingExpression(TextBlock.ForegroundProperty), foregroundBinding) &&
                    ReferenceEquals(backdrop.GetBindingExpression(Panel.BackgroundProperty), backgroundBinding) &&
                    ReferenceEquals(text.GetAnimationBaseValue(TextBlock.ForegroundProperty), foregroundBase) &&
                    ReferenceEquals(backdrop.GetAnimationBaseValue(Panel.BackgroundProperty), backgroundBase);
                observations.Add(new { intervention, identityRestored,
                    geometryUnchanged = GeometryWithoutPaint(before.Geometry) == GeometryWithoutPaint(after.Geometry),
                    restoredPhaseRaster = before.Pixels.SequenceEqual(after.Pixels),
                    restoredOriginalRaster = after.Pixels.SequenceEqual(baseline.Pixels) });
            }
            await File.WriteAllTextAsync(System.IO.Path.Combine(folder, $"contrast-ink-{name}-failed-interventions.json"),
                JsonSerializer.Serialize(new { diagnosticOnly = true, originalFailureRetained = true, observations }));
        }

    }
    private static void Override(Storyboard storyboard, DependencyObject target, string property, object value)
    {
        var animation = new ObjectAnimationUsingKeyFrames { Duration = TimeSpan.FromSeconds(1), RepeatBehavior = RepeatBehavior.Forever };
        animation.KeyFrames.Add(new DiscreteObjectKeyFrame { KeyTime = TimeSpan.Zero, Value = value });
        Storyboard.SetTarget(animation, target); Storyboard.SetTargetProperty(animation, property); storyboard.Children.Add(animation);
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
