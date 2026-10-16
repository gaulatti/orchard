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
    }

    private void UpdateShadow()
    {
        if (shadowHost is null || nearMask is null || farMask is null) return;
        ClearShadow();
        var visible = IsLoaded && !accessibility.HighContrast;
        shadowHost.Visibility = nearMask.Visibility = farMask.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        if (!visible || ActualWidth <= 0 || ActualHeight <= 0) return;
        var compositor = ElementCompositionPreview.GetElementVisual(shadowHost).Compositor;
        var layers = compositor.CreateContainerVisual();
        shadowResources.Add(layers);
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
            var sourceVisual = compositor.CreateSpriteVisual();
            sourceVisual.Size = new Vector2((float)mask.Width, (float)mask.Height);
            sourceVisual.Brush = mask.GetAlphaMask();
            var source = compositor.CreateVisualSurface();
            source.SourceVisual = sourceVisual;
            source.SourceOffset = new Vector2(-extent);
            var size = new Vector2((float)mask.Width + extent * 2, (float)mask.Height + extent * 2);
            source.SourceSize = size;
            var sourceBrush = compositor.CreateSurfaceBrush(source);
            using var blur = new GaussianBlurEffect { Name = "cssBlur", BlurAmount = sigma, BorderMode = EffectBorderMode.Soft, Optimization = EffectOptimization.Quality, Source = new CompositionEffectSourceParameter("mask") };
            using var tint = new ColorSourceEffect { Name = "shadowTint", Color = Windows.UI.Color.FromArgb(255, layer.R, layer.G, layer.B) };
            using var colored = new CompositeEffect { Mode = CanvasComposite.SourceIn, Sources = { blur, tint } };
            var factory = compositor.CreateEffectFactory(colored, ["cssBlur.BlurAmount", "shadowTint.Color"]);
            var brush = factory.CreateBrush();
            brush.SetSourceParameter("mask", sourceBrush);
            var sprite = compositor.CreateSpriteVisual();
            sprite.Size = size;
            sprite.Offset = new Vector3(inset + layer.X - extent, inset + layer.Y - extent, 0);
            sprite.Opacity = layer.Opacity;
            sprite.Brush = brush;
            layers.Children.InsertAtTop(sprite);
            shadowResources.Add(sourceVisual); shadowResources.Add(source); shadowResources.Add(sourceBrush); shadowResources.Add(factory); shadowResources.Add(brush); shadowResources.Add(sprite);
        }
        ElementCompositionPreview.SetElementChildVisual(shadowHost, layers);
    }
}
