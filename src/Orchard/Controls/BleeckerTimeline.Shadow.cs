using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

public sealed partial class BleeckerTimeline
{
    private sealed partial class Entry
    {
        private readonly Grid shadowHost = new() { Name = "TimelineShadowHost", IsHitTestVisible = false };
        private readonly Microsoft.UI.Xaml.Shapes.Path shadowMask = new() { Width = 36, Height = 36, Fill = new SolidColorBrush(Microsoft.UI.Colors.White) };
        private DropShadow? shadow;
        private SpriteVisual? shadowVisual;
        private ContainerVisual? shadowContainer;
        private CompositionPathGeometry? shadowClipGeometry;
        private CompositionGeometricClip? shadowClip;
        private CanvasGeometry? shadowOutside;
        private Rect? shadowViewport;
        private void InitializeShadow()
        {
            var maskHost = new Canvas { Opacity = 0, IsHitTestVisible = false }; maskHost.Children.Add(shadowMask);
            shadowMask.Data = BleeckerShadowGeometry.RoundedMask(36, 36, new CornerRadius(9), 0);
            icon.Children.Add(maskHost); icon.Children.Add(shadowHost);
            Loaded += (_, _) => UpdateShadow(); Unloaded += (_, _) => { if (!IsLoaded) ClearShadow(); };
        }
        private void UpdateShadow()
        {
            if (!IsLoaded || owner.XamlRoot is null || owner.accessibility.HighContrast || icon.ActualWidth <= 0) { ClearShadow(); return; }
            var viewport = ((UIElement)owner.XamlRoot.Content).TransformToVisual(shadowHost).TransformBounds(new Rect(0, 0, owner.XamlRoot.Size.Width, owner.XamlRoot.Size.Height));
            if (shadow is null)
            {
                var compositor = ElementCompositionPreview.GetElementVisual(shadowHost).Compositor;
                shadow = compositor.CreateDropShadow(); shadow.Mask = shadowMask.GetAlphaMask(); shadow.BlurRadius = 2; shadow.Offset = new Vector3(0, 1, 0);
                shadowVisual = compositor.CreateSpriteVisual(); shadowVisual.Size = new Vector2(36, 36); shadowVisual.Shadow = shadow;
                shadowContainer = compositor.CreateContainerVisual(); shadowContainer.Children.InsertAtTop(shadowVisual);
                shadowClipGeometry = compositor.CreatePathGeometry(); shadowClip = compositor.CreateGeometricClip(shadowClipGeometry); shadowContainer.Clip = shadowClip;
                ElementCompositionPreview.SetElementChildVisual(shadowHost, shadowContainer);
            }
            var paint = owner.Paint("Shadow") as SolidColorBrush ?? throw new InvalidOperationException("Timeline shadow requires a solid brush.");
            shadow.Color = paint.Color; shadow.Opacity = (float)paint.Opacity;
            if (shadowViewport == viewport) return;
            using var outer = CanvasGeometry.CreateRectangle(CanvasDevice.GetSharedDevice(), viewport);
            using var borderBox = CanvasGeometry.CreateRoundedRectangle(CanvasDevice.GetSharedDevice(), 0, 0, 36, 36, 9, 9);
            var outside = outer.CombineWith(borderBox, Matrix3x2.Identity, CanvasGeometryCombine.Exclude);
            shadowClipGeometry!.Path = new CompositionPath(outside); shadowOutside?.Dispose(); shadowOutside = outside; shadowViewport = viewport;
        }
        internal void ClearShadow()
        {
            ElementCompositionPreview.SetElementChildVisual(shadowHost, null);
            shadowContainer?.Dispose(); shadowVisual?.Dispose(); shadow?.Dispose(); shadowClip?.Dispose(); shadowClipGeometry?.Dispose(); shadowOutside?.Dispose();
            shadowContainer = null; shadowVisual = null; shadow = null; shadowClip = null; shadowClipGeometry = null; shadowOutside = null; shadowViewport = null;
        }
    }
}
