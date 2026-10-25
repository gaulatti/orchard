using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard;

public sealed partial class BleeckerTooltip
{
    private readonly UISettings motionSettings = new();
    private FrameworkElement? motionRoot;
    private Storyboard? motion;
    private bool delayedEntry;

    private void OnMotionSettingsChanged(UISettings sender, UISettingsAnimationsEnabledChangedEventArgs args) => DispatcherQueue.TryEnqueue(() =>
    {
        if (motionSettings.AnimationsEnabled) return;
        if (IsOpen) ResetMotion(); else FinishClose();
    });

    private void ResetMotion()
    {
        motion?.Stop();
        motion = null;
        if (motionRoot is null) return;
        motionRoot.Opacity = 1;
        if (motionRoot.RenderTransform is ScaleTransform scale) scale.ScaleX = scale.ScaleY = 1;
    }

    private void FinishClose()
    {
        ResetMotion();
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) motionSettings.AnimationsEnabledChanged -= OnMotionSettingsChanged;
        ClosePresentation();
    }

    private void BeginMotion(bool opening)
    {
        var animateEntry = delayedEntry;
        delayedEntry = false;
        if (!motionSettings.AnimationsEnabled || motionRoot?.RenderTransform is not ScaleTransform scale || (opening && !animateEntry))
        {
            if (opening) ResetMotion(); else FinishClose();
            return;
        }
        var opacity = opening ? 0 : motionRoot.Opacity;
        var size = opening ? .98 : scale.ScaleX;
        ResetMotion();
        var storyboard = new Storyboard();
        var duration = TimeSpan.FromMilliseconds(BleeckerTokens.Durations[opening ? "surface" : "motionExit"]);
        storyboard.Duration = duration;
        Add(motionRoot, "Opacity", opacity, opening ? 1 : 0);
        Add(scale, "ScaleX", size, opening ? 1 : .98);
        Add(scale, "ScaleY", size, opening ? 1 : .98);
        storyboard.Completed += (_, _) =>
        {
            if (!ReferenceEquals(motion, storyboard)) return;
            if (IsOpen) ResetMotion(); else FinishClose();
        };
        motion = storyboard;
        storyboard.Begin();

        void Add(DependencyObject target, string property, double from, double to)
        {
            var animation = new DoubleAnimationUsingKeyFrames { Duration = duration };
            animation.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = TimeSpan.Zero, Value = from });
            animation.KeyFrames.Add(new SplineDoubleKeyFrame
            {
                KeyTime = duration, Value = to,
                KeySpline = new KeySpline
                {
                    ControlPoint1 = opening ? new Point(.22, 1) : new Point(.42, 0),
                    ControlPoint2 = opening ? new Point(.36, 1) : new Point(1, 1)
                }
            });
            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation, property);
            storyboard.Children.Add(animation);
        }
    }
}
