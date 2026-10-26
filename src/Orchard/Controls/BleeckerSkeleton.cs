using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard;

/// <summary>A decorative Bleecker loading shape with consumer-owned content, width and height.</summary>
public sealed class BleeckerSkeleton : ContentControl
{
    private readonly UISettings settings = new();
    private Border? surface;
    private Storyboard? pulse;
    private double pulseBaseOpacity = 1;
    private readonly List<(FrameworkElement Owner, long Token)> ancestors = [];
    private bool observesSettings;

    public BleeckerSkeleton()
    {
        DefaultStyleKey = typeof(BleeckerSkeleton);
        SizeChanged += (_, _) => UpdateCorners();
        RegisterPropertyChangedCallback(CornerRadiusProperty, (_, _) => UpdateCorners());
        RegisterPropertyChangedCallback(VisibilityProperty, (_, _) => UpdateMotion());
        RegisterPropertyChangedCallback(OpacityProperty, (_, _) =>
        {
            var baseOpacity = Convert.ToDouble(GetAnimationBaseValue(OpacityProperty));
            if (pulseBaseOpacity == baseOpacity) return;
            var time = pulse?.GetCurrentTime();
            pulseBaseOpacity = baseOpacity;
            StopMotion(); UpdateMotion();
            if (time is TimeSpan current) pulse?.Seek(current);
        });
        Loaded += (_, _) =>
        {
            ObserveAncestors();
            if (!observesSettings && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
            {
                settings.AnimationsEnabledChanged += OnAnimationsChanged;
                observesSettings = true;
            }
            UpdateMotion();
        };
        Unloaded += (_, _) =>
        {
            ClearAncestors();
            if (observesSettings && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) settings.AnimationsEnabledChanged -= OnAnimationsChanged;
            observesSettings = false;
            StopMotion();
        };
    }

    public static readonly DependencyProperty CircleProperty = DependencyProperty.Register(nameof(Circle), typeof(bool), typeof(BleeckerSkeleton),
        new PropertyMetadata(false, (owner, _) => ((BleeckerSkeleton)owner).UpdateCorners()));
    public bool Circle { get => (bool)GetValue(CircleProperty); set => SetValue(CircleProperty, value); }

    protected override void OnApplyTemplate()
    {
        StopMotion();
        surface = null;
        base.OnApplyTemplate();
        if (Template is null) return;
        surface = GetTemplateChild("PART_Surface") as Border ?? throw new InvalidOperationException("Skeleton template requires PART_Surface.");
        UpdateCorners();
        UpdateMotion();
    }

    private void UpdateCorners()
    {
        if (surface is null) return;
        var corners = Circle ? new CornerRadius(Math.Min(ActualWidth, ActualHeight) / 2) : CornerRadius;
        // CSS scales every radius together when adjacent corners overlap.
        // A 12px-high default shape therefore has circular 6px ends.
        var scale = Math.Min(1, Math.Min(Math.Min(Ratio(ActualWidth, corners.TopLeft + corners.TopRight), Ratio(ActualWidth, corners.BottomLeft + corners.BottomRight)),
            Math.Min(Ratio(ActualHeight, corners.TopLeft + corners.BottomLeft), Ratio(ActualHeight, corners.TopRight + corners.BottomRight))));
        surface.CornerRadius = new CornerRadius(corners.TopLeft * scale, corners.TopRight * scale, corners.BottomRight * scale, corners.BottomLeft * scale);
        static double Ratio(double size, double radii) => radii == 0 ? 1 : size / radii;
    }
    private void OnAnimationsChanged(UISettings sender, UISettingsAnimationsEnabledChangedEventArgs args) => DispatcherQueue.TryEnqueue(UpdateMotion);
    private void ObserveAncestors()
    {
        ClearAncestors();
        for (var parent = VisualTreeHelper.GetParent(this); parent is not null; parent = VisualTreeHelper.GetParent(parent))
            if (parent is FrameworkElement element)
                ancestors.Add((element, element.RegisterPropertyChangedCallback(VisibilityProperty, (_, _) => UpdateMotion())));
    }
    private void ClearAncestors()
    {
        foreach (var (owner, token) in ancestors) owner.UnregisterPropertyChangedCallback(VisibilityProperty, token);
        ancestors.Clear();
    }
    private void StopMotion()
    {
        var active = pulse;
        pulse = null;
        active?.Stop();
    }
    private void UpdateMotion()
    {
        if (!IsLoaded || Visibility != Visibility.Visible || ancestors.Any(entry => entry.Owner.Visibility != Visibility.Visible) ||
            !settings.AnimationsEnabled) { StopMotion(); return; }
        if (pulse is not null || surface is null) return;
        // Tailwind 4.2.2 animate-pulse: 2s cubic-bezier(.4,0,.6,1), 50% opacity .5.
        // CSS synthesizes its missing 0%/100% frames from the consumer's
        // underlying opacity. Animation overrides that property temporarily;
        // stopping restores its value or binding without a local SetValue.
        pulseBaseOpacity = Convert.ToDouble(GetAnimationBaseValue(OpacityProperty));
        var animation = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromSeconds(2), RepeatBehavior = RepeatBehavior.Forever };
        animation.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = TimeSpan.Zero, Value = pulseBaseOpacity });
        foreach (var (seconds, opacity) in new[] { (1, .5), (2, pulseBaseOpacity) })
            animation.KeyFrames.Add(new SplineDoubleKeyFrame { KeyTime = TimeSpan.FromSeconds(seconds), Value = opacity,
                KeySpline = new KeySpline { ControlPoint1 = new Point(.4, 0), ControlPoint2 = new Point(.6, 1) } });
        Storyboard.SetTarget(animation, this);
        Storyboard.SetTargetProperty(animation, "Opacity");
        pulse = new Storyboard();
        pulse.Children.Add(animation);
        pulse.Begin();
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new BleeckerSkeletonAutomationPeer(this);
}

internal sealed class BleeckerSkeletonAutomationPeer(FrameworkElement owner) : FrameworkElementAutomationPeer(owner)
{
    protected override string GetClassNameCore() => Owner.GetType().Name;
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;
    protected override bool IsControlElementCore() => false;
    protected override bool IsContentElementCore() => false;
}
