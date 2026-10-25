using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Composition;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Testing;

internal static class OverlayNearGeometryFixture
{
    private delegate bool Visitor(nint handle, nint value);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor, nint value);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle, out uint process);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint handle);
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static async Task<object> TraceAsync(IDisposable owner, Type ownerType, Vector4 tint, string folder, string id)
    {
        object Field(string name) => ownerType.GetField(name, Private)!.GetValue(owner)!;
        var actual = (PixelShaderEffect)Field("encoded");
        var surface = (CompositionDrawingSurface)Field("surface");
        var sprite = (SpriteVisual)Field("sprite");
        var previous = JsonSerializer.SerializeToElement(Field("previous"));
        var width = checked((int)surface.Size.Width); var height = checked((int)surface.Size.Height);
        var device = CanvasDevice.GetSharedDevice();
        Require(device.IsBufferPrecisionSupported(CanvasBufferPrecision.Precision32Float), "Precision trace needs float32 targets");
        CanvasRenderTarget Target(bool floating) => new(device, width, height, 96, floating ? Windows.Graphics.DirectX.DirectXPixelFormat.R32G32B32A32Float : Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized, CanvasAlphaMode.Premultiplied);
        void Draw(CanvasRenderTarget target, ICanvasImage image, CanvasBufferPrecision precision)
        {
            using var drawing = target.CreateDrawingSession(); drawing.Clear(Microsoft.UI.Colors.Transparent);
            drawing.Blend = CanvasBlend.Copy; drawing.EffectBufferPrecision = precision;
            drawing.DrawImage(image, Vector2.Zero, new Windows.Foundation.Rect(0, 0, width, height));
        }
        var rows = new List<object>();
        async Task Save(string name, CanvasRenderTarget target, bool floating)
        {
            var bytes = target.GetPixelBytes(); var size = target.SizeInPixels; var stride = floating ? 16 : 4;
            Require(bytes.Length == checked((int)size.Width * (int)size.Height * stride), "Precision trace has an incomplete pixel buffer");
            var file = id + "-trace-" + name + (floating ? ".rgba32f" : ".bgra8");
            await File.WriteAllBytesAsync(Path.Combine(folder, file), bytes);
            rows.Add(new { name, file, sha256 = Hash(bytes), width = size.Width, height = size.Height, bytesPerPixel = stride, format = floating ? "R32G32B32A32Float" : "B8G8R8A8UIntNormalized", alphaMode = "Premultiplied", dpi = 96 });
        }
        using var encodedBytes = Target(false); Draw(encodedBytes, actual, CanvasBufferPrecision.Precision8UIntNormalized);
        await Save("encoded-bytes", encodedBytes, false);
        using var resource = typeof(BleeckerOverlaySurface).Assembly.GetManifestResourceStream("Orchard.NearShadow.encode2.bin") ?? throw new InvalidOperationException("Missing precision trace shader");
        using var memory = new MemoryStream(); await resource.CopyToAsync(memory);
        using var floatingShader = new PixelShaderEffect(memory.ToArray())
        {
            BufferPrecision = CanvasBufferPrecision.Precision32Float, Source1 = actual.Source1,
            Source1Interpolation = actual.Source1Interpolation, Source1BorderMode = actual.Source1BorderMode, Source1Mapping = actual.Source1Mapping
        };
        foreach (var property in new[] { "Box", "CasterBox", "Radius", "Dimension", "General", "ProfileRadius" }) floatingShader.Properties[property] = actual.Properties[property];
        using var encodedFloat = Target(true); Draw(encodedFloat, floatingShader, CanvasBufferPrecision.Precision32Float);
        await Save("encoded-float", encodedFloat, true);
        var actualEffects = ((IEnumerable<IDisposable>)Field("graphResources")).OfType<ColorMatrixEffect>().ToArray();
        Require(actualEffects.Length == 1, "Precision trace requires the verified library decoder");
        var matrix = actualEffects[0].ColorMatrix;
        using var fromBytes = new ColorMatrixEffect { AlphaMode = actualEffects[0].AlphaMode, ClampOutput = actualEffects[0].ClampOutput, ColorMatrix = matrix, BufferPrecision = CanvasBufferPrecision.Precision32Float, Source = encodedBytes };
        using var fromFloat = new ColorMatrixEffect { AlphaMode = actualEffects[0].AlphaMode, ClampOutput = actualEffects[0].ClampOutput, ColorMatrix = matrix, BufferPrecision = CanvasBufferPrecision.Precision32Float, Source = encodedFloat };
        using var byteColorFloat = Target(true); Draw(byteColorFloat, fromBytes, CanvasBufferPrecision.Precision32Float);
        await Save("byte-color-float", byteColorFloat, true);
        using var floatColorFloat = Target(true); Draw(floatColorFloat, fromFloat, CanvasBufferPrecision.Precision32Float);
        await Save("float-color-float", floatColorFloat, true);
        using var byteColorByte = Target(false); Draw(byteColorByte, fromBytes, CanvasBufferPrecision.Precision32Float);
        await Save("byte-color-byte", byteColorByte, false);
        using var copiedFloatByte = Target(false); Draw(copiedFloatByte, byteColorFloat, CanvasBufferPrecision.Precision32Float);
        await Save("byte-color-from-float-target", copiedFloatByte, false);
        using var floatColorByte = Target(false); Draw(floatColorByte, fromFloat, CanvasBufferPrecision.Precision32Float);
        await Save("float-color-byte", floatColorByte, false);
        fromBytes.CacheOutput = true;
        using var cachedColorByte = Target(false); Draw(cachedColorByte, fromBytes, CanvasBufferPrecision.Precision32Float);
        await Save("byte-color-cached", cachedColorByte, false);
        await Save("kernel", (CanvasRenderTarget)actual.Source1!, false);
        object floatSurfaceCapability;
        var probeStage = "allocate";
        try
        {
            var shared = Field("shared");
            var graphics = (CompositionGraphicsDevice)shared.GetType().GetProperty("Graphics", Private)!.GetValue(shared)!;
            using var candidate = graphics.CreateDrawingSurface(new Windows.Foundation.Size(1, 1), Microsoft.Graphics.DirectX.DirectXPixelFormat.R32G32B32A32Float, Microsoft.Graphics.DirectX.DirectXAlphaMode.Premultiplied);
            probeStage = "draw";
            using (var drawing = CanvasComposition.CreateDrawingSession(candidate)) drawing.Clear(Microsoft.UI.Colors.Transparent);
            floatSurfaceCapability = new { requestedFormat = "R32G32B32A32Float", created = true, drawSucceeded = true, sharedGraphicsDeviceDisposed = false };
        }
        catch (Exception error)
        {
            floatSurfaceCapability = new { requestedFormat = "R32G32B32A32Float", created = probeStage == "draw", drawSucceeded = false, failedStage = probeStage, hresult = error.HResult, errorType = error.GetType().FullName, sharedGraphicsDeviceDisposed = false };
        }
        var samplingTrace = await OverlayShadowSamplingTrace.CaptureAsync(actual, width, height, folder, id);
        return new
        {
            diagnosticOnly = true, rows, previous, floatSurfaceCapability, samplingTrace,
            textureOrigin = new { x = previous.GetProperty("X").GetDouble() + sprite.Offset.X, y = previous.GetProperty("Y").GetDouble() + sprite.Offset.Y },
            box = JsonSerializer.SerializeToElement(actual.Properties["Box"], new JsonSerializerOptions { IncludeFields = true }),
            casterBox = JsonSerializer.SerializeToElement(actual.Properties["CasterBox"], new JsonSerializerOptions { IncludeFields = true }),
            radius = actual.Properties["Radius"], dimension = actual.Properties["Dimension"], tint = new[] { tint.X, tint.Y, tint.Z, tint.W },
            colorMatrixRows = new[] { new[] { matrix.M11, matrix.M12, matrix.M13, matrix.M14 }, new[] { matrix.M21, matrix.M22, matrix.M23, matrix.M24 }, new[] { matrix.M31, matrix.M32, matrix.M33, matrix.M34 }, new[] { matrix.M41, matrix.M42, matrix.M43, matrix.M44 }, new[] { matrix.M51, matrix.M52, matrix.M53, matrix.M54 } },
            sourcePixelsUsed = false, readbackPixelsUsedAsInputs = false, componentShaderPropertiesChanged = false, componentSurfaceDrawnByDriver = false,
            shaderBytecodeSha256 = Hash(memory.ToArray()), nativeComponentAccepted = false
        };
    }

    internal static async Task RunAsync(Window window, string output, string referencePath, string hostAssembly)
    {
        var referenceBytes = await File.ReadAllBytesAsync(referencePath);
        var fractional = Hash(referenceBytes) == "e441b1449a2e10224b9c551e9f767df160092b63c1363f437425fd7722b6644a";
        var rectangles = Hash(referenceBytes) == "e39d5712d634f0a2f185946e3b26d0f8a5aedd77b10ff711c98b9eabb5eff9d5";
        Require(fractional || rectangles || Hash(referenceBytes) == "f10cd1f8baf5a8dfe80c895f8c9dadd63e763d7426f567858ef2cdee166bfea9", "Unexpected geometry reference");
        using var source = JsonDocument.Parse(referenceBytes);
        var inputs = source.RootElement.GetProperty("cases").EnumerateArray().Select(c => c.Clone()).ToArray();
        Require(inputs.Length == (rectangles ? 44 : fractional ? 57 : 103), "Incomplete geometry source");
        var root = new Canvas { UseLayoutRounding = false };
        window.Content = root;
        var library = typeof(BleeckerOverlaySurface).Assembly;
        var ownerType = library.GetType("Orchard.BleeckerNearShadowLayer", true)!;
        var deviceType = library.GetType("Orchard.BleeckerNearShadowDevice", true)!;
        var folder = Path.GetDirectoryName(output)!;
        var width = 520; var height = 440; var guards = 0; var focus = 0; var activations = 0; var stage = "initial";
        var cases = new List<object>(); var rejected = new List<object>(); var excluded = new List<string>();
        IDisposable? active = null; object? geometryOwnership = null;
        root.GotFocus += (_, _) => focus++;
        window.Activated += (_, a) => { if (a.WindowActivationState != WindowActivationState.Deactivated) activations++; };
        int RegistryCount() => (deviceType.GetField("devices", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null) as System.Collections.IDictionary)?.Count ?? 0;
        JsonElement State(IDisposable owner) => JsonSerializer.SerializeToElement(ownerType.GetMethod("Snapshot", Private)!.Invoke(owner, null));
        object Field(IDisposable owner, string name) => ownerType.GetField(name, Private)!.GetValue(owner)!;
        void Guard()
        {
            guards++;
            GetWindowThreadProcessId(GetForegroundWindow(), out var foreground);
            var visible = false;
            EnumWindows((h, _) => { GetWindowThreadProcessId(h, out var process); visible |= process == Environment.ProcessId && IsWindowVisible(h); return true; }, 0);
            Require(!visible && !window.Visible && !window.AppWindow.IsVisible && foreground != Environment.ProcessId && focus == 0 && activations == 0, "Geometry sweep left hidden boundary");
            Require(root.XamlRoot is null || VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Count == 0, "Geometry sweep opened a popup");
        }
        async Task Layout()
        {
            for (var pass = 0; pass < 3; pass++)
            {
                var idle = new TaskCompletionSource();
                Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => idle.SetResult()), "Geometry dispatcher unavailable");
                await idle.Task;
                root.Measure(new(width, height)); root.Arrange(new(0, 0, width, height)); root.UpdateLayout(); Guard();
            }
            await Task.Delay(20);
        }
        async Task<(byte[] Pixels, object Record)> Capture(string id)
        {
            await Layout(); byte[]? prior = null, raw = null; var attempts = 0;
            for (; attempts < 6; attempts++)
            {
                var bitmap = new RenderTargetBitmap(); await bitmap.RenderAsync(root, width, height); raw = (await bitmap.GetPixelsAsync()).ToArray();
                Require(bitmap.PixelWidth == width && bitmap.PixelHeight == height && raw.Length == width * height * 4, "Incomplete geometry pixel buffer");
                if (prior is not null && prior.AsSpan().SequenceEqual(raw)) break;
                prior = raw; await Layout();
            }
            Require(attempts < 6, "Unstable geometry frame");
            var pixels = raw ?? throw new InvalidOperationException("Geometry capture has no pixels");
            var file = id + ".bgra8"; var pngFile = id + ".png";
            await File.WriteAllBytesAsync(Path.Combine(folder, file), pixels);
            using var stream = new InMemoryRandomAccessStream(); var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, (uint)width, (uint)height, 96, 96, ReferencePngCodec.EncodeBgra(pixels));
            await encoder.FlushAsync(); stream.Seek(0);
            using (var target = File.Create(Path.Combine(folder, pngFile))) await stream.AsStreamForRead().CopyToAsync(target);
            Guard(); return (pixels, new { file, sha256 = Hash(pixels), pngFile, pngSha256 = Hash(File.ReadAllBytes(Path.Combine(folder, pngFile))), width, height, stableFrames = 2, attempts = attempts + 1 });
        }
        try
        {
            foreach (var spec in inputs)
            {
                stage = spec.GetProperty("id").GetString()!;
                if (spec.TryGetProperty("noBlur", out var noBlur) && noBlur.GetBoolean()) { excluded.Add(stage); continue; }
                await File.WriteAllTextAsync(Path.Combine(folder, "geometry-stage.json"), JsonSerializer.Serialize(new { stage, cases = cases.Count, rejected = rejected.Count, guards }));
                root.Children.Clear(); width = spec.GetProperty("viewport").GetProperty("width").GetInt32(); height = spec.GetProperty("viewport").GetProperty("height").GetInt32(); root.Width = width; root.Height = height; window.AppWindow.ResizeClient(new(width, height));
                var canvas = spec.GetProperty("canvas").GetProperty("rgba"); root.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(canvas[3].GetByte(), canvas[0].GetByte(), canvas[1].GetByte(), canvas[2].GetByte()));
                var box = spec.GetProperty("box"); var corners = spec.GetProperty("corners").EnumerateArray().Select(c => c.GetDouble()).ToArray(); var scale = spec.GetProperty("scale").GetDouble();
                var paint = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                var target = new BleeckerOverlaySurface { UseLayoutRounding = false, Width = box.GetProperty("width").GetDouble(), Height = box.GetProperty("height").GetDouble(), CornerRadius = new(corners[0], corners[1], corners[2], corners[3]), Background = paint, BorderBrush = paint, BorderThickness = new(1) };
                if (scale != 1) target.RenderTransform = new ScaleTransform { ScaleX = scale, ScaleY = scale };
                Canvas.SetLeft(target, box.GetProperty("x").GetDouble()); Canvas.SetTop(target, box.GetProperty("y").GetDouble()); root.Children.Add(target); await Layout();
                Require(target.IsLoaded && root.XamlRoot?.RasterizationScale == 1, "Geometry sweep requires a loaded scale-one root");
                var snappedWidth = Math.Floor(box.GetProperty("x").GetDouble() + target.ActualWidth + .5) - Math.Floor(box.GetProperty("x").GetDouble() + .5);
                var snappedHeight = Math.Floor(box.GetProperty("y").GetDouble() + target.ActualHeight + .5) - Math.Floor(box.GetProperty("y").GetDouble() + .5);
                var actualRadius = Math.Min(corners[0], Math.Min(snappedWidth, snappedHeight) / 2);
                var canonical = snappedWidth >= Math.Ceiling(actualRadius) * 2 + 43 && snappedHeight >= Math.Ceiling(actualRadius) * 2 + 43;
                var supported = scale == 1 && corners.All(c => c == corners[0]) && corners[0] >= 0 && (actualRadius == 0 || canonical || (snappedWidth == snappedHeight && actualRadius == snappedWidth / 2) || snappedWidth > 64 || snappedHeight > 64);
                var before = supported ? default : await Capture(stage + "-before");
                try
                {
                    var offset = spec.GetProperty("offset"); var tint = spec.GetProperty("white").GetBoolean() ? Vector4.One : new Vector4(2 / 255f, 3 / 255f, 5 / 255f, 15 / 255f);
                    active = (IDisposable)Activator.CreateInstance(ownerType, Private, null, [target, new Vector2((float)offset.GetProperty("x").GetDouble(), (float)offset.GetProperty("y").GetDouble()), tint], null)!;
                    Require(supported, "Unsupported geometry unexpectedly attached");
                    await Layout(); ((SpriteVisual)Field(active, "far")).IsVisible = false;
                    var frame = await Capture(stage); var loaded = State(active); object? roundtrip = null; object? precisionTrace = null; object? actualKernelTrace = null;
                    if (loaded.GetProperty("general").GetBoolean())
                    {
                        var kernel = (CanvasRenderTarget)((PixelShaderEffect)Field(active, "encoded")).Source1!;
                        var pixels = kernel.GetPixelBytes(); var file = stage + "-actual-kernel.bgra8";
                        await File.WriteAllBytesAsync(Path.Combine(folder, file), pixels);
                        actualKernelTrace = new { file, sha256 = Hash(pixels), width = kernel.SizeInPixels.Width, height = kernel.SizeInPixels.Height, diagnosticOnly = true, readbackUsedAsRendererInput = false };
                    }
                    if (stage.StartsWith("overlap-r13.25-", StringComparison.Ordinal) || stage is "overlap-r13.5-Transparent" or "overlap-r27.5-WhiteMask")
                    {
                        var beforeTrace = State(active); var graph = Field(active, "container"); var shader = Field(active, "encoded");
                        var trace = await TraceAsync(active, ownerType, tint, folder, stage); var afterTrace = await Capture(stage + "-after-trace");
                        Require(frame.Pixels.AsSpan().SequenceEqual(afterTrace.Pixels) && ReferenceEquals(graph, Field(active, "container")) && ReferenceEquals(shader, Field(active, "encoded")), "Precision trace changed the component frame or graph");
                        precisionTrace = new { trace, before = beforeTrace, after = State(active), frame = afterTrace.Record, componentPixelsRetained = true, graphRetained = true };
                    }
                    if (corners[0] != Math.Floor(corners[0]))
                    {
                        var graph = Field(active, "container"); var drawingSurface = Field(active, "surface"); var brush = Field(active, "brush");
                        var shader = Field(active, "encoded"); var sourceProperty = shader.GetType().GetProperty("Source1")!; var kernel = sourceProperty.GetValue(shader);
                        target.CornerRadius = new(Math.Ceiling(corners[0])); await Layout(); var integer = State(active);
                        target.CornerRadius = new(corners[0]); await Layout(); var restored = State(active); var restoredFrame = await Capture(stage + "-roundtrip");
                        Require(ReferenceEquals(graph, Field(active, "container")) && ReferenceEquals(drawingSurface, Field(active, "surface")) && ReferenceEquals(brush, Field(active, "brush")) && ReferenceEquals(kernel, sourceProperty.GetValue(shader)), "Fractional update replaced its graph or shared ceiling-radius kernel");
                        Require(frame.Pixels.AsSpan().SequenceEqual(restoredFrame.Pixels), "Fractional radius roundtrip changed pixels");
                        Require(restored.GetProperty("updates").GetInt32() >= loaded.GetProperty("updates").GetInt32() + 2 && restored.GetProperty("cornerEvents").GetInt32() >= loaded.GetProperty("cornerEvents").GetInt32() + 2, "Fractional corner updates were not observed");
                        roundtrip = new { integer, restored, frame = restoredFrame.Record, graphRetained = true, kernelRetained = true };
                    }
                    Require(ReferenceEquals(target.Background, paint) && target.Opacity == 1, "Geometry sweep changed caller paint");
                    root.Children.Remove(target); await Layout(); var unloaded = State(active);
                    Require(unloaded.GetProperty("disposed").GetBoolean() && unloaded.GetProperty("graphResourceCount").GetInt32() == 0 && RegistryCount() == 0, "Geometry unload leaked renderer resources");
                    cases.Add(new { id = stage, sourceSha256 = spec.GetProperty("sha256").GetString(), frame = frame.Record, loaded, unloaded, roundtrip, precisionTrace, actualKernelTrace, callerPaintRetained = true, automaticUnload = true }); active = null;
                }
                catch (TargetInvocationException error) when (!supported && error.InnerException is NotSupportedException)
                {
                    var after = await Capture(stage + "-after"); Require(before.Pixels is not null && before.Pixels.AsSpan().SequenceEqual(after.Pixels), "Rejected geometry changed the public renderer");
                    Require(ReferenceEquals(target.Background, paint) && RegistryCount() == 0, "Rejected geometry changed paint or leaked a cache owner");
                    rejected.Add(new { id = stage, before = before.Record, after = after.Record, rejected = true, originalDefaultBytesRetained = true, cacheRegistryEntries = RegistryCount() }); root.Children.Remove(target); await Layout();
                }
                Require(typeof(BleeckerOverlaySurface).GetField("shadowVisual", Private)!.GetValue(target) is null && ((System.Collections.ICollection)typeof(BleeckerOverlaySurface).GetField("shadowResources", Private)!.GetValue(target)!).Count == 0, "Public shadow graph survived unload");
            }
            if (!fractional && !rectangles)
            {
                stage = "actual-shape-ownership";
                root.Children.Clear(); width = 520; height = 440; root.Width = width; root.Height = height; root.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent); window.AppWindow.ResizeClient(new(width, height));
                var paint = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                var donor = new TextBlock { Foreground = paint };
                BleeckerOverlaySurface Component(double x)
                {
                    var component = new BleeckerOverlaySurface { UseLayoutRounding = false, Width = 69, Height = 69, CornerRadius = new(14), Background = paint, BorderBrush = paint, BorderThickness = new(1) };
                    Canvas.SetLeft(component, x); Canvas.SetTop(component, 20); root.Children.Add(component); return component;
                }
                var first = Component(20); var second = Component(150);
                first.SetBinding(Control.BackgroundProperty, new Microsoft.UI.Xaml.Data.Binding { Source = donor, Path = new("Foreground"), Mode = Microsoft.UI.Xaml.Data.BindingMode.OneWay });
                await Layout();
                IDisposable Attach(BleeckerOverlaySurface component) => (IDisposable)Activator.CreateInstance(ownerType, Private, null, [component, new Vector2(200, 200), Vector4.One], null)!;
                var firstOwner = Attach(first); IDisposable? secondOwner = null;
                try
                {
                    secondOwner = Attach(second); await Layout();
                    void HideSecond() { ((SpriteVisual)Field(firstOwner, "far")).IsVisible = false; ((SpriteVisual)Field(secondOwner, "far")).IsVisible = false; ((SpriteVisual)Field(secondOwner, "sprite")).IsVisible = false; }
                    HideSecond(); var baseline = await Capture("ownership-baseline");
                    var shared = Field(firstOwner, "shared"); var firstShader = (PixelShaderEffect)Field(firstOwner, "encoded"); var secondShader = (PixelShaderEffect)Field(secondOwner, "encoded"); var originalKernel = firstShader.Source1;
                    var graph = Field(firstOwner, "container"); var surface = Field(firstOwner, "surface"); var brush = Field(firstOwner, "brush");
                    Require(ReferenceEquals(shared, Field(secondOwner, "shared")) && ReferenceEquals(originalKernel, secondShader.Source1), "Equal actual shapes did not share their kernel");
                    var equal = State(firstOwner);
                    second.Width = 70; second.Height = 70; await Layout(); HideSecond();
                    Require(ReferenceEquals(firstShader.Source1, originalKernel) && !ReferenceEquals(firstShader.Source1, secondShader.Source1) && State(firstOwner).GetProperty("shared").GetProperty("kernelCount").GetInt32() == 2, "Dimension changes collided in the actual-shape cache");
                    var different = State(firstOwner);
                    second.Width = 80; second.Height = 80; second.CornerRadius = new(40); await Layout(); HideSecond();
                    var circleKernel = secondShader.Source1; var circular = State(firstOwner);
                    Require(!ReferenceEquals(firstShader.Source1, circleKernel) && circular.GetProperty("shared").GetProperty("kernelBytes").GetInt64() == 128 * 128 * 4 + 512 * 4, "Circle and rectangular caches were not independently owned");
                    var transitions = new List<object>(); object? rectangleSharing = null;
                    foreach (var shape in new[] { "actual-70", "canonical", "rectangle", "actual-69" })
                    {
                        first.Width = shape is "canonical" or "rectangle" ? 200 : shape == "actual-70" ? 70 : 69; first.Height = shape is "canonical" or "rectangle" ? 120 : first.Width; first.CornerRadius = new(shape == "rectangle" ? 0 : 14); await Layout(); HideSecond();
                        Require(ReferenceEquals(graph, Field(firstOwner, "container")) && ReferenceEquals(surface, Field(firstOwner, "surface")) && ReferenceEquals(brush, Field(firstOwner, "brush")) && ReferenceEquals(circleKernel, secondShader.Source1), "Shape transitions replaced a graph or another owner's kernel");
                        Require(first.GetBindingExpression(Control.BackgroundProperty) is not null && ReferenceEquals(first.Background, paint) && first.Opacity == 1, "Shape transitions lost caller paint or binding");
                        object? shapeFrame = shape == "rectangle" ? (await Capture("ownership-rectangle")).Record : null;
                        transitions.Add(new { shape, state = State(firstOwner), frame = shapeFrame, graphRetained = true, bindingRetained = true, survivorKernelRetained = true });
                        if (shape == "rectangle")
                        {
                            var rectangleKernel = firstShader.Source1;
                            second.CornerRadius = new(0); await Layout(); HideSecond(); var bothRectangles = State(firstOwner);
                            Require(ReferenceEquals(rectangleKernel, firstShader.Source1) && ReferenceEquals(rectangleKernel, secondShader.Source1) && bothRectangles.GetProperty("shared").GetProperty("kernelCount").GetInt32() == 1 && bothRectangles.GetProperty("shared").GetProperty("kernelLeaseCount").GetInt32() == 2, "Different rectangle sizes did not share the integral profile");
                            var sharedFrame = await Capture("ownership-shared-rectangle");
                            second.CornerRadius = new(40); await Layout(); HideSecond(); circleKernel = secondShader.Source1;
                            Require(ReferenceEquals(rectangleKernel, firstShader.Source1) && !ReferenceEquals(rectangleKernel, circleKernel), "Rectangle survivor lost its profile after the other owner changed shape");
                            rectangleSharing = new { bothRectangles, restored = State(firstOwner), frame = sharedFrame.Record, profileSharedAcrossDimensions = true, survivingRectangleProfileRetained = true, graphRetained = ReferenceEquals(graph, Field(firstOwner, "container")) };
                        }
                    }
                    var roundtrip = await Capture("ownership-roundtrip"); Require(baseline.Pixels.AsSpan().SequenceEqual(roundtrip.Pixels), "Actual/canonical roundtrip changed pixels");
                    var beforeReset = State(firstOwner); ownerType.GetMethod("InvalidateGeneratedResources", Private)!.Invoke(firstOwner, null); await Layout(); HideSecond();
                    var afterReset = State(firstOwner); var regenerated = await Capture("ownership-regenerated");
                    Require(baseline.Pixels.AsSpan().SequenceEqual(regenerated.Pixels) && !ReferenceEquals(circleKernel, secondShader.Source1) && afterReset.GetProperty("shared").GetProperty("generation").GetInt32() == beforeReset.GetProperty("shared").GetProperty("generation").GetInt32() + 1, "Actual-shape invalidation failed to regenerate independently leased kernels");
                    var host = (Grid)Field(firstOwner, "host"); host.Visibility = Visibility.Collapsed; await Layout(); var suspended = State(firstOwner);
                    Require(suspended.GetProperty("graphResourceCount").GetInt32() == 0 && suspended.GetProperty("shared").GetProperty("ownerCount").GetInt32() == 2 && suspended.GetProperty("shared").GetProperty("kernelLeaseCount").GetInt32() == 1 && suspended.GetProperty("shared").GetProperty("kernelBytes").GetInt64() == 512 * 4, "Suspending an actual shape retained a lease or damaged its circle survivor");
                    host.Visibility = Visibility.Visible; await Layout(); HideSecond(); var resumed = await Capture("ownership-resumed"); Require(baseline.Pixels.AsSpan().SequenceEqual(resumed.Pixels), "Actual-shape suspension changed restored pixels");
                    var afterResume = State(firstOwner); root.Children.Remove(first); await Layout(); var unloaded = State(firstOwner); var survivor = State(secondOwner);
                    Require(unloaded.GetProperty("disposed").GetBoolean() && unloaded.GetProperty("graphResourceCount").GetInt32() == 0 && survivor.GetProperty("shared").GetProperty("ownerCount").GetInt32() == 1 && survivor.GetProperty("shared").GetProperty("kernelBytes").GetInt64() == 512 * 4, "Actual-shape unload damaged a circle owner");
                    root.Children.Remove(second); await Layout(); var lastUnloaded = State(secondOwner); Require(RegistryCount() == 0 && lastUnloaded.GetProperty("disposed").GetBoolean(), "Last actual-shape owner leaked shared resources");
                    var emptyBounds = new List<object>();
                    foreach (var axis in new[] { "width", "height" })
                    {
                        var empty = Component(20); if (axis == "width") empty.Width = .25; else empty.Height = .25; await Layout(); var before = await Capture("empty-snapped-" + axis + "-before");
                        var rejectedEmpty = false;
                        try { using var unexpected = Attach(empty); }
                        catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { rejectedEmpty = true; }
                        Require(rejectedEmpty && RegistryCount() == 0 && ReferenceEquals(empty.Background, paint), "Empty snapped bounds did not fail cleanly");
                        var after = await Capture("empty-snapped-" + axis + "-after"); Require(before.Pixels.AsSpan().SequenceEqual(after.Pixels), "Rejected empty snapped bounds changed public pixels");
                        emptyBounds.Add(new { axis, rejected = true, before = before.Record, after = after.Record, cacheRegistryEntries = RegistryCount(), originalDefaultBytesRetained = true }); root.Children.Remove(empty); await Layout();
                    }
                    geometryOwnership = new { equal, different, circular, transitions, rectangleSharing, emptyBounds, baseline = baseline.Record, roundtrip = roundtrip.Record, beforeReset, afterReset, regenerated = regenerated.Record, suspended, afterResume, resumed = resumed.Record, unloaded, survivor, lastUnloaded, shapeKeySharing = true, callerBindingRetained = true, actualDeviceLossSimulated = false, hardwareInputSent = false };
                }
                finally { firstOwner.Dispose(); secondOwner?.Dispose(); root.Children.Clear(); await Layout(); }
            }
            Require(cases.Count == (rectangles ? 44 : 57) && rejected.Count == (fractional || rectangles ? 0 : 14) && excluded.Count == (fractional || rectangles ? 0 : 32) && RegistryCount() == 0, "Incomplete geometry boundary sweep"); Guard();
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = true, format = "orchard-overlay-near-geometry-native-v6", referenceSha256 = Hash(referenceBytes), librarySha256 = Hash(File.ReadAllBytes(library.Location)), hostSha256 = Hash(File.ReadAllBytes(hostAssembly)), cases, rejected, excluded, geometryOwnership, guards, focusEvents = focus, activations, hostVisible = window.Visible, hardwareInputSent = false, cpuBitmapInputs = false, readbacksUsedAsRendererInputs = false, driverIssuesDraws = false, libraryImplementationChanged = true, pngExport = ReferencePngCodec.Name, referenceCodecSha256 = ReferencePngCodec.Sha256, nativeComponentAccepted = false }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(0);
        }
        catch (Exception error)
        {
            try { active?.Dispose(); } catch { }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = false, stage, error = error.ToString(), cases, rejected, excluded, guards, focusEvents = focus, activations }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(1);
        }
    }
}
