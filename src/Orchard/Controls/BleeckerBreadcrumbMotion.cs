using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.ViewManagement;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace Orchard;

/// <summary>Animates the reference palette without changing native activation.</summary>
internal sealed class BleeckerBreadcrumbMotion
{
    private readonly ButtonBase action;
    private readonly UISettings settings = new();
    private readonly AccessibilitySettings accessibility = new();
    private Border? palette, chrome;
    private TextBlock? text;
    private Path? icon;
    private ScaleTransform? scale;
    private readonly List<(DependencyObject Owner, DependencyProperty Property, long Token)> subscriptions = [];
    private Storyboard? motion;
    private int version;

    internal static void Attach(ButtonBase action) => _ = new BleeckerBreadcrumbMotion(action);
    private BleeckerBreadcrumbMotion(ButtonBase action)
    {
        this.action = action;
        action.Loaded += (_, _) =>
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) settings.AnimationsEnabledChanged += OnPreferenceChanged;
            settings.ColorValuesChanged += OnColorsChanged;
            ApplyTemplate();
        };
        action.Unloaded += (_, _) =>
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) settings.AnimationsEnabledChanged -= OnPreferenceChanged;
            settings.ColorValuesChanged -= OnColorsChanged;
            ClearTemplate();
        };
        action.RegisterPropertyChangedCallback(Control.TemplateProperty, (_, _) =>
        {
            if (action.IsLoaded) action.DispatcherQueue.TryEnqueue(ApplyTemplate);
        });
        action.RegisterPropertyChangedCallback(ButtonBase.IsPressedProperty, (_, _) => Request(true));
        action.ActualThemeChanged += (_, _) => Request(false);
    }
    private void OnPreferenceChanged(UISettings sender, UISettingsAnimationsEnabledChangedEventArgs args) => action.DispatcherQueue.TryEnqueue(() => Request(false));
    private void OnColorsChanged(UISettings sender, object args) => action.DispatcherQueue.TryEnqueue(() => Request(false));
    private void ClearTemplate()
    {
        version++; motion?.Stop(); motion = null;
        foreach (var (owner, property, token) in subscriptions) owner.UnregisterPropertyChangedCallback(property, token);
        subscriptions.Clear(); palette = null; chrome = null; text = null; icon = null; scale = null;
    }
    private void ApplyTemplate()
    {
        ClearTemplate(); action.ApplyTemplate();
        if (!action.IsLoaded || VisualTreeHelper.GetChildrenCount(action) == 0) return;
        var root = (FrameworkElement)VisualTreeHelper.GetChild(action, 0);
        palette = root.FindName("PART_Palette") as Border;
        chrome = root.FindName("PART_Chrome") as Border;
        text = root.FindName("PART_Text") as TextBlock;
        icon = root.FindName("PART_Icon") as Path;
        scale = root.FindName("PART_Scale") as ScaleTransform;
        if (palette is null) throw new InvalidOperationException("Breadcrumb action template requires PART_Palette.");
        Observe(palette, Border.BackgroundProperty); Observe(palette, Border.BorderBrushProperty);
        Paint(false);
    }
    private void Observe(DependencyObject owner, DependencyProperty property)
    {
        var token = owner.RegisterPropertyChangedCallback(property, (_, _) => Request(true));
        subscriptions.Add((owner, property, token));
    }
    private void Request(bool animate)
    {
        var current = ++version;
        action.DispatcherQueue.TryEnqueue(() => { if (current == version) Paint(animate); });
    }
    private void Paint(bool animate)
    {
        if (palette is null) return;
        var foreground = ColorOf(palette.BorderBrush);
        var background = ColorOf(palette.Background);
        var fromForeground = ColorOf(text?.Foreground ?? icon?.Stroke);
        var fromBackground = ColorOf(chrome?.Background);
        var fromScale = scale?.ScaleX ?? 1;
        var toScale = action.IsPressed ? .9 : 1;
        motion?.Stop(); motion = null;
        var duration = TimeSpan.FromMilliseconds(BleeckerTokens.Durations["control"]);
        var storyboard = new Storyboard { Duration = duration };
        var enabled = animate && action.IsLoaded && settings.AnimationsEnabled && !accessibility.HighContrast;
        if (text is not null) PaintBrush(text, TextBlock.ForegroundProperty, fromForeground, foreground);
        if (icon is not null) PaintBrush(icon, Shape.StrokeProperty, fromForeground, foreground);
        if (chrome is not null) PaintBrush(chrome, Border.BackgroundProperty, fromBackground, background);
        if (scale is not null)
        {
            scale.ScaleX = toScale; scale.ScaleY = toScale;
            if (enabled && Math.Abs(fromScale - toScale) > .000001)
                foreach (var property in new[] { "ScaleX", "ScaleY" })
                {
                    var animation = new DoubleAnimationUsingKeyFrames { Duration = duration, EnableDependentAnimation = true };
                    animation.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = TimeSpan.Zero, Value = fromScale });
                    animation.KeyFrames.Add(new SplineDoubleKeyFrame { KeyTime = duration, Value = toScale, KeySpline = Spline() });
                    Storyboard.SetTarget(animation, scale); Storyboard.SetTargetProperty(animation, property); storyboard.Children.Add(animation);
                }
        }
        if (storyboard.Children.Count == 0) return;
        motion = storyboard;
        storyboard.Completed += (_, _) => { if (ReferenceEquals(motion, storyboard)) { storyboard.Stop(); motion = null; } };
        storyboard.Begin();

        void PaintBrush(DependencyObject target, DependencyProperty property, Color from, Color to)
        {
            var brush = new SolidColorBrush(to); target.SetValue(property, brush);
            if (!enabled || from == to) return;
            var animation = new ColorAnimationUsingKeyFrames { Duration = duration, EnableDependentAnimation = true };
            animation.KeyFrames.Add(new DiscreteColorKeyFrame { KeyTime = TimeSpan.Zero, Value = from });
            animation.KeyFrames.Add(new SplineColorKeyFrame { KeyTime = duration, Value = to, KeySpline = Spline() });
            Storyboard.SetTarget(animation, brush); Storyboard.SetTargetProperty(animation, "Color"); storyboard.Children.Add(animation);
        }
    }
    private static Color ColorOf(Brush? brush) => brush is SolidColorBrush solid
        ? Color.FromArgb((byte)Math.Round(solid.Color.A * solid.Opacity), solid.Color.R, solid.Color.G, solid.Color.B) : Microsoft.UI.Colors.Transparent;
    private static KeySpline Spline() => new() { ControlPoint1 = new Point(.22, 1), ControlPoint2 = new Point(.36, 1) };
}
