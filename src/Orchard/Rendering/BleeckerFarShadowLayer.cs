using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Composition;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

// Internal far-layer candidate. Rectangular spread casters are implemented;
// rounded casters, transforms and other scaling remain explicit acceptance work.
// Attachment requires the original default graph and does not change the public
// renderer. A shared attachment for both exact layers is still required.
internal sealed class BleeckerFarShadowLayer : IDisposable
{
    private readonly BleeckerOverlaySurface target;
    private readonly Vector4 tint;
    private readonly BleeckerFarShadowDevice shared;
    private readonly BleeckerCombinedShadowGraph? combined;
    private BleeckerCombinedShadowGraph.Entry? combinedEntry;
    private readonly Vector2 offset;
    private readonly float sigma;
    private readonly float spread;
    private readonly List<IDisposable> graphResources = [];
    private readonly long cornerToken;
    private Grid? host;
    private ContainerVisual? container;
    private SpriteVisual? originalFar;
    private SpriteVisual? near;
    private CompositionGeometricClip? outside;
    private CompositionClip? originalNearClip;
    private Matrix3x2 originalClipTransform;
    private bool originalFarVisible;
    private CompositionDrawingSurface? surface;
    private readonly List<CompositionDrawingSurface> planes = [];
    private PixelShaderEffect? shader;
    private CompositionEffectBrush? brush;
    private SpriteVisual? sprite;
    private Frame? previous;
    private bool queued;
    private bool disposed;
    private int updates;
    private int skipped;
    private int graphGenerations;
    private int layoutEvents;
    private int sizeEvents;
    private int cornerEvents;
    private sealed record Frame(double X, double Y, double Width, double Height, float Radius, int DeviceGeneration);

    internal BleeckerFarShadowLayer(BleeckerOverlaySurface target, Vector4 premultipliedTint)
        : this(target, premultipliedTint, null) { }

    internal BleeckerFarShadowLayer(BleeckerOverlaySurface target, Vector4 premultipliedTint, BleeckerCombinedShadowGraph? combined)
    {
        if (!target.DispatcherQueue.HasThreadAccess || !target.IsLoaded)
            throw new InvalidOperationException("Attach a far-shadow renderer to a loaded component on its UI thread.");
        if (!float.IsFinite(premultipliedTint.X) || !float.IsFinite(premultipliedTint.Y) || !float.IsFinite(premultipliedTint.Z) || !float.IsFinite(premultipliedTint.W)
            || premultipliedTint.W < 0 || premultipliedTint.W > 1 || premultipliedTint.X < 0 || premultipliedTint.Y < 0 || premultipliedTint.Z < 0
            || premultipliedTint.X > premultipliedTint.W || premultipliedTint.Y > premultipliedTint.W || premultipliedTint.Z > premultipliedTint.W)
            throw new ArgumentOutOfRangeException(nameof(premultipliedTint));
        var recipe = BleeckerTokens.OverlayShadows[0];
        sigma = recipe.Blur / 2; spread = recipe.Spread; offset = new(recipe.X, recipe.Y);
        if (sigma != 32 || spread != -20 || offset != new Vector2(0, 24))
            throw new NotSupportedException("Far-shadow source parity is currently validated against the canonical overlay recipe.");
        this.target = target; tint = premultipliedTint; this.combined = combined;
        shared = BleeckerFarShadowDevice.Acquire(ElementCompositionPreview.GetElementVisual(target).Compositor, target.DispatcherQueue, this);
        cornerToken = target.RegisterPropertyChangedCallback(Control.CornerRadiusProperty, OnCornerChanged);
        target.LayoutUpdated += OnLayoutUpdated; target.SizeChanged += OnSizeChanged;
        target.Unloaded += OnUnloaded; target.ShadowGraphChanged += OnShadowGraphChanged;
        try { Refresh(); } catch { Dispose(); throw; }
    }

    private static float Snap(double value) => (float)Math.Floor(value + .5);
    private Frame CalculateFrame()
    {
        if (target.XamlRoot is null || target.XamlRoot.RasterizationScale != 1)
            throw new NotSupportedException("Source-exact far-shadow scaling is not validated beyond scale one.");
        var radius = target.CornerRadius;
        if (radius.TopLeft != radius.TopRight || radius.TopLeft != radius.BottomRight || radius.TopLeft != radius.BottomLeft
            || !double.IsFinite(radius.TopLeft) || radius.TopLeft < 0 || radius.TopLeft > -spread)
            throw new NotSupportedException("Far-shadow rectangle rendering requires uniform finite corners erased by the negative spread.");
        var transform = target.TransformToVisual(target.XamlRoot.Content);
        var point = transform.TransformPoint(new Point(0, 0));
        var x = transform.TransformPoint(new Point(1, 0)); var y = transform.TransformPoint(new Point(0, 1));
        if (x.X - point.X != 1 || x.Y != point.Y || y.X != point.X || y.Y - point.Y != 1)
            throw new NotSupportedException("Source-exact far-shadow transformed geometry is not validated.");
        return new(point.X, point.Y, target.ActualWidth, target.ActualHeight, (float)radius.TopLeft, shared.Generation);
    }

    private static Grid? FindHostOrNull(DependencyObject parent)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is Grid grid && grid.Name == "PART_ShadowHost") return grid;
            var found = FindHostOrNull(child); if (found is not null) return found;
        }
        return null;
    }

    private void BindGraph(Grid nextHost, ContainerVisual nextContainer)
    {
        var entry = combined?.Acquire(nextContainer, BleeckerCombinedShadowGraph.Slot.Far);
        var layers = entry is null ? nextContainer.Children.Cast<SpriteVisual>().ToArray() : new[] { entry.Near, entry.Far };
        var clip = entry?.Outside ?? nextContainer.Clip as CompositionGeometricClip;
        if (layers.Length != 2 || clip is null)
            throw new InvalidOperationException("Far-shadow attachment requires the original default two-layer overlay graph.");
        combinedEntry = entry;
        host = nextHost; container = nextContainer; near = layers[0]; originalFar = layers[1]; outside = clip;
        originalNearClip = near.Clip; originalClipTransform = clip.TransformMatrix; originalFarVisible = originalFar.IsVisible;
        T Own<T>(T resource) where T : IDisposable { graphResources.Add(resource); return resource; }
        var compositor = ElementCompositionPreview.GetElementVisual(target).Compositor;
        var sources = new List<CompositionSurfaceBrush>(3);
        for (var plane = 0; plane < 3; plane++)
        {
            var item = Own(shared.Graphics.CreateDrawingSurface(new Size(1, 1), Microsoft.Graphics.DirectX.DirectXPixelFormat.R16G16B16A16Float, Microsoft.Graphics.DirectX.DirectXAlphaMode.Premultiplied));
            planes.Add(item);
            var source = Own(compositor.CreateSurfaceBrush(item)); source.Stretch = CompositionStretch.Fill;
            source.BitmapInterpolationMode = CompositionBitmapInterpolationMode.NearestNeighbor; sources.Add(source);
        }
        surface = planes[0];
        shader = Own(BleeckerFarShadowDevice.Shader("rectangle"));
        var coarse = Own(new ArithmeticCompositeEffect { Source1 = new CompositionEffectSourceParameter("plane0"), Source2 = new CompositionEffectSourceParameter("plane1"), MultiplyAmount = 0, Source1Amount = 1, Source2Amount = 1f / 2048, Offset = 0, ClampOutput = true, BufferPrecision = CanvasBufferPrecision.Precision32Float });
        var coverage = Own(new ArithmeticCompositeEffect { Source1 = coarse, Source2 = new CompositionEffectSourceParameter("plane2"), MultiplyAmount = 0, Source1Amount = 1, Source2Amount = 1f / 4194304, Offset = 0, ClampOutput = true, BufferPrecision = CanvasBufferPrecision.Precision32Float });
        var paint = new Matrix5x4 { M41 = tint.X, M42 = tint.Y, M43 = tint.Z, M44 = tint.W };
        var effect = Own(new ColorMatrixEffect { AlphaMode = CanvasAlphaMode.Straight, ClampOutput = false, ColorMatrix = paint, BufferPrecision = CanvasBufferPrecision.Precision32Float, Source = coverage });
        var factory = Own(compositor.CreateEffectFactory(effect)); brush = Own(factory.CreateBrush());
        for (var plane = 0; plane < sources.Count; plane++) brush.SetSourceParameter("plane" + plane, sources[plane]);
        sprite = Own(compositor.CreateSpriteVisual()); sprite.Brush = brush;
        if (combinedEntry is not null) combinedEntry.Attach(BleeckerCombinedShadowGraph.Slot.Far, sprite);
        else
        {
            originalFar.IsVisible = false; container.Clip = null; near.Clip = outside;
            container.Children.InsertAtBottom(sprite);
        }
        graphGenerations++; previous = null;
    }

    internal void RequestRefresh()
    {
        if (disposed || queued) return;
        queued = true;
        if (!target.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            queued = false; if (!disposed) Refresh();
        })) throw new InvalidOperationException("Far-shadow refresh dispatcher is unavailable.");
    }

    private void Refresh()
    {
        if (!target.IsLoaded) return;
        if (target.ActualWidth <= 0 || target.ActualHeight <= 0) { ReleaseGraph(CanRestoreGraph()); return; }
        var nextHost = FindHostOrNull(target);
        var child = nextHost is null ? null : ElementCompositionPreview.GetElementChildVisual(nextHost);
        if (nextHost is null || nextHost.Visibility != Visibility.Visible || child is null) { ReleaseGraph(CanRestoreGraph()); return; }
        var frame = CalculateFrame();
        var nextContainer = child as ContainerVisual ?? throw new InvalidOperationException("Unexpected overlay shadow graph type.");
        if (!ReferenceEquals(container, nextContainer)) { ReleaseGraph(false); BindGraph(nextHost, nextContainer); }
        if (previous == frame) { skipped++; return; }
        var left = Snap(frame.X); var top = Snap(frame.Y);
        var width = Snap(frame.X + frame.Width) - left; var height = Snap(frame.Y + frame.Height) - top;
        var casterLeft = left - spread + offset.X; var casterTop = top - spread + offset.Y;
        var casterWidth = width + 2 * spread; var casterHeight = height + 2 * spread;
        // CSS floors contracted dimensions at zero. An empty caster contributes
        // no shadow; retain the loaded graph so a later resize can restore it.
        var emptyCaster = casterWidth <= 0 || casterHeight <= 0;
        var extent = MathF.Ceiling(3 * sigma);
        var textureLeft = casterLeft - extent; var textureTop = casterTop - extent;
        var textureWidth = emptyCaster ? 1 : casterWidth + 2 * extent;
        var textureHeight = emptyCaster ? 1 : casterHeight + 2 * extent;
        shader!.Source1 = shared.Table;
        shader.Source1Interpolation = CanvasImageInterpolation.Linear; shader.Source1BorderMode = EffectBorderMode.Hard;
        shader.Source1Mapping = SamplerCoordinateMapping.Unknown;
        shader.Properties["Box"] = new Vector4(left - textureLeft, top - textureTop, width, height);
        shader.Properties["Caster"] = new Vector4(extent, extent, casterWidth, casterHeight);
        shader.Properties["Radius"] = frame.Radius;
        shader.Properties["Sigma"] = sigma; shader.Properties["Dimension"] = (float)shared.Dimension;
        shader.Properties["Fast"] = casterWidth >= 6 * sigma && casterHeight >= 6 * sigma ? 1f : 0f;
        for (var plane = 0; plane < planes.Count; plane++)
        {
            var item = planes[plane];
            shader.Properties["Plane"] = (float)plane;
            if (item.Size.Width != textureWidth || item.Size.Height != textureHeight) CanvasComposition.Resize(item, new Size(textureWidth, textureHeight));
            using var drawing = CanvasComposition.CreateDrawingSession(item);
            drawing.Clear(Microsoft.UI.Colors.Transparent); drawing.Blend = CanvasBlend.Copy;
            drawing.EffectBufferPrecision = CanvasBufferPrecision.Precision32Float;
            if (!emptyCaster) drawing.DrawImage(shader, Vector2.Zero, new Rect(0, 0, textureWidth, textureHeight));
        }
        sprite!.Size = new(textureWidth, textureHeight); sprite.Offset = new(textureLeft - (float)frame.X, textureTop - (float)frame.Y, 0);
        if (combinedEntry is not null) combinedEntry.UpdateClips();
        else outside!.TransformMatrix = Matrix3x2.CreateTranslation(-near!.Offset.X, -near.Offset.Y);
        previous = frame; updates++;
    }

    private void OnLayoutUpdated(object? sender, object args) { layoutEvents++; RequestRefresh(); }
    private void OnSizeChanged(object sender, SizeChangedEventArgs args) { sizeEvents++; RequestRefresh(); }
    private void OnCornerChanged(DependencyObject sender, DependencyProperty property) { cornerEvents++; RequestRefresh(); }
    private void OnShadowGraphChanged(object? sender, EventArgs args) => RequestRefresh();
    private void OnUnloaded(object sender, RoutedEventArgs args) => DisposeCore(false);
    internal void InvalidateGeneratedResources() => shared.InvalidateGeneratedResources();

    private void ReleaseGraph(bool restore)
    {
        if (combinedEntry is not null)
        {
            combinedEntry.Release(BleeckerCombinedShadowGraph.Slot.Far, sprite, restore);
            combinedEntry = null;
        }
        else if (restore && container is not null && sprite is not null)
        {
            if (ReferenceEquals(sprite.Parent, container)) container.Children.Remove(sprite);
            container.Clip = outside; near!.Clip = originalNearClip;
            outside!.TransformMatrix = originalClipTransform; originalFar!.IsVisible = originalFarVisible;
        }
        for (var i = graphResources.Count - 1; i >= 0; i--) graphResources[i].Dispose();
        graphResources.Clear(); planes.Clear(); host = null; container = null; originalFar = null; near = null; outside = null;
        originalNearClip = null; surface = null; shader = null; brush = null; sprite = null; previous = null;
    }

    internal object Snapshot() => new
    {
        disposed, queued, updates, skipped, graphGenerations, layoutEvents, sizeEvents, cornerEvents,
        emptyCaster = previous is not null && (Snap(previous.X + previous.Width) - Snap(previous.X) + 2 * spread <= 0
            || Snap(previous.Y + previous.Height) - Snap(previous.Y) + 2 * spread <= 0),
        graphResourceCount = graphResources.Count, planeCount = planes.Count, shared = shared.Snapshot()
    };
    internal int GraphResourceCount => graphResources.Count;
    internal bool RefreshQueued => queued;
    private bool CanRestoreGraph() => target.IsLoaded && host is not null && ReferenceEquals(ElementCompositionPreview.GetElementChildVisual(host), container);
    public void Dispose() => DisposeCore(CanRestoreGraph());
    private void DisposeCore(bool restore)
    {
        if (disposed) return;
        disposed = true;
        target.LayoutUpdated -= OnLayoutUpdated; target.SizeChanged -= OnSizeChanged;
        target.Unloaded -= OnUnloaded; target.ShadowGraphChanged -= OnShadowGraphChanged;
        target.UnregisterPropertyChangedCallback(Control.CornerRadiusProperty, cornerToken);
        ReleaseGraph(restore); shared.Release(this);
    }
}
