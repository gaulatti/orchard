using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard;

public sealed partial class BleeckerMenubar
{
    private sealed partial class MenuLevel
    {
        private readonly Grid motionRoot = new() { Name = "MenubarMotionRoot", UseLayoutRounding = false, RenderTransformOrigin = new Point(0, 0) };
        private readonly CompositeTransform motionTransform = new();
        private readonly BleeckerCssTransition<double> motionProgress = new((from, to, progress) => from + (to - from) * progress);
        private readonly Stopwatch motionClock = Stopwatch.StartNew();
        private readonly UISettings motionSettings = new();
        private bool motionRendering;

        private void InitializeMotion()
        {
            // Animate an owned parent so caller surface opacity/transforms and
            // their bindings remain untouched. Layout dimensions stay unchanged.
            motionRoot.RenderTransform = motionTransform;
            motionRoot.Children.Add(Surface);
            RetargetSurfaceMotion(false, motionClock.Elapsed.TotalMilliseconds, false);
        }
        private void BeginSurfaceMotion(bool visible)
        {
            RetargetSurfaceMotion(visible, motionClock.Elapsed.TotalMilliseconds, motionSettings.AnimationsEnabled);
            if (!motionRendering && motionProgress.IsRunning(motionClock.Elapsed.TotalMilliseconds))
            {
                CompositionTarget.Rendering += RenderSurfaceMotion;
                motionRendering = true;
            }
        }
        private void RetargetSurfaceMotion(bool visible, double time, bool animate)
        {
            motionProgress.Retarget(visible ? 1 : 0, time, BleeckerTokens.Durations[visible ? "surface" : "motionExit"], animate,
                visible ? BleeckerColorMix.Premium : EaseIn);
            // The frozen Tailwind build uses individual scale/translate
            // properties, outside transition-[opacity,transform]. They switch
            // with the visibility state; only opacity has a transition.
            motionTransform.ScaleX = motionTransform.ScaleY = visible ? 1 : .992;
            motionTransform.TranslateY = visible ? 0 : -4;
            PaintSurfaceMotion(time);
        }
        private void PaintSurfaceMotion(double time)
        {
            motionRoot.Opacity = motionProgress.ValueAt(time);
        }
        private void RenderSurfaceMotion(object? sender, object args)
        {
            var time = motionClock.Elapsed.TotalMilliseconds;
            if (!motionSettings.AnimationsEnabled) RetargetSurfaceMotion(open, time, false);
            else PaintSurfaceMotion(time);
            if (!motionProgress.IsRunning(time)) StopSurfaceMotion();
        }
        private void StopSurfaceMotion()
        {
            if (motionRendering) CompositionTarget.Rendering -= RenderSurfaceMotion;
            motionRendering = false;
        }
        private static double EaseIn(double progress)
        {
            if (progress <= 0) return 0;
            if (progress >= 1) return 1;
            double low = 0, high = 1;
            for (var i = 0; i < 24; i++)
            {
                var t = (low + high) / 2;
                var x = 3 * (1 - t) * (1 - t) * t * .42 + 3 * (1 - t) * t * t + t * t * t;
                if (x < progress) low = t; else high = t;
            }
            var p = (low + high) / 2;
            return 3 * (1 - p) * p * p + p * p * p;
        }
    }
}
