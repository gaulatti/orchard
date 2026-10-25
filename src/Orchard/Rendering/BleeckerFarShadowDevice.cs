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

// Owns only this renderer's mathematical GPU table and Composition wrapper.
// Win2D's shared CanvasDevice is borrowed and must never be disposed here.
internal sealed class BleeckerFarShadowDevice : IDisposable
{
    [ThreadStatic] private static Dictionary<Compositor, BleeckerFarShadowDevice>? devices;
    private static readonly ConcurrentDictionary<string, byte[]> shaderBytes = new();
    private readonly Compositor compositor;
    private readonly DispatcherQueue dispatcher;
    private readonly HashSet<BleeckerFarShadowLayer> owners = [];
    private CanvasDevice device;
    private CanvasRenderTarget? table;
    private bool disposed;
    private bool deviceLostSubscribed;
    private int recoveryQueued;
    private int tableGenerations;
    internal CompositionGraphicsDevice Graphics { get; }
    internal int Generation { get; private set; }
    internal int Dimension { get; }

    private BleeckerFarShadowDevice(Compositor compositor, DispatcherQueue dispatcher)
    {
        this.compositor = compositor;
        this.dispatcher = dispatcher;
        var sigma = BleeckerTokens.OverlayShadows[0].Blur / 2;
        Dimension = checked((int)BitOperations.RoundUpToPowerOf2((uint)MathF.Max(32, 2 * MathF.Ceiling(6 * sigma))));
        device = CanvasDevice.GetSharedDevice();
        if (!device.IsBufferPrecisionSupported(CanvasBufferPrecision.Precision32Float))
            throw new NotSupportedException("Far-shadow rendering requires float32 effect precision.");
        Graphics = CanvasComposition.CreateCompositionGraphicsDevice(compositor, device);
        device.DeviceLost += OnDeviceLost;
        deviceLostSubscribed = true;
    }

    internal static BleeckerFarShadowDevice Acquire(Compositor compositor, DispatcherQueue dispatcher, BleeckerFarShadowLayer owner)
    {
        if (!dispatcher.HasThreadAccess) throw new InvalidOperationException("Far-shadow ownership requires the compositor thread.");
        devices ??= [];
        if (!devices.TryGetValue(compositor, out var shared)) { shared = new(compositor, dispatcher); devices.Add(compositor, shared); }
        if (!shared.owners.Add(owner)) throw new InvalidOperationException("Duplicate far-shadow owner.");
        return shared;
    }

    internal void Release(BleeckerFarShadowLayer owner)
    {
        if (!dispatcher.HasThreadAccess) throw new InvalidOperationException("Far-shadow release requires the compositor thread.");
        if (!owners.Remove(owner)) throw new InvalidOperationException("Unknown far-shadow owner.");
        if (owners.Count == 0) Dispose();
    }

    internal CanvasRenderTarget Table
    {
        get
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (table is not null) return table;
            var candidate = new CanvasRenderTarget(device, Dimension, 1, 96, DirectXPixelFormat.B8G8R8A8UIntNormalized, CanvasAlphaMode.Premultiplied);
            try
            {
                using var generator = Shader("integral");
                generator.Properties["Dimension"] = (float)Dimension;
                using (var drawing = candidate.CreateDrawingSession())
                {
                    drawing.Blend = CanvasBlend.Copy;
                    drawing.EffectBufferPrecision = CanvasBufferPrecision.Precision32Float;
                    drawing.DrawImage(generator, Vector2.Zero, new Rect(0, 0, Dimension, 1));
                }
                table = candidate;
                tableGenerations++;
                return table;
            }
            catch { candidate.Dispose(); throw; }
        }
    }

    internal static PixelShaderEffect Shader(string name)
    {
        var bytes = shaderBytes.GetOrAdd(name, static key =>
        {
            using var input = typeof(BleeckerFarShadowDevice).Assembly.GetManifestResourceStream($"Orchard.FarShadow.{key}.bin")
                ?? throw new InvalidOperationException($"Missing embedded far-shadow shader: {key}.");
            using var output = new MemoryStream(); input.CopyTo(output); return output.ToArray();
        });
        return new(bytes) { BufferPrecision = CanvasBufferPrecision.Precision32Float };
    }

    private void OnDeviceLost(CanvasDevice sender, object args)
    {
        if (disposed || Interlocked.Exchange(ref recoveryQueued, 1) != 0) return;
        if (!dispatcher.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            Interlocked.Exchange(ref recoveryQueued, 0);
            if (!disposed) InvalidateGeneratedResources();
        })) throw new InvalidOperationException("Far-shadow recovery dispatcher is unavailable.");
    }

    internal void InvalidateGeneratedResources()
    {
        if (!dispatcher.HasThreadAccess) throw new InvalidOperationException("Far-shadow invalidation requires the compositor thread.");
        ObjectDisposedException.ThrowIf(disposed, this);
        device.DeviceLost -= OnDeviceLost; deviceLostSubscribed = false;
        device = CanvasDevice.GetSharedDevice();
        device.DeviceLost += OnDeviceLost; deviceLostSubscribed = true;
        CanvasComposition.SetCanvasDevice(Graphics, device);
        table?.Dispose(); table = null; Generation++;
        foreach (var owner in owners.ToArray()) owner.RequestRefresh();
    }

    internal object Snapshot() => new
    {
        disposed, ownerCount = owners.Count, tableCount = table is null ? 0 : 1, dimension = Dimension,
        tableGenerations, generation = Generation, tableBytes = table is null ? 0 : Dimension * 4,
        deviceLostSubscribed, sharedDeviceDisposed = false,
        registered = devices is not null && devices.TryGetValue(compositor, out var current) && ReferenceEquals(current, this)
    };

    public void Dispose()
    {
        if (disposed) return;
        if (owners.Count != 0) throw new InvalidOperationException("Cannot release a far-shadow device with active owners.");
        disposed = true;
        device.DeviceLost -= OnDeviceLost; deviceLostSubscribed = false;
        table?.Dispose(); table = null; Graphics.Dispose(); devices!.Remove(compositor);
    }
}
