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

// Internal source-exact renderer under validation. The default overlay has not
// switched to it: scale, small/nonuniform geometry and complete parity remain
// acceptance work. Unsupported geometry fails explicitly in this primitive.
internal sealed class BleeckerNearShadowLayer : IDisposable
{
    private readonly BleeckerOverlaySurface target;
    private readonly Vector4 tint;
    private readonly BleeckerNearShadowDevice shared;
    private readonly BleeckerCombinedShadowGraph? combined;
    private BleeckerCombinedShadowGraph.Entry? combinedEntry;
    private readonly List<IDisposable> graphResources = [];
    private readonly long cornerToken;
    private Vector2 offset;
    private Grid? host;
    private ContainerVisual? container;
    private SpriteVisual? originalNear;
    private SpriteVisual? far;
    private CompositionGeometricClip? outside;
    private CompositionClip? originalFarClip;
    private Matrix3x2 originalClipTransform;
    private bool originalNearVisible;
    private CompositionDrawingSurface? surface;
    private PixelShaderEffect? encoded;
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
    private sealed record Frame(double X, double Y, double Width, double Height, float Radius, Vector2 Offset, int DeviceGeneration, bool General);

    internal BleeckerNearShadowLayer(BleeckerOverlaySurface target, Vector2 offset, Vector4 premultipliedTint)
        : this(target, offset, premultipliedTint, null) { }

    internal BleeckerNearShadowLayer(BleeckerOverlaySurface target, Vector2 offset, Vector4 premultipliedTint, BleeckerCombinedShadowGraph? combined)
    {
        if (!target.DispatcherQueue.HasThreadAccess || !target.IsLoaded)
            throw new InvalidOperationException("Attach a near-shadow renderer to a loaded component on its UI thread.");
        ValidateOffset(offset);
        if (!float.IsFinite(premultipliedTint.X) || !float.IsFinite(premultipliedTint.Y) || !float.IsFinite(premultipliedTint.Z) || !float.IsFinite(premultipliedTint.W)
            || premultipliedTint.W < 0 || premultipliedTint.W > 1 || premultipliedTint.X < 0 || premultipliedTint.Y < 0 || premultipliedTint.Z < 0
            || premultipliedTint.X > premultipliedTint.W || premultipliedTint.Y > premultipliedTint.W || premultipliedTint.Z > premultipliedTint.W)
            throw new ArgumentOutOfRangeException(nameof(premultipliedTint));
        this.target = target;
        this.combined = combined;
        this.offset = offset;
        tint = premultipliedTint;
        shared = BleeckerNearShadowDevice.Acquire(ElementCompositionPreview.GetElementVisual(target).Compositor, target.DispatcherQueue, this);
        cornerToken = target.RegisterPropertyChangedCallback(Control.CornerRadiusProperty, OnCornerChanged);
        target.LayoutUpdated += OnLayoutUpdated;
        target.SizeChanged += OnSizeChanged;
        target.Unloaded += OnUnloaded;
        target.ShadowGraphChanged += OnShadowGraphChanged;
        try { Refresh(); }
        catch { Dispose(); throw; }
    }

    internal Vector2 Offset
    {
        get => offset;
        set { ValidateOffset(value); if (offset != value) { offset = value; RequestRefresh(); } }
    }

    private static void ValidateOffset(Vector2 value)
    {
        if (!float.IsFinite(value.X) || !float.IsFinite(value.Y)) throw new ArgumentOutOfRangeException(nameof(value));
    }

    private Frame CalculateFrame()
    {
        if (target.XamlRoot is null || target.XamlRoot.RasterizationScale != 1)
            throw new NotSupportedException("Source-exact near-shadow scaling is not yet validated beyond scale one.");
        var radius = target.CornerRadius;
        if (radius.TopLeft != radius.TopRight || radius.TopLeft != radius.BottomRight || radius.TopLeft != radius.BottomLeft
            || !double.IsFinite(radius.TopLeft) || radius.TopLeft < 0 || radius.TopLeft > (int.MaxValue - 85) / 2)
            throw new NotSupportedException("Source-exact near-shadow geometry requires uniform nonnegative finite corners.");
        var root = target.XamlRoot.Content;
        var transform = target.TransformToVisual(root);
        var point = transform.TransformPoint(new Point(0, 0));
        var xAxis = transform.TransformPoint(new Point(1, 0));
        var yAxis = transform.TransformPoint(new Point(0, 1));
        if (xAxis.X - point.X != 1 || xAxis.Y != point.Y || yAxis.X != point.X || yAxis.Y - point.Y != 1)
            throw new NotSupportedException("Source-exact near-shadow transformed scaling/rotation is not yet validated.");
        var width = Snap(point.X + target.ActualWidth) - Snap(point.X);
        var height = Snap(point.Y + target.ActualHeight) - Snap(point.Y);
        if (width <= 0 || height <= 0)
            throw new NotSupportedException("Source-exact near-shadow bounds must cover at least one physical pixel per axis.");
        var sourceRadius = MathF.Min((float)radius.TopLeft, MathF.Min(width, height) / 2);
        var kernelRadius = MathF.Ceiling(sourceRadius);
        var general = sourceRadius == 0 || width < kernelRadius * 2 + 43 || height < kernelRadius * 2 + 43;
        if (general && sourceRadius != 0 && width <= 64 && height <= 64 && !(width == height && sourceRadius == width / 2))
            throw new NotSupportedException("The source's small software-mask path is not yet validated in the GPU renderer.");
        return new(point.X, point.Y, target.ActualWidth, target.ActualHeight, sourceRadius, offset, shared.Generation, general);
    }

    private static float Snap(double value) => (float)Math.Floor(value + .5);
    private static Grid? FindHostOrNull(DependencyObject parent)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is Grid grid && grid.Name == "PART_ShadowHost") return grid;
            var found = FindHostOrNull(child);
            if (found is not null) return found;
        }
        return null;
    }

    private void BindGraph(Grid nextHost, ContainerVisual nextContainer)
    {
        var entry = combined?.Acquire(nextContainer, BleeckerCombinedShadowGraph.Slot.Near);
        var layers = entry is null ? nextContainer.Children.Cast<SpriteVisual>().ToArray() : new[] { entry.Near, entry.Far };
        var clip = entry?.Outside ?? nextContainer.Clip as CompositionGeometricClip;
        if (layers.Length != 2 || clip is null)
            throw new InvalidOperationException("Near-shadow attachment requires the original two-layer overlay graph.");
        combinedEntry = entry;
        host = nextHost;
        container = nextContainer;
        originalNear = layers[0];
        far = layers[1];
        outside = clip;
        originalFarClip = far.Clip;
        originalClipTransform = clip.TransformMatrix;
        originalNearVisible = originalNear.IsVisible;
        T Own<T>(T resource) where T : IDisposable { graphResources.Add(resource); return resource; }
        surface = Own(shared.Graphics.CreateDrawingSurface(new Size(1, 1), Microsoft.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized, Microsoft.Graphics.DirectX.DirectXAlphaMode.Premultiplied));
        encoded = Own(BleeckerNearShadowDevice.Shader("encode2"));
        var source = Own(ElementCompositionPreview.GetElementVisual(target).Compositor.CreateSurfaceBrush(surface));
        source.Stretch = CompositionStretch.Fill;
        var matrix = new Matrix5x4
        {
            M11 = tint.X, M12 = tint.Y, M13 = tint.Z, M14 = tint.W,
            M21 = tint.X / 255, M22 = tint.Y / 255, M23 = tint.Z / 255, M24 = tint.W / 255,
            M31 = tint.X / 65025, M32 = tint.Y / 65025, M33 = tint.Z / 65025, M34 = tint.W / 65025
        };
        var effect = Own(new ColorMatrixEffect
        {
            AlphaMode = CanvasAlphaMode.Straight, ClampOutput = false, ColorMatrix = matrix,
            BufferPrecision = CanvasBufferPrecision.Precision32Float, Source = new CompositionEffectSourceParameter("mask")
        });
        var compositor = ElementCompositionPreview.GetElementVisual(target).Compositor;
        var factory = Own(compositor.CreateEffectFactory(effect));
        brush = Own(factory.CreateBrush());
        brush.SetSourceParameter("mask", source);
        sprite = Own(compositor.CreateSpriteVisual());
        sprite.Brush = brush;
        if (combinedEntry is not null) combinedEntry.Attach(BleeckerCombinedShadowGraph.Slot.Near, sprite);
        else
        {
            originalNear.IsVisible = false;
            container.Clip = null;
            far.Clip = outside;
            container.Children.InsertAtBottom(sprite);
        }
        graphGenerations++;
        previous = null;
    }

    internal void RequestRefresh()
    {
        if (disposed || queued) return;
        queued = true;
        if (!target.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            queued = false;
            if (!disposed) Refresh();
        })) throw new InvalidOperationException("Near-shadow refresh dispatcher is unavailable.");
    }

    private void Refresh()
    {
        if (!target.IsLoaded) return;
        if (target.ActualWidth <= 0 || target.ActualHeight <= 0)
        {
            ReleaseGraph(CanRestoreGraph());
            return;
        }
        var nextHost = FindHostOrNull(target);
        var child = nextHost is null ? null : ElementCompositionPreview.GetElementChildVisual(nextHost);
        if (nextHost is null || nextHost.Visibility != Visibility.Visible || child is null)
        {
            ReleaseGraph(CanRestoreGraph());
            return;
        }
        var frame = CalculateFrame();
        var nextContainer = child as ContainerVisual
            ?? throw new InvalidOperationException("The overlay shadow graph has an unexpected visual type.");
        if (!ReferenceEquals(container, nextContainer))
        {
            ReleaseGraph(false);
            BindGraph(nextHost, nextContainer);
        }
        if (previous == frame) { skipped++; return; }
        var left = Snap(frame.X);
        var top = Snap(frame.Y);
        var width = Snap(frame.X + frame.Width) - left;
        var height = Snap(frame.Y + frame.Height) - top;
        var textureLeft = Snap(left + frame.Offset.X) - 21;
        var textureTop = Snap(top + frame.Offset.Y) - 21;
        var textureWidth = width + 42;
        var textureHeight = height + 42;
        var kernelRadius = (int)MathF.Ceiling(frame.Radius);
        var rectangle = frame.Radius == 0;
        encoded!.Source1 = rectangle ? shared.RectangleKernel(this) : frame.General ? shared.ActualKernel(this, (int)width, (int)height, frame.Radius) : shared.Kernel(this, kernelRadius);
        encoded.Source1Interpolation = rectangle ? CanvasImageInterpolation.Linear : CanvasImageInterpolation.NearestNeighbor;
        encoded.Source1BorderMode = EffectBorderMode.Hard;
        encoded.Source1Mapping = SamplerCoordinateMapping.Unknown;
        encoded.Properties["Radius"] = (float)frame.Radius;
        encoded.Properties["Dimension"] = (float)(kernelRadius * 2 + 85);
        var circle = frame.General && width == height && frame.Radius == width / 2;
        encoded.Properties["General"] = rectangle ? 3f : circle ? 2f : frame.General ? 1f : 0f;
        encoded.Properties["ProfileRadius"] = circle ? BleeckerNearShadowDevice.CircleTextureRadius(frame.Radius) : 0f;
        encoded.Properties["Box"] = new Vector4(left - textureLeft, top - textureTop, width, height);
        encoded.Properties["CasterBox"] = new Vector4(left + frame.Offset.X - textureLeft, top + frame.Offset.Y - textureTop, width, height);
        if (surface!.Size.Width != textureWidth || surface.Size.Height != textureHeight)
            CanvasComposition.Resize(surface, new Size(textureWidth, textureHeight));
        using (var draw = CanvasComposition.CreateDrawingSession(surface))
        {
            draw.Clear(Microsoft.UI.Colors.Transparent);
            draw.EffectBufferPrecision = CanvasBufferPrecision.Precision8UIntNormalized;
            draw.Blend = CanvasBlend.Copy;
            draw.DrawImage(encoded, Vector2.Zero, new Rect(0, 0, textureWidth, textureHeight));
        }
        sprite!.Size = new(textureWidth, textureHeight);
        sprite.Offset = new(textureLeft - (float)frame.X, textureTop - (float)frame.Y, 0);
        if (combinedEntry is not null) combinedEntry.UpdateClips();
        else outside!.TransformMatrix = Matrix3x2.CreateTranslation(-far!.Offset.X, -far.Offset.Y);
        previous = frame;
        updates++;
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
            combinedEntry.Release(BleeckerCombinedShadowGraph.Slot.Near, sprite, restore);
            combinedEntry = null;
        }
        else if (restore && container is not null && sprite is not null)
        {
            if (ReferenceEquals(sprite.Parent, container)) container.Children.Remove(sprite);
            container.Clip = outside;
            far!.Clip = originalFarClip;
            outside!.TransformMatrix = originalClipTransform;
            originalNear!.IsVisible = originalNearVisible;
        }
        for (var i = graphResources.Count - 1; i >= 0; i--) graphResources[i].Dispose();
        graphResources.Clear();
        shared.ReleaseKernel(this);
        host = null; container = null; originalNear = null; far = null; outside = null;
        originalFarClip = null; surface = null; encoded = null; brush = null; sprite = null; previous = null;
    }

    internal object Snapshot() => new
    {
        disposed, queued, updates, skipped, graphGenerations, layoutEvents, sizeEvents, cornerEvents,
        general = previous?.General ?? false,
        graphResourceCount = graphResources.Count, shared = shared.Snapshot()
    };

    internal int GraphResourceCount => graphResources.Count;
    internal bool RefreshQueued => queued;

    private bool CanRestoreGraph() => target.IsLoaded && host is not null
        && ReferenceEquals(ElementCompositionPreview.GetElementChildVisual(host), container);
    public void Dispose() => DisposeCore(CanRestoreGraph());
    private void DisposeCore(bool restore)
    {
        if (disposed) return;
        disposed = true;
        target.LayoutUpdated -= OnLayoutUpdated;
        target.SizeChanged -= OnSizeChanged;
        target.Unloaded -= OnUnloaded;
        target.ShadowGraphChanged -= OnShadowGraphChanged;
        target.UnregisterPropertyChangedCallback(Control.CornerRadiusProperty, cornerToken);
        ReleaseGraph(restore);
        shared.Release(this);
    }
}
