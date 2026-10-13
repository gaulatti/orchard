using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard;

/// <summary>A decorative Bleecker loading shape. Width and Height belong to the consumer.</summary>
public sealed class BleeckerSkeleton : Control
{
    private readonly UISettings settings = new();
    private Border? surface;
    private Storyboard? pulse;

    public BleeckerSkeleton()
    {
        DefaultStyleKey = typeof(BleeckerSkeleton);
        SizeChanged += (_, _) => UpdateCorners();
        RegisterPropertyChangedCallback(CornerRadiusProperty, (_, _) => UpdateCorners());
        RegisterPropertyChangedCallback(VisibilityProperty, (_, _) => UpdateMotion());
        Loaded += (_, _) =>
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) settings.AnimationsEnabledChanged += OnAnimationsChanged;
            UpdateMotion();
        };
        Unloaded += (_, _) =>
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) settings.AnimationsEnabledChanged -= OnAnimationsChanged;
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
    private void StopMotion()
    {
        pulse?.Stop();
        pulse = null;
        if (surface is not null) surface.Opacity = 1;
    }
    private void UpdateMotion()
    {
        if (!IsLoaded || Visibility != Visibility.Visible || !settings.AnimationsEnabled) { StopMotion(); return; }
        if (pulse is not null || surface is null) return;
        // Tailwind 4.2.2 animate-pulse: 2s cubic-bezier(.4,0,.6,1), 50% opacity .5.
        // Animate template-owned opacity, preserving consumer opacity and bindings.
        var animation = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromSeconds(2), RepeatBehavior = RepeatBehavior.Forever };
        animation.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = TimeSpan.Zero, Value = 1 });
        foreach (var (seconds, opacity) in new[] { (1, .5), (2, 1d) })
            animation.KeyFrames.Add(new SplineDoubleKeyFrame { KeyTime = TimeSpan.FromSeconds(seconds), Value = opacity,
                KeySpline = new KeySpline { ControlPoint1 = new Point(.4, 0), ControlPoint2 = new Point(.6, 1) } });
        Storyboard.SetTarget(animation, surface);
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
