using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.UI.ViewManagement;

namespace Orchard;

public enum BleeckerProgressSize { Small, Medium, Large }
public enum BleeckerProgressVariant { Default, Success, Warning, Destructive }

/// <summary>A determinate progress display with consumer-owned value and maximum.</summary>
public sealed class BleeckerProgress : Control
{
    private ResourceDictionary? palette;
    private Border? track;
    private Border? fill;
    private TextBlock? label;
    private Storyboard? motion;
    private readonly UISettings settings = new();

    public BleeckerProgress()
    {
        DefaultStyleKey = typeof(BleeckerProgress);
        ApplyVariant();
        Loaded += (_, _) =>
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) settings.AnimationsEnabledChanged += OnMotionChanged;
            UpdateValue(false);
        };
        Unloaded += (_, _) =>
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) settings.AnimationsEnabledChanged -= OnMotionChanged;
            StopMotion();
        };
    }

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(double), typeof(BleeckerProgress),
        new PropertyMetadata(0d, (owner, args) =>
        {
            var control = (BleeckerProgress)owner;
            control.UpdateValue(true);
            FrameworkElementAutomationPeer.FromElement(control)?.RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty, args.OldValue, args.NewValue);
        }));
    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public static readonly DependencyProperty MaxProperty = DependencyProperty.Register(nameof(Max), typeof(double), typeof(BleeckerProgress),
        new PropertyMetadata(100d, (owner, args) =>
        {
            var control = (BleeckerProgress)owner;
            control.UpdateValue(true);
            FrameworkElementAutomationPeer.FromElement(control)?.RaisePropertyChangedEvent(RangeValuePatternIdentifiers.MaximumProperty, args.OldValue, args.NewValue);
        }));
    public double Max { get => (double)GetValue(MaxProperty); set => SetValue(MaxProperty, value); }
    public static readonly DependencyProperty ShowLabelProperty = DependencyProperty.Register(nameof(ShowLabel), typeof(bool), typeof(BleeckerProgress),
        new PropertyMetadata(false, (owner, _) => ((BleeckerProgress)owner).UpdateValue(false)));
    public bool ShowLabel { get => (bool)GetValue(ShowLabelProperty); set => SetValue(ShowLabelProperty, value); }
    public static readonly DependencyProperty ProgressSizeProperty = DependencyProperty.Register(nameof(ProgressSize), typeof(BleeckerProgressSize), typeof(BleeckerProgress),
        new PropertyMetadata(BleeckerProgressSize.Medium, (owner, _) => ((BleeckerProgress)owner).UpdateSize()));
    public BleeckerProgressSize ProgressSize { get => (BleeckerProgressSize)GetValue(ProgressSizeProperty); set => SetValue(ProgressSizeProperty, value); }
    public static readonly DependencyProperty VariantProperty = DependencyProperty.Register(nameof(Variant), typeof(BleeckerProgressVariant), typeof(BleeckerProgress),
        new PropertyMetadata(BleeckerProgressVariant.Default, (owner, _) => ((BleeckerProgress)owner).ApplyVariant()));
    public BleeckerProgressVariant Variant { get => (BleeckerProgressVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    protected override void OnApplyTemplate()
    {
        StopMotion();
        if (track is not null) track.SizeChanged -= OnTrackSizeChanged;
        base.OnApplyTemplate();
        track = GetTemplateChild("PART_Track") as Border;
        fill = GetTemplateChild("PART_Fill") as Border;
        label = GetTemplateChild("PART_Label") as TextBlock;
        if (track is not null) track.SizeChanged += OnTrackSizeChanged;
        UpdateSize();
        UpdateValue(false);
    }

    private void OnTrackSizeChanged(object sender, SizeChangedEventArgs args) => UpdateValue(false);
    private void OnMotionChanged(UISettings sender, UISettingsAnimationsEnabledChangedEventArgs args) => DispatcherQueue.TryEnqueue(() => UpdateValue(false));
    private void UpdateSize()
    {
        var height = ProgressSize switch { BleeckerProgressSize.Small => 6d, BleeckerProgressSize.Medium => 8d,
            BleeckerProgressSize.Large => 12d, _ => throw new ArgumentOutOfRangeException(nameof(ProgressSize)) };
        if (track is not null)
        {
            track.Height = height;
            track.CornerRadius = new CornerRadius(height / 2);
        }
        UpdateValue(false);
    }

    private void UpdateValue(bool animate)
    {
        if (track is null || fill is null || label is null) return;
        var percentage = Math.Clamp(Value / Max * 100, 0, 100);
        var from = fill.ActualWidth;
        StopMotion();
        var width = track.ActualWidth * percentage / 100;
        fill.Width = width;
        UpdateFillRadius(width);
        label.Text = $"{Math.Floor(percentage + .5).ToString(System.Globalization.CultureInfo.InvariantCulture)}%";
        label.Visibility = ShowLabel ? Visibility.Visible : Visibility.Collapsed;
        if (!animate || !IsLoaded || !settings.AnimationsEnabled || !double.IsFinite(width) || Math.Abs(from - width) < .000001) return;
        UpdateFillRadius(from);
        var duration = TimeSpan.FromMilliseconds(BleeckerTokens.Durations["overlay"]);
        var animation = new DoubleAnimationUsingKeyFrames { Duration = duration, EnableDependentAnimation = true };
        animation.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = TimeSpan.Zero, Value = from });
        animation.KeyFrames.Add(new SplineDoubleKeyFrame { KeyTime = duration, Value = width,
            KeySpline = new() { ControlPoint1 = new(.22, 1), ControlPoint2 = new(.36, 1) } });
        Storyboard.SetTarget(animation, fill);
        Storyboard.SetTargetProperty(animation, "Width");
        var storyboard = new Storyboard { Duration = duration };
        storyboard.Children.Add(animation);
        storyboard.Completed += (_, _) => { if (ReferenceEquals(motion, storyboard)) { StopMotion(); UpdateFillRadius(width); } };
        motion = storyboard;
        storyboard.Begin();
        CompositionTarget.Rendering += OnWidthFrame;
    }

    private void StopMotion()
    {
        CompositionTarget.Rendering -= OnWidthFrame;
        motion?.Stop();
        motion = null;
    }

    private void OnWidthFrame(object? sender, object args)
    {
        if (motion is not null && fill is not null) UpdateFillRadius(fill.ActualWidth);
    }

    private void UpdateFillRadius(double width)
    {
        if (track is null || fill is null || !double.IsFinite(width)) return;
        var radius = Math.Min(track.Height, width) / 2;
        if (fill.CornerRadius.TopLeft != radius) fill.CornerRadius = new CornerRadius(radius);
    }

    private void ApplyVariant()
    {
        if (!Enum.IsDefined(Variant)) throw new ArgumentOutOfRangeException(nameof(Variant));
        if (palette is null)
        {
            palette = new ResourceDictionary { Source = new Uri("ms-appx:///Orchard/Themes/Progress.xaml") };
            Resources.MergedDictionaries.Add(palette);
        }
        var name = Variant == BleeckerProgressVariant.Success ? "Default" : Variant.ToString();
        foreach (var theme in new[] { "Light", "Default" })
        {
            var resources = (ResourceDictionary)palette.ThemeDictionaries[theme];
            var source = (SolidColorBrush)resources[$"Progress{name}"];
            var destination = (SolidColorBrush)resources["ProgressFill"];
            destination.Color = source.Color;
            destination.Opacity = source.Opacity;
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new ProgressPeer(this);
    private sealed class ProgressPeer(BleeckerProgress owner) : FrameworkElementAutomationPeer(owner), IRangeValueProvider
    {
        protected override string GetClassNameCore() => nameof(BleeckerProgress);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ProgressBar;
        protected override object GetPatternCore(PatternInterface pattern) => pattern == PatternInterface.RangeValue ? this : base.GetPatternCore(pattern);
        public bool IsReadOnly => true;
        public double LargeChange => 0;
        public double SmallChange => 0;
        public double Minimum => 0;
        public double Maximum => owner.Max;
        public double Value => owner.Value;
        public void SetValue(double value) => throw new InvalidOperationException("Progress is a read-only display.");
    }
}
