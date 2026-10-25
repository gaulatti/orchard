using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.UI.ViewManagement;

namespace Orchard;

/// <summary>A content surface with the canonical Bleecker overlay shadow.</summary>
public sealed class BleeckerOverlaySurface : ContentControl
{
    private Grid? shadowHost;
    private Microsoft.UI.Xaml.Shapes.Path? nearMask;
    private Microsoft.UI.Xaml.Shapes.Path? farMask;
    private readonly AccessibilitySettings accessibility = new();
    private readonly UISettings settings = new();
    private readonly List<IDisposable> shadowResources = [];
    private readonly List<ShadowLayer> shadowLayers = [];
    private readonly List<(UIElement Element, long Token)> visibilityObservers = [];
    private ContainerVisual? shadowVisual;
    private CompositionPathGeometry? shadowClipPath;
    private CompositionGeometricClip? shadowClip;
    private CanvasGeometry? shadowOutside;
    private (double Width, double Height, CornerRadius Radius)? shadowGeometry;
    internal event EventHandler? ShadowGraphChanged;
    private sealed record ShadowLayer(ShapeVisual Caster, CompositionVisualSurface Source, SpriteVisual Output, CompositionPathGeometry MaskPath)
    {
        internal CanvasGeometry? MaskGeometry { get; set; }
    }

    public BleeckerOverlaySurface()
    {
        DefaultStyleKey = typeof(BleeckerOverlaySurface);
        Loaded += (_, _) => { settings.ColorValuesChanged += OnColorsChanged; ObserveVisibility(); UpdateShadow(); };
        Unloaded += (_, _) => { settings.ColorValuesChanged -= OnColorsChanged; StopObservingVisibility(); ClearShadow(); };
        SizeChanged += (_, _) => UpdateShadow();
        RegisterPropertyChangedCallback(CornerRadiusProperty, (_, _) => UpdateShadow());
    }

    protected override void OnApplyTemplate()
    {
        ClearShadow();
        base.OnApplyTemplate();
        shadowHost = GetTemplateChild("PART_ShadowHost") as Grid;
        nearMask = GetTemplateChild("PART_NearMask") as Microsoft.UI.Xaml.Shapes.Path;
        farMask = GetTemplateChild("PART_FarMask") as Microsoft.UI.Xaml.Shapes.Path;
        // These template parts carry geometry only. The composition caster is
        // opaque and independent of the caller's surface/background brush.
        if (nearMask is not null) nearMask.Opacity = 0;
        if (farMask is not null) farMask.Opacity = 0;
        UpdateShadow();
    }

    private void OnColorsChanged(UISettings sender, object args) => DispatcherQueue.TryEnqueue(UpdateShadow);

    private void StopObservingVisibility()
    {
        foreach (var (element, token) in visibilityObservers)
            element.UnregisterPropertyChangedCallback(VisibilityProperty, token);
        visibilityObservers.Clear();
    }

    private void ObserveVisibility()
    {
        StopObservingVisibility();
        for (DependencyObject? current = this; current is not null; current = VisualTreeHelper.GetParent(current))
            if (current is UIElement element)
                visibilityObservers.Add((element, element.RegisterPropertyChangedCallback(VisibilityProperty, (_, _) => UpdateShadow())));
    }

    private bool HasVisibleAncestors()
    {
        for (DependencyObject? current = this; current is not null; current = VisualTreeHelper.GetParent(current))
            if (current is UIElement { Visibility: not Visibility.Visible }) return false;
        return true;
    }

    private void ClearShadow()
    {
        if (shadowHost is not null) ElementCompositionPreview.SetElementChildVisual(shadowHost, null);
        for (var i = shadowResources.Count - 1; i >= 0; i--) shadowResources[i].Dispose();
        shadowResources.Clear();
        foreach (var layer in shadowLayers) layer.MaskGeometry?.Dispose();
        shadowOutside?.Dispose(); shadowOutside = null; shadowClip = null; shadowClipPath = null;
        shadowLayers.Clear(); shadowVisual = null; shadowGeometry = null;
        ShadowGraphChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateShadow()
    {
        if (shadowHost is null || nearMask is null || farMask is null) return;
        var visible = IsLoaded && HasVisibleAncestors() && !accessibility.HighContrast;
        shadowHost.Visibility = nearMask.Visibility = farMask.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        if (!visible || ActualWidth <= 0 || ActualHeight <= 0) { ClearShadow(); return; }
        var geometry = (ActualWidth, ActualHeight, CornerRadius);
        if (shadowGeometry == geometry) return;
        var compositor = ElementCompositionPreview.GetElementVisual(shadowHost).Compositor;
        if (shadowVisual is null)
        {
            shadowVisual = compositor.CreateContainerVisual(); shadowClipPath = compositor.CreatePathGeometry(); shadowClip = compositor.CreateGeometricClip(shadowClipPath); shadowVisual.Clip = shadowClip;
            shadowResources.AddRange([shadowVisual, shadowClipPath, shadowClip]);
        }
        for (var i = 0; i < BleeckerTokens.OverlayShadows.Count; i++)
        {
            var layer = BleeckerTokens.OverlayShadows[i];
            var mask = i == 0 ? farMask : nearMask;
            var inset = -layer.Spread;
            mask.Width = Math.Max(0, ActualWidth - inset * 2);
            mask.Height = Math.Max(0, ActualHeight - inset * 2);
            mask.Data = BleeckerShadowGeometry.RoundedMask(mask.Width, mask.Height, CornerRadius, inset);
            Canvas.SetLeft(mask, inset);
            Canvas.SetTop(mask, inset);
            // CSS box-shadow uses a Gaussian standard deviation of blur / 2.
            // Pad the source by its complete three-sigma kernel before blurring.
            var sigma = layer.Blur / 2;
            var extent = MathF.Ceiling(sigma * 3);
            if (shadowLayers.Count <= i)
            {
                var maskPath = compositor.CreatePathGeometry();
                var maskFill = compositor.CreateColorBrush(Microsoft.UI.Colors.White);
                var maskShape = compositor.CreateSpriteShape(maskPath); maskShape.FillBrush = maskFill;
                var caster = compositor.CreateShapeVisual(); caster.Shapes.Add(maskShape);
                var source = compositor.CreateVisualSurface(); source.SourceVisual = caster;
                var sourceBrush = compositor.CreateSurfaceBrush(source);
                using var blur = new GaussianBlurEffect { Name = "cssBlur", BlurAmount = sigma, BorderMode = EffectBorderMode.Soft, Optimization = EffectOptimization.Quality, Source = new CompositionEffectSourceParameter("mask") };
                using var tint = new ColorSourceEffect { Name = "shadowTint", ColorHdr = BleeckerShadowPaint.EncodeColor(layer.Opacity, layer.R, layer.G, layer.B) };
                using var colored = new CompositeEffect { Mode = CanvasComposite.SourceIn, Sources = { blur, tint } };
                var factory = compositor.CreateEffectFactory(colored, ["cssBlur.BlurAmount", "shadowTint.Color"]);
                var brush = factory.CreateBrush(); brush.SetSourceParameter("mask", sourceBrush);
                // The encoded tint already carries the source opacity.
                var sprite = compositor.CreateSpriteVisual(); sprite.Opacity = 1; sprite.Brush = brush;
                // CSS lists shadows front to back; later recipes belong behind
                // the first layer, even when their colours happen to match.
                shadowVisual.Children.InsertAtBottom(sprite);
                shadowResources.AddRange([maskPath, maskFill, maskShape, caster, source, sourceBrush, factory, brush, sprite]);
                shadowLayers.Add(new(caster,source,sprite,maskPath));
            }
            // Layout changes update the live mask and extent, retaining the
            // loaded effect factories until visibility suppression or unload.
            var active = shadowLayers[i];
            active.Caster.Size = new Vector2((float)mask.Width, (float)mask.Height);
            var nextMask = BleeckerShadowGeometry.CanvasMask(CanvasDevice.GetSharedDevice(), mask.Width, mask.Height, CornerRadius, inset);
            active.MaskPath.Path = new CompositionPath(nextMask); active.MaskGeometry?.Dispose(); active.MaskGeometry = nextMask;
            active.Source.SourceOffset = new Vector2(-extent);
            var size = new Vector2((float)mask.Width + extent * 2, (float)mask.Height + extent * 2);
            active.Source.SourceSize = size; active.Output.Size = size;
            active.Output.Offset = new Vector3(inset + layer.X - extent, inset + layer.Y - extent, 0);
        }
        // CSS outer shadows cast an opaque border box and exclude its entire
        // interior, including when the caller paints a transparent background.
        var paddingX = BleeckerTokens.OverlayShadows.Max(layer => MathF.Ceiling(layer.Blur / 2 * 3) + Math.Abs(layer.Spread) + Math.Abs(layer.X));
        var paddingY = BleeckerTokens.OverlayShadows.Max(layer => MathF.Ceiling(layer.Blur / 2 * 3) + Math.Abs(layer.Spread) + Math.Abs(layer.Y));
        var device = CanvasDevice.GetSharedDevice();
        using var outer = CanvasGeometry.CreateRectangle(device, -paddingX, -paddingY, (float)ActualWidth + paddingX * 2, (float)ActualHeight + paddingY * 2);
        using var borderBox = BleeckerShadowGeometry.CanvasMask(device, ActualWidth, ActualHeight, CornerRadius, 0);
        var outside = outer.CombineWith(borderBox, Matrix3x2.Identity, CanvasGeometryCombine.Exclude);
        shadowClipPath!.Path = new CompositionPath(outside); shadowOutside?.Dispose(); shadowOutside = outside;
        ElementCompositionPreview.SetElementChildVisual(shadowHost, shadowVisual);
        shadowGeometry = geometry;
        ShadowGraphChanged?.Invoke(this, EventArgs.Empty);
    }
}
