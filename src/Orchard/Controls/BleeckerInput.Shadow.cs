using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Windows.UI.ViewManagement;

namespace Orchard;

public sealed partial class BleeckerInput
{
    private Grid? shadowHost;
    private Microsoft.UI.Xaml.Shapes.Path? shadowMask;
    private readonly AccessibilitySettings shadowAccessibility = new();
    private readonly UISettings shadowSettings = new();

    private void InitializeShadow()
    {
        Loaded += (_, _) => { shadowSettings.ColorValuesChanged += OnShadowColorsChanged; UpdateShadow(); };
        Unloaded += (_, _) =>
        {
            shadowSettings.ColorValuesChanged -= OnShadowColorsChanged;
            pointerOver = false;
            ClearShadow();
        };
        RegisterPropertyChangedCallback(CornerRadiusProperty, (_, _) => UpdateShadow());
    }

    private void ApplyShadowTemplate()
    {
        if (shadowHost is not null) shadowHost.SizeChanged -= OnShadowSizeChanged;
        ClearShadow();
        shadowHost = GetTemplateChild("InputShadowHost") as Grid;
        shadowMask = GetTemplateChild("InputShadowMask") as Microsoft.UI.Xaml.Shapes.Path;
        if (shadowHost is not null) shadowHost.SizeChanged += OnShadowSizeChanged;
    }

    private void OnShadowSizeChanged(object sender, SizeChangedEventArgs args) => UpdateShadow();
    private void OnShadowColorsChanged(UISettings sender, object args) => DispatcherQueue.TryEnqueue(UpdateShadow);

    private void ClearShadow()
    {
        if (shadowHost is not null) ElementCompositionPreview.SetElementChildVisual(shadowHost, null);
    }

    private void UpdateShadow()
    {
        if (shadowHost is null || shadowMask is null) return;
        ClearShadow();
        if (!IsLoaded || shadowAccessibility.HighContrast || shadowHost.ActualWidth <= 0 || shadowHost.ActualHeight <= 0) return;
        // Pinned input.tsx: rest 0 1px 2px / .025; hover 0 3px 10px -8px / .25.
        var hover = pointerOver;
        var inset = hover ? 8f : 0f;
        var width = Math.Max(0, shadowHost.ActualWidth - inset * 2);
        var height = Math.Max(0, shadowHost.ActualHeight - inset * 2);
        if (width == 0 || height == 0) return;
        shadowMask.Width = width;
        shadowMask.Height = height;
        shadowMask.Data = BleeckerShadowGeometry.RoundedMask(width, height, CornerRadius, inset);
        var compositor = ElementCompositionPreview.GetElementVisual(shadowHost).Compositor;
        var shadow = compositor.CreateDropShadow();
        shadow.BlurRadius = hover ? 10 : 2;
        shadow.Offset = new Vector3(0, hover ? 3 : 1, 0);
        shadow.Color = Windows.UI.Color.FromArgb(255, 26, 55, 77);
        shadow.Opacity = hover ? .25f : .025f;
        shadow.Mask = shadowMask.GetAlphaMask();
        var sprite = compositor.CreateSpriteVisual();
        sprite.Size = new Vector2((float)width, (float)height);
        sprite.Offset = new Vector3(inset, inset, 0);
        sprite.Shadow = shadow;
        ElementCompositionPreview.SetElementChildVisual(shadowHost, sprite);
    }
}
