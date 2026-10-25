using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI.ViewManagement;

namespace Orchard;

public sealed partial class BleeckerMenubar
{
    private UISettings? rowMotionSettings;
    private bool RowAnimationsEnabled => (rowMotionSettings ??= new()).AnimationsEnabled;

    private sealed partial class MenubarButton
    {
        private ContentPresenter? rowPalette, rowPresenter;
        private Border? rowChrome;
        private readonly SolidColorBrush rowBackground = new(), rowForeground = new();
        private readonly BleeckerCssTransition<BleeckerCssColor> backgroundMotion = new(BleeckerCssColor.Mix), foregroundMotion = new(BleeckerCssColor.Mix);
        private readonly Stopwatch rowClock = Stopwatch.StartNew();
        private readonly List<(SolidColorBrush Brush, long Color, long Opacity)> rowBrushTokens = [];
        private long paletteBackgroundToken, paletteForegroundToken;
        private bool backgroundSolid, foregroundSolid, rowRendering, rowPaletteObserved;
        private int rowPaintVersion;

        private void InitializeRowPaint()
        {
            Loaded += (_, _) => { ObserveRowPalette(); UpdateVisualState(); RequestRowPaint(false); };
            Unloaded += (_, _) => { hovered = false; StopRowPaint(); ClearRowBrushObservers(); UnobserveRowPalette(); };
            ActualThemeChanged += (_, _) => RequestRowPaint(true);
            RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) ReleaseRowPaint(); });
        }
        protected override void OnApplyTemplate()
        {
            ReleaseRowPaint(); base.OnApplyTemplate();
            // Caller templates without this palette own their presentation.
            rowPalette = GetTemplateChild("MenuPalette") as ContentPresenter;
            if (rowPalette is null) return;
            rowChrome = GetTemplateChild("MenuPaint") as Border ?? throw new InvalidOperationException("Menubar paint template requires MenuPaint.");
            rowPresenter = GetTemplateChild("Presenter") as ContentPresenter ?? throw new InvalidOperationException("Menubar paint template requires Presenter.");
            ObserveRowPalette();
            UpdateVisualState(); RetargetRowPaint(rowClock.Elapsed.TotalMilliseconds, false);
        }
        private void RequestRowPaint(bool animate)
        {
            var version = ++rowPaintVersion;
            if (rowPalette is null || !IsLoaded) return;
            if (!animate || !owner.RowAnimationsEnabled) { RetargetRowPaint(rowClock.Elapsed.TotalMilliseconds, false); return; }
            if (!DispatcherQueue.TryEnqueue(() => { if (version == rowPaintVersion && IsLoaded) RetargetRowPaint(rowClock.Elapsed.TotalMilliseconds, true); }))
                throw new InvalidOperationException("Menubar dispatcher rejected its paint update.");
        }
        private void RetargetRowPaint(double time, bool animate)
        {
            if (rowPalette is null || rowChrome is null || rowPresenter is null) return;
            var duration = BleeckerTokens.Durations["control"];
            static BleeckerCssColor Color(SolidColorBrush brush, bool legacy) => BleeckerCssColor.FromRgba((brush.Color.R, brush.Color.G, brush.Color.B, brush.Color.A / 255d * brush.Opacity), legacy);
            var backgroundWasSolid = backgroundSolid; var foregroundWasSolid = foregroundSolid;
            backgroundSolid = rowPalette.Background is SolidColorBrush; foregroundSolid = rowPalette.Foreground is SolidColorBrush;
            if (rowPalette.Background is SolidColorBrush background)
            {
                backgroundMotion.Retarget(Color(background, !(hovered && !(IsRootTrigger && expanded))), time, duration, animate && backgroundWasSolid);
                rowChrome.Background = rowBackground;
            }
            else rowChrome.Background = rowPalette.Background; // Preserve original non-color brushes without interpolation.
            if (rowPalette.Foreground is SolidColorBrush foreground)
            {
                foregroundMotion.Retarget(Color(foreground, true), time, duration, animate && foregroundWasSolid);
                rowPresenter.Foreground = rowForeground;
            }
            else rowPresenter.Foreground = rowPalette.Foreground;
            ClearRowBrushObservers();
            if (IsLoaded) foreach (var brush in new[] { rowPalette.Background, rowPalette.Foreground }.OfType<SolidColorBrush>().Distinct())
                rowBrushTokens.Add((brush, brush.RegisterPropertyChangedCallback(SolidColorBrush.ColorProperty, (_, _) => RequestRowPaint(true)), brush.RegisterPropertyChangedCallback(Brush.OpacityProperty, (_, _) => RequestRowPaint(true))));
            PaintRowAt(time);
            if (RowPaintRunning(time)) { if (!rowRendering) { CompositionTarget.Rendering += RenderRowPaint; rowRendering = true; } }
            else StopRowRendering();
        }
        private bool RowPaintRunning(double time) => backgroundSolid && backgroundMotion.IsRunning(time) || foregroundSolid && foregroundMotion.IsRunning(time);
        private void PaintRowAt(double time)
        {
            static void Paint(SolidColorBrush brush, BleeckerCssColor value) { var c = value.Encode(); brush.Color = Windows.UI.Color.FromArgb(255, c.R, c.G, c.B); brush.Opacity = c.Alpha; }
            if (backgroundSolid) Paint(rowBackground, backgroundMotion.ValueAt(time));
            if (foregroundSolid) Paint(rowForeground, foregroundMotion.ValueAt(time));
        }
        private void RenderRowPaint(object? sender, object args)
        {
            var time = rowClock.Elapsed.TotalMilliseconds;
            if (!owner.RowAnimationsEnabled) RetargetRowPaint(time, false); else PaintRowAt(time);
            if (!RowPaintRunning(time)) StopRowRendering();
        }
        private void StopRowRendering() { if (rowRendering) CompositionTarget.Rendering -= RenderRowPaint; rowRendering = false; }
        private void StopRowPaint() { rowPaintVersion++; StopRowRendering(); }
        private void ClearRowBrushObservers()
        {
            foreach (var (brush, color, opacity) in rowBrushTokens) { brush.UnregisterPropertyChangedCallback(SolidColorBrush.ColorProperty, color); brush.UnregisterPropertyChangedCallback(Brush.OpacityProperty, opacity); }
            rowBrushTokens.Clear();
        }
        private void ObserveRowPalette()
        {
            if (rowPalette is null || rowPaletteObserved) return;
            paletteBackgroundToken = rowPalette.RegisterPropertyChangedCallback(ContentPresenter.BackgroundProperty, (_, _) => RequestRowPaint(true));
            paletteForegroundToken = rowPalette.RegisterPropertyChangedCallback(ContentPresenter.ForegroundProperty, (_, _) => RequestRowPaint(true));
            rowPaletteObserved = true;
        }
        private void UnobserveRowPalette()
        {
            if (rowPalette is not null && rowPaletteObserved) { rowPalette.UnregisterPropertyChangedCallback(ContentPresenter.BackgroundProperty, paletteBackgroundToken); rowPalette.UnregisterPropertyChangedCallback(ContentPresenter.ForegroundProperty, paletteForegroundToken); }
            rowPaletteObserved = false;
        }
        private void ReleaseRowPaint()
        {
            StopRowPaint(); ClearRowBrushObservers();
            UnobserveRowPalette();
            rowPalette = rowPresenter = null; rowChrome = null; backgroundSolid = foregroundSolid = false;
        }
    }
}
