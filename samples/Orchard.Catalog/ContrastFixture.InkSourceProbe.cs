using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace Orchard.Catalog;

public sealed partial class ContrastFixture
{
    // Diagnostic only. Never returns an accepted InkSample or changes the normal
    // calibration/contrast path. Each mode runs in its own fresh hosted process.
    internal static async Task ProbeInkSourceAsync(NativeInputSession input, TextBlock text,
        Panel backdrop, string folder, string mode)
    {
        Require(Environment.GetEnvironmentVariable("CI") == "true" &&
            Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true" &&
            Environment.GetEnvironmentVariable("RUNNER_ENVIRONMENT") == "github-hosted",
            "Ink source probing requires an isolated GitHub-hosted runner.");
        Require(mode is "foreground-color" or "backdrop-object", "Unknown ink source probe.");
        var native = NativeContrastSession.Read();
        Color[] Palette() => new[] { NativeContrastSession.Color(5), NativeContrastSession.Color(8),
            NativeContrastSession.Color(13), NativeContrastSession.Color(14) };
        var nativePalette = Palette();
        Require(!native.Enabled, "Probe must not enter Windows contrast mode.");
        Require(text.Foreground is SolidColorBrush { Opacity: 1 } &&
            ((SolidColorBrush)text.Foreground).Color.A == 255, "Opaque foreground required.");
        Require(IsDescendantOf(text, backdrop), "The actual backdrop must contain the glyphs.");
        var brush = (SolidColorBrush)text.Foreground;
        var color = brush.Color;
        var colorLocal = brush.ReadLocalValue(SolidColorBrush.ColorProperty);
        var colorBase = brush.GetAnimationBaseValue(SolidColorBrush.ColorProperty);
        Require(colorLocal is Color literal && literal == color && Equals(colorBase, color),
            "Probe requires the known non-contrast literal Color with no existing animation.");
        var background = backdrop.Background;
        var foregroundLocal = text.ReadLocalValue(TextBlock.ForegroundProperty);
        var backgroundLocal = backdrop.ReadLocalValue(Panel.BackgroundProperty);
        var foregroundBinding = text.GetBindingExpression(TextBlock.ForegroundProperty);
        var backgroundBinding = backdrop.GetBindingExpression(Panel.BackgroundProperty);
        var foregroundBase = text.GetAnimationBaseValue(TextBlock.ForegroundProperty);
        var backgroundBase = backdrop.GetAnimationBaseValue(Panel.BackgroundProperty);
        Require(ReferenceEquals(foregroundBase, brush) && ReferenceEquals(backgroundBase, background),
            "An existing property animation makes source attribution ambiguous.");
        var consumers = ProbeBrushConsumers(text.XamlRoot.Content, brush);
        var phases = new List<object>();
        var untouched = new List<object>();
        var passed = false;
        string? failure = null;
        await MainWindow.AwaitPresentationAsync(text);
        var baseline = await ReadScreenInkAsync(input, text, folder, "ink-source-baseline");
        var geometry = GeometryWithoutPaint(baseline.Geometry);
        var originalSolidPixels = Enumerable.Range(0, baseline.Width * baseline.Height)
            .Where(index => Exact(baseline.Pixels, index, color)).ToArray();
        await SaveInkRasterAsync(baseline.Width, baseline.Height, baseline.Pixels,
            System.IO.Path.Combine(folder, "ink-probe-baseline.png"));
        try
        {
            Require(originalSolidPixels.Length > 0, "The first raster has no original solid glyph samples.");
            for (var index = 1; index <= 2; index++)
            {
                await MainWindow.AwaitPresentationAsync(text);
                var read = await ReadScreenInkAsync(input, text, folder, "ink-source-untouched-" + index);
                var exact = SameRaster(read);
                untouched.Add(new { index, originalRaster = exact });
                Require(exact, "The first untouched raster changed before any probe paint.");
            }
            foreach (var endpoint in new[] { Color.FromArgb(255, 0, 0, 0), Color.FromArgb(255, 255, 255, 255) })
            {
                var name = endpoint.R == 0 ? "black" : "white";
                var paint = new Storyboard();
                if (mode == "foreground-color")
                {
                    var animation = new ColorAnimation { From = endpoint, To = endpoint,
                        Duration = TimeSpan.FromSeconds(1), RepeatBehavior = RepeatBehavior.Forever,
                        EnableDependentAnimation = true };
                    Storyboard.SetTarget(animation, brush);
                    Storyboard.SetTargetProperty(animation, "Color");
                    paint.Children.Add(animation);
                }
                else Override(paint, backdrop, "Background", new SolidColorBrush(endpoint));
                Exception? primary = null;
                var stable = false;
                var changedRaster = false;
                var expectedSolidPixels = 0;
                var newEndpointPixels = 0;
                try
                {
                    paint.Begin();
                    await MainWindow.AwaitPresentationAsync(text);
                    Require(ReferenceEquals(text.Foreground, brush), "Foreground brush identity changed.");
                    Require(mode == "foreground-color"
                        ? brush.Color == endpoint && ReferenceEquals(backdrop.Background, background)
                        : brush.Color == color && backdrop.Background is SolidColorBrush paper && paper.Color == endpoint && paper.Opacity == 1,
                        "Probe did not establish its isolated opaque paint intervention.");
                    var first = await ReadScreenInkAsync(input, text, folder, "ink-source-" + name + "-first");
                    await SaveInkRasterAsync(first.Width, first.Height, first.Pixels,
                        System.IO.Path.Combine(folder, "ink-probe-" + name + "-painted.png"));
                    await MainWindow.AwaitPresentationAsync(text);
                    var second = await ReadScreenInkAsync(input, text, folder, "ink-source-" + name + "-second");
                    Require(GeometryWithoutPaint(first.Geometry) == geometry && GeometryWithoutPaint(second.Geometry) == geometry &&
                        first.Width == baseline.Width && first.Height == baseline.Height &&
                        second.Width == baseline.Width && second.Height == baseline.Height, "Probe changed actual glyph geometry.");
                    stable = first.Pixels.SequenceEqual(second.Pixels);
                    changedRaster = !first.Pixels.SequenceEqual(baseline.Pixels);
                    expectedSolidPixels = originalSolidPixels.Count(index => Exact(first.Pixels, index,
                        mode == "foreground-color" ? endpoint : color));
                    newEndpointPixels = Enumerable.Range(0, baseline.Width * baseline.Height).Count(index =>
                        Exact(first.Pixels, index, endpoint) && !Exact(baseline.Pixels, index, endpoint) &&
                        (mode == "foreground-color" || !Exact(baseline.Pixels, index, color)));
                    Require(stable, "Probe paint changed between presented captures.");
                    Require(changedRaster && newEndpointPixels > 0 && expectedSolidPixels == originalSolidPixels.Length,
                        "The composed raster did not visibly establish the requested paint at the original solid glyph samples.");
                }
                catch (Exception error) { primary = error; throw; }
                finally
                {
                    try
                    {
                        paint.Stop();
                        await MainWindow.AwaitPresentationAsync(text);
                        var sources = ReferenceEquals(text.Foreground, brush) && ReferenceEquals(backdrop.Background, background) &&
                            ReferenceEquals(text.ReadLocalValue(TextBlock.ForegroundProperty), foregroundLocal) &&
                            ReferenceEquals(backdrop.ReadLocalValue(Panel.BackgroundProperty), backgroundLocal) &&
                            ReferenceEquals(text.GetBindingExpression(TextBlock.ForegroundProperty), foregroundBinding) &&
                            ReferenceEquals(backdrop.GetBindingExpression(Panel.BackgroundProperty), backgroundBinding) &&
                            ReferenceEquals(text.GetAnimationBaseValue(TextBlock.ForegroundProperty), foregroundBase) &&
                            ReferenceEquals(backdrop.GetAnimationBaseValue(Panel.BackgroundProperty), backgroundBase) &&
                            brush.Color == color && Equals(brush.ReadLocalValue(SolidColorBrush.ColorProperty), colorLocal) &&
                            Equals(brush.GetAnimationBaseValue(SolidColorBrush.ColorProperty), colorBase);
                        var sharedSources = consumers.All(use => ReferenceEquals(use.Owner.GetValue(use.Property), brush) &&
                            ReferenceEquals(use.Owner.ReadLocalValue(use.Property), use.Local) &&
                            ReferenceEquals(use.Owner.GetAnimationBaseValue(use.Property), use.Base));
                        var restored = await ReadScreenInkAsync(input, text, folder, "ink-source-" + name + "-restored");
                        var exact = SameRaster(restored);
                        var nativeUnchanged = NativeContrastSession.Read() == native && Palette().SequenceEqual(nativePalette);
                        await SaveInkRasterAsync(restored.Width, restored.Height, restored.Pixels,
                            System.IO.Path.Combine(folder, "ink-probe-" + name + "-restored.png"));
                        phases.Add(new { endpoint = name, stable, changedRaster, newEndpointPixels,
                            expectedSolidPixels, originalSolidPixels = originalSolidPixels.Length, sourcesRestored = sources,
                            sharedConsumerSourcesRestored = sharedSources, nativeUnchanged, originalRasterRestored = exact,
                            primaryFailure = primary?.ToString() });
                        Require(sources && sharedSources && nativeUnchanged, "Probe did not restore exact property/native sources.");
                        Require(exact, "Probe did not restore the first original composed glyph raster.");
                    }
                    catch (Exception restoreError) when (primary is not null)
                    { throw new AggregateException("Probe paint and restoration both failed.", primary, restoreError); }
                }
            }
            passed = true;
        }
        catch (Exception error) { failure = error.ToString(); throw; }
        finally
        {
            await File.WriteAllTextAsync(System.IO.Path.Combine(folder, "ink-source-probe.json"), JsonSerializer.Serialize(new
            {
                diagnosticOnly = true, componentAcceptance = false, mode, passed, failure,
                originalBaselineRetained = true, untouched, phases, nativeBefore = native,
                originalSolidPixels = originalSolidPixels.Length,
                nativeAfter = NativeContrastSession.Read(), baseline.Width, baseline.Height, baseline.Geometry,
                nativePaletteBefore = nativePalette, nativePaletteAfter = Palette(),
                colorLocalType = colorLocal.GetType().FullName,
                sharedVisualConsumers = consumers.Select(use => use.Site),
                limits = "Observed mounted visual consumer properties only; not all resource consumers. No accepted InkSample, combined calibration, Windows contrast transition or native component acceptance. Color animation permits dependent fallback; compositor path is unproved."
            }));
        }
        bool SameRaster(InkRaster read) => read.Width == baseline.Width && read.Height == baseline.Height &&
            GeometryWithoutPaint(read.Geometry) == geometry && read.Pixels.SequenceEqual(baseline.Pixels);
    }

    private sealed record InkProbeBrushUse(DependencyObject Owner, DependencyProperty Property, string Site, object Local, object Base);
    private static List<InkProbeBrushUse> ProbeBrushConsumers(DependencyObject root, Brush brush)
    {
        var result = new List<InkProbeBrushUse>();
        void Read(DependencyObject owner, DependencyProperty property, string propertyName)
        {
            if (ReferenceEquals(owner.GetValue(property), brush)) result.Add(new(owner, property,
                owner.GetType().Name + ":" + (owner as FrameworkElement)?.Name + "." + propertyName,
                owner.ReadLocalValue(property), owner.GetAnimationBaseValue(property)));
        }
        void Visit(DependencyObject owner)
        {
            if (owner is TextBlock) Read(owner, TextBlock.ForegroundProperty, "Foreground");
            if (owner is Control) Read(owner, Control.ForegroundProperty, "Foreground");
            if (owner is ContentPresenter) Read(owner, ContentPresenter.ForegroundProperty, "Foreground");
            if (owner is Panel) Read(owner, Panel.BackgroundProperty, "Background");
            if (owner is Border) { Read(owner, Border.BackgroundProperty, "Background"); Read(owner, Border.BorderBrushProperty, "BorderBrush"); }
            if (owner is Shape) { Read(owner, Shape.FillProperty, "Fill"); Read(owner, Shape.StrokeProperty, "Stroke"); }
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(owner); index++) Visit(VisualTreeHelper.GetChild(owner, index));
        }
        Visit(root);
        return result;
    }
}
