using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard;

public sealed partial class BleeckerFieldHelpTooltip
{
    private readonly UISettings motionSettings = new();
    private VisualStateGroup? commonStates;
    private ScaleTransform? scale;
    private SolidColorBrush? ink;
    private Storyboard? motion;

    public static readonly DependencyProperty PointerOverForegroundProperty = DependencyProperty.Register(nameof(PointerOverForeground), typeof(SolidColorBrush), typeof(BleeckerFieldHelpTooltip),
        new PropertyMetadata(null, (owner, _) => ((BleeckerFieldHelpTooltip)owner).UpdateMotion(false)));
    public SolidColorBrush? PointerOverForeground { get => (SolidColorBrush?)GetValue(PointerOverForegroundProperty); set => SetValue(PointerOverForegroundProperty, value); }

    private void InitializeMotion()
    {
        RegisterPropertyChangedCallback(ForegroundProperty, (_, _) => UpdateMotion(false));
        RegisterPropertyChangedCallback(IsPointerOverProperty, (_, _) => UpdateMotion(true));
        ActualThemeChanged += (_, _) => DispatcherQueue.TryEnqueue(() => UpdateMotion(false));
        Loaded += (_, _) =>
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) motionSettings.AnimationsEnabledChanged += OnMotionPreferenceChanged;
            UpdateMotion(false);
        };
        Unloaded += (_, _) =>
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) motionSettings.AnimationsEnabledChanged -= OnMotionPreferenceChanged;
            UpdateMotion(false);
        };
    }

    protected override void OnApplyTemplate()
    {
        motion?.Stop();
        motion = null;
        if (commonStates is not null) commonStates.CurrentStateChanged -= OnCommonStateChanged;
        base.OnApplyTemplate();
        var root = GetTemplateChild("HelpRoot") as FrameworkElement;
        scale = root?.RenderTransform as ScaleTransform;
        ink = new SolidColorBrush();
        if (GetTemplateChild("InfoCircle") is Ellipse circle) circle.Stroke = ink;
        if (GetTemplateChild("InfoMark") is Microsoft.UI.Xaml.Shapes.Path mark) mark.Stroke = ink;
        commonStates = root is null ? null : VisualStateManager.GetVisualStateGroups(root).Single(group => group.Name == "CommonStates");
        if (commonStates is not null) commonStates.CurrentStateChanged += OnCommonStateChanged;
        UpdateMotion(false);
    }

    private void OnCommonStateChanged(object sender, VisualStateChangedEventArgs args) => UpdateMotion(true);
    private void OnMotionPreferenceChanged(UISettings sender, UISettingsAnimationsEnabledChangedEventArgs args) => DispatcherQueue.TryEnqueue(() => UpdateMotion(false));

    private void UpdateMotion(bool animate)
    {
        if (ink is null || scale is null) return;
        var state = commonStates?.CurrentState?.Name ?? "Normal";
        var hover = state == "PointerOver" || (state == "Pressed" && IsPointerOver);
        var destination = (hover ? PointerOverForeground : Foreground) as SolidColorBrush
            ?? throw new InvalidOperationException("Field help requires solid foreground and pointer-over foreground brushes.");
        var color = ink.Color;
        var size = scale.ScaleX;
        motion?.Stop();
        motion = null;
        var targetSize = state == "Pressed" ? .9 : 1;
        ink.Color = destination.Color;
        scale.ScaleX = scale.ScaleY = targetSize;
        if (!animate || !IsLoaded || !motionSettings.AnimationsEnabled) return;
        var duration = TimeSpan.FromMilliseconds(BleeckerTokens.Durations["control"]);
        var storyboard = new Storyboard { Duration = duration };
        if (color != destination.Color)
        {
            var colorAnimation = new ColorAnimationUsingKeyFrames { Duration = duration, EnableDependentAnimation = true };
            colorAnimation.KeyFrames.Add(new DiscreteColorKeyFrame { KeyTime = TimeSpan.Zero, Value = color });
            colorAnimation.KeyFrames.Add(new SplineColorKeyFrame { KeyTime = duration, Value = destination.Color, KeySpline = PremiumSpline() });
            Storyboard.SetTarget(colorAnimation, ink);
            Storyboard.SetTargetProperty(colorAnimation, "Color");
            storyboard.Children.Add(colorAnimation);
        }
        foreach (var property in new[] { "ScaleX", "ScaleY" })
        {
            var animation = new DoubleAnimationUsingKeyFrames { Duration = duration };
            animation.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = TimeSpan.Zero, Value = size });
            animation.KeyFrames.Add(new SplineDoubleKeyFrame { KeyTime = duration, Value = targetSize, KeySpline = PremiumSpline() });
            Storyboard.SetTarget(animation, scale);
            Storyboard.SetTargetProperty(animation, property);
            storyboard.Children.Add(animation);
        }
        storyboard.Completed += (_, _) => { if (ReferenceEquals(motion, storyboard)) { storyboard.Stop(); motion = null; } };
        motion = storyboard;
        storyboard.Begin();
    }

    private static KeySpline PremiumSpline() => new() { ControlPoint1 = new Point(.22, 1), ControlPoint2 = new Point(.36, 1) };
}
