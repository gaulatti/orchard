using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
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
    private ContainerVisual? shadowVisual;
    private (double Width, double Height, CornerRadius Radius)? shadowGeometry;
    private sealed record ShadowLayer(SpriteVisual Caster, CompositionVisualSurface Source, SpriteVisual Output);

    public BleeckerOverlaySurface()
    {
        DefaultStyleKey = typeof(BleeckerOverlaySurface);
        Loaded += (_, _) => { settings.ColorValuesChanged += OnColorsChanged; UpdateShadow(); };
        Unloaded += (_, _) => { settings.ColorValuesChanged -= OnColorsChanged; ClearShadow(); };
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
        UpdateShadow();
    }

    private void OnColorsChanged(UISettings sender, object args) => DispatcherQueue.TryEnqueue(UpdateShadow);
    private void ClearShadow()
    {
        if (shadowHost is not null) ElementCompositionPreview.SetElementChildVisual(shadowHost, null);
        for (var i = shadowResources.Count - 1; i >= 0; i--) shadowResources[i].Dispose();
        shadowResources.Clear();
        shadowLayers.Clear(); shadowVisual = null; shadowGeometry = null;
    }

    private void UpdateShadow()
    {
        if (shadowHost is null || nearMask is null || farMask is null) return;
        var visible = IsLoaded && !accessibility.HighContrast;
        shadowHost.Visibility = nearMask.Visibility = farMask.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        if (!visible || ActualWidth <= 0 || ActualHeight <= 0) { ClearShadow(); return; }
        var geometry = (ActualWidth, ActualHeight, CornerRadius);
        if (shadowGeometry == geometry) return;
        var compositor = ElementCompositionPreview.GetElementVisual(shadowHost).Compositor;
        if (shadowVisual is null) { shadowVisual = compositor.CreateContainerVisual(); shadowResources.Add(shadowVisual); }
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
                var caster = compositor.CreateSpriteVisual();
                var source = compositor.CreateVisualSurface(); source.SourceVisual = caster;
                var sourceBrush = compositor.CreateSurfaceBrush(source);
                using var blur = new GaussianBlurEffect { Name = "cssBlur", BlurAmount = sigma, BorderMode = EffectBorderMode.Soft, Optimization = EffectOptimization.Quality, Source = new CompositionEffectSourceParameter("mask") };
                using var tint = new ColorSourceEffect { Name = "shadowTint", Color = Windows.UI.Color.FromArgb(255, layer.R, layer.G, layer.B) };
                using var colored = new CompositeEffect { Mode = CanvasComposite.SourceIn, Sources = { blur, tint } };
                var factory = compositor.CreateEffectFactory(colored, ["cssBlur.BlurAmount", "shadowTint.Color"]);
                var brush = factory.CreateBrush(); brush.SetSourceParameter("mask", sourceBrush);
                var sprite = compositor.CreateSpriteVisual(); sprite.Opacity = layer.Opacity; sprite.Brush = brush;
                shadowVisual.Children.InsertAtTop(sprite);
                shadowResources.Add(caster); shadowResources.Add(source); shadowResources.Add(sourceBrush); shadowResources.Add(factory); shadowResources.Add(brush); shadowResources.Add(sprite);
                shadowLayers.Add(new(caster,source,sprite));
            }
            // Layout changes update the live mask and extent, retaining the
            // loaded effect factories until contrast suppression or unload.
            var active = shadowLayers[i];
            active.Caster.Size = new Vector2((float)mask.Width, (float)mask.Height);
            active.Caster.Brush = mask.GetAlphaMask();
            active.Source.SourceOffset = new Vector2(-extent);
            var size = new Vector2((float)mask.Width + extent * 2, (float)mask.Height + extent * 2);
            active.Source.SourceSize = size; active.Output.Size = size;
            active.Output.Offset = new Vector3(inset + layer.X - extent, inset + layer.Y - extent, 0);
        }
        ElementCompositionPreview.SetElementChildVisual(shadowHost, shadowVisual);
        shadowGeometry = geometry;
    }
}
