using System.Numerics;
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
            var shadow = compositor.CreateDropShadow();
            shadow.BlurRadius = layer.Blur;
            shadow.Offset = new Vector3(layer.X, layer.Y, 0);
            shadow.Color = Windows.UI.Color.FromArgb(255, layer.R, layer.G, layer.B);
            shadow.Opacity = layer.Opacity;
            shadow.Mask = mask.GetAlphaMask();
            var sprite = compositor.CreateSpriteVisual();
            sprite.Size = new Vector2((float)mask.Width, (float)mask.Height);
            sprite.Offset = new Vector3(inset, inset, 0);
            sprite.Shadow = shadow;
            layers.Children.InsertAtTop(sprite);
        }
        ElementCompositionPreview.SetElementChildVisual(shadowHost, layers);
    }
}
