using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard;

public sealed partial class BleeckerInput
{
    private readonly UISettings motionSettings = new();
    private Border? palette;
    private Border? ringPalette;
    private Border? chrome;
    private Border? focusRing;
    private Grid? editor;
    private Storyboard? motion;
    private int motionRequestVersion;
    private readonly List<(SolidColorBrush Brush, long Color, long Opacity)> observedBrushes = [];
    private readonly List<(DependencyObject Owner, DependencyProperty Property, long Token)> paletteSubscriptions = [];

    private void InitializeMotion()
    {
        ActualThemeChanged += (_, _) => DispatcherQueue.TryEnqueue(() => RequestMotion(false));
        RegisterPropertyChangedCallback(BackgroundProperty, (_, _) => DispatcherQueue.TryEnqueue(() => RequestMotion(false)));
        RegisterPropertyChangedCallback(BorderBrushProperty, (_, _) => DispatcherQueue.TryEnqueue(() => RequestMotion(false)));
        Loaded += (_, _) =>
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) motionSettings.AnimationsEnabledChanged += OnInputMotionPreferenceChanged;
        };
        Unloaded += (_, _) =>
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) motionSettings.AnimationsEnabledChanged -= OnInputMotionPreferenceChanged;
            RequestMotion(false);
            ClearObservedBrushes();
        };
    }

    private void OnInputMotionPreferenceChanged(UISettings sender, UISettingsAnimationsEnabledChangedEventArgs args) =>
        DispatcherQueue.TryEnqueue(() => RequestMotion(false));

    private void RequestMotion(bool animate)
    {
        var version = ++motionRequestVersion;
        if (!animate || !IsLoaded || !motionSettings.AnimationsEnabled)
        {
            UpdateMotion(false);
            return;
        }
        // Coalesce changes from one input event before starting a storyboard.
        // Otherwise a second same-frame change can sample its predecessor's
        // destination before the compositor has presented the first keyframe.
        DispatcherQueue.TryEnqueue(() => { if (version == motionRequestVersion) UpdateMotion(true); });
    }

    private void ApplyMotionTemplate()
    {
        motion?.Stop();
        motion = null;
        ClearObservedBrushes();
        foreach (var (owner, property, token) in paletteSubscriptions) owner.UnregisterPropertyChangedCallback(property, token);
        paletteSubscriptions.Clear();
        palette = GetTemplateChild("InputPalette") as Border;
        ringPalette = GetTemplateChild("InputRingPalette") as Border;
        chrome = GetTemplateChild("BorderElement") as Border;
        focusRing = GetTemplateChild("InputRing") as Border;
        editor = GetTemplateChild("Editor") as Grid;
        if (chrome is not null) { chrome.Background = new SolidColorBrush(); chrome.BorderBrush = new SolidColorBrush(); }
        if (focusRing is not null) focusRing.BorderBrush = new SolidColorBrush();
        if (palette is not null)
        {
            ObservePalette(palette, Border.BackgroundProperty);
            ObservePalette(palette, Border.BorderBrushProperty);
        }
        if (ringPalette is not null) ObservePalette(ringPalette, Border.BorderBrushProperty);
    }

    private void ObservePalette(DependencyObject owner, DependencyProperty property)
    {
        var token = owner.RegisterPropertyChangedCallback(property, (_, _) =>
        {
            if (!applyingAppearance) DispatcherQueue.TryEnqueue(() => RequestMotion(false));
        });
        paletteSubscriptions.Add((owner, property, token));
    }

    private void ClearObservedBrushes()
    {
        foreach (var (brush, color, opacity) in observedBrushes)
        {
            brush.UnregisterPropertyChangedCallback(SolidColorBrush.ColorProperty, color);
            brush.UnregisterPropertyChangedCallback(Brush.OpacityProperty, opacity);
        }
        observedBrushes.Clear();
    }

    private void UpdateMotion(bool animate)
    {
        if (palette is null || ringPalette is null || chrome is null || focusRing is null || editor is null) return;
        var fromBackground = Snapshot(chrome.Background);
        var fromBorder = Snapshot(chrome.BorderBrush);
        var fromRing = Snapshot(focusRing.BorderBrush);
        var fromOpacity = editor.Opacity;
        var fromRingOpacity = focusRing.Opacity;
        motion?.Stop();
        motion = null;
        var duration = TimeSpan.FromMilliseconds(BleeckerTokens.Durations["control"]);
        var storyboard = new Storyboard { Duration = duration };
        var enabled = animate && IsLoaded && motionSettings.AnimationsEnabled && !shadowAccessibility.HighContrast;
        Paint(chrome, Border.BackgroundProperty, palette.Background, fromBackground);
        Paint(chrome, Border.BorderBrushProperty, palette.BorderBrush, fromBorder);
        Paint(focusRing, Border.BorderBrushProperty, ringPalette.BorderBrush, fromRing);
        Fade(editor, fromOpacity, palette.Opacity);
        Fade(focusRing, fromRingOpacity, ringPalette.Opacity);
        ClearObservedBrushes();
        if (IsLoaded)
            foreach (var brush in new[] { palette.Background, palette.BorderBrush, ringPalette.BorderBrush }.OfType<SolidColorBrush>().Distinct())
                observedBrushes.Add((brush,
                    brush.RegisterPropertyChangedCallback(SolidColorBrush.ColorProperty, (_, _) => RequestMotion(false)),
                    brush.RegisterPropertyChangedCallback(Brush.OpacityProperty, (_, _) => RequestMotion(false))));
        if (storyboard.Children.Count == 0) return;
        storyboard.Completed += (_, _) => { if (ReferenceEquals(motion, storyboard)) { storyboard.Stop(); motion = null; } };
        motion = storyboard;
        storyboard.Begin();

        void Paint(Border target, DependencyProperty property, Brush source, Windows.UI.Color? from)
        {
            if (source is not SolidColorBrush solid) { target.SetValue(property, source); return; }
            var color = Snapshot(solid)!.Value;
            var brush = new SolidColorBrush(color);
            target.SetValue(property, brush);
            if (!enabled || from is null || from == color) return;
            var animation = new ColorAnimationUsingKeyFrames { Duration = duration, EnableDependentAnimation = true };
            animation.KeyFrames.Add(new DiscreteColorKeyFrame { KeyTime = TimeSpan.Zero, Value = from.Value });
            animation.KeyFrames.Add(new SplineColorKeyFrame { KeyTime = duration, Value = color, KeySpline = InputSpline() });
            Storyboard.SetTarget(animation, brush);
            Storyboard.SetTargetProperty(animation, "Color");
            storyboard.Children.Add(animation);
        }
        void Fade(UIElement target, double from, double to)
        {
            target.Opacity = to;
            if (!enabled || Math.Abs(from - to) < .000001) return;
            var animation = new DoubleAnimationUsingKeyFrames { Duration = duration };
            animation.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = TimeSpan.Zero, Value = from });
            animation.KeyFrames.Add(new SplineDoubleKeyFrame { KeyTime = duration, Value = to, KeySpline = InputSpline() });
            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation, "Opacity");
            storyboard.Children.Add(animation);
        }
    }

    private static Windows.UI.Color? Snapshot(Brush brush) => brush is SolidColorBrush solid
        ? Windows.UI.Color.FromArgb((byte)Math.Round(solid.Color.A * solid.Opacity), solid.Color.R, solid.Color.G, solid.Color.B) : null;
    private static KeySpline InputSpline() => new() { ControlPoint1 = new Point(.22, 1), ControlPoint2 = new Point(.36, 1) };
}
