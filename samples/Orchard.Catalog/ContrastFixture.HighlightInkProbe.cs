using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.UI;

namespace Orchard.Catalog;

public sealed partial class ContrastFixture
{
    // A distinct diagnostic rendering path. Never changes the accepted
    // calibration, returns an InkSample, or writes Windows contrast settings.
    internal static async Task ProbeHighlightInkAsync(NativeInputSession input, TextBlock text,
        Panel backdrop, string folder, string label)
    {
        Require(Environment.GetEnvironmentVariable("CI") == "true" &&
            Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true" &&
            Environment.GetEnvironmentVariable("RUNNER_ENVIRONMENT") == "github-hosted",
            "Highlight ink probing requires an isolated GitHub-hosted runner.");
        Require(!string.IsNullOrWhiteSpace(text.Text) && text.SelectedText.Length == 0,
            "The actual live label must be nonempty and have no overriding text selection.");
        void Trace(int phase)
        {
            if (label != "error-description") return;
            Require(text.Name == "PART_Description" && text.Text == "We couldn't load this data. Please try again.",
                "Native trace must retain the actual Default description.");
            GlyphNativeTrace.Mark(phase);
        }
        Require(text.Foreground is SolidColorBrush { Opacity: 1 } &&
            ((SolidColorBrush)text.Foreground).Color.A == 255 && IsDescendantOf(text, backdrop),
            "The actual glyphs require an opaque semantic foreground and containing backdrop.");
        var native = NativeContrastSession.Read();
        Require(!native.Enabled, "This diagnostic must not enter Windows contrast mode.");
        Color[] Palette() => new[] { NativeContrastSession.Color(5), NativeContrastSession.Color(8),
            NativeContrastSession.Color(13), NativeContrastSession.Color(14) };
        var palette = Palette();
        var registry = ReadHighlightProbeRegistry();
        var originalText = text.Text;
        var brush = (SolidColorBrush)text.Foreground;
        var color = brush.Color;
        var colorLocal = brush.ReadLocalValue(SolidColorBrush.ColorProperty);
        var colorBase = brush.GetAnimationBaseValue(SolidColorBrush.ColorProperty);
        var background = backdrop.Background;
        var foregroundLocal = text.ReadLocalValue(TextBlock.ForegroundProperty);
        var backgroundLocal = backdrop.ReadLocalValue(Panel.BackgroundProperty);
        var foregroundBinding = text.GetBindingExpression(TextBlock.ForegroundProperty);
        var backgroundBinding = backdrop.GetBindingExpression(Panel.BackgroundProperty);
        var foregroundBase = text.GetAnimationBaseValue(TextBlock.ForegroundProperty);
        var backgroundBase = backdrop.GetAnimationBaseValue(Panel.BackgroundProperty);
        Require(ReferenceEquals(foregroundBase, brush) && ReferenceEquals(backgroundBase, background),
            "Existing paint animations make the source attribution ambiguous.");
        var consumers = ProbeBrushConsumers(text.XamlRoot.Content, brush);
        var observations = new List<object>();
        var phases = new List<object>();
        IList<TextHighlighter>? collection = null;
        HighlightProbeOriginal[] originals = [];
        var passed = false;
        string? failure = null;
        var mask = Array.Empty<int>();
        // Do not access TextHighlighters before the first untouched baseline:
        // its getter installs native collection/view state and changes rendering.
        Trace(1);
        await MainWindow.AwaitPresentationAsync(text);
        var baseline = await ReadScreenInkAsync(input, text, folder, "highlight-baseline");
        Trace(2);
        var geometry = GeometryWithoutPaint(baseline.Geometry);
        await Save("baseline", baseline);
        try
        {
            for (var index = 1; index <= 2; index++)
                await ObserveOriginal("untouched-" + index);
            Trace(3);
            collection = text.TextHighlighters;
            originals = collection.Select(item => new HighlightProbeOriginal(item, item.Foreground,
                item.Background, item.Ranges.ToArray(), Hex(item.Foreground), Hex(item.Background))).ToArray();
            await ObserveOriginal("getter-only");

            var black = Color.FromArgb(255, 0, 0, 0);
            var white = Color.FromArgb(255, 255, 255, 255);
            var onWhite = await Paint(black, white, "black-on-white");
            var onBlack = await Paint(white, black, "white-on-black");
            // Identical to the original calibration's exact three-way contract.
            // All-initial-solid equality is retained as a diagnostic count only.
            mask = Enumerable.Range(0, baseline.Width * baseline.Height).Where(index =>
                Exact(onWhite.Pixels, index, black) && Exact(onBlack.Pixels, index, white) &&
                Exact(baseline.Pixels, index, color)).ToArray();
            Require(mask.Length > 0, "Opposite live highlight paints established no original fully covered glyph samples.");
            passed = true;
        }
        catch (Exception error) { failure = error.ToString(); throw; }
        finally
        {
            await File.WriteAllTextAsync(System.IO.Path.Combine(folder, "highlight-ink-probe.json"),
                JsonSerializer.Serialize(new { diagnosticOnly = true, componentAcceptance = false,
                    mechanism = "owned-live-text-highlighter-with-scoped-backdrop-animation", label,
                    actualText = originalText, passed, failure, originalBaselineRetained = true,
                    baseline.Width, baseline.Height, baseline.Geometry, originalForeground = ColorHex(color),
                    observations, phases, exactIntersectionPixels = mask, nativeBefore = native,
                    nativeAfter = NativeContrastSession.Read(), paletteBefore = palette, paletteAfter = Palette(),
                    registryBefore = registry, registryAfter = ReadHighlightProbeRegistry(),
                    originalCollectionCount = originals.Length, collectionContentsRestored = CollectionUnchanged(),
                    foregroundLocalType = foregroundLocal?.GetType().FullName,
                    foregroundLocalUnset = ReferenceEquals(foregroundLocal, DependencyProperty.UnsetValue),
                    sharedVisualConsumers = consumers.Select(use => use.Site),
                    limits = "No accepted InkSample, component acceptance, Windows contrast transition or OS-restoration repair. Lazy collection internal absence is not restored; exact first-raster and specified source/native equality are mandatory. Default highlighter resources may be realized/frozen; no global mutation-free claim." }));
        }

        bool SourcesUnchanged(bool paintedBackdrop = false) => text.Text == originalText && text.SelectedText.Length == 0 &&
            ReferenceEquals(text.Foreground, brush) && brush.Color == color &&
            Equals(brush.ReadLocalValue(SolidColorBrush.ColorProperty), colorLocal) &&
            Equals(brush.GetAnimationBaseValue(SolidColorBrush.ColorProperty), colorBase) &&
            (paintedBackdrop || ReferenceEquals(backdrop.Background, background)) &&
            ReferenceEquals(text.ReadLocalValue(TextBlock.ForegroundProperty), foregroundLocal) &&
            ReferenceEquals(backdrop.ReadLocalValue(Panel.BackgroundProperty), backgroundLocal) &&
            ReferenceEquals(text.GetBindingExpression(TextBlock.ForegroundProperty), foregroundBinding) &&
            ReferenceEquals(backdrop.GetBindingExpression(Panel.BackgroundProperty), backgroundBinding) &&
            ReferenceEquals(text.GetAnimationBaseValue(TextBlock.ForegroundProperty), foregroundBase) &&
            ReferenceEquals(backdrop.GetAnimationBaseValue(Panel.BackgroundProperty), backgroundBase);
        bool SharedUnchanged() => consumers.All(use => ReferenceEquals(use.Owner.GetValue(use.Property), brush) &&
            ReferenceEquals(use.Owner.ReadLocalValue(use.Property), use.Local) &&
            ReferenceEquals(use.Owner.GetAnimationBaseValue(use.Property), use.Base));
        bool NativeUnchanged() => NativeContrastSession.Read() == native && Palette().SequenceEqual(palette) &&
            ReadHighlightProbeRegistry() == registry;
        bool CollectionUnchanged() => collection is null ||
            (ReferenceEquals(text.TextHighlighters, collection) && collection.Count == originals.Length &&
             originals.Select((before, index) => ReferenceEquals(collection[index], before.Item) &&
                 ReferenceEquals(before.Item.Foreground, before.Foreground) && ReferenceEquals(before.Item.Background, before.Background) &&
                 before.Item.Ranges.SequenceEqual(before.Ranges) && Hex(before.Item.Foreground) == before.ForegroundValue &&
                 Hex(before.Item.Background) == before.BackgroundValue).All(equal => equal));
        bool SameRaster(InkRaster raster) => raster.Width == baseline.Width && raster.Height == baseline.Height &&
            GeometryWithoutPaint(raster.Geometry) == geometry && raster.Pixels.SequenceEqual(baseline.Pixels);
        async Task Save(string name, InkRaster raster) => await SaveInkRasterAsync(raster.Width, raster.Height,
            raster.Pixels, System.IO.Path.Combine(folder, "highlight-ink-" + name + ".png"));
        async Task ObserveOriginal(string name)
        {
            await MainWindow.AwaitPresentationAsync(text);
            var raster = await ReadScreenInkAsync(input, text, folder, "highlight-" + name);
            await Save(name, raster);
            var exact = SameRaster(raster); var sources = SourcesUnchanged(); var shared = SharedUnchanged();
            var state = NativeUnchanged(); var items = CollectionUnchanged();
            observations.Add(new { phase = name, originalRasterRestored = exact, sourcesRestored = sources,
                sharedSourcesRestored = shared, nativeUnchanged = state, collectionContentsRestored = items });
            if (name == "getter-only") Trace(4);
            else if (name == "black-on-white-restored") Trace(8);
            else if (name == "white-on-black-restored") Trace(12);
            Require(exact && sources && shared && state && items,
                "Highlight initialization/cleanup changed the first original raster or specified sources: " + name);
        }
        async Task<InkRaster> Paint(Color foreground, Color paper, string name)
        {
            Require(collection is not null && CollectionUnchanged(), "Original highlight collection must be intact.");
            var owned = new TextHighlighter { Foreground = new SolidColorBrush(foreground),
                Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)) };
            owned.Ranges.Add(new TextRange { StartIndex = 0, Length = originalText.Length });
            var paint = new Storyboard();
            Override(paint, backdrop, "Background", new SolidColorBrush(paper));
            Exception? primary = null;
            var stable = false; var changed = false; var actualEndpointPixels = 0;
            var initialSolidAtEndpoint = 0;
            try
            {
                Trace(name == "black-on-white" ? 5 : 9);
                collection!.Add(owned);
                paint.Begin();
                await MainWindow.AwaitPresentationAsync(text);
                Require(SourcesUnchanged(paintedBackdrop: true) && SharedUnchanged() && NativeUnchanged() &&
                    owned.Foreground is SolidColorBrush ink && ink.Color == foreground && ink.Opacity == 1 &&
                    owned.Background is SolidColorBrush clear && clear.Color.A == 0 &&
                    backdrop.Background is SolidColorBrush surface && surface.Color == paper && surface.Opacity == 1,
                    "Highlight producer is not the requested opaque live paint with preserved original foreground sources.");
                var first = await ReadScreenInkAsync(input, text, folder, "highlight-" + name + "-first");
                await Save(name + "-first", first);
                Trace(name == "black-on-white" ? 6 : 10);
                await MainWindow.AwaitPresentationAsync(text);
                var second = await ReadScreenInkAsync(input, text, folder, "highlight-" + name + "-second");
                await Save(name + "-second", second);
                Require(first.Width == baseline.Width && first.Height == baseline.Height && second.Width == baseline.Width &&
                    second.Height == baseline.Height && GeometryWithoutPaint(first.Geometry) == geometry &&
                    GeometryWithoutPaint(second.Geometry) == geometry, "Highlight paint moved or reconfigured the actual live glyphs.");
                stable = first.Pixels.SequenceEqual(second.Pixels); changed = !SameRaster(first);
                actualEndpointPixels = Enumerable.Range(0, baseline.Width * baseline.Height).Count(index =>
                    Exact(first.Pixels, index, foreground) && !Exact(baseline.Pixels, index, foreground));
                initialSolidAtEndpoint = Enumerable.Range(0, baseline.Width * baseline.Height).Count(index =>
                    Exact(baseline.Pixels, index, color) && Exact(first.Pixels, index, foreground));
                Require(stable, "Highlight paint is unstable across presented captures.");
                Require(changed && actualEndpointPixels > 0,
                    "The actual live capture did not establish new exact RGBA8 endpoint pixels.");
                return first;
            }
            catch (Exception error) { primary = error; throw; }
            finally
            {
                var restorationErrors = new List<Exception>();
                try { Trace(name == "black-on-white" ? 7 : 11); }
                catch (Exception error) { restorationErrors.Add(error); }
                try
                {
                    for (var index = collection!.Count - 1; index >= 0; index--)
                        if (ReferenceEquals(collection[index], owned)) collection.RemoveAt(index);
                }
                catch (Exception error) { restorationErrors.Add(error); }
                try { paint.Stop(); }
                catch (Exception error) { restorationErrors.Add(error); }
                try { await ObserveOriginal(name + "-restored"); }
                catch (Exception error) { restorationErrors.Add(error); }
                phases.Add(new { phase = name, stable, changedRaster = changed, actualEndpointPixels,
                    initialSolidAtEndpoint, restored = restorationErrors.Count == 0, primaryFailure = primary?.ToString() });
                if (restorationErrors.Count > 0)
                {
                    if (primary is not null) restorationErrors.Insert(0, primary);
                    throw new AggregateException("Highlight exact restoration failed; all owned cleanup operations were attempted.", restorationErrors);
                }
            }
        }
    }

    private sealed record HighlightProbeOriginal(TextHighlighter Item, Brush? Foreground, Brush? Background,
        TextRange[] Ranges, string? ForegroundValue, string? BackgroundValue);

    private static string ReadHighlightProbeRegistry()
    {
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\Accessibility\HighContrast", false);
        var names = key?.GetValueNames() ?? [];
        return JsonSerializer.Serialize(new[] { "Flags", "High Contrast Scheme" }.Select(name =>
        {
            var present = names.Contains(name, StringComparer.OrdinalIgnoreCase);
            return new { name, present, kind = present ? key!.GetValueKind(name).ToString() : null,
                value = present ? key!.GetValue(name, null, Microsoft.Win32.RegistryValueOptions.DoNotExpandEnvironmentNames) : null };
        }));
    }
}
