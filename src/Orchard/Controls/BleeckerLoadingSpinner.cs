using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard;

public enum BleeckerLoadingSize { Small, Medium, Large }

/// <summary>An indeterminate, reference-shaped loading indicator. Supply a localized automation name.</summary>
public sealed class BleeckerLoadingSpinner : Control
{
    private readonly UISettings settings = new();
    private Canvas? canvas;
    private Microsoft.UI.Xaml.Shapes.Path? arc;
    private RotateTransform? rotation;
    private Storyboard? animation;

    public BleeckerLoadingSpinner()
    {
        DefaultStyleKey = typeof(BleeckerLoadingSpinner);
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
        RegisterPropertyChangedCallback(VisibilityProperty, (_, _) => UpdateMotion());
    }

    public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
        nameof(Size), typeof(BleeckerLoadingSize), typeof(BleeckerLoadingSpinner),
        new PropertyMetadata(BleeckerLoadingSize.Medium, (owner, _) => ((BleeckerLoadingSpinner)owner).UpdateGeometry()));

    public BleeckerLoadingSize Size { get => (BleeckerLoadingSize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    protected override void OnApplyTemplate()
    {
        StopMotion();
        base.OnApplyTemplate();
        canvas = GetTemplateChild("PART_Canvas") as Canvas;
        arc = GetTemplateChild("PART_Arc") as Microsoft.UI.Xaml.Shapes.Path;
        rotation = canvas?.RenderTransform as RotateTransform;
        UpdateGeometry();
    }

    private void UpdateGeometry()
    {
        var (diameter, thickness) = Size switch
        {
            BleeckerLoadingSize.Small => (16d, 2d),
            BleeckerLoadingSize.Medium => (32d, 2d),
            BleeckerLoadingSize.Large => (48d, 3d),
            _ => throw new ArgumentOutOfRangeException(nameof(Size))
        };
        if (canvas is null || arc is null || rotation is null) return;
        canvas.Width = canvas.Height = diameter;
        rotation.CenterX = rotation.CenterY = diameter / 2;
        var radius = (diameter - thickness) / 2;
        var offset = radius / Math.Sqrt(2);
        var figure = new PathFigure { StartPoint = new Point(diameter / 2 + offset, diameter / 2 - offset), IsClosed = false, IsFilled = false };
        figure.Segments.Add(new ArcSegment { Point = new Point(diameter / 2 - offset, diameter / 2 - offset), Size = new Size(radius, radius), IsLargeArc = true, SweepDirection = SweepDirection.Clockwise });
        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        arc.Data = geometry;
        arc.StrokeThickness = thickness;
        UpdateMotion();
    }

    private void OnAnimationsChanged(UISettings sender, UISettingsAnimationsEnabledChangedEventArgs args) => DispatcherQueue.TryEnqueue(UpdateMotion);
    private void StopMotion()
    {
        animation?.Stop();
        animation = null;
        if (rotation is not null) rotation.Angle = 0;
    }
    private void UpdateMotion()
    {
        if (!IsLoaded || Visibility != Visibility.Visible || !settings.AnimationsEnabled) { StopMotion(); return; }
        if (animation is not null || rotation is null) return;
        var turn = new DoubleAnimation { From = 0, To = 360, Duration = TimeSpan.FromSeconds(1), RepeatBehavior = RepeatBehavior.Forever };
        Storyboard.SetTarget(turn, rotation);
        Storyboard.SetTargetProperty(turn, "Angle");
        animation = new Storyboard();
        animation.Children.Add(turn);
        animation.Begin();
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new SpinnerPeer(this);
    private sealed class SpinnerPeer(BleeckerLoadingSpinner owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerLoadingSpinner);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ProgressBar;
    }
}
