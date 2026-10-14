using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard;

public sealed partial class BleeckerLineChart
{
    private readonly UISettings tooltipSettings = new();
    private readonly AccessibilitySettings tooltipAccessibility = new();
    private Grid? tooltipShadowHost, tooltipBackdropHost;
    private DropShadow? tooltipShadow;
    private SpriteVisual? tooltipShadowVisual, tooltipBlurVisual;
    private ContainerVisual? tooltipShadowContainer;
    private CompositionPathGeometry? tooltipOutsidePath, tooltipInsidePath;
    private CompositionGeometricClip? tooltipOutsideClip, tooltipInsideClip;
    private CanvasGeometry? tooltipOutsideGeometry, tooltipInsideGeometry;
    private CompositionEffectFactory? tooltipBlurFactory;
    private CompositionEffectBrush? tooltipBlurBrush;
    private CompositionBackdropBrush? tooltipBackdropBrush;
    private ShapeVisual? tooltipMaskVisual;
    private CompositionRoundedRectangleGeometry? tooltipMaskGeometry;
    private CompositionSpriteShape? tooltipMaskShape;
    private CompositionColorBrush? tooltipMaskPaint;
    private CompositionVisualSurface? tooltipMaskSurface;
    private CompositionSurfaceBrush? tooltipMaskBrush;
    private (double Width, double Height, Rect Viewport)? tooltipEffectGeometry;
    private bool tooltipEffectsConnected;

    private void ApplyTooltipEffectsTemplate()
    {
        tooltipShadowHost = GetTemplateChild("PART_TooltipShadow") as Grid ?? throw new InvalidOperationException("Line Chart requires PART_TooltipShadow.");
        tooltipBackdropHost = GetTemplateChild("PART_TooltipBackdrop") as Grid ?? throw new InvalidOperationException("Line Chart requires PART_TooltipBackdrop.");
        if (IsLoaded) ConnectTooltipEffects();
    }
    private void ConnectTooltipEffects()
    {
        if (tooltipEffectsConnected || tooltipContainer is null) return;
        tooltipEffectsConnected = true; tooltipContainer.LayoutUpdated += OnTooltipLayout;
        tooltipSettings.ColorValuesChanged += OnTooltipColors; UpdateTooltipEffects();
    }
    private void DisconnectTooltipEffects()
    {
        if (tooltipEffectsConnected)
        {
            if (tooltipContainer is not null) tooltipContainer.LayoutUpdated -= OnTooltipLayout;
            tooltipSettings.ColorValuesChanged -= OnTooltipColors; tooltipEffectsConnected = false;
        }
        ClearTooltipEffects();
    }
    private void OnTooltipLayout(object? sender, object args) => UpdateTooltipEffects();
    private void OnTooltipColors(UISettings sender, object args) => DispatcherQueue.TryEnqueue(UpdateTooltipEffects);
    private void UpdateTooltipEffects()
    {
        if (!IsLoaded || tooltipContainer?.Visibility != Visibility.Visible || tooltip is null || tooltipShadowHost is null || tooltipBackdropHost is null || tooltipAccessibility.HighContrast || tooltip.ActualWidth <= 0 || tooltip.ActualHeight <= 0)
        { ClearTooltipEffects(); return; }
        var width = tooltip.ActualWidth; var height = tooltip.ActualHeight;
        var viewport = ((UIElement)XamlRoot.Content).TransformToVisual(tooltipShadowHost).TransformBounds(new Rect(0, 0, XamlRoot.Size.Width, XamlRoot.Size.Height));
        var next = (width, height, viewport); if (tooltipEffectGeometry == next) return;
        var theme = BleeckerChartTheme.Tooltip; var compositor = ElementCompositionPreview.GetElementVisual(tooltipShadowHost).Compositor;
        if (tooltipShadow is null)
        {
            tooltipShadow = compositor.CreateDropShadow(); tooltipShadow.Color = Windows.UI.Color.FromArgb(255, theme.ShadowRed, theme.ShadowGreen, theme.ShadowBlue);
            tooltipShadow.BlurRadius = (float)theme.ShadowBlurCssPixels; tooltipShadow.Offset = new Vector3((float)theme.ShadowX, (float)theme.ShadowY, 0); tooltipShadow.Opacity = (float)theme.ShadowOpacity;
            tooltipShadowVisual = compositor.CreateSpriteVisual(); tooltipShadowVisual.Shadow = tooltipShadow;
            tooltipMaskGeometry=compositor.CreateRoundedRectangleGeometry();tooltipMaskGeometry.CornerRadius=new Vector2((float)theme.CornerRadius);
            tooltipMaskPaint=compositor.CreateColorBrush(Microsoft.UI.Colors.White);
            tooltipMaskShape=compositor.CreateSpriteShape(tooltipMaskGeometry);tooltipMaskShape.FillBrush=tooltipMaskPaint;
            tooltipMaskVisual=compositor.CreateShapeVisual();tooltipMaskVisual.Shapes.Add(tooltipMaskShape);
            tooltipMaskSurface=compositor.CreateVisualSurface();tooltipMaskSurface.SourceVisual=tooltipMaskVisual;
            tooltipMaskBrush=compositor.CreateSurfaceBrush(tooltipMaskSurface);tooltipShadow.Mask=tooltipMaskBrush;
            tooltipShadowContainer = compositor.CreateContainerVisual(); tooltipShadowContainer.Children.InsertAtTop(tooltipShadowVisual);
            tooltipOutsidePath = compositor.CreatePathGeometry(); tooltipOutsideClip = compositor.CreateGeometricClip(tooltipOutsidePath); tooltipShadowContainer.Clip = tooltipOutsideClip;
            ElementCompositionPreview.SetElementChildVisual(tooltipShadowHost, tooltipShadowContainer);
            tooltipBackdropBrush = compositor.CreateBackdropBrush();
            using var blur = new GaussianBlurEffect { Name = "chartBackdrop", BlurAmount = (float)theme.BackdropBlurCssPixels, BorderMode = EffectBorderMode.Hard, Source = new CompositionEffectSourceParameter("backdrop") };
            tooltipBlurFactory = compositor.CreateEffectFactory(blur, ["chartBackdrop.BlurAmount"]); tooltipBlurBrush = tooltipBlurFactory.CreateBrush(); tooltipBlurBrush.SetSourceParameter("backdrop", tooltipBackdropBrush);
            tooltipBlurVisual = compositor.CreateSpriteVisual(); tooltipBlurVisual.Brush = tooltipBlurBrush;
            tooltipInsidePath = compositor.CreatePathGeometry(); tooltipInsideClip = compositor.CreateGeometricClip(tooltipInsidePath); tooltipBlurVisual.Clip = tooltipInsideClip;
            ElementCompositionPreview.SetElementChildVisual(tooltipBackdropHost, tooltipBlurVisual);
        }
        var device = CanvasDevice.GetSharedDevice(); using var outer = CanvasGeometry.CreateRectangle(device, viewport);
        var inner = CanvasGeometry.CreateRoundedRectangle(device, new Rect(0, 0, width, height), (float)theme.CornerRadius, (float)theme.CornerRadius);
        var outside = outer.CombineWith(inner, Matrix3x2.Identity, CanvasGeometryCombine.Exclude);
        tooltipInsidePath!.Path = new CompositionPath(inner); tooltipOutsidePath!.Path = new CompositionPath(outside);
        tooltipInsideGeometry?.Dispose(); tooltipOutsideGeometry?.Dispose(); tooltipInsideGeometry = inner; tooltipOutsideGeometry = outside;
        var size=new Vector2((float)width,(float)height);
        tooltipMaskGeometry!.Size=size;tooltipMaskVisual!.Size=size;tooltipMaskSurface!.SourceSize=size;
        tooltipShadowVisual!.Size=tooltipBlurVisual!.Size=size;tooltipEffectGeometry=next;
    }
    private void ClearTooltipEffects()
    {
        if (tooltipShadowHost is not null) ElementCompositionPreview.SetElementChildVisual(tooltipShadowHost, null);
        if (tooltipBackdropHost is not null) ElementCompositionPreview.SetElementChildVisual(tooltipBackdropHost, null);
        tooltipShadowVisual?.Dispose(); tooltipShadowContainer?.Dispose(); tooltipShadow?.Dispose(); tooltipBlurVisual?.Dispose(); tooltipBlurBrush?.Dispose(); tooltipBlurFactory?.Dispose(); tooltipBackdropBrush?.Dispose();
        tooltipOutsideClip?.Dispose(); tooltipInsideClip?.Dispose(); tooltipOutsidePath?.Dispose(); tooltipInsidePath?.Dispose(); tooltipOutsideGeometry?.Dispose(); tooltipInsideGeometry?.Dispose();
        tooltipMaskBrush?.Dispose();tooltipMaskSurface?.Dispose();tooltipMaskVisual?.Dispose();tooltipMaskShape?.Dispose();tooltipMaskGeometry?.Dispose();tooltipMaskPaint?.Dispose();
        tooltipShadowVisual = null; tooltipShadowContainer = null; tooltipShadow = null; tooltipBlurVisual = null; tooltipBlurBrush = null; tooltipBlurFactory = null; tooltipBackdropBrush = null;
        tooltipOutsideClip = null; tooltipInsideClip = null; tooltipOutsidePath = null; tooltipInsidePath = null; tooltipOutsideGeometry = null; tooltipInsideGeometry = null; tooltipEffectGeometry = null;
        tooltipMaskBrush=null;tooltipMaskSurface=null;tooltipMaskVisual=null;tooltipMaskShape=null;tooltipMaskGeometry=null;tooltipMaskPaint=null;
    }
}
