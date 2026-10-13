using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard;

public sealed partial class BleeckerAlert
{
    private Storyboard? surfaceMotion, closeMotion;
    private ScaleTransform? closeScale;
    private Border? closeChrome;
    private Border? closePalette;
    private SolidColorBrush? wash, observedWash;
    private Storyboard? washMotion;
    private long? paletteSubscription;
    private long washColorSubscription, washOpacitySubscription;
    private long pressedSubscription;

    private void OnMotionChanged(UISettings sender, UISettingsAnimationsEnabledChangedEventArgs args) => DispatcherQueue.TryEnqueue(() =>
    {
        if (!settings.AnimationsEnabled) AnimateSurface(IsOpen);
        UpdateCloseMotion(false);
        UpdateWash(false);
    });
    private void StopSurfaceMotion() { surfaceMotion?.Stop(); surfaceMotion = null; }
    private void AnimateSurface(bool opening)
    {
        if (motionRoot?.RenderTransform is not TranslateTransform shift) return;
        var fromOpacity = motionRoot.Opacity; var fromY = shift.Y;
        StopSurfaceMotion();
        motionRoot.Opacity = opening ? 1 : 0; shift.Y = opening ? 0 : 4;
        if (!settings.AnimationsEnabled) return;
        var storyboard = new Storyboard();
        var duration = TimeSpan.FromMilliseconds(BleeckerTokens.Durations["surface"]);
        AddTween(storyboard, motionRoot, "Opacity", fromOpacity, motionRoot.Opacity, duration);
        AddTween(storyboard, shift, "Y", fromY, shift.Y, duration);
        surfaceMotion = storyboard; storyboard.Begin();
    }
    private void ConnectCloseMotion()
    {
        if (close is null) return;
        close.Loaded += OnCloseLoaded;
        pressedSubscription = close.RegisterPropertyChangedCallback(Microsoft.UI.Xaml.Controls.Primitives.ButtonBase.IsPressedProperty, (_, _) => UpdateCloseMotion(true));
        FindCloseParts();
    }
    private void OnCloseLoaded(object sender, RoutedEventArgs args) => FindCloseParts();
    private void FindCloseParts()
    {
        if (close is null) return;
        DisconnectWash();
        close.ApplyTemplate();
        if (VisualTreeHelper.GetChildrenCount(close) == 0) return;
        var root = (FrameworkElement)VisualTreeHelper.GetChild(close, 0);
        closeScale = root.RenderTransform as ScaleTransform;
        closeChrome = root.FindName("PART_CloseChrome") as Border;
        closePalette = root.FindName("PART_ClosePalette") as Border;
        if (closeChrome is not null && closePalette is not null)
        {
            wash = new SolidColorBrush(); closeChrome.Background = wash;
            paletteSubscription = closePalette.RegisterPropertyChangedCallback(Border.BackgroundProperty, (_, _) => UpdateWash(true));
            UpdateWash(false);
        }
        UpdateCloseMotion(false);
    }
    private void UpdateCloseMotion(bool animate)
    {
        if (closeScale is null || close is null) return;
        var from = closeScale.ScaleX;
        StopCloseMotion();
        var target = close.IsEnabled && close.IsPressed ? .9 : 1;
        closeScale.ScaleX = closeScale.ScaleY = target;
        if (!animate || !IsLoaded || !settings.AnimationsEnabled || from == target) return;
        var storyboard = new Storyboard();
        var duration = TimeSpan.FromMilliseconds(BleeckerTokens.Durations["control"]);
        AddTween(storyboard, closeScale, "ScaleX", from, target, duration);
        AddTween(storyboard, closeScale, "ScaleY", from, target, duration);
        closeMotion = storyboard; storyboard.Begin();
    }
    private void DisconnectCloseMotion()
    {
        if (close is not null)
        {
            close.Loaded -= OnCloseLoaded;
            close.UnregisterPropertyChangedCallback(Microsoft.UI.Xaml.Controls.Primitives.ButtonBase.IsPressedProperty, pressedSubscription);
        }
        DisconnectWash();
        closeScale = null; closeChrome = null; closePalette = null; wash = null;
    }
    private void DisconnectWash()
    {
        if (closePalette is not null && paletteSubscription is long token) closePalette.UnregisterPropertyChangedCallback(Border.BackgroundProperty, token);
        paletteSubscription = null;
        ObserveWash(null);
        washMotion?.Stop(); washMotion = null;
    }
    private void ObserveWash(SolidColorBrush? source)
    {
        if (ReferenceEquals(observedWash, source)) return;
        observedWash?.UnregisterPropertyChangedCallback(SolidColorBrush.ColorProperty, washColorSubscription);
        observedWash?.UnregisterPropertyChangedCallback(Brush.OpacityProperty, washOpacitySubscription);
        observedWash = source;
        if (source is not null)
        {
            washColorSubscription = source.RegisterPropertyChangedCallback(SolidColorBrush.ColorProperty, (_, _) => UpdateWash(false));
            washOpacitySubscription = source.RegisterPropertyChangedCallback(Brush.OpacityProperty, (_, _) => UpdateWash(false));
        }
    }
    private void UpdateWash(bool animate)
    {
        if (wash is null || closePalette is null || closeChrome is null) return;
        if (closePalette.Background is not SolidColorBrush source)
        {
            ObserveWash(null); washMotion?.Stop(); washMotion = null;
            closeChrome.Background = closePalette.Background;
            return;
        }
        ObserveWash(source);
        var from = wash.Opacity;
        washMotion?.Stop(); washMotion = null;
        var target = source.Opacity * source.Color.A / 255d;
        // Keep the RGB of a fading wash while its alpha approaches zero.
        // This matches premultiplied CSS interpolation rather than fading white
        // through gray to transparent black in the dark appearance.
        if (target > 0) wash.Color = Windows.UI.Color.FromArgb(255, source.Color.R, source.Color.G, source.Color.B);
        wash.Opacity = target; closeChrome.Background = wash;
        if (!animate || !IsLoaded || !settings.AnimationsEnabled || from == target) return;
        var storyboard = new Storyboard();
        AddTween(storyboard, wash, "Opacity", from, target, TimeSpan.FromMilliseconds(BleeckerTokens.Durations["control"]));
        washMotion = storyboard; storyboard.Begin();
    }
    private void StopCloseMotion() { closeMotion?.Stop(); closeMotion = null; }
    private static void AddTween(Storyboard storyboard, DependencyObject target, string property, double from, double to, TimeSpan duration)
    {
        var animation = new DoubleAnimationUsingKeyFrames { Duration = duration };
        animation.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = TimeSpan.Zero, Value = from });
        animation.KeyFrames.Add(new SplineDoubleKeyFrame { KeyTime = duration, Value = to,
            KeySpline = new KeySpline { ControlPoint1 = new Point(.22, 1), ControlPoint2 = new Point(.36, 1) } });
        Storyboard.SetTarget(animation, target); Storyboard.SetTargetProperty(animation, property); storyboard.Children.Add(animation);
    }
}
