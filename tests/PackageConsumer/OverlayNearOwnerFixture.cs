using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Graphics.Canvas;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Testing;

internal static class OverlayNearOwnerFixture
{
    private delegate bool Visitor(nint handle, nint value);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor, nint value);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle, out uint process);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint handle);
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    internal static async Task RunAsync(Window window, string output, string referencePath, string hostAssembly)
    {
        var referenceBytes = await File.ReadAllBytesAsync(referencePath);
        using var reference = JsonDocument.Parse(referenceBytes);
        var inputs = reference.RootElement.GetProperty("cases").EnumerateArray().Select(c => c.Clone()).ToArray();
        Require(inputs.Length == 48 && Hash(referenceBytes) == "98352ef757aef4ab6833a987865196f91a5befe65e9fcf4a2411ab0d39d27adc", "Wrong owner source reference");
        var folder = Path.GetDirectoryName(output)!;
        var root = new Canvas { UseLayoutRounding = false, Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent) };
        window.Content = root;
        var library = typeof(BleeckerOverlaySurface).Assembly;
        var type = library.GetType("Orchard.BleeckerNearShadowLayer", throwOnError: true)!;
        var live = new List<IDisposable>();
        var cases = new List<object>();
        var updates = new List<object>();
        var bindings = new List<object>();
        var ownership = new List<object>();
        var leases = new List<object>();
        var suspensions = new List<object>();
        var templates = new List<object>();
        var restorations = new List<object>();
        var rejectedGeometry = new List<object>();
        var width = 520; var height = 440; var guards = 0; var focus = 0; var activations = 0; var stage = "initial";
        root.GotFocus += (_, _) => focus++;
        window.Activated += (_, a) => { if (a.WindowActivationState != WindowActivationState.Deactivated) activations++; };
        void Guard()
        {
            guards++;
            GetWindowThreadProcessId(GetForegroundWindow(), out var foreground);
            var visible = false;
            EnumWindows((h, _) => { GetWindowThreadProcessId(h, out var process); visible |= process == Environment.ProcessId && IsWindowVisible(h); return true; }, 0);
            Require(!visible && !window.Visible && !window.AppWindow.IsVisible && foreground != Environment.ProcessId && focus == 0 && activations == 0, "Owner probe left hidden boundary");
            Require(root.XamlRoot is null || VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Count == 0, "Owner probe opened a popup");
        }
        async Task Layout()
        {
            for (var pass = 0; pass < 3; pass++)
            {
                var idle = new TaskCompletionSource();
                Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => idle.SetResult()), "Owner dispatcher unavailable");
                await idle.Task;
                root.Measure(new(width, height)); root.Arrange(new(0, 0, width, height)); root.UpdateLayout(); Guard();
            }
            await Task.Delay(20);
        }
        void Viewport(JsonElement spec)
        {
            width = spec.GetProperty("viewport").GetProperty("width").GetInt32();
            height = spec.GetProperty("viewport").GetProperty("height").GetInt32();
            root.Width = width; root.Height = height; window.AppWindow.ResizeClient(new(width, height));
            var rgb = spec.GetProperty("canvas").GetProperty("rgba");
            root.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(rgb[3].GetByte(), rgb[0].GetByte(), rgb[1].GetByte(), rgb[2].GetByte()));
        }
        void Geometry(BleeckerOverlaySurface target, JsonElement spec)
        {
            var b = spec.GetProperty("box"); target.Width = b.GetProperty("width").GetDouble(); target.Height = b.GetProperty("height").GetDouble();
            target.CornerRadius = new(spec.GetProperty("radius").GetInt32()); Canvas.SetLeft(target, b.GetProperty("x").GetDouble()); Canvas.SetTop(target, b.GetProperty("y").GetDouble());
        }
        BleeckerOverlaySurface Component(JsonElement spec)
        {
            var target = new BleeckerOverlaySurface { UseLayoutRounding = false, Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent), BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Transparent), BorderThickness = new(1) };
            Geometry(target, spec); root.Children.Add(target); return target;
        }
        static Vector2 Offset(JsonElement spec) => new((float)spec.GetProperty("offset").GetProperty("x").GetDouble(), (float)spec.GetProperty("offset").GetProperty("y").GetDouble());
        IDisposable Attach(BleeckerOverlaySurface target, JsonElement spec)
        {
            var tint = spec.GetProperty("white").GetBoolean() ? Vector4.One : new Vector4(2 / 255f, 3 / 255f, 5 / 255f, 15 / 255f);
            var owner = (IDisposable)Activator.CreateInstance(type, Private, null, [target, Offset(spec), tint], null)!;
            live.Add(owner); return owner;
        }
        object Field(IDisposable owner, string name) => type.GetField(name, Private)!.GetValue(owner)!;
        JsonElement State(IDisposable owner) => JsonSerializer.SerializeToElement(type.GetMethod("Snapshot", Private)!.Invoke(owner, null));
        void SetOffset(IDisposable owner, JsonElement spec) => type.GetProperty("Offset", Private)!.SetValue(owner, Offset(spec));
        static ContainerVisual Container(BleeckerOverlaySurface target) => (ContainerVisual)typeof(BleeckerOverlaySurface).GetField("shadowVisual", Private)!.GetValue(target)!;
        static void Cleared(BleeckerOverlaySurface target) => Require(typeof(BleeckerOverlaySurface).GetField("shadowVisual", Private)!.GetValue(target) is null && ((System.Collections.ICollection)typeof(BleeckerOverlaySurface).GetField("shadowResources", Private)!.GetValue(target)!).Count == 0, "Original component graph survived unload");
        async Task<(byte[] Pixels, object Record)> Capture(string id)
        {
            await Layout(); byte[]? prior = null, raw = null; var tries = 0;
            for (; tries < 6; tries++)
            {
                var bitmap = new RenderTargetBitmap(); await bitmap.RenderAsync(root, width, height); raw = (await bitmap.GetPixelsAsync()).ToArray();
                Require(bitmap.PixelWidth == width && bitmap.PixelHeight == height && raw.Length == width * height * 4, "Owner capture has no complete pixel buffer");
                if (prior is not null && prior.AsSpan().SequenceEqual(raw)) break;
                prior = raw; await Layout();
            }
            Require(tries < 6, "Owner frame did not settle");
            var file = id.Replace('/', '-') + ".bgra8"; var pngFile = id.Replace('/', '-') + ".png";
            await File.WriteAllBytesAsync(Path.Combine(folder, file), raw!);
            using var stream = new InMemoryRandomAccessStream(); var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
            // Preserve raw native bytes and serialize through the independently
            // verified, injective encoder mapping of the pinned source browser.
            var pixelBytes = raw ?? throw new InvalidOperationException("Owner PNG export has no pixel buffer.");
            var straight = ReferencePngCodec.EncodeBgra(pixelBytes);
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, (uint)width, (uint)height, 96, 96, straight); await encoder.FlushAsync(); stream.Seek(0);
            using (var target = File.Create(Path.Combine(folder, pngFile))) await stream.AsStreamForRead().CopyToAsync(target);
            Guard(); return (raw!, new { file, sha256 = Hash(raw!), pngFile, pngSha256 = Hash(File.ReadAllBytes(Path.Combine(folder, pngFile))), width, height, stableFrames = 2, attempts = tries + 1 });
        }
        try
        {
            foreach (var spec in inputs)
            {
                stage = spec.GetProperty("id").GetString()!; await File.WriteAllTextAsync(Path.Combine(folder, "owner-stage.json"), JsonSerializer.Serialize(new { stage, cases = cases.Count, guards }));
                root.Children.Clear(); Viewport(spec); await Layout();
                var component = Component(spec); await Layout();
                Require(component.IsLoaded && root.XamlRoot is not null && root.XamlRoot.RasterizationScale == 1, "Owner fixture requires a loaded scale-one component");
                var originalBackground = component.Background; var original = Container(component); var oldLayers = original.Children.Cast<SpriteVisual>().ToArray();
                Require(oldLayers.Length == 2, "Original overlay layers absent");
                var checkFar = stage.StartsWith("baseline-", StringComparison.Ordinal) && !spec.GetProperty("white").GetBoolean();
                object? farBefore = null, farAfter = null, combined = null; byte[]? beforePixels = null;
                if (checkFar) { oldLayers[0].IsVisible = false; var before = await Capture(stage + "-far-before"); farBefore = before.Record; beforePixels = before.Pixels; oldLayers[0].IsVisible = true; }
                var owner = Attach(component, spec); await Layout(); var sprite = (SpriteVisual)Field(owner, "sprite"); var far = (SpriteVisual)Field(owner, "far");
                if (checkFar) { sprite.IsVisible = false; var after = await Capture(stage + "-far-after"); Require(beforePixels!.AsSpan().SequenceEqual(after.Pixels), "Owner attachment changed far clipping"); farAfter = after.Record; sprite.IsVisible = true; }
                far.IsVisible = false; var primary = await Capture(stage + "-near");
                Require(ReferenceEquals(component.Background, originalBackground) && component.Opacity == 1, "Owner changed the caller background/opacity");
                if (checkFar) { far.IsVisible = true; combined = (await Capture(stage + "-combined")).Record; far.IsVisible = false; }
                var loaded = State(owner);
                if (stage == "baseline-fixture-Light")
                {
                    var graph = Field(owner, "container"); var brush = Field(owner, "brush"); var drawingSurface = Field(owner, "surface"); var shared = Field(owner, "shared");
                    var donor = new TextBlock { Foreground = originalBackground };
                    component.SetBinding(Control.BackgroundProperty, new Binding { Source = donor, Path = new("Foreground"), Mode = BindingMode.OneWay });
                    foreach (var next in inputs.Where(c => c.GetProperty("canvas").GetProperty("theme").GetString() == "Light"))
                    {
                        var before = State(owner); Viewport(next); Geometry(component, next); SetOffset(owner, next); await Layout();
                        Require(ReferenceEquals(Field(owner, "container"), graph) && ReferenceEquals(Field(owner, "brush"), brush) && ReferenceEquals(Field(owner, "surface"), drawingSurface), "Automatic update replaced the loaded graph");
                        Require(component.GetBindingExpression(Control.BackgroundProperty) is not null && ReferenceEquals(component.Background, originalBackground), "Automatic update lost the caller binding");
                        Require(State(owner).GetProperty("shared").GetProperty("kernelCount").GetInt32() == 1, "Inactive radius survived an automatic update");
                        updates.Add(new { id = next.GetProperty("id").GetString(), sourceSha256 = next.GetProperty("sha256").GetString(), frame = (await Capture("update-" + next.GetProperty("id").GetString())).Record, before, after = State(owner), graphRetained = true, brushRetained = true, drawingSurfaceRetained = true, bindingRetained = true, driverIssuedDraw = false });
                    }
                    Viewport(spec); Geometry(component, spec); SetOffset(owner, spec); await Layout();
                    var gradient = new LinearGradientBrush { StartPoint = new(0, 0), EndPoint = new(1, 0) }; gradient.GradientStops.Add(new() { Color = Windows.UI.Color.FromArgb(90, 48, 96, 144), Offset = 0 }); gradient.GradientStops.Add(new() { Color = Windows.UI.Color.FromArgb(210, 210, 80, 30), Offset = 1 });
                    Brush[] paints = [new SolidColorBrush(Windows.UI.Color.FromArgb(128, 48, 96, 144)), gradient, originalBackground!];
                    for (var paint = 0; paint < paints.Length; paint++)
                    {
                        donor.Foreground = paints[paint]; await Layout(); Require(ReferenceEquals(component.Background, paints[paint]) && component.GetBindingExpression(Control.BackgroundProperty) is not null && component.Opacity == 1, "Bound paint was replaced");
                        sprite.IsVisible = true; var on = await Capture("bound-" + paint + "-on"); sprite.IsVisible = false; var off = await Capture("bound-" + paint + "-off"); sprite.IsVisible = true;
                        var interiorPixels = 0; for (var y = 155; y < 245; y++) for (var x = 175; x < 345; x++) { var at = (y * width + x) * 4; Require(on.Pixels.AsSpan(at, 4).SequenceEqual(off.Pixels.AsSpan(at, 4)), "Owned near layer paints the caller interior"); interiorPixels++; }
                        bindings.Add(new { id = paint == 0 ? "partial" : paint == 1 ? "gradient" : "transparent", on = on.Record, off = off.Record, interiorPixels, backgroundReferenceRetained = true, bindingRetained = true, opacityRetained = true });
                    }
                    donor.Foreground = originalBackground; await Layout();
                    var templateBefore = State(owner); var savedTemplate = component.Template; component.Template = null; component.ApplyTemplate(); component.Template = savedTemplate; component.ApplyTemplate(); await Layout();
                    far = (SpriteVisual)Field(owner, "far"); far.IsVisible = false; sprite = (SpriteVisual)Field(owner, "sprite");
                    templates.Add(new { id = stage, before = templateBefore, after = State(owner), frame = (await Capture("template-restored")).Record, oldGraphReplaced = !ReferenceEquals(Field(owner, "brush"), brush), driverIssuedDraw = false });
                    foreach (var mode in new[] { "host-hidden", "component-suppressed", "zero-width", "zero-height", "empty-template" })
                    {
                        var before = State(owner); var currentHost = (Grid)Field(owner, "host");
                        if (mode == "host-hidden") currentHost.Visibility = Visibility.Collapsed;
                        else if (mode == "component-suppressed")
                        {
                            currentHost.Visibility = Visibility.Collapsed;
                            typeof(BleeckerOverlaySurface).GetMethod("ClearShadow", Private)!.Invoke(component, null);
                        }
                        else if (mode == "zero-width") component.Width = 0;
                        else if (mode == "zero-height") component.Height = 0;
                        else
                        {
                            component.Template = (ControlTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='ContentControl'><Grid /></ControlTemplate>");
                            component.ApplyTemplate();
                        }
                        await Layout(); var suspended = State(owner);
                        Require(!suspended.GetProperty("disposed").GetBoolean() && suspended.GetProperty("graphResourceCount").GetInt32() == 0 && suspended.GetProperty("shared").GetProperty("kernelCount").GetInt32() == 0 && suspended.GetProperty("shared").GetProperty("kernelLeaseCount").GetInt32() == 0, "Suspended owner retained graph or kernel resources");
                        var blank = await Capture("suspended-" + mode);
                        if (mode == "host-hidden") currentHost.Visibility = Visibility.Visible;
                        else if (mode == "component-suppressed")
                        {
                            currentHost.Visibility = Visibility.Visible;
                            typeof(BleeckerOverlaySurface).GetMethod("UpdateShadow", Private)!.Invoke(component, null);
                        }
                        else if (mode == "empty-template") { component.Template = savedTemplate; component.ApplyTemplate(); }
                        else Geometry(component, spec);
                        await Layout(); ((SpriteVisual)Field(owner, "far")).IsVisible = false; sprite = (SpriteVisual)Field(owner, "sprite");
                        var resumed = await Capture("resumed-" + mode); Require(primary.Pixels.AsSpan().SequenceEqual(resumed.Pixels), "Resumed owner changed near pixels");
                        Require(component.GetBindingExpression(Control.BackgroundProperty) is not null && ReferenceEquals(component.Background, originalBackground), "Suspension lost the caller binding");
                        suspensions.Add(new { mode, before, suspended, after = State(owner), blank = blank.Record, resumed = resumed.Record, bindingRetained = true, driverIssuedDraw = false, osSettingsChanged = false });
                    }
                    var second = Component(spec); await Layout(); var secondOwner = Attach(second, spec); await Layout(); var secondSprite = (SpriteVisual)Field(secondOwner, "sprite"); ((SpriteVisual)Field(secondOwner, "far")).IsVisible = false; secondSprite.IsVisible = false;
                    Require(ReferenceEquals(Field(secondOwner, "shared"), shared), "Concurrent components did not share compositor resources");
                    var firstEncoded = (Microsoft.Graphics.Canvas.Effects.PixelShaderEffect)Field(owner, "encoded"); var secondEncoded = (Microsoft.Graphics.Canvas.Effects.PixelShaderEffect)Field(secondOwner, "encoded"); Require(ReferenceEquals(firstEncoded.Source1, secondEncoded.Source1), "Concurrent components duplicated a kernel bitmap");
                    var sharedKernel = firstEncoded.Source1; var leaseBefore = new { first = State(owner), second = State(secondOwner) };
                    var seven = inputs.Single(c => c.GetProperty("id").GetString() == "fixture-r7-Light"); Geometry(second, seven); SetOffset(secondOwner, seven); await Layout();
                    Require(ReferenceEquals(firstEncoded.Source1, sharedKernel) && !ReferenceEquals(firstEncoded.Source1, secondEncoded.Source1), "One radius change replaced another owner's kernel");
                    var different = new { first = State(owner), second = State(secondOwner) }; Require(State(owner).GetProperty("shared").GetProperty("kernelCount").GetInt32() == 2, "Concurrent active radii were not retained");
                    Canvas.SetLeft(component, 161); await Layout(); Canvas.SetLeft(component, 160); await Layout();
                    var firstPreserved = await Capture("lease-first-preserved"); Require(primary.Pixels.AsSpan().SequenceEqual(firstPreserved.Pixels), "A different radius invalidated the first owner");
                    sprite.IsVisible = false; secondSprite.IsVisible = true; var secondSeven = await Capture("lease-second-r7");
                    secondSprite.IsVisible = false; sprite.IsVisible = true; Geometry(second, spec); SetOffset(secondOwner, spec); await Layout();
                    Require(ReferenceEquals(firstEncoded.Source1, sharedKernel) && ReferenceEquals(firstEncoded.Source1, secondEncoded.Source1) && State(owner).GetProperty("shared").GetProperty("kernelCount").GetInt32() == 1, "Unleased radius was not evicted");
                    leases.Add(new { before = leaseBefore, different, restored = new { first = State(owner), second = State(secondOwner) }, firstPreserved = firstPreserved.Record, secondSeven = secondSeven.Record, originalKernelRetained = true, inactiveRadiusEvicted = true, driverIssuedDraw = false });
                    var bothBefore = new { first = State(owner), second = State(secondOwner) }; type.GetMethod("InvalidateGeneratedResources", Private)!.Invoke(owner, null); await Layout();
                    Require(ReferenceEquals(firstEncoded.Source1, secondEncoded.Source1), "Regenerated kernel was not shared"); var reset = await Capture("shared-reset"); Require(primary.Pixels.AsSpan().SequenceEqual(reset.Pixels), "Generated-resource reset changed near pixels");
                    var bothAfter = new { first = State(owner), second = State(secondOwner) }; var secondBrush = Field(secondOwner, "brush"); root.Children.Remove(component); await Layout(); Cleared(component);
                    Require(State(owner).GetProperty("disposed").GetBoolean() && ReferenceEquals(Field(secondOwner, "brush"), secondBrush), "One unload invalidated the surviving owner"); secondSprite.IsVisible = true; var survivor = await Capture("surviving-owner"); Require(primary.Pixels.AsSpan().SequenceEqual(survivor.Pixels), "Surviving owner lost its pixels");
                    var surviving = State(secondOwner); root.Children.Remove(second); await Layout(); Cleared(second);
                    ownership.Add(new { bothBefore, bothAfter, reset = reset.Record, survivor = survivor.Record, surviving, firstDisposed = State(owner), lastDisposed = State(secondOwner), sharedDeviceDisposed = false, actualDeviceLossSimulated = false, generatedResourcesInvalidated = true, kernelBitmapShared = true, survivingBrushRetained = true });
                }
                else { root.Children.Remove(component); await Layout(); Cleared(component); }
                var unloaded = State(owner); Require(unloaded.GetProperty("disposed").GetBoolean() && unloaded.GetProperty("graphResourceCount").GetInt32() == 0, "Owner did not release automatically");
                cases.Add(new { id = stage, sourceSha256 = spec.GetProperty("sha256").GetString(), frame = primary.Record, loaded, unloaded, farBefore, farAfter, combined, actualComponentRendered = true, libraryOwnedRenderer = true, backgroundRetained = true, farFrontmost = true, automaticUnload = true });
            }
            var baseline = inputs[0]; Viewport(baseline); var restored = Component(baseline); await Layout();
            var originalPixels = await Capture("detach-before"); var detachable = Attach(restored, baseline); await Layout(); detachable.Dispose(); await Layout();
            var restoredPixels = await Capture("detach-after"); Require(originalPixels.Pixels.AsSpan().SequenceEqual(restoredPixels.Pixels), "Loaded disposal changed the original default renderer");
            Require(Container(restored).Children.Count == 2 && Container(restored).Clip is CompositionGeometricClip, "Loaded disposal did not restore the original graph");
            restorations.Add(new { before = originalPixels.Record, after = restoredPixels.Record, owner = State(detachable), originalDefaultBytesRetained = true, targetStillLoaded = restored.IsLoaded }); root.Children.Remove(restored); await Layout(); Cleared(restored);
            foreach (var shape in new[] { "nonuniform", "small", "fractional-small", "transformed-scale" })
            {
                var target = Component(baseline);
                if (shape == "nonuniform") target.CornerRadius = new(7, 14, 7, 14);
                else if (shape == "small") { target.Width = 30; target.Height = 30; }
                else if (shape == "fractional-small") { target.CornerRadius = new(7.5); target.Width = 58; target.Height = 58; }
                else target.RenderTransform = new ScaleTransform { ScaleX = 1.25, ScaleY = 1.25 };
                await Layout(); var before = await Capture("unsupported-" + shape + "-before"); var rejected = false;
                try { Attach(target, baseline); } catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { rejected = true; }
                Require(rejected, "Unsupported geometry did not fail explicitly");
                var after = await Capture("unsupported-" + shape + "-after"); Require(before.Pixels.AsSpan().SequenceEqual(after.Pixels), "Rejected attachment changed the default renderer");
                var deviceType = library.GetType("Orchard.BleeckerNearShadowDevice", throwOnError: true)!; var registry = (System.Collections.IDictionary)deviceType.GetField("devices", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!; Require(registry.Count == 0, "Rejected attachment leaked a cache owner");
                rejectedGeometry.Add(new { id = shape, before = before.Record, after = after.Record, rejected = true, originalDefaultBytesRetained = true, cacheRegistryEntries = registry.Count }); root.Children.Remove(target); await Layout(); Cleared(target);
            }
            // Shared-device release is process-local; other library renderers must
            // still be able to draw after the last near owner is gone.
            using (var bitmap = new CanvasRenderTarget(CanvasDevice.GetSharedDevice(), 1, 1, 96)) { using (var draw = bitmap.CreateDrawingSession()) draw.Clear(Microsoft.UI.Colors.Red); Require(bitmap.GetPixelBytes().SequenceEqual(new byte[] { 0, 0, 255, 255 }), "Last owner disposed the shared Canvas device"); }
            var shaderResources = library.GetManifestResourceNames().Where(n => n.StartsWith("Orchard.NearShadow.", StringComparison.Ordinal)).Order().Select(n => { using var stream = library.GetManifestResourceStream(n)!; using var bytes = new MemoryStream(); stream.CopyTo(bytes); return new { name = n, sha256 = Hash(bytes.ToArray()), bytes = bytes.Length }; }).ToArray(); Require(shaderResources.Length == 6, "Embedded shaders absent");
            Guard(); await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-overlay-near-owner-native-v2", passed = true, referenceSha256 = Hash(referenceBytes), librarySha256 = Hash(File.ReadAllBytes(library.Location)), hostSha256 = Hash(File.ReadAllBytes(hostAssembly)), shaderResources, cases, updates, bindings, ownership, leases, suspensions, templates, restorations, rejectedGeometry, pngExport = ReferencePngCodec.Name, referenceCodecSha256 = ReferencePngCodec.Sha256, guards, focusEvents = focus, activations, hostVisible = window.Visible, hardwareInputSent = false, cpuBitmapInputs = false, readbacksUsedAsRendererInputs = false, driverIssuesDraws = false, defaultRendererChanged = false, nativeComponentAccepted = false }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(0);
        }
        catch (Exception error)
        {
            foreach (var owner in live) try { owner.Dispose(); } catch { }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = false, stage, error = error.ToString(), cases, updates, bindings, ownership, leases, suspensions, templates, restorations, rejectedGeometry, guards, focusEvents = focus, activations }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(1);
        }
    }
}
