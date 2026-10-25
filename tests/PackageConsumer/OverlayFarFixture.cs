using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Testing;

internal static class OverlayFarFixture
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private delegate bool Visitor(nint handle, nint value);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor, nint value);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle, out uint process);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint handle);
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static object ProbeSurface(IDisposable owner, Type ownerType, Microsoft.Graphics.DirectX.DirectXPixelFormat format)
    {
        var stage = "allocate";
        try
        {
            var shared = ownerType.GetField("shared", Private)!.GetValue(owner)!;
            var graphics = (CompositionGraphicsDevice)shared.GetType().GetProperty("Graphics", Private)!.GetValue(shared)!;
            using var surface = graphics.CreateDrawingSurface(new Windows.Foundation.Size(1, 1), format, Microsoft.Graphics.DirectX.DirectXAlphaMode.Premultiplied);
            stage = "draw";
            using (var drawing = Microsoft.Graphics.Canvas.UI.Composition.CanvasComposition.CreateDrawingSession(surface)) drawing.Clear(Microsoft.UI.Colors.Transparent);
            return new { requestedFormat = format.ToString(), created = true, drawSucceeded = true, sharedGraphicsDeviceDisposed = false };
        }
        catch (Exception error)
        {
            return new { requestedFormat = format.ToString(), created = stage == "draw", drawSucceeded = false, failedStage = stage, hresult = error.HResult, errorType = error.GetType().FullName, sharedGraphicsDeviceDisposed = false };
        }
    }

    internal static async Task RunAsync(Window window, string output, string reference, string hostAssembly)
    {
        var referenceBytes = await File.ReadAllBytesAsync(reference);
        if (Hash(referenceBytes) == "feee0444cc32ca0f566d777971f2daff6e2c3ebab61774054151ff08470f5d1c")
        {
            await OverlayEmptyCasterFixture.RunAsync(window, output, reference, hostAssembly);
            return;
        }
        Require(Hash(referenceBytes) == "634719e50a749049934a0b878d54a63bee842b19ca4393d17ef75d4a7823fe88", "Unexpected far reference");
        using var source = JsonDocument.Parse(referenceBytes);
        var inputs = source.RootElement.GetProperty("cases").EnumerateArray().Select(c => c.Clone()).ToArray();
        Require(inputs.Length == 8, "Incomplete far source");
        var library = typeof(BleeckerOverlaySurface).Assembly;
        var ownerType = library.GetType("Orchard.BleeckerFarShadowLayer", true)!;
        var deviceType = library.GetType("Orchard.BleeckerFarShadowDevice", true)!;
        var root = new Canvas { UseLayoutRounding = false }; window.Content = root;
        var folder = Path.GetDirectoryName(output)!;
        var width = 520; var height = 440; var guards = 0; var focus = 0; var activations = 0; var stage = "initial";
        var cases = new List<object>(); var rejections = new List<object>(); object? concurrent = null; object[]? surfaceCapabilities = null;
        IDisposable? active = null; IDisposable? other = null;
        root.GotFocus += (_, _) => focus++;
        window.Activated += (_, e) => { if (e.WindowActivationState != WindowActivationState.Deactivated) activations++; };
        object Field(IDisposable owner, string name) => ownerType.GetField(name, Private)!.GetValue(owner)!;
        JsonElement State(IDisposable owner) => JsonSerializer.SerializeToElement(ownerType.GetMethod("Snapshot", Private)!.Invoke(owner, null));
        int RegistryCount() => (deviceType.GetField("devices", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null) as System.Collections.IDictionary)?.Count ?? 0;
        IDisposable Attach(BleeckerOverlaySurface target, Vector4 tint) => (IDisposable)Activator.CreateInstance(ownerType, Private, null, [target, tint], null)!;
        void Guard()
        {
            guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var visible = false;
            EnumWindows((h, _) => { GetWindowThreadProcessId(h, out var p); visible |= p == Environment.ProcessId && IsWindowVisible(h); return true; }, 0);
            Require(!visible && !window.Visible && !window.AppWindow.IsVisible && foreground != Environment.ProcessId && focus == 0 && activations == 0, "Far probe left hidden boundary");
            Require(root.XamlRoot is null || VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Count == 0, "Far probe opened a popup");
        }
        async Task Layout()
        {
            for (var pass = 0; pass < 3; pass++)
            {
                var idle = new TaskCompletionSource(); Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => idle.SetResult()), "Far dispatcher unavailable");
                await idle.Task; root.Measure(new(width, height)); root.Arrange(new(0, 0, width, height)); root.UpdateLayout(); Guard();
            }
            await Task.Delay(20);
        }
        BleeckerOverlaySurface Target(JsonElement spec)
        {
            var geometry = spec.GetProperty("geometry"); var box = geometry.GetProperty("box");
            var transparent = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            var t = new BleeckerOverlaySurface { UseLayoutRounding = false, Width = box.GetProperty("width").GetDouble(), Height = box.GetProperty("height").GetDouble(), CornerRadius = new(geometry.GetProperty("radius").GetDouble()), Background = transparent, BorderBrush = transparent, BorderThickness = new(1) };
            Canvas.SetLeft(t, box.GetProperty("x").GetDouble()); Canvas.SetTop(t, box.GetProperty("y").GetDouble()); return t;
        }
        async Task Configure(JsonElement spec)
        {
            root.Children.Clear(); var viewport = spec.GetProperty("geometry").GetProperty("viewport"); width = viewport.GetProperty("width").GetInt32(); height = viewport.GetProperty("height").GetInt32();
            root.Width = width; root.Height = height; root.Clip = new RectangleGeometry { Rect = new(0, 0, width, height) }; window.AppWindow.ResizeClient(new(width, height));
            var rgba = spec.GetProperty("canvas").GetProperty("rgba"); root.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(rgba[3].GetByte(), rgba[0].GetByte(), rgba[1].GetByte(), rgba[2].GetByte())); await Layout();
        }
        async Task<(byte[] Pixels, object Record)> Capture(string id)
        {
            await Layout(); byte[]? previous = null; byte[]? pixels = null; var attempt = 0;
            for (; attempt < 6; attempt++)
            {
                var bitmap = new RenderTargetBitmap(); await bitmap.RenderAsync(root, width, height); pixels = (await bitmap.GetPixelsAsync()).ToArray();
                Require(bitmap.PixelWidth == width && bitmap.PixelHeight == height && pixels.Length == width * height * 4, "Incomplete far frame");
                if (previous is not null && previous.AsSpan().SequenceEqual(pixels)) break;
                previous = pixels; await Layout();
            }
            Require(attempt < 6 && pixels is not null, "Unstable far frame");
            var raw = pixels!; var file = id + ".bgra8"; var pngFile = id + ".png";
            await File.WriteAllBytesAsync(Path.Combine(folder, file), raw);
            using var stream = new InMemoryRandomAccessStream(); var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, (uint)width, (uint)height, 96, 96, ScreenshotPngCodec.EncodeBgra(raw));
            await encoder.FlushAsync(); stream.Seek(0); using (var destination = File.Create(Path.Combine(folder, pngFile))) await stream.AsStreamForRead().CopyToAsync(destination);
            Guard(); return (raw, new { file, sha256 = Hash(raw), pngFile, pngSha256 = Hash(File.ReadAllBytes(Path.Combine(folder, pngFile))), width, height, stableFrames = 2, attempts = attempt + 1 });
        }
        void Isolate(IDisposable owner) => ((SpriteVisual)Field(owner, "near")).IsVisible = false;
        try
        {
            foreach (var spec in inputs)
            {
                stage = spec.GetProperty("id").GetString()!.Replace('/', '-'); await File.WriteAllTextAsync(Path.Combine(folder, "far-stage.json"), JsonSerializer.Serialize(new { stage, cases = cases.Count, guards }));
                await Configure(spec); var target = Target(spec); root.Children.Add(target); await Layout(); Require(target.IsLoaded && root.XamlRoot?.RasterizationScale == 1, "Far probe needs a loaded scale-one root");
                var paint = target.Background; var defaultBefore = await Capture(stage + "-default-before");
                var tint = spec.GetProperty("white").GetBoolean() ? Vector4.One : new Vector4(7 / 255f, 15 / 255f, 21 / 255f, 71 / 255f);
                active = Attach(target, tint); await Layout(); Isolate(active); var frame = await Capture(stage); var loaded = State(active);
                surfaceCapabilities ??= [ProbeSurface(active, ownerType, Microsoft.Graphics.DirectX.DirectXPixelFormat.R16G16B16A16Float), ProbeSurface(active, ownerType, Microsoft.Graphics.DirectX.DirectXPixelFormat.R16G16B16A16UIntNormalized)];
                var graph = Field(active, "container"); var drawingSurface = Field(active, "surface"); var brush = Field(active, "brush"); var shader = (PixelShaderEffect)Field(active, "shader"); var table = shader.Source1;
                var planeReferences = ((IEnumerable<CompositionDrawingSurface>)Field(active, "planes")).ToArray(); Require(planeReferences.Length == 3, "Missing far alpha planes");
                var tableBytes = ((CanvasRenderTarget)table!).GetPixelBytes(); var tableFile = stage + "-table.bgra8"; await File.WriteAllBytesAsync(Path.Combine(folder, tableFile), tableBytes);
                var effects = ((IEnumerable<IDisposable>)Field(active, "graphResources")).ToArray(); var actualPaint = effects.OfType<ColorMatrixEffect>().Single();
                var sprite = (SpriteVisual)Field(active, "sprite");
                var precision = await OverlayFarPrecisionTrace.CaptureAsync(shader, actualPaint, effects.OfType<ArithmeticCompositeEffect>().ToArray(), (int)planeReferences[0].Size.Width, (int)planeReferences[0].Size.Height,
                    new Vector2((float)(Canvas.GetLeft(target) + sprite.Offset.X), (float)(Canvas.GetTop(target) + sprite.Offset.Y)), ((SolidColorBrush)root.Background).Color, folder, stage);
                var afterPrecision = await Capture(stage + "-after-precision"); Require(frame.Pixels.AsSpan().SequenceEqual(afterPrecision.Pixels), "Far precision trace changed actual component pixels");
                var precisionTrace = new { trace = precision, frame = afterPrecision.Record, componentPixelsRetained = true, actualGraphRetained = ReferenceEquals(graph, Field(active, "container")), actualBrushRetained = ReferenceEquals(brush, Field(active, "brush")) };
                target.Width += 37; target.Height += 19; Canvas.SetLeft(target, Canvas.GetLeft(target) + .375); Canvas.SetTop(target, Canvas.GetTop(target) + .1875); target.CornerRadius = new(13.25); await Layout(); var changed = State(active);
                var box = spec.GetProperty("geometry").GetProperty("box"); target.Width = box.GetProperty("width").GetDouble(); target.Height = box.GetProperty("height").GetDouble(); Canvas.SetLeft(target, box.GetProperty("x").GetDouble()); Canvas.SetTop(target, box.GetProperty("y").GetDouble()); target.CornerRadius = new(14); await Layout();
                var roundtrip = await Capture(stage + "-roundtrip"); var restored = State(active);
                var restoredPlanes = ((IEnumerable<CompositionDrawingSurface>)Field(active, "planes")).ToArray();
                Require(restoredPlanes.Length == planeReferences.Length && planeReferences.Zip(restoredPlanes).All(pair => ReferenceEquals(pair.First, pair.Second)), "Far geometry update replaced an alpha plane");
                Require(ReferenceEquals(graph, Field(active, "container")) && ReferenceEquals(drawingSurface, Field(active, "surface")) && ReferenceEquals(brush, Field(active, "brush")) && ReferenceEquals(table, shader.Source1), "Far geometry update replaced loaded graph or shared table");
                Require(frame.Pixels.AsSpan().SequenceEqual(roundtrip.Pixels), "Far geometry roundtrip changed pixels");
                Require(changed.GetProperty("updates").GetInt32() > loaded.GetProperty("updates").GetInt32() && restored.GetProperty("updates").GetInt32() > changed.GetProperty("updates").GetInt32(), "Far geometry updates were missed");
                Require(ReferenceEquals(target.Background, paint) && target.Opacity == 1, "Far renderer changed caller paint");
                ((SpriteVisual)Field(active, "near")).IsVisible = true; active.Dispose(); var disposed = State(active); var defaultAfter = await Capture(stage + "-default-after");
                Require(defaultBefore.Pixels.AsSpan().SequenceEqual(defaultAfter.Pixels) && RegistryCount() == 0, "Far disposal did not restore public default bytes"); active = null;
                active = Attach(target, tint); await Layout(); Isolate(active); var reattached = await Capture(stage + "-reattached"); Require(frame.Pixels.AsSpan().SequenceEqual(reattached.Pixels), "Far reattachment changed pixels");
                root.Children.Remove(target); await Layout(); var unloaded = State(active); Require(unloaded.GetProperty("disposed").GetBoolean() && unloaded.GetProperty("graphResourceCount").GetInt32() == 0 && RegistryCount() == 0, "Far unload leaked resources"); active = null;
                Require(typeof(BleeckerOverlaySurface).GetField("shadowVisual", Private)!.GetValue(target) is null, "Public shadow graph survived unload");
                cases.Add(new { precisionTrace, id = spec.GetProperty("id").GetString(), sourceSha256 = spec.GetProperty("sha256").GetString(), frame = frame.Record, loaded, changed, restored, roundtrip = roundtrip.Record, disposed, defaultBefore = defaultBefore.Record, defaultAfter = defaultAfter.Record, reattached = reattached.Record, unloaded, table = new { file = tableFile, sha256 = Hash(tableBytes), width = 512, height = 1 }, graphRetained = true, tableRetained = true, surfacePlanesRetained = true, callerPaintRetained = true, publicDefaultRestored = true, automaticUnload = true });
            }
            var control = inputs.Single(c => c.GetProperty("id").GetString() == "fixture/mask-blurred-far"); await Configure(control); var first = Target(control); var second = Target(control); root.Children.Add(first); root.Children.Add(second); await Layout();
            active = Attach(first, Vector4.One); other = Attach(second, Vector4.One); await Layout(); Isolate(active); Isolate(other); ((SpriteVisual)Field(other, "sprite")).IsVisible = false;
            var firstShader = (PixelShaderEffect)Field(active, "shader"); var secondShader = (PixelShaderEffect)Field(other, "shader"); var sharedTable = firstShader.Source1; Require(ReferenceEquals(sharedTable, secondShader.Source1), "Concurrent far owners did not share the GPU table");
            var sharedBefore = new { first = State(active), second = State(other) }; var pixelsBefore = await Capture("shared-before"); var retainedBrush = Field(active, "brush"); ownerType.GetMethod("InvalidateGeneratedResources", Private)!.Invoke(active, null); await Layout();
            var sharedAfter = new { first = State(active), second = State(other) }; var pixelsAfter = await Capture("shared-after");
            var tableSharedAfter = ReferenceEquals(firstShader.Source1, secondShader.Source1); var tableRegenerated = !ReferenceEquals(sharedTable, firstShader.Source1); var survivingBrushRetained = ReferenceEquals(retainedBrush, Field(active, "brush")); var resetPixelsStable = pixelsBefore.Pixels.AsSpan().SequenceEqual(pixelsAfter.Pixels);
            concurrent = new { sharedBefore, sharedAfter, before = pixelsBefore.Record, after = pixelsAfter.Record, tableSharedAfter, tableRegenerated, survivingBrushRetained, resetPixelsStable, peerSpriteHiddenForCapture = true, sameGeometryBounds = true };
            Require(tableSharedAfter, "Far reset split the shared table"); Require(tableRegenerated, "Far reset retained the old table"); Require(survivingBrushRetained, "Far reset replaced the loaded brush"); Require(resetPixelsStable, "Far reset changed physical pixels");
            active.Dispose(); var firstDisposed = State(active); active = null; var survivor = State(other); other.Dispose(); var lastDisposed = State(other); other = null; Require(RegistryCount() == 0, "Last far owner leaked the cache registry");
            concurrent = new { sharedBefore, sharedAfter, firstDisposed, survivor, lastDisposed, before = pixelsBefore.Record, after = pixelsAfter.Record, tableShared = true, tableSharedAfter, tableRegenerated, survivingBrushRetained, resetPixelsStable, peerSpriteHiddenForCapture = true, sameGeometryBounds = true, actualDeviceLossSimulated = false, sharedDeviceDisposed = false }; root.Children.Clear(); await Layout();
            foreach (var id in new[] { "rounded-caster", "nonuniform", "transformed" })
            {
                stage = id; await Configure(control); var target = Target(control); if (id == "rounded-caster") target.CornerRadius = new(21); if (id == "nonuniform") target.CornerRadius = new(14, 10, 14, 14); if (id == "empty-caster") target.Width = 40; if (id == "transformed") target.RenderTransform = new ScaleTransform { ScaleX = 1.25, ScaleY = 1.25 }; root.Children.Add(target); await Layout(); var before = await Capture("rejected-" + id + "-before");
                try { active = Attach(target, Vector4.One); throw new InvalidOperationException("Unsupported far geometry attached"); }
                catch (TargetInvocationException e) when (e.InnerException is NotSupportedException)
                {
                    var after = await Capture("rejected-" + id + "-after"); Require(before.Pixels.AsSpan().SequenceEqual(after.Pixels) && RegistryCount() == 0, "Far rejection changed default bytes or leaked ownership"); rejections.Add(new { id, before = before.Record, after = after.Record, rejected = true, publicDefaultRetained = true, cacheRegistryEntries = RegistryCount() });
                }
            }
            root.Children.Clear(); await Layout(); Require(cases.Count == 8 && rejections.Count == 3 && RegistryCount() == 0, "Incomplete far fixture"); Guard();
            var shaderResources = library.GetManifestResourceNames().Where(n => n.StartsWith("Orchard.FarShadow.", StringComparison.Ordinal)).Order().Select(n => { using var stream = library.GetManifestResourceStream(n)!; using var memory = new MemoryStream(); stream.CopyTo(memory); return new { name = n, sha256 = Hash(memory.ToArray()) }; }).ToArray(); Require(shaderResources.Length == 2, "Missing embedded far shaders");
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = true, format = "orchard-overlay-far-native-v2", referenceSha256 = Hash(referenceBytes), librarySha256 = Hash(File.ReadAllBytes(library.Location)), hostSha256 = Hash(File.ReadAllBytes(hostAssembly)), shaderResources, cases, concurrent, rejections, surfaceCapabilities, guards, focusEvents = focus, activations, hostVisible = window.Visible, hardwareInputSent = false, cpuBitmapInputs = false, readbacksUsedAsRendererInputs = false, driverIssuesDraws = false, publicDefaultChanged = false, pngExport = ScreenshotPngCodec.Name, referenceCodecSha256 = ScreenshotPngCodec.Sha256, nativeComponentAccepted = false }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(0);
        }
        catch (Exception error)
        {
            try { active?.Dispose(); other?.Dispose(); } catch { }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = false, stage, error = error.ToString(), cases, concurrent, rejections, surfaceCapabilities, guards, focusEvents = focus, activations }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(1);
        }
    }
}


