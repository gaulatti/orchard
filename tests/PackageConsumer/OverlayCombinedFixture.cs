using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Testing;

internal static class OverlayCombinedFixture
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private delegate bool Visitor(nint handle, nint value);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor, nint value);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle, out uint process);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint handle);
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
    private static object Field(object owner, string name) => owner.GetType().GetField(name, Private)!.GetValue(owner)!;
    private static JsonElement State(object owner) => JsonSerializer.SerializeToElement(owner.GetType().GetMethod("Snapshot", Private)!.Invoke(owner, null));

    internal static async Task RunAsync(Window window, string output, string reference, string hostAssembly)
    {
        var referenceBytes = await File.ReadAllBytesAsync(reference); using var source = JsonDocument.Parse(referenceBytes);
        Require(source.RootElement.GetProperty("format").GetString() == "orchard-overlay-combined-source-v1", "Unexpected combined reference");
        var inputs = source.RootElement.GetProperty("cases").EnumerateArray().Select(c => c.Clone()).ToArray(); Require(inputs.Length == 6, "Incomplete combined cases");
        var library = typeof(BleeckerOverlaySurface).Assembly;
        var root = new Canvas { UseLayoutRounding = false }; window.Content = root;
        var folder = Path.GetDirectoryName(output)!; var width = 520; var height = 440; var guards = 0; var focus = 0; var activations = 0; var stage = "initial";
        var compositorCommits = 0;
        var cases = new List<object>(); var rejections = new List<object>(); object? legacy = null; IDisposable? active = null;
        root.GotFocus += (_, _) => focus++; window.Activated += (_, e) => { if (e.WindowActivationState != WindowActivationState.Deactivated) activations++; };
        int Registry(string name) => (library.GetType(name, true)!.GetField("devices", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null) as System.Collections.IDictionary)?.Count ?? 0;
        object Registries() => new { near = Registry("Orchard.BleeckerNearShadowDevice"), far = Registry("Orchard.BleeckerFarShadowDevice") };
        IDisposable Construct(string name, params object[] args) => (IDisposable)Activator.CreateInstance(library.GetType(name, true)!, Private, null, args, null)!;
        IDisposable Attach(BleeckerOverlaySurface target) => Construct("Orchard.BleeckerCombinedShadowLayer", target, new Vector4(2, 3, 5, 15) / 255, new Vector4(7, 15, 21, 71) / 255);
        void Guard()
        {
            guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var visible = false;
            EnumWindows((h, _) => { GetWindowThreadProcessId(h, out var p); visible |= p == Environment.ProcessId && IsWindowVisible(h); return true; }, 0);
            Require(!visible && !window.Visible && !window.AppWindow.IsVisible && foreground != Environment.ProcessId && focus == 0 && activations == 0, "Combined fixture left hidden boundary");
            Require(root.XamlRoot is null || VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Count == 0, "Combined fixture opened a popup");
        }
        async Task Layout()
        {
            for (var pass = 0; pass < 3; pass++) { var idle = new TaskCompletionSource(); Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => idle.SetResult()), "Combined dispatcher unavailable"); await idle.Task; root.Measure(new(width, height)); root.Arrange(new(0, 0, width, height)); root.UpdateLayout(); Guard(); }
            await Task.Delay(20);
            await Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.GetElementVisual(root).Compositor.RequestCommitAsync();
            compositorCommits++; Guard();
        }
        async Task<(byte[] Pixels, object Record)> Capture(string id)
        {
            await Layout(); byte[]? previous = null; byte[]? pixels = null; var attempt = 0;
            for (; attempt < 6; attempt++) { var bitmap = new RenderTargetBitmap(); await bitmap.RenderAsync(root, width, height); pixels = (await bitmap.GetPixelsAsync()).ToArray(); Require(bitmap.PixelWidth == width && bitmap.PixelHeight == height && pixels.Length == width * height * 4, "Incomplete combined frame"); if (previous is not null && previous.AsSpan().SequenceEqual(pixels)) break; previous = pixels; await Layout(); }
            Require(attempt < 6 && pixels is not null, "Unstable combined frame"); var raw = pixels!; var file = id + ".bgra8"; var pngFile = id + ".png"; await File.WriteAllBytesAsync(Path.Combine(folder, file), raw);
            using var stream = new InMemoryRandomAccessStream(); var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream); encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, (uint)width, (uint)height, 96, 96, ScreenshotPngCodec.EncodeBgra(raw)); await encoder.FlushAsync(); stream.Seek(0); using (var destination = File.Create(Path.Combine(folder, pngFile))) await stream.AsStreamForRead().CopyToAsync(destination);
            Guard(); return (raw, new { file, sha256 = Hash(raw), pngFile, pngSha256 = Hash(File.ReadAllBytes(Path.Combine(folder, pngFile))), width, height, stableFrames = 2, attempts = attempt + 1 });
        }
        object Order(IDisposable owner)
        {
            var near = Field(owner, "near"); var far = Field(owner, "far"); var container = (ContainerVisual)Field(near, "container");
            Require(ReferenceEquals(container, Field(far, "container")) && container.Clip is null, "Combined owners do not share the unclipped container");
            var children = container.Children.Cast<SpriteVisual>().ToArray(); var originalNear = (SpriteVisual)Field(near, "originalNear"); var originalFar = (SpriteVisual)Field(far, "originalFar");
            Require(children.Length == 4 && ReferenceEquals(children[0], originalNear) && ReferenceEquals(children[1], Field(near, "sprite")) && ReferenceEquals(children[2], originalFar) && ReferenceEquals(children[3], Field(far, "sprite")), "Combined replacement changed source stacking");
            Require(!originalNear.IsVisible && !originalFar.IsVisible, "Combined graph paints an original shadow twice");
            return new { children = 4, nearCandidateIndex = 1, farCandidateIndex = 3, originalsHidden = true, sharedContainer = true };
        }
        static float[] Matrix(Matrix3x2 value) => [value.M11, value.M12, value.M21, value.M22, value.M31, value.M32];
        try
        {
            foreach (var spec in inputs)
            {
                stage = spec.GetProperty("id").GetString()!.Replace('/', '-'); await File.WriteAllTextAsync(Path.Combine(folder, "combined-stage.json"), JsonSerializer.Serialize(new { stage, cases = cases.Count, guards }));
                root.Children.Clear(); var geometry = spec.GetProperty("geometry"); var box = geometry.GetProperty("box"); var viewport = geometry.GetProperty("viewport"); width = viewport.GetProperty("width").GetInt32(); height = viewport.GetProperty("height").GetInt32(); root.Width = width; root.Height = height; root.Clip = new RectangleGeometry { Rect = new(0, 0, width, height) }; window.AppWindow.ResizeClient(new(width, height));
                var rgba = spec.GetProperty("canvas").GetProperty("rgba"); root.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(rgba[3].GetByte(), rgba[0].GetByte(), rgba[1].GetByte(), rgba[2].GetByte()));
                var transparent = new SolidColorBrush(Microsoft.UI.Colors.Transparent); var target = new BleeckerOverlaySurface { UseLayoutRounding = false, Width = box.GetProperty("width").GetDouble(), Height = box.GetProperty("height").GetDouble(), CornerRadius = new(geometry.GetProperty("radius").GetDouble()), Background = transparent, BorderBrush = transparent, BorderThickness = new(1) }; Canvas.SetLeft(target, box.GetProperty("x").GetDouble()); Canvas.SetTop(target, box.GetProperty("y").GetDouble()); root.Children.Add(target); await Layout();
                Require(target.IsLoaded && root.XamlRoot?.RasterizationScale == 1, "Combined fixture requires loaded scale-one root"); var paint = target.Background; var defaultBefore = await Capture(stage + "-default-before");
                if (legacy is null)
                {
                    using var single = Construct("Orchard.BleeckerNearShadowLayer", target, new Vector2(0, 4), new Vector4(2, 3, 5, 15) / 255); var rejected = false; string? errorText = null;
                    try { using var invalid = Construct("Orchard.BleeckerFarShadowLayer", target, new Vector4(7, 15, 21, 71) / 255); }
                    catch (TargetInvocationException error) when (error.InnerException is InvalidOperationException) { rejected = true; errorText = error.InnerException.Message; }
                    Require(rejected, "Standalone owners silently accepted an incompatible graph"); single.Dispose(); await Layout(); var legacyRestored = await Capture(stage + "-legacy-restored"); Require(defaultBefore.Pixels.AsSpan().SequenceEqual(legacyRestored.Pixels), "Legacy rejection damaged original graph"); legacy = new { incompatibleStandaloneAttachmentRejected = true, error = errorText, restored = legacyRestored.Record, originalPixelsRestored = true };
                }
                if (cases.Count == 0)
                {
                    foreach (var slot in new[] { "near-tint", "far-tint" })
                    {
                        var rejected = false; var validNear = new Vector4(2, 3, 5, 15) / 255; var validFar = new Vector4(7, 15, 21, 71) / 255;
                        try { using var invalid = Construct("Orchard.BleeckerCombinedShadowLayer", target, slot == "near-tint" ? new Vector4(float.NaN) : validNear, slot == "far-tint" ? new Vector4(float.NaN) : validFar); }
                        catch (TargetInvocationException error) when (error.InnerException is ArgumentOutOfRangeException) { rejected = true; }
                        Require(rejected && Registry("Orchard.BleeckerNearShadowDevice") == 0 && Registry("Orchard.BleeckerFarShadowDevice") == 0, "Combined constructor rejection leaked a candidate or cache");
                        var restoredFailure = await Capture(stage + "-rejected-" + slot); Require(defaultBefore.Pixels.AsSpan().SequenceEqual(restoredFailure.Pixels), "Failed combined constructor changed default pixels");
                        rejections.Add(new { id = slot, rejected = true, frame = restoredFailure.Record, originalPixelsRestored = true, registries = Registries() });
                    }
                    foreach (var geometryFailure in new[] { "small-near", "rounded-far" })
                    {
                        if (geometryFailure == "small-near") { target.Width = 30; target.Height = 30; } else target.CornerRadius = new(20.5);
                        await Layout(); var rejected = false;
                        try { using var invalid = Attach(target); }
                        catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { rejected = true; }
                        Require(rejected && Registry("Orchard.BleeckerNearShadowDevice") == 0 && Registry("Orchard.BleeckerFarShadowDevice") == 0, "Unsupported combined geometry leaked graph or cache ownership");
                        target.Width = box.GetProperty("width").GetDouble(); target.Height = box.GetProperty("height").GetDouble(); target.CornerRadius = new(geometry.GetProperty("radius").GetDouble()); await Layout();
                        var restoredFailure = await Capture(stage + "-rejected-" + geometryFailure); Require(defaultBefore.Pixels.AsSpan().SequenceEqual(restoredFailure.Pixels), "Unsupported geometry rejection damaged restored default pixels");
                        rejections.Add(new { id = geometryFailure, rejected = true, frame = restoredFailure.Record, originalPixelsRestored = true, registries = Registries() });
                    }
                }
                active = Attach(target); await Layout(); var order = Order(active); var loaded = State(active); var frame = await Capture(stage); var near = Field(active, "near"); var far = Field(active, "far"); var nearBrush = Field(near, "brush"); var farBrush = Field(far, "brush");
                var originalNear = (SpriteVisual)Field(near, "originalNear"); var originalFar = (SpriteVisual)Field(far, "originalFar"); var originalOutside = (CompositionGeometricClip)Field(near, "outside");
                if (cases.Count == 0)
                {
                    var rejected = false; try { using var invalid = Attach(target); } catch (TargetInvocationException error) when (error.InnerException is InvalidOperationException) { rejected = true; }
                    Require(rejected && ReferenceEquals(nearBrush, Field(near, "brush")) && ReferenceEquals(farBrush, Field(far, "brush")), "Duplicate combined attachment changed the active graph");
                    await Layout(); Order(active); var duplicate = await Capture(stage + "-rejected-duplicate"); Require(frame.Pixels.AsSpan().SequenceEqual(duplicate.Pixels), "Duplicate combined attachment changed visible pixels");
                    rejections.Add(new { id = "duplicate-owner", rejected = true, frame = duplicate.Record, originalPixelsRestored = true, registries = Registries(), retainedOwner = State(active) });
                }
                target.Width += 37; target.Height += 19; Canvas.SetLeft(target, Canvas.GetLeft(target) + .375); Canvas.SetTop(target, Canvas.GetTop(target) + .1875); target.CornerRadius = new(13.25); await Layout(); var changed = State(active); Order(active);
                target.Width = box.GetProperty("width").GetDouble(); target.Height = box.GetProperty("height").GetDouble(); Canvas.SetLeft(target, box.GetProperty("x").GetDouble()); Canvas.SetTop(target, box.GetProperty("y").GetDouble()); target.CornerRadius = new(geometry.GetProperty("radius").GetDouble()); await Layout(); var roundtrip = await Capture(stage + "-roundtrip");
                await File.WriteAllTextAsync(Path.Combine(folder, "combined-roundtrip-state.json"), JsonSerializer.Serialize(new { stage, loaded, changed, restored = State(active), graphRetained = ReferenceEquals(nearBrush, Field(near, "brush")) && ReferenceEquals(farBrush, Field(far, "brush")), before = frame.Record, after = roundtrip.Record, compositorCommits }, new JsonSerializerOptions { WriteIndented = true }));
                Require(frame.Pixels.AsSpan().SequenceEqual(roundtrip.Pixels) && ReferenceEquals(nearBrush, Field(near, "brush")) && ReferenceEquals(farBrush, Field(far, "brush")), "Combined geometry update replaced graphs or changed restored pixels");
                var beforeReset = State(active); active.GetType().GetMethod("InvalidateGeneratedResources", Private)!.Invoke(active, null); await Layout(); var resetState = State(active); var reset = await Capture(stage + "-reset"); Require(frame.Pixels.AsSpan().SequenceEqual(reset.Pixels), "Combined resource regeneration changed visible pixels");
                var disposeNear = cases.Count % 2 == 0; ((IDisposable)(disposeNear ? near : far)).Dispose(); await Layout(); var partialState = State(active); var partial = await Capture(stage + "-partial");
                var nearClip = originalNear.Clip as CompositionGeometricClip; var farClip = originalFar.Clip as CompositionGeometricClip;
                Require(nearClip is not null && farClip is not null && !ReferenceEquals(nearClip, farClip) && ReferenceEquals(nearClip.Geometry, originalOutside.Geometry) && ReferenceEquals(farClip.Geometry, originalOutside.Geometry), "Partial disposal lost independent translated clips");
                Require(originalNear.IsVisible == disposeNear && originalFar.IsVisible != disposeNear && nearClip!.TransformMatrix == originalOutside.TransformMatrix * Matrix3x2.CreateTranslation(-originalNear.Offset.X, -originalNear.Offset.Y) && farClip!.TransformMatrix == originalOutside.TransformMatrix * Matrix3x2.CreateTranslation(-originalFar.Offset.X, -originalFar.Offset.Y), "Partial disposal changed original visibility or clip coordinates");
                var partialContract = new { independentClips = true, sharedOriginalGeometry = true, originalNearVisible = originalNear.IsVisible, originalFarVisible = originalFar.IsVisible, baseMatrix = Matrix(originalOutside.TransformMatrix), nearClipMatrix = Matrix(nearClip!.TransformMatrix), farClipMatrix = Matrix(farClip!.TransformMatrix), nearOffset = new[] { originalNear.Offset.X, originalNear.Offset.Y }, farOffset = new[] { originalFar.Offset.X, originalFar.Offset.Y } };
                active.Dispose(); var disposed = State(active); await Layout(); var defaultAfter = await Capture(stage + "-default-after"); Require(defaultBefore.Pixels.AsSpan().SequenceEqual(defaultAfter.Pixels), "Combined disposal did not restore original default pixels"); Require(Registry("Orchard.BleeckerNearShadowDevice") == 0 && Registry("Orchard.BleeckerFarShadowDevice") == 0, "Combined disposal retained a cache registry");
                active = Attach(target); await Layout(); Order(active); var reattached = await Capture(stage + "-reattached"); Require(frame.Pixels.AsSpan().SequenceEqual(reattached.Pixels), "Combined reattachment changed pixels"); root.Children.Remove(target); await Layout(); var unloaded = State(active); Require(ReferenceEquals(target.Background, paint) && target.Opacity == 1, "Combined owner changed caller paint"); Require(Registry("Orchard.BleeckerNearShadowDevice") == 0 && Registry("Orchard.BleeckerFarShadowDevice") == 0, "Combined unload retained caches");
                cases.Add(new { id = spec.GetProperty("id").GetString(), sourceSha256 = spec.GetProperty("sha256").GetString(), frame = frame.Record, loaded, changed, roundtrip = roundtrip.Record, order, beforeReset, resetState, reset = reset.Record, partialDisposed = disposeNear ? "near" : "far", partialState, partialContract, partial = partial.Record, disposed, defaultBefore = defaultBefore.Record, defaultAfter = defaultAfter.Record, reattached = reattached.Record, unloaded, registries = Registries(), graphRetained = true, callerPaintRetained = true, defaultPixelsRestored = true, automaticUnload = true }); active.Dispose(); active = null;
            }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = true, format = "orchard-overlay-combined-native-v3", compositorCommits, referenceSha256 = Hash(referenceBytes), librarySha256 = Hash(File.ReadAllBytes(library.Location)), hostSha256 = Hash(File.ReadAllBytes(hostAssembly)), legacy, rejections, cases, finalRegistries = Registries(), guards, focusEvents = focus, activations, hostVisible = window.Visible, hardwareInputSent = false, cpuBitmapInputs = false, readbacksUsedAsRendererInputs = false, publicDefaultChanged = false, pngExport = ScreenshotPngCodec.Name, referenceCodecSha256 = ScreenshotPngCodec.Sha256, nativeComponentAccepted = false }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(0);
        }
        catch (Exception error) { try { active?.Dispose(); } catch { } await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = false, stage, compositorCommits, error = error.ToString(), legacy, cases, guards, focusEvents = focus, activations }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(1); }
    }
}
