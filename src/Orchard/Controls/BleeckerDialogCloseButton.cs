using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI.ViewManagement;

namespace Orchard;

/// <summary>A native close action. The consumer supplies its accessible name and owns dismissal.</summary>
public sealed class BleeckerDialogCloseButton : Button
{
    private readonly UISettings settings = new();
    private readonly AccessibilitySettings accessibility = new();
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private readonly BleeckerDialogCloseAnimation animation = new();
    private readonly SolidColorBrush foreground = new(), background = new(), outline = new();
    private readonly Viewbox icon = new() { Name = "CloseIcon", Width = 18, Height = 18, IsHitTestVisible = false, UseLayoutRounding = false };
    private readonly List<(DependencyObject Owner, DependencyProperty Property, long Token)> observers = [];
    private Grid? root;
    private Border? border, palette;
    private TextBlock? foregroundPalette;
    private ContentPresenter? presenter;
    private CompositeTransform? transform;
    private VisualStateGroup? commonStates;
    private BleeckerCssShadow? shadow;
    private bool updating, rendering;

    public BleeckerDialogCloseButton()
    {
        DefaultStyleKey = typeof(BleeckerDialogCloseButton);
        var canvas = new Canvas { Width = 24, Height = 24 };
        foreach (var (x1, y1, x2, y2) in new[] { (18d, 6d, 6d, 18d), (6d, 6d, 18d, 18d) })
            canvas.Children.Add(new Microsoft.UI.Xaml.Shapes.Line { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = foreground,
                StrokeThickness = 2, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round });
        icon.Child = canvas; Content = icon;
        Loaded += (_, _) => { Subscribe(); Paint(false); };
        Unloaded += (_, _) => { if (!IsLoaded) { Unsubscribe(); Stop(); } };
        ActualThemeChanged += (_, _) => Paint(true);
        RegisterPropertyChangedCallback(IsPointerOverProperty, (_, _) => Paint(true));
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) Detach(); });
    }

    public static readonly DependencyProperty IconSizeProperty = DependencyProperty.Register(nameof(IconSize), typeof(double), typeof(BleeckerDialogCloseButton),
        new PropertyMetadata(18d, (owner, args) =>
        {
            var size = (double)args.NewValue; BleeckerDialogCloseAnimation.ValidateIconSize(size);
            var button = (BleeckerDialogCloseButton)owner; button.icon.Width = button.icon.Height = size;
        }));
    public double IconSize
    {
        get => (double)GetValue(IconSizeProperty);
        set { BleeckerDialogCloseAnimation.ValidateIconSize(value); SetValue(IconSizeProperty, value); }
    }

    protected override void OnApplyTemplate()
    {
        Detach(); base.OnApplyTemplate();
        root = GetTemplateChild("CloseRoot") as Grid;
        if (root is null) return; // A consumer template owns its presentation.
        border = (Border)GetTemplateChild("CloseBorder"); palette = (Border)GetTemplateChild("ClosePalette");
        foregroundPalette = (TextBlock)GetTemplateChild("CloseForegroundPalette"); presenter = (ContentPresenter)GetTemplateChild("CloseContent");
        transform = (CompositeTransform)root.RenderTransform;
        border.Background = background; border.BorderBrush = outline; presenter.Foreground = foreground;
        shadow = new(this, border, (Grid)GetTemplateChild("CloseShadowHost"), (Microsoft.UI.Xaml.Shapes.Path)GetTemplateChild("CloseShadowMask"), (Microsoft.UI.Xaml.Shapes.Path)GetTemplateChild("CloseShadowExterior"));
        commonStates = VisualStateManager.GetVisualStateGroups(root).Single(group => group.Name == "CommonStates");
        commonStates.CurrentStateChanged += StateChanged;
        if (IsLoaded) Subscribe();
        Paint(false);
    }

    private void StateChanged(object sender, VisualStateChangedEventArgs args) => Paint(true);
    private void MotionChanged(UISettings sender, UISettingsAnimationsEnabledChangedEventArgs args) => DispatcherQueue.TryEnqueue(() => Paint(false));
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
        if (palette is null || foregroundPalette is null) return;
        foreach (var (source, property) in new (DependencyObject, DependencyProperty)[]
            { (palette, Border.BackgroundProperty), (palette, Border.BorderBrushProperty), (foregroundPalette, TextBlock.ForegroundProperty) })
            observers.Add((source, property, source.RegisterPropertyChangedCallback(property, (_, _) => { if (!updating) { ObservePalette(); Paint(true); } })));
        foreach (var brush in new[] { palette.Background, palette.BorderBrush, foregroundPalette.Foreground }.OfType<SolidColorBrush>().Distinct())
            foreach (var property in new[] { SolidColorBrush.ColorProperty, Brush.OpacityProperty })
                observers.Add((brush, property, brush.RegisterPropertyChangedCallback(property, (_, _) => { if (!updating) Paint(true); })));
    }
    private static BleeckerCssColor Color(Brush? brush, bool legacy)
    {
        if (brush is null) return BleeckerCssColor.FromRgba((0, 0, 0, 0), legacy);
        if (brush is not SolidColorBrush solid) throw new InvalidOperationException("Dialog Close Button paint requires solid brushes.");
        return BleeckerCssColor.FromRgba((solid.Color.R, solid.Color.G, solid.Color.B, solid.Color.A / 255d * solid.Opacity), legacy);
    }
    private void Paint(bool animate)
    {
        if (root is null || palette is null || foregroundPalette is null) return;
        var state = commonStates?.CurrentState?.Name;
        var pressed = state == "Pressed";
        var hovered = IsPointerOver;
        updating = true;
        try { VisualStateManager.GoToState(this, hovered ? "CloseHover" : "CloseRest", false); }
        finally { updating = false; }
        // CSS translate/scale are individual properties; the source transitions
        // transform, so these two pressed changes apply immediately.
        transform!.TranslateY = pressed ? 1 : 0;
        transform.ScaleX = transform.ScaleY = pressed ? .97 : 1;
        var target = new BleeckerDialogCloseAnimation.Frame(Color(foregroundPalette.Foreground, true), Color(palette.Background, !hovered),
            Color(palette.BorderBrush, false), hovered && !pressed ? 1 : 0);
        var time = clock.Elapsed.TotalMilliseconds;
        animation.Retarget(target, time, animate && IsLoaded && settings.AnimationsEnabled && !accessibility.HighContrast);
        Draw(time);
        if (animation.IsRunning(time)) { if (!rendering) { CompositionTarget.Rendering += Frame; rendering = true; } }
        else Stop();
    }
    private static void Set(SolidColorBrush brush, BleeckerCssColor color)
    { var encoded = color.Encode(); brush.Color = Windows.UI.Color.FromArgb(255, encoded.R, encoded.G, encoded.B); brush.Opacity = encoded.Alpha; }
    private void Draw(double milliseconds)
    {
        var value = animation.At(milliseconds);
        Set(foreground, value.Foreground); Set(background, value.Background); Set(outline, value.Border);
        shadow?.Set(value.Shadow <= 0 ? null : new((float)(9 * value.Shadow), (float)(3 * value.Shadow), (float)(-6 * value.Shadow), (float)(.28 * value.Shadow), 26, 55, 77));
    }
    private void Frame(object? sender, object args) { var time = clock.Elapsed.TotalMilliseconds; Draw(time); if (!animation.IsRunning(time)) Stop(); }
    private void Stop() { CompositionTarget.Rendering -= Frame; rendering = false; }
    private void Detach()
    {
        if (commonStates is not null) commonStates.CurrentStateChanged -= StateChanged;
        Unsubscribe(); Stop(); shadow?.Dispose(); shadow = null;
        root = null; border = palette = null; foregroundPalette = null; presenter = null; transform = null; commonStates = null;
    }
}
