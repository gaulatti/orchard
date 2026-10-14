using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard;

public sealed partial class BleeckerAttentionSurface
{
    private DropShadow? nativeShadow;
    private SpriteVisual? shadowVisual;
    private ContainerVisual? shadowContainer;
    private CompositionPathGeometry? shadowClipGeometry;
    private CompositionGeometricClip? shadowClip;
    private CanvasGeometry? shadowOutside;
    private (double Width,double Height,CornerRadius Corners,ShadowState State,Rect Viewport)? shadowGeometry;
    private readonly record struct ShadowState(double Y,double Blur,double Spread,double Alpha)
    {
        internal static ShadowState Mix(ShadowState from,ShadowState to,double progress) => new(
            from.Y+(to.Y-from.Y)*progress,from.Blur+(to.Blur-from.Blur)*progress,
            from.Spread+(to.Spread-from.Spread)*progress,from.Alpha+(to.Alpha-from.Alpha)*progress);
    }
    private void UpdateGeometry()
    {
        if (surface is null || backdrop is null || focus is null) return;
        surface.Padding = HasPaddingOverride() ? Padding : new Thickness(Density == BleeckerAttentionDensity.Compact ? 16 : 20);
        surface.BorderThickness = BorderThickness;
        // The public radius retains CSS's outer contour. WinUI Border specifies
        // its stroke centerline, so the one-pixel default subtracts half a pixel.
        var half = Math.Min(Math.Min(BorderThickness.Left,BorderThickness.Right),Math.Min(BorderThickness.Top,BorderThickness.Bottom))/2;
        var corners = CornerRadius;
        surface.CornerRadius = new CornerRadius(Math.Max(0,corners.TopLeft-half),Math.Max(0,corners.TopRight-half),Math.Max(0,corners.BottomRight-half),Math.Max(0,corners.BottomLeft-half));
        backdrop.CornerRadius = corners;
        focus.CornerRadius = new CornerRadius(corners.TopLeft+1,corners.TopRight+1,corners.BottomRight+1,corners.BottomLeft+1);
        if (recipe is not null && XamlRoot is not null) gradient.Update(recipe,ActualWidth,ActualHeight,BorderThickness,XamlRoot.RasterizationScale);
        UpdateTypography(); if (IsLoaded) Paint(clock.Elapsed.TotalMilliseconds);
    }
    private void UpdateAppearance(bool animate)
    {
        if (updatingRecipe || recipe is null || surface is null || backdrop is null || contrastBackground is null || contrastBorder is null) return;
        var contrast = accessibility.HighContrast; renderedContrast = contrast; renderedTheme = ActualTheme;
        var now = clock.Elapsed.TotalMilliseconds;
        var animated = animate && IsLoaded && settings.AnimationsEnabled && !contrast;
        var border = contrast ? ReadSolid(contrastBorder.Foreground) : ReferenceEquals(BorderBrush,sourceBorder) ? recipe.Border : ReadSolid(BorderBrush);
        borderMotion.Retarget(border,now,BleeckerTokens.Durations["surface"],animated);
        var solid = contrast ? contrastBackground.Foreground : Background is SolidColorBrush ? Background : null;
        backgroundMotion.Retarget(ReadSolid(solid),now,BleeckerTokens.Durations["surface"],animated);
        surface.Background = contrast || Background is SolidColorBrush ? null : Background;
        var hovered = IsInteractive && pointerOver && !contrast;
        liftMotion.Retarget(hovered ? -1 : 0,now,BleeckerTokens.Durations["surface"],animated);
        shadowMotion.Retarget(hovered ? new(24,48,-28,.9) : new(16,38,-30,.92),now,BleeckerTokens.Durations["surface"],animated);
        running = IsRunning(now); Paint(now); UpdateFocus();
    }
    private static (double L,double A,double B,double Alpha) ReadSolid(Brush? brush)
    {
        if (brush is null) return (0,0,0,0);
        if (brush is not SolidColorBrush solid) throw new InvalidOperationException("Attention Surface border color transitions require a SolidColorBrush.");
        return BleeckerColorMix.Premultiply((solid.Color.R,solid.Color.G,solid.Color.B,solid.Color.A/255d*solid.Opacity));
    }
    private bool IsRunning(double now) => borderMotion.IsRunning(now) || backgroundMotion.IsRunning(now) || liftMotion.IsRunning(now) || shadowMotion.IsRunning(now);
    private void Paint(double now)
    {
        if (lift is null || surface is null) return;
        PaintBrush(borderPaint,borderMotion.ValueAt(now)); PaintBrush(backgroundPaint,backgroundMotion.ValueAt(now));
        lift.Y = liftMotion.ValueAt(now);
        UpdateShadow(shadowMotion.ValueAt(now));
    }
    private static void PaintBrush(SolidColorBrush brush,(double L,double A,double B,double Alpha) value)
    {
        var paint = BleeckerColorMix.EncodePremultiplied(value);
        brush.Color = Windows.UI.Color.FromArgb(255,paint.R,paint.G,paint.B); brush.Opacity = paint.Alpha;
    }
    private void UpdateFocus()
    {
        if (focus is not null) focus.Visibility = IsInteractive && (Element == BleeckerAttentionElement.Button ? action?.FocusState != FocusState.Unfocused : FocusState != FocusState.Unfocused)
            ? Visibility.Visible : Visibility.Collapsed;
    }
    private void UpdateShadow(ShadowState state)
    {
        if (shadowHost is null || shadowMask is null || !IsLoaded || accessibility.HighContrast || ActualWidth <= 0 || ActualHeight <= 0)
        { ClearShadow(); return; }
        var width = Math.Max(0,ActualWidth+state.Spread*2); var height = Math.Max(0,ActualHeight+state.Spread*2);
        if (width == 0 || height == 0) { ClearShadow(); return; }
        var viewport = ((UIElement)XamlRoot.Content).TransformToVisual(shadowHost).TransformBounds(new Rect(0,0,XamlRoot.Size.Width,XamlRoot.Size.Height));
        var next = (ActualWidth,ActualHeight,CornerRadius,state,viewport);
        if (shadowGeometry == next) return;
        shadowMask.Width = width; shadowMask.Height = height;
        shadowMask.Data = BleeckerShadowGeometry.RoundedMask(width,height,CornerRadius,-state.Spread);
        if (nativeShadow is null)
        {
            var compositor = ElementCompositionPreview.GetElementVisual(shadowHost).Compositor;
            nativeShadow = compositor.CreateDropShadow(); nativeShadow.Color = Microsoft.UI.Colors.Black;
            shadowVisual = compositor.CreateSpriteVisual(); shadowVisual.Shadow = nativeShadow;
            shadowContainer = compositor.CreateContainerVisual(); shadowContainer.Children.InsertAtTop(shadowVisual);
            shadowClipGeometry = compositor.CreatePathGeometry(); shadowClip = compositor.CreateGeometricClip(shadowClipGeometry);
            shadowContainer.Clip = shadowClip;
            ElementCompositionPreview.SetElementChildVisual(shadowHost,shadowContainer);
        }
        // CSS outer box shadows are cut out at the owner's border contour,
        // including when its background is translucent. Clip the composition
        // shadow to the visible viewport minus that exact rounded border box.
        var device = CanvasDevice.GetSharedDevice();
        using var outer = CanvasGeometry.CreateRectangle(device,viewport);
        using var builder = new CanvasPathBuilder(device);
        var contour = BleeckerShadowGeometry.RoundedMask(ActualWidth,ActualHeight,CornerRadius,0).Figures[0];
        builder.BeginFigure(new Vector2((float)contour.StartPoint.X,(float)contour.StartPoint.Y));
        foreach(var segment in contour.Segments)
        {
            if(segment is LineSegment line) builder.AddLine(new Vector2((float)line.Point.X,(float)line.Point.Y));
            else if(segment is ArcSegment arc) builder.AddArc(new Vector2((float)arc.Point.X,(float)arc.Point.Y),
                (float)arc.Size.Width,(float)arc.Size.Height,0,CanvasSweepDirection.Clockwise,CanvasArcSize.Small);
        }
        builder.EndFigure(CanvasFigureLoop.Closed);
        using var borderBox = CanvasGeometry.CreatePath(builder);
        var outside = outer.CombineWith(borderBox,Matrix3x2.Identity,CanvasGeometryCombine.Exclude);
        shadowClipGeometry!.Path = new CompositionPath(outside);
        shadowOutside?.Dispose(); shadowOutside = outside;
        nativeShadow.Mask = shadowMask.GetAlphaMask(); nativeShadow.BlurRadius = (float)state.Blur;
        nativeShadow.Offset = new Vector3(0,(float)state.Y,0); nativeShadow.Opacity = (float)state.Alpha;
        shadowVisual!.Size = new Vector2((float)width,(float)height); shadowVisual.Offset = new Vector3((float)-state.Spread,(float)-state.Spread,0);
        shadowGeometry = next;
    }
    private void ClearShadow()
    {
        if (shadowHost is not null) ElementCompositionPreview.SetElementChildVisual(shadowHost,null);
        shadowContainer?.Dispose(); shadowVisual?.Dispose(); nativeShadow?.Dispose(); shadowClip?.Dispose(); shadowClipGeometry?.Dispose(); shadowOutside?.Dispose();
        shadowContainer = null; shadowVisual = null; nativeShadow = null; shadowClip = null; shadowClipGeometry = null; shadowOutside = null; shadowGeometry = null;
    }
    private void UpdateTypography()
    {
        if (XamlRoot is null || text is null || metrics is null || paragraph is null || baseline is null || text.Visibility != Visibility.Visible || metrics.ActualHeight <= 0) return;
        var scale = XamlRoot.RasterizationScale;
        var origin = paragraph.TransformToVisual(XamlRoot.Content).TransformPoint(new Windows.Foundation.Point()).Y;
        // Exclude the transient hover lift from the layout baseline's device snap.
        origin -= lift?.Y ?? 0;
        var lineHeight = text.LineHeight * (text.IsTextScaleFactorEnabled ? settings.TextScaleFactor : 1);
        var target = Math.Round((origin+(lineHeight-metrics.ActualHeight)/2+metrics.BaselineOffset)*scale)/scale;
        // Native glyph runs quantize their ascent before the viewport transform.
        // Use that device-pixel ascent so the final text origin also lands on the
        // device grid when a ScrollViewer changes its fractional offset.
        var nativeAscent = Math.Round(text.BaselineOffset * scale) / scale;
        var offset = target-origin-nativeAscent;
        // Preserve the paragraph's measured height while positioning the native
        // text in layout, before glyph rasterization and its pixel snapping.
        baseline.Y=0;
        if (Math.Abs(text.Margin.Top-offset) > .000001 || Math.Abs(text.Margin.Bottom+offset) > .000001)
            text.Margin=new Thickness(0,offset,0,-offset);
    }
    private void OnFrame(object? sender,object args)
    {
        UpdateTypography();
        if (renderedContrast != accessibility.HighContrast || renderedTheme != ActualTheme) UpdateAppearance(false);
        if (running) { var now = clock.Elapsed.TotalMilliseconds; Paint(now); running = IsRunning(now); }
        else if (nativeShadow is not null) UpdateShadow(shadowMotion.ValueAt(clock.Elapsed.TotalMilliseconds));
    }
    private void ObserveBrushes()
    {
        StopObserving(); if (!IsLoaded) return;
        foreach (var brush in new[] { Background,BorderBrush }.OfType<SolidColorBrush>())
            foreach (var property in new[] { SolidColorBrush.ColorProperty,Brush.OpacityProperty })
                observations.Add((brush,property,brush.RegisterPropertyChangedCallback(property,(_,_) => UpdateAppearance(true))));
        foreach (var probe in new[] { contrastBackground,contrastBorder }.OfType<TextBlock>())
            observations.Add((probe,TextBlock.ForegroundProperty,probe.RegisterPropertyChangedCallback(TextBlock.ForegroundProperty,(_,_) => UpdateAppearance(false))));
    }
    private void StopObserving() { foreach (var item in observations) item.Owner.UnregisterPropertyChangedCallback(item.Property,item.Token); observations.Clear(); }
    private void OnRootChanged(XamlRoot sender,XamlRootChangedEventArgs args) => UpdateGeometry();
    private void OnColorsChanged(UISettings sender,object args) => DispatcherQueue.TryEnqueue(() => UpdateAppearance(false));
    private void OnAnimationsChanged(UISettings sender,UISettingsAnimationsEnabledChangedEventArgs args) => DispatcherQueue.TryEnqueue(() => UpdateAppearance(false));
}
