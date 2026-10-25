using System.Collections.Concurrent;
using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Composition;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Windows.Foundation;
using Windows.Graphics.DirectX;

namespace Orchard;

// Shared only by owners on the compositor's UI thread. The shared CanvasDevice
// belongs to Win2D; the last owner releases our textures and graphics wrapper.
internal sealed class BleeckerNearShadowDevice : IDisposable
{
    [ThreadStatic] private static Dictionary<Compositor, BleeckerNearShadowDevice>? devices;
    private static readonly ConcurrentDictionary<string, byte[]> shaderBytes = new();
    private readonly Compositor compositor;
    private readonly DispatcherQueue dispatcher;
    private readonly HashSet<BleeckerNearShadowLayer> owners = [];
    private readonly record struct KernelShape(int Radius, int Width, int Height, float ActualRadius);
    private readonly Dictionary<KernelShape, CanvasRenderTarget> kernels = [];
    private readonly Dictionary<BleeckerNearShadowLayer, KernelShape> leases = [];
    private readonly Dictionary<KernelShape, int> radiusOwners = [];
    private CanvasDevice device;
    private int recoveryQueued;
    private volatile bool disposed;
    private bool deviceLostSubscribed;
    internal CompositionGraphicsDevice Graphics { get; }
    internal int Generation { get; private set; }

    private BleeckerNearShadowDevice(Compositor compositor, DispatcherQueue dispatcher)
    {
        this.compositor = compositor;
        this.dispatcher = dispatcher;
        device = CanvasDevice.GetSharedDevice();
        if (!device.IsBufferPrecisionSupported(CanvasBufferPrecision.Precision32Float))
            throw new NotSupportedException("Near-shadow composition requires float32 effect precision.");
        Graphics = CanvasComposition.CreateCompositionGraphicsDevice(compositor, device);
        device.DeviceLost += OnDeviceLost;
        deviceLostSubscribed = true;
    }

    internal static BleeckerNearShadowDevice Acquire(Compositor compositor, DispatcherQueue dispatcher, BleeckerNearShadowLayer owner)
    {
        if (!dispatcher.HasThreadAccess) throw new InvalidOperationException("Near-shadow ownership requires the compositor thread.");
        devices ??= [];
        if (!devices.TryGetValue(compositor, out var shared))
        {
            shared = new(compositor, dispatcher);
            devices.Add(compositor, shared);
        }
        if (!shared.owners.Add(owner)) throw new InvalidOperationException("Duplicate near-shadow owner.");
        return shared;
    }

    internal void Release(BleeckerNearShadowLayer owner)
    {
        if (!dispatcher.HasThreadAccess) throw new InvalidOperationException("Near-shadow release requires the compositor thread.");
        ReleaseKernel(owner);
        if (!owners.Remove(owner)) throw new InvalidOperationException("Unknown near-shadow owner.");
        if (owners.Count == 0) Dispose();
    }

    internal CanvasRenderTarget Kernel(BleeckerNearShadowLayer owner, int radius) => Kernel(owner, new KernelShape(radius, 0, 0, 0));

    internal CanvasRenderTarget ActualKernel(BleeckerNearShadowLayer owner, int width, int height, float radius)
        => Kernel(owner, new KernelShape((int)MathF.Ceiling(radius), width, height, radius));

    internal CanvasRenderTarget RectangleKernel(BleeckerNearShadowLayer owner) => Kernel(owner, new KernelShape(0, 0, 0, 0));

    private CanvasRenderTarget Kernel(BleeckerNearShadowLayer owner, KernelShape radius)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (!owners.Contains(owner)) throw new InvalidOperationException("Unknown near-shadow owner.");
        if (!kernels.TryGetValue(radius, out var kernel))
        {
            kernel = radius.Radius == 0 ? CreateRectangleKernel() : radius.Width == 0 ? CreateKernel(radius.Radius) : radius.Width == radius.Height && radius.ActualRadius == radius.Width / 2f ? CreateCircleKernel(radius.ActualRadius) : CreateActualKernel(radius);
            kernels.Add(radius, kernel);
        }
        if (!leases.TryGetValue(owner, out var previous) || previous != radius)
        {
            ReleaseKernel(owner);
            leases.Add(owner, radius);
            radiusOwners[radius] = radiusOwners.GetValueOrDefault(radius) + 1;
        }
        return kernel;
    }

    internal void ReleaseKernel(BleeckerNearShadowLayer owner)
    {
        if (!leases.Remove(owner, out var radius)) return;
        var remaining = radiusOwners[radius] - 1;
        if (remaining > 0) radiusOwners[radius] = remaining;
        else
        {
            radiusOwners.Remove(radius);
            if (kernels.Remove(radius, out var kernel)) kernel.Dispose();
        }
    }

    internal static PixelShaderEffect Shader(string name, ICanvasImage? source = null)
    {
        var bytes = shaderBytes.GetOrAdd(name, static key =>
        {
            using var input = typeof(BleeckerNearShadowDevice).Assembly.GetManifestResourceStream($"Orchard.NearShadow.{key}.bin")
                ?? throw new InvalidOperationException($"Missing embedded near-shadow shader: {key}.");
            using var memory = new MemoryStream();
            input.CopyTo(memory);
            return memory.ToArray();
        });
        var shader = new PixelShaderEffect(bytes) { BufferPrecision = CanvasBufferPrecision.Precision8UIntNormalized };
        if (source is not null)
        {
            shader.Source1 = source;
            shader.Source1Interpolation = CanvasImageInterpolation.Linear;
            shader.Source1BorderMode = EffectBorderMode.Hard;
            shader.Source1Mapping = SamplerCoordinateMapping.Unknown;
        }
        return shader;
    }

    private CanvasRenderTarget CreateKernel(int radius)
    {
        var dimension = checked(radius * 2 + 85);
        var reduced = (int)Math.Floor(dimension * 4d / 7);
        var padded = reduced + 2;
        CanvasRenderTarget Target(int size) => new(device, size, size, 96, DirectXPixelFormat.B8G8R8A8UIntNormalized, CanvasAlphaMode.Premultiplied);
        static void Draw(CanvasRenderTarget target, ICanvasImage image, Rect? clip = null)
        {
            using var draw = target.CreateDrawingSession();
            draw.Clear(Microsoft.UI.Colors.Transparent);
            draw.EffectBufferPrecision = CanvasBufferPrecision.Precision8UIntNormalized;
            draw.Blend = CanvasBlend.Copy;
            var region = clip ?? new Rect(0, 0, target.SizeInPixels.Width, target.SizeInPixels.Height);
            draw.DrawImage(image, new Vector2((float)region.X, (float)region.Y), region);
        }
        using var input = Target(dimension);
        using var caster = Shader("caster");
        caster.Properties["Box"] = new Vector4(21, 21, radius * 2 + 43, radius * 2 + 43);
        caster.Properties["Radius"] = (float)radius;
        Draw(input, caster);
        using var small = Target(padded);
        using var resize = Shader("mapping", input);
        resize.Properties["Scale"] = new Vector2(dimension / (float)reduced);
        resize.Properties["Offset"] = new Vector2(-dimension / (float)reduced);
        Draw(small, resize, new Rect(1, 1, reduced, reduced));
        var sigma = 7f * (reduced / (float)dimension);
        var kernelRadius = (int)MathF.Ceiling(3f * sigma);
        if (kernelRadius != 12) throw new NotSupportedException("Unexpected near-shadow Gaussian kernel size.");
        var weights = new float[25];
        float sum = 0, denominator = 1f / ((2f * sigma) * sigma);
        for (var i = 0; i < weights.Length; i++)
        {
            var distance = i - 12;
            weights[i] = MathF.Exp(-(distance * distance * denominator));
            sum += weights[i];
        }
        var normalization = 1f / sum;
        for (var i = 0; i < weights.Length; i++) weights[i] *= normalization;
        var taps = new List<Vector2>(13);
        void Pair(int index)
        {
            var weight = weights[index + 12] + weights[index + 13];
            taps.Add(new(index + weights[index + 13] / weight, weight));
        }
        for (var i = -12; i < 0; i += 2) Pair(i);
        taps.Add(new(0, weights[12]));
        for (var i = 1; i <= 12; i += 2) Pair(i);
        void Configure(PixelShaderEffect shader, Vector2 direction)
        {
            shader.Source1Mapping = SamplerCoordinateMapping.Offset;
            shader.MaxSamplerOffset = 12;
            shader.Properties["Direction"] = direction;
            for (var i = 0; i < taps.Count; i++) shader.Properties["Tap" + i] = taps[i];
        }
        using var x = Target(padded);
        using var horizontal = Shader("gaussian", small);
        Configure(horizontal, new(1, 0));
        Draw(x, horizontal);
        using var xy = Target(padded);
        using var vertical = Shader("gaussian", x);
        Configure(vertical, new(0, 1));
        Draw(xy, vertical);
        var cache = Target(dimension);
        try
        {
            using var expand = Shader("mapping", xy);
            expand.Properties["Scale"] = new Vector2(reduced / (float)dimension);
            expand.Properties["Offset"] = Vector2.One;
            Draw(cache, expand);
            return cache;
        }
        catch { cache.Dispose(); throw; }
    }

    private CanvasRenderTarget CreateRectangleKernel()
    {
        var cache = new CanvasRenderTarget(device, 128, 1, 96, DirectXPixelFormat.B8G8R8A8UIntNormalized, CanvasAlphaMode.Premultiplied);
        try
        {
            using var profile = Shader("rectangle-profile");
            using var drawing = cache.CreateDrawingSession(); drawing.Clear(Microsoft.UI.Colors.Transparent);
            drawing.EffectBufferPrecision = CanvasBufferPrecision.Precision8UIntNormalized; drawing.Blend = CanvasBlend.Copy;
            drawing.DrawImage(profile, Vector2.Zero, new Rect(0, 0, 128, 1));
            return cache;
        }
        catch { cache.Dispose(); throw; }
    }

    internal static float CircleTextureRadius(float radius)
    {
        var ratio = MathF.Min(7f / radius, 8f);
        ratio = ((int)(ratio * 65536) & ~255) / 65536f;
        return radius + 3f * (radius * ratio);
    }

    private CanvasRenderTarget CreateCircleKernel(float radius)
    {
        var textureRadius = CircleTextureRadius(radius);
        var ratio = ((int)(MathF.Min(7f / radius, 8f) * 65536) & ~255) / 65536f;
        var sigma = radius * ratio;
        var scale = 512f / textureRadius;
        sigma *= scale;
        var halfCount = (((int)MathF.Ceiling(6f * sigma) + 1) & ~1) >> 1;
        if (halfCount <= 0 || halfCount > 512) throw new NotSupportedException("Unexpected circle-profile kernel size.");
        var coefficients = new Vector2[512];
        var invSigma = 1f / sigma; var denominator = (-.5f * invSigma) * invSigma; float total = 0;
        for (var i = 0; i < halfCount; i++)
        {
            var distance = i + .5f;
            var weight = MathF.Exp((distance * distance) * denominator);
            coefficients[i].X = weight; total += weight;
        }
        total *= 2f; float sum = 0;
        for (var i = 0; i < halfCount; i++) { coefficients[i].X /= total; sum += coefficients[i].X; coefficients[i].Y = sum; }
        var cache = new CanvasRenderTarget(device, 512, 1, 96, DirectXPixelFormat.B8G8R8A8UIntNormalized, CanvasAlphaMode.Premultiplied);
        try
        {
            using var profile = Shader("circle-profile");
            profile.Properties["Coefficients"] = coefficients;
            profile.Properties["Count"] = halfCount;
            profile.Properties["CircleRadius"] = radius * scale;
            using var drawing = cache.CreateDrawingSession(); drawing.Clear(Microsoft.UI.Colors.Transparent);
            drawing.EffectBufferPrecision = CanvasBufferPrecision.Precision8UIntNormalized; drawing.Blend = CanvasBlend.Copy;
            drawing.DrawImage(profile, Vector2.Zero, new Rect(0, 0, 512, 1));
            return cache;
        }
        catch { cache.Dispose(); throw; }
    }

    private CanvasRenderTarget CreateActualKernel(KernelShape shape)
    {
        static int Backing(int size)
        {
            var result = 1;
            while (result < size) result = checked(result * 2);
            return result;
        }
        var width = Math.Min(Backing(checked(shape.Width + 42)), checked(shape.Width + 63)); var height = Math.Min(Backing(checked(shape.Height + 42)), checked(shape.Height + 63));
        var reducedWidth = (int)Math.Floor(width * 4d / 7); var reducedHeight = (int)Math.Floor(height * 4d / 7);
        var paddedWidth = reducedWidth + 2; var paddedHeight = reducedHeight + 2;
        CanvasRenderTarget Target(int w, int h) => new(device, w, h, 96, DirectXPixelFormat.B8G8R8A8UIntNormalized, CanvasAlphaMode.Premultiplied);
        static void Draw(CanvasRenderTarget target, ICanvasImage image, Rect? region = null)
        {
            using var drawing = target.CreateDrawingSession(); drawing.Clear(Microsoft.UI.Colors.Transparent);
            drawing.EffectBufferPrecision = CanvasBufferPrecision.Precision8UIntNormalized; drawing.Blend = CanvasBlend.Copy;
            var clip = region ?? new Rect(0, 0, target.SizeInPixels.Width, target.SizeInPixels.Height);
            drawing.DrawImage(image, new Vector2((float)clip.X, (float)clip.Y), clip);
        }
        static void Configure(PixelShaderEffect shader, float sigma, Vector2 direction)
        {
            var weights = new float[25]; float sum = 0, denominator = 1f / ((2f * sigma) * sigma);
            for (var i = 0; i < weights.Length; i++) { var distance = i - 12; weights[i] = MathF.Exp(-(distance * distance * denominator)); sum += weights[i]; }
            var normalization = 1f / sum; for (var i = 0; i < weights.Length; i++) weights[i] *= normalization;
            var taps = new List<Vector2>(13);
            void Pair(int index) { var weight = weights[index + 12] + weights[index + 13]; taps.Add(new(index + weights[index + 13] / weight, weight)); }
            for (var i = -12; i < 0; i += 2) Pair(i); taps.Add(new(0, weights[12])); for (var i = 1; i <= 12; i += 2) Pair(i);
            shader.Source1Mapping = SamplerCoordinateMapping.Offset; shader.MaxSamplerOffset = 12; shader.Properties["Direction"] = direction;
            for (var i = 0; i < taps.Count; i++) shader.Properties["Tap" + i] = taps[i];
        }
        using var input = Target(width, height); using var caster = Shader("caster");
        caster.Properties["Box"] = new Vector4(21, 21, shape.Width, shape.Height); caster.Properties["Radius"] = shape.ActualRadius; Draw(input, caster);
        using var small = Target(paddedWidth, paddedHeight); using var resize = Shader("mapping", input);
        resize.Properties["Scale"] = new Vector2(width / (float)reducedWidth, height / (float)reducedHeight);
        resize.Properties["Offset"] = new Vector2(-width / (float)reducedWidth, -height / (float)reducedHeight);
        Draw(small, resize, new Rect(1, 1, reducedWidth, reducedHeight));
        using var x = Target(paddedWidth, paddedHeight); using var horizontal = Shader("gaussian", small);
        Configure(horizontal, 7f * (reducedWidth / (float)width), new(1, 0)); Draw(x, horizontal);
        using var xy = Target(paddedWidth, paddedHeight); using var vertical = Shader("gaussian", x);
        Configure(vertical, 7f * (reducedHeight / (float)height), new(0, 1)); Draw(xy, vertical);
        var cache = Target(width, height);
        try
        {
            using var expand = Shader("mapping", xy);
            expand.Properties["Scale"] = new Vector2(reducedWidth / (float)width, reducedHeight / (float)height); expand.Properties["Offset"] = Vector2.One;
            Draw(cache, expand); return cache;
        }
        catch { cache.Dispose(); throw; }
    }

    private void OnDeviceLost(CanvasDevice sender, object args)
    {
        if (disposed || Interlocked.Exchange(ref recoveryQueued, 1) != 0) return;
        if (!dispatcher.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            Interlocked.Exchange(ref recoveryQueued, 0);
            if (!disposed) InvalidateGeneratedResources();
        })) throw new InvalidOperationException("Near-shadow device recovery dispatcher is unavailable.");
    }

    // Invalidates resources owned by this renderer. This does not reset the
    // system GPU or dispose Win2D's device shared with the rest of the library.
    internal void InvalidateGeneratedResources()
    {
        if (!dispatcher.HasThreadAccess) throw new InvalidOperationException("Near-shadow invalidation requires the compositor thread.");
        ObjectDisposedException.ThrowIf(disposed, this);
        device.DeviceLost -= OnDeviceLost;
        deviceLostSubscribed = false;
        device = CanvasDevice.GetSharedDevice();
        device.DeviceLost += OnDeviceLost;
        deviceLostSubscribed = true;
        CanvasComposition.SetCanvasDevice(Graphics, device);
        foreach (var kernel in kernels.Values) kernel.Dispose();
        kernels.Clear();
        Generation++;
        foreach (var owner in owners.ToArray()) owner.RequestRefresh();
    }

    internal object Snapshot() => new
    {
        disposed, ownerCount = owners.Count, kernelCount = kernels.Count, generation = Generation,
        kernelLeaseCount = leases.Count,
        activeRadii = radiusOwners.OrderBy(r => r.Key.Radius).Select(r => new { radius = r.Key.Radius, owners = r.Value }).ToArray(),
        activeShapes = radiusOwners.Select(r => new { radius = r.Key.Radius, width = r.Key.Width, height = r.Key.Height, actualRadius = r.Key.ActualRadius, owners = r.Value }).ToArray(),
        kernelBytes = kernels.Sum(r => (long)r.Value.SizeInPixels.Width * r.Value.SizeInPixels.Height * 4),
        deviceLostSubscribed, sharedDeviceDisposed = false,
        registered = devices is not null && devices.TryGetValue(compositor, out var current) && ReferenceEquals(current, this)
    };

    public void Dispose()
    {
        if (disposed) return;
        if (owners.Count != 0) throw new InvalidOperationException("Cannot release a near-shadow device with active owners.");
        if (leases.Count != 0 || radiusOwners.Count != 0) throw new InvalidOperationException("Cannot release leased near-shadow kernels.");
        disposed = true;
        device.DeviceLost -= OnDeviceLost;
        deviceLostSubscribed = false;
        foreach (var kernel in kernels.Values) kernel.Dispose();
        kernels.Clear();
        Graphics.Dispose();
        devices!.Remove(compositor);
    }
}
