using System.Numerics;
using System.Diagnostics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.UI.ViewManagement;

namespace Orchard;

public sealed partial class BleeckerTextarea
{
    private Grid? shadowHost;
    private Microsoft.UI.Xaml.Shapes.Path? shadowMask;
    private Microsoft.UI.Xaml.Shapes.Path? shadowExteriorMask;
    private SpriteVisual? shadowOutput;
    private CompositionVisualSurface? shadowSurface;
    private CompositionMaskBrush? shadowOutputBrush;
    private double exteriorWidth;
    private double exteriorHeight;
    private CornerRadius exteriorCorners;
    // Covers the maximum blur (10) and vertical offset (3) with three blur radii.
    private const float ShadowExtent = 33;
    private readonly AccessibilitySettings shadowAccessibility = new();
    private readonly UISettings shadowSettings = new();
    private SpriteVisual? shadowVisual;
    private DropShadow? inputShadow;
    private readonly Stopwatch shadowClock = new();
    private double shadowProgress;
    private double shadowFrom;
    private double shadowTarget;
    private double shadowDurationMilliseconds;

    private void InitializeShadow()
    {
        Loaded += (_, _) => { shadowSettings.ColorValuesChanged += OnShadowColorsChanged; UpdateShadow(); };
        Unloaded += (_, _) =>
        {
            shadowSettings.ColorValuesChanged -= OnShadowColorsChanged;
            pointerOver = false;
            ClearShadow();
        };
        RegisterPropertyChangedCallback(CornerRadiusProperty, (_, _) => PaintShadow());
        ActualThemeChanged += (_, _) => UpdateShadow();
    }

    private void ApplyShadowTemplate()
    {
        if (shadowHost is not null) shadowHost.SizeChanged -= OnShadowSizeChanged;
        ClearShadow();
        shadowHost = GetTemplateChild("InputShadowHost") as Grid;
        shadowMask = GetTemplateChild("InputShadowMask") as Microsoft.UI.Xaml.Shapes.Path;
        shadowExteriorMask = GetTemplateChild("InputShadowExteriorMask") as Microsoft.UI.Xaml.Shapes.Path;
        if (shadowHost is not null) shadowHost.SizeChanged += OnShadowSizeChanged;
    }

    private void OnShadowSizeChanged(object sender, SizeChangedEventArgs args) => PaintShadow();
    private void OnShadowColorsChanged(UISettings sender, object args) => DispatcherQueue.TryEnqueue(UpdateShadow);

    private void ClearShadow()
    {
        StopShadowMotion();
        if (shadowHost is not null) ElementCompositionPreview.SetElementChildVisual(shadowHost, null);
        shadowVisual = null;
        inputShadow = null;
        if (shadowSurface is not null) shadowSurface.SourceVisual = null;
        shadowSurface = null;
        shadowOutput = null;
        shadowOutputBrush = null;
        exteriorWidth = exteriorHeight = 0;
    }

    private void UpdateShadow() => UpdateShadow(false);

    private void UpdateShadow(bool animate)
    {
        if (shadowHost is null || shadowMask is null || shadowExteriorMask is null) return;
        if (!IsLoaded || shadowAccessibility.HighContrast) { ClearShadow(); return; }
        var target = pointerOver ? 1d : 0d;
        var enabled = animate && shadowSettings.AnimationsEnabled && shadowVisual is not null;
        if (enabled && shadowTarget == target) { PaintShadow(); return; }
        // Sample the running transition at the style change, as CSS does.
        if (shadowClock.IsRunning) OnShadowRendering(null, EventArgs.Empty);
        StopShadowMotion();
        shadowTarget = target;
        if (!enabled || Math.Abs(shadowProgress - target) < .000001)
        {
            shadowProgress = target;
            PaintShadow();
            return;
        }
        shadowFrom = shadowProgress;
        // With two endpoints (0 and 1) and a non-overshooting curve, CSS's
        // reversing shortening factor equals the distance to the new endpoint,
        // including repeated reversals. A completed transition travels distance 1.
        shadowDurationMilliseconds = BleeckerTokens.Durations["control"] * Math.Abs(target - shadowProgress);
        shadowClock.Restart();
        CompositionTarget.Rendering += OnShadowRendering;
    }

    private void StopShadowMotion()
    {
        CompositionTarget.Rendering -= OnShadowRendering;
        shadowClock.Stop();
    }

    private void OnShadowRendering(object? sender, object args)
    {
        var fraction = Math.Clamp(shadowClock.Elapsed.TotalMilliseconds / shadowDurationMilliseconds, 0, 1);
        // Invert the premium curve's x coordinate before evaluating its y.
        var low = 0d;
        var high = 1d;
        for (var i = 0; i < 24; i++)
        {
            var t = (low + high) / 2;
            var x = 3 * (1 - t) * (1 - t) * t * .22 + 3 * (1 - t) * t * t * .36 + t * t * t;
            if (x < fraction) low = t; else high = t;
        }
        var parameter = (low + high) / 2;
        var eased = 1 - Math.Pow(1 - parameter, 3);
        shadowProgress = fraction >= 1 ? shadowTarget : shadowFrom + (shadowTarget - shadowFrom) * eased;
        PaintShadow();
        if (fraction >= 1) StopShadowMotion();
    }

    private void PaintShadow()
    {
        if (shadowHost is null || shadowMask is null || shadowExteriorMask is null || !IsLoaded || shadowAccessibility.HighContrast) return;
        // Pinned input.tsx: rest 0 1px 2px / .025; hover 0 3px 10px -8px / .25.
        var inset = (float)(8 * shadowProgress);
        var width = Math.Max(0, shadowHost.ActualWidth - inset * 2);
        var height = Math.Max(0, shadowHost.ActualHeight - inset * 2);
        if (width == 0 || height == 0)
        {
            if (shadowVisual is not null) shadowVisual.IsVisible = false;
            if (shadowOutput is not null) shadowOutput.IsVisible = false;
            return;
        }
        shadowMask.Width = width;
        shadowMask.Height = height;
        shadowMask.Data = BleeckerShadowGeometry.RoundedMask(width, height, CornerRadius, inset);
        if (shadowVisual is null)
        {
            var compositor = ElementCompositionPreview.GetElementVisual(shadowHost).Compositor;
            inputShadow = compositor.CreateDropShadow();
            inputShadow.Color = Windows.UI.Color.FromArgb(255, 26, 55, 77);
            shadowVisual = compositor.CreateSpriteVisual();
            shadowVisual.Shadow = inputShadow;
            var sourceRoot = compositor.CreateContainerVisual();
            sourceRoot.Children.InsertAtTop(shadowVisual);
            shadowSurface = compositor.CreateVisualSurface();
            shadowSurface.SourceVisual = sourceRoot;
            shadowSurface.SourceOffset = new Vector2(-ShadowExtent);
            shadowOutputBrush = compositor.CreateMaskBrush();
            shadowOutputBrush.Source = compositor.CreateSurfaceBrush(shadowSurface);
            shadowOutput = compositor.CreateSpriteVisual();
            shadowOutput.Brush = shadowOutputBrush;
            shadowOutput.Offset = new Vector3(-ShadowExtent, -ShadowExtent, 0);
            ElementCompositionPreview.SetElementChildVisual(shadowHost, shadowOutput);
        }
        UpdateShadowExterior();
        inputShadow!.BlurRadius = (float)(2 + 8 * shadowProgress);
        shadowVisual.IsVisible = true;
        shadowOutput!.IsVisible = true;
        inputShadow.Offset = new Vector3(0, (float)(1 + 2 * shadowProgress), 0);
        inputShadow.Opacity = (float)(.025 + .225 * shadowProgress);
        inputShadow.Mask = shadowMask.GetAlphaMask();
        shadowVisual.Size = new Vector2((float)width, (float)height);
        shadowVisual.Offset = new Vector3(inset, inset, 0);
    }

    private void UpdateShadowExterior()
    {
        var width = shadowHost!.ActualWidth;
        var height = shadowHost.ActualHeight;
        if (width == exteriorWidth && height == exteriorHeight && CornerRadius == exteriorCorners) return;
        exteriorWidth = width;
        exteriorHeight = height;
        exteriorCorners = CornerRadius;
        // CSS outer shadows exclude the complete rounded border box, even when
        // its background is translucent. Mask the rendered shadow, not its caster.
        var geometry = BleeckerShadowGeometry.RoundedMask(width, height, CornerRadius, 0);
        geometry.FillRule = FillRule.EvenOdd;
        var outer = new PathFigure { StartPoint = new Windows.Foundation.Point(-ShadowExtent, -ShadowExtent), IsClosed = true, IsFilled = true };
        outer.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(width + ShadowExtent, -ShadowExtent) });
        outer.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(width + ShadowExtent, height + ShadowExtent) });
        outer.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(-ShadowExtent, height + ShadowExtent) });
        geometry.Figures.Add(outer);
        geometry.Transform = new TranslateTransform { X = ShadowExtent, Y = ShadowExtent };
        shadowExteriorMask!.Width = width + 2 * ShadowExtent;
        shadowExteriorMask.Height = height + 2 * ShadowExtent;
        shadowExteriorMask.Data = geometry;
        shadowOutputBrush!.Mask = shadowExteriorMask.GetAlphaMask();
        var size = new Vector2((float)shadowExteriorMask.Width, (float)shadowExteriorMask.Height);
        shadowSurface!.SourceSize = size;
        shadowOutput!.Size = size;
    }
}
