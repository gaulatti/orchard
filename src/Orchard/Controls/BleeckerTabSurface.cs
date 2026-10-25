using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI.ViewManagement;
using ShapePath = Microsoft.UI.Xaml.Shapes.Path;

namespace Orchard;

// A consumer renderer may reuse the source style. Its original Control values
// remain the palette inputs; only the template's painted brushes are animated.
internal sealed class BleeckerTabSurface : IDisposable
{
    private readonly Control owner;
    private readonly Border border, palette;
    private readonly ContentPresenter content;
    private readonly TextBlock foregroundPalette;
    private readonly SolidColorBrush foreground = new(), background = new(), outline = new();
    private readonly UISettings settings = new();
    private readonly AccessibilitySettings accessibility = new();
    private readonly BleeckerTabAnimation motion = new();
    private readonly BleeckerCssShadow shadow;
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private readonly List<(DependencyObject Owner, DependencyProperty Property, long Token)> observers = [];
    private BleeckerTabsVariant variant;
    private bool selected, hovered, updating, rendering, disposed;

    internal BleeckerTabSurface(Control owner, Border border, Border palette, TextBlock foregroundPalette,
        ContentPresenter content, Grid shadowHost, ShapePath mask, ShapePath exterior)
    {
        this.owner = owner; this.border = border; this.palette = palette; this.foregroundPalette = foregroundPalette; this.content = content;
        shadow = new(owner, border, shadowHost, mask, exterior);
        border.Background = background; border.BorderBrush = outline; content.Foreground = foreground;
        owner.Loaded += Loaded; owner.Unloaded += Unloaded; owner.ActualThemeChanged += ThemeChanged;
        if (owner.IsLoaded) Subscribe();
    }

    internal void Update(BleeckerTabsVariant newVariant, bool newSelected, bool newHovered, Action applyState)
    {
        if (disposed) return;
        variant = newVariant; selected = newSelected; hovered = newHovered;
        updating = true;
        try { applyState(); }
        finally { updating = false; }
        if (owner.IsLoaded) ObservePalette();
        Paint(true);
    }

    private void Loaded(object sender, RoutedEventArgs args) { Subscribe(); Paint(false); }
    private void Unloaded(object sender, RoutedEventArgs args) { if (!owner.IsLoaded) { Unsubscribe(); Stop(); } }
    private void ThemeChanged(FrameworkElement sender, object args) => Paint(true);
    private void MotionChanged(UISettings sender, UISettingsAnimationsEnabledChangedEventArgs args) => owner.DispatcherQueue.TryEnqueue(() => Paint(false));
    private void Subscribe()
    {
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
        { settings.AnimationsEnabledChanged -= MotionChanged; settings.AnimationsEnabledChanged += MotionChanged; }
        ObservePalette();
    }
    private void Unsubscribe()
    {
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) settings.AnimationsEnabledChanged -= MotionChanged;
        foreach (var observer in observers) observer.Owner.UnregisterPropertyChangedCallback(observer.Property, observer.Token);
        observers.Clear();
    }
    private void ObservePalette()
    {
        foreach (var observer in observers) observer.Owner.UnregisterPropertyChangedCallback(observer.Property, observer.Token);
        observers.Clear();
        foreach (var (source, property) in new (DependencyObject, DependencyProperty)[]
            { (palette, Border.BackgroundProperty), (palette, Border.BorderBrushProperty), (foregroundPalette, TextBlock.ForegroundProperty) })
            observers.Add((source, property, source.RegisterPropertyChangedCallback(property, (_, _) => { if (updating) return; ObservePalette(); Paint(true); })));
        foreach (var brush in new[] { palette.Background, palette.BorderBrush, foregroundPalette.Foreground }.OfType<SolidColorBrush>().Distinct())
            foreach (var property in new[] { SolidColorBrush.ColorProperty, Brush.OpacityProperty })
                observers.Add((brush, property, brush.RegisterPropertyChangedCallback(property, (_, _) => { if (!updating) Paint(true); })));
    }
    private static BleeckerCssColor Color(Brush? brush, bool legacy)
    {
        if (brush is null) return BleeckerCssColor.FromRgba((0, 0, 0, 0), legacy);
        if (brush is not SolidColorBrush solid) throw new InvalidOperationException("Tab source paint requires solid brushes.");
        return BleeckerCssColor.FromRgba((solid.Color.R, solid.Color.G, solid.Color.B, solid.Color.A / 255d * solid.Opacity), legacy);
    }
    private void Paint(bool animate)
    {
        if (disposed) return;
        var spaces = BleeckerTabAnimation.ColorSpaces(variant, selected, hovered);
        var target = new BleeckerTabAnimation.Frame(Color(foregroundPalette.Foreground, true), Color(palette.Background, spaces.BackgroundLegacy),
            Color(palette.BorderBrush, spaces.BorderLegacy), variant == BleeckerTabsVariant.Segmented && selected ? 1 : 0);
        var time = clock.Elapsed.TotalMilliseconds;
        motion.Retarget(target, time, animate && owner.IsLoaded && settings.AnimationsEnabled && !accessibility.HighContrast);
        Draw(time);
        if (motion.IsRunning(time)) { if (!rendering) { CompositionTarget.Rendering += Frame; rendering = true; } }
        else Stop();
    }
    private static void Set(SolidColorBrush brush, BleeckerCssColor color)
    {
        var encoded = color.Encode(); brush.Color = Windows.UI.Color.FromArgb(255, encoded.R, encoded.G, encoded.B); brush.Opacity = encoded.Alpha;
    }
    private void Draw(double milliseconds)
    {
        var frame = motion.At(milliseconds);
        Set(foreground, frame.Foreground); Set(background, frame.Background); Set(outline, frame.Border);
        shadow.Set(frame.Shadow <= 0 ? null : new((float)(3 * frame.Shadow), (float)frame.Shadow, 0, (float)(.1 * frame.Shadow), 21, 48, 66));
    }
    private void Frame(object? sender, object args) { var time = clock.Elapsed.TotalMilliseconds; Draw(time); if (!motion.IsRunning(time)) Stop(); }
    private void Stop() { CompositionTarget.Rendering -= Frame; rendering = false; }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true; owner.Loaded -= Loaded; owner.Unloaded -= Unloaded; owner.ActualThemeChanged -= ThemeChanged;
        Unsubscribe(); Stop(); shadow.Dispose();
    }
}
