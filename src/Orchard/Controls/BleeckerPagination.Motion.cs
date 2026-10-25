using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.UI.ViewManagement;

namespace Orchard;

public sealed partial class BleeckerPagination
{
    private readonly UISettings motionSettings = new();
    private readonly Stopwatch paintClock = Stopwatch.StartNew();
    private ResourceDictionary? actionTemplateResources;
    private ControlTemplate ActionTemplate => (ControlTemplate)(actionTemplateResources ??=
        new ResourceDictionary { Source = new Uri("ms-appx:///Orchard/Themes/PaginationAction.xaml") })["BleeckerPaginationActionTemplate"];

    private void OnPaginationLoaded(object sender, RoutedEventArgs args)
    {
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) motionSettings.AnimationsEnabledChanged += OnPaginationMotionPreferenceChanged;
        Refresh(); foreach (var button in Ordered()) button.RequestPaint(false);
    }
    private void OnPaginationUnloaded(object sender, RoutedEventArgs args)
    {
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) motionSettings.AnimationsEnabledChanged -= OnPaginationMotionPreferenceChanged;
        foreach (var button in Ordered()) button.StopPaint();
    }
    private void OnPaginationMotionPreferenceChanged(UISettings sender, UISettingsAnimationsEnabledChangedEventArgs args) =>
        DispatcherQueue.TryEnqueue(() => { foreach (var button in Ordered()) button.RequestPaint(false); });

    private sealed partial class ActionButton
    {
        private readonly BleeckerPaginationAnimation paintMotion = new();
        private ContentPresenter? paintPresenter, hoverPalette;
        private readonly SolidColorBrush paintedBackground = new(), paintedBorder = new(), paintedForeground = new();
        private readonly List<(DependencyProperty Property, long Token)> hoverTokens = [];
        private readonly List<(SolidColorBrush Brush, long ColorToken, long OpacityToken)> brushTokens = [];
        private bool pointerOver, rendering;
        private int paintVersion;

        private void InitializePaint()
        {
            foreach (var property in new[] { BackgroundProperty, BorderBrushProperty, ForegroundProperty })
                RegisterPropertyChangedCallback(property, (_, _) => RequestPaint(true));
            ActualThemeChanged += (_, _) => RequestPaint(true);
            AddHandler(PointerEnteredEvent, new PointerEventHandler((_, _) => { pointerOver = true; RequestPaint(true); }), true);
            AddHandler(PointerExitedEvent, new PointerEventHandler((_, _) => { pointerOver = false; RequestPaint(true); }), true);
            Loaded += (_, _) => RequestPaint(false);
            Unloaded += (_, _) => { pointerOver = false; StopPaint(); };
        }

        private void ApplyPaintTemplate()
        {
            paintPresenter = GetTemplateChild("ContentPresenter") as ContentPresenter ?? throw new InvalidOperationException("Pagination requires its paint presenter.");
            hoverPalette = GetTemplateChild("PaginationHoverPalette") as ContentPresenter ?? throw new InvalidOperationException("Pagination requires its hover palette.");
            paintPresenter.Background = paintedBackground; paintPresenter.BorderBrush = paintedBorder; paintPresenter.Foreground = paintedForeground;
            ObserveHoverPalette();
            UpdatePaint(false);
        }

        private void ReleasePaintTemplate()
        {
            StopPaint();
            paintPresenter = null; hoverPalette = null;
        }

        internal void RequestPaint(bool animate)
        {
            var version = ++paintVersion;
            if (paintPresenter is null || hoverPalette is null || !IsLoaded || !Active) return;
            ObserveHoverPalette();
            if (!animate || !owner.motionSettings.AnimationsEnabled) { UpdatePaint(false); return; }
            DispatcherQueue.TryEnqueue(() => { if (version == paintVersion && IsLoaded) UpdatePaint(true); });
        }

        private void UpdatePaint(bool animate)
        {
            if (paintPresenter is null || hoverPalette is null) return;
            var background = pointerOver ? hoverPalette.Background : Background;
            var border = pointerOver ? hoverPalette.BorderBrush : BorderBrush;
            var foreground = pointerOver ? hoverPalette.Foreground : Foreground;
            var spaces = BleeckerPaginationAnimation.ColorSpaces(recipe == "Selected", pointerOver, Direction != 0);
            static BleeckerCssColor Color(Brush brush, bool legacy)
            {
                if (brush is not SolidColorBrush solid) throw new InvalidOperationException("Pagination source transitions require solid palette brushes.");
                return BleeckerCssColor.FromRgba((solid.Color.R, solid.Color.G, solid.Color.B, solid.Color.A / 255d * solid.Opacity), legacy);
            }
            var now = owner.paintClock.Elapsed.TotalMilliseconds;
            paintMotion.Retarget(new(Color(foreground, true), Color(background, spaces.BackgroundLegacy), Color(border, spaces.BorderLegacy), IsEnabled ? 1 : .4), now, animate && IsLoaded && owner.motionSettings.AnimationsEnabled);
            ClearBrushObservers();
            if (IsLoaded) foreach (var brush in new[] { Background, BorderBrush, Foreground, hoverPalette.Background, hoverPalette.BorderBrush, hoverPalette.Foreground }.OfType<SolidColorBrush>().Distinct())
                brushTokens.Add((brush, brush.RegisterPropertyChangedCallback(SolidColorBrush.ColorProperty, (_, _) => RequestPaint(true)), brush.RegisterPropertyChangedCallback(Brush.OpacityProperty, (_, _) => RequestPaint(true))));
            PaintFrame(now);
            if (paintMotion.IsRunning(now)) { if (!rendering) { rendering = true; CompositionTarget.Rendering += OnPaintFrame; } }
            else StopRendering();
        }

        private void PaintFrame(double now)
        {
            var frame = paintMotion.At(now);
            static void Paint(SolidColorBrush brush, BleeckerCssColor value) { var color = value.Encode(); brush.Color = Windows.UI.Color.FromArgb(255, color.R, color.G, color.B); brush.Opacity = color.Alpha; }
            Paint(paintedBackground, frame.Background); Paint(paintedBorder, frame.Border); Paint(paintedForeground, frame.Foreground); Opacity = frame.Opacity;
        }
        private void OnPaintFrame(object? sender, object args)
        {
            var now = owner.paintClock.Elapsed.TotalMilliseconds; PaintFrame(now); if (!paintMotion.IsRunning(now)) StopRendering();
        }
        private void StopRendering() { if (rendering) CompositionTarget.Rendering -= OnPaintFrame; rendering = false; }
        private void ClearBrushObservers()
        {
            foreach (var (brush, color, opacity) in brushTokens) { brush.UnregisterPropertyChangedCallback(SolidColorBrush.ColorProperty, color); brush.UnregisterPropertyChangedCallback(Brush.OpacityProperty, opacity); }
            brushTokens.Clear();
        }
        private void ObserveHoverPalette()
        {
            if (hoverPalette is null || hoverTokens.Count != 0) return;
            foreach (var property in new[] { ContentPresenter.BackgroundProperty, ContentPresenter.BorderBrushProperty, ContentPresenter.ForegroundProperty })
                hoverTokens.Add((property, hoverPalette.RegisterPropertyChangedCallback(property, (_, _) => RequestPaint(true))));
        }
        internal void StopPaint()
        {
            paintVersion++; StopRendering(); ClearBrushObservers();
            if (hoverPalette is not null) foreach (var (property, token) in hoverTokens) hoverPalette.UnregisterPropertyChangedCallback(property, token);
            hoverTokens.Clear();
        }
    }
}
