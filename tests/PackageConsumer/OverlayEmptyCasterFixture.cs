using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Testing;

internal static class OverlayEmptyCasterFixture
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private delegate bool Visitor(nint handle, nint value);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor, nint value);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle, out uint process);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint handle);
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    internal static async Task RunAsync(Window window, string output, string reference, string hostAssembly)
    {
        var referenceBytes = await File.ReadAllBytesAsync(reference);
        Require(Hash(referenceBytes) == "feee0444cc32ca0f566d777971f2daff6e2c3ebab61774054151ff08470f5d1c", "Unexpected empty-caster reference");
        using var source = JsonDocument.Parse(referenceBytes);
        var inputs = source.RootElement.GetProperty("cases").EnumerateArray().Where(c => c.GetProperty("selected").GetString() != "near").Select(c => c.Clone()).ToArray();
        Require(inputs.Length == 104, "Incomplete empty-caster reference");
        var library = typeof(BleeckerOverlaySurface).Assembly;
        var root = new Canvas { Width = 520, Height = 440, UseLayoutRounding = false, Clip = new RectangleGeometry { Rect = new(0, 0, 520, 440) } };
        window.Content = root; window.AppWindow.ResizeClient(new(520, 440));
        var folder = Path.GetDirectoryName(output)!; var guards = 0; var focus = 0; var activations = 0; var stage = "initial";
        var cases = new List<object>(); var resetModes = new HashSet<string>(); IDisposable? owner = null;
        root.GotFocus += (_, _) => focus++;
        window.Activated += (_, e) => { if (e.WindowActivationState != WindowActivationState.Deactivated) activations++; };
        int Registry(string type) => (library.GetType(type, true)!.GetField("devices", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null) as System.Collections.IDictionary)?.Count ?? 0;
        void EmptyRegistries() => Require(Registry("Orchard.BleeckerNearShadowDevice") == 0 && Registry("Orchard.BleeckerFarShadowDevice") == 0, "Empty-caster probe leaked a device registry");
        void Guard()
        {
            guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var visible = false;
            EnumWindows((h, _) => { GetWindowThreadProcessId(h, out var p); visible |= p == Environment.ProcessId && IsWindowVisible(h); return true; }, 0);
            Require(!visible && !window.Visible && !window.AppWindow.IsVisible && foreground != Environment.ProcessId && focus == 0 && activations == 0, "Empty-caster probe left hidden boundary");
            Require(root.XamlRoot is null || VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Count == 0, "Empty-caster probe opened a popup");
        }
        async Task Layout()
        {
            for (var i = 0; i < 3; i++) { var idle = new TaskCompletionSource(); Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => idle.SetResult()), "Empty-caster dispatcher unavailable"); await idle.Task; root.Measure(new(520, 440)); root.Arrange(new(0, 0, 520, 440)); root.UpdateLayout(); Guard(); }
            await Task.Delay(20);
        }
        async Task<(byte[] Pixels, object Record)> Capture(string id)
        {
            await Layout(); byte[]? previous = null; byte[]? pixels = null; var attempt = 0;
            for (; attempt < 6; attempt++) { var bitmap = new RenderTargetBitmap(); await bitmap.RenderAsync(root, 520, 440); pixels = (await bitmap.GetPixelsAsync()).ToArray(); Require(bitmap.PixelWidth == 520 && bitmap.PixelHeight == 440 && pixels.Length == 520 * 440 * 4, "Incomplete empty-caster frame"); if (previous is not null && previous.AsSpan().SequenceEqual(pixels)) break; previous = pixels; await Layout(); }
            Require(attempt < 6 && pixels is not null, "Unstable empty-caster frame"); var raw = pixels!; var file = id + ".bgra8"; var pngFile = id + ".png"; await File.WriteAllBytesAsync(Path.Combine(folder, file), raw);
            using var stream = new InMemoryRandomAccessStream(); var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream); encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, 520, 440, 96, 96, ScreenshotPngCodec.EncodeBgra(raw)); await encoder.FlushAsync(); stream.Seek(0); using (var destination = File.Create(Path.Combine(folder, pngFile))) await stream.AsStreamForRead().CopyToAsync(destination);
            Guard(); return (raw, new { file, sha256 = Hash(raw), pngFile, pngSha256 = Hash(File.ReadAllBytes(Path.Combine(folder, pngFile))), width = 520, height = 440, stableFrames = 2, attempts = attempt + 1 });
        }
        try
        {
            foreach (var spec in inputs)
            {
                stage = spec.GetProperty("id").GetString()!; await File.WriteAllTextAsync(Path.Combine(folder, "empty-caster-stage.json"), JsonSerializer.Serialize(new { stage, cases = cases.Count, guards }));
                root.Children.Clear(); var rgba = spec.GetProperty("canvas").GetProperty("rgba"); root.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(rgba[3].GetByte(), rgba[0].GetByte(), rgba[1].GetByte(), rgba[2].GetByte()));
                var geometry = spec.GetProperty("geometry"); var box = geometry.GetProperty("box"); var donor = new TextBlock { Foreground = new SolidColorBrush(Microsoft.UI.Colors.Transparent) };
                var target = new BleeckerOverlaySurface { Width = box.GetProperty("width").GetDouble(), Height = box.GetProperty("height").GetDouble(), CornerRadius = new(geometry.GetProperty("radius").GetDouble()), BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Transparent), BorderThickness = new(1), UseLayoutRounding = false };
                target.SetBinding(Control.BackgroundProperty, new Binding { Source = donor, Path = new("Foreground"), Mode = BindingMode.OneWay });
                Canvas.SetLeft(target, box.GetProperty("x").GetDouble()); Canvas.SetTop(target, box.GetProperty("y").GetDouble()); root.Children.Add(target); await Layout(); Require(target.IsLoaded && root.XamlRoot?.RasterizationScale == 1, "Empty-caster probe requires a loaded scale-one root");
                var before = await Capture(stage + "-default-before"); var mode = spec.GetProperty("selected").GetString()!; var combined = mode == "combined";
                var type = library.GetType(combined ? "Orchard.BleeckerCombinedShadowLayer" : "Orchard.BleeckerFarShadowLayer", true)!;
                var white = spec.GetProperty("white").GetBoolean(); var nearTint = white ? Vector4.One : new Vector4(2, 3, 5, 15) / 255; var farTint = white ? Vector4.One : new Vector4(7, 15, 21, 71) / 255;
                owner = (IDisposable)Activator.CreateInstance(type, Private, null, combined ? [target, nearTint, farTint] : [target, farTint], null)!;
                var far = combined ? (IDisposable)type.GetField("far", Private)!.GetValue(owner)! : owner; var farType = far.GetType();
                object Field(string name) => farType.GetField(name, Private)!.GetValue(far)!;
                JsonElement State() => JsonSerializer.SerializeToElement(type.GetMethod("Snapshot", Private)!.Invoke(owner, null));
                void Isolate() { if (!combined) ((SpriteVisual)Field("near")).IsVisible = false; }
                void Binding() => Require(target.GetBindingExpression(Control.BackgroundProperty) is not null && ReferenceEquals(target.Background, donor.Foreground) && target.Opacity == 1, "Empty-caster transition changed caller paint");
                await Layout(); Isolate(); Binding(); var frame = await Capture(stage); var loaded = State();
                var graph = Field("container"); var brush = Field("brush"); var drawingSurface = Field("surface"); var planes = ((IEnumerable<CompositionDrawingSurface>)Field("planes")).ToArray();
                target.Width = 237; target.Height = 139; await Layout(); Isolate(); Binding(); var positive = State();
                target.Width = box.GetProperty("width").GetDouble(); target.Height = box.GetProperty("height").GetDouble(); await Layout(); Isolate(); Binding(); var restored = State(); var roundtrip = await Capture(stage + "-roundtrip");
                var restoredPlanes = ((IEnumerable<CompositionDrawingSurface>)Field("planes")).ToArray();
                Require(ReferenceEquals(graph, Field("container")) && ReferenceEquals(brush, Field("brush")) && ReferenceEquals(drawingSurface, Field("surface")) && planes.Length == restoredPlanes.Length && planes.Zip(restoredPlanes).All(p => ReferenceEquals(p.First, p.Second)), "Empty-caster resize replaced graph, brush or alpha planes");
                Require(frame.Pixels.AsSpan().SequenceEqual(roundtrip.Pixels), "Empty-caster resize roundtrip changed pixels");
                object? reset = null;
                if (spec.GetProperty("emptyFar").GetBoolean() && resetModes.Add(mode))
                {
                    var prior = State(); type.GetMethod("InvalidateGeneratedResources", Private)!.Invoke(owner, null); await Layout(); Isolate(); Binding(); var after = State(); var regenerated = await Capture(stage + "-regenerated");
                    Require(ReferenceEquals(graph, Field("container")) && ReferenceEquals(brush, Field("brush")) && frame.Pixels.AsSpan().SequenceEqual(regenerated.Pixels), "Empty-caster invalidation changed graph or pixels");
                    reset = new { before = prior, after, frame = regenerated.Record, graphRetained = true, brushRetained = true, bindingRetained = true, actualDeviceLossSimulated = false };
                }
                if (!combined) ((SpriteVisual)Field("near")).IsVisible = true;
                owner.Dispose(); var disposed = State(); owner = null; await Layout(); EmptyRegistries(); Binding(); var afterDefault = await Capture(stage + "-default-after"); Require(before.Pixels.AsSpan().SequenceEqual(afterDefault.Pixels), "Empty-caster disposal changed public default pixels");
                cases.Add(new { id = stage, mode, sourceSha256 = spec.GetProperty("sha256").GetString(), frame = frame.Record, loaded, positive, restored, roundtrip = roundtrip.Record, reset, disposed, defaultBefore = before.Record, defaultAfter = afterDefault.Record, graphRetained = true, surfacePlanesRetained = true, bindingRetained = true, publicDefaultRestored = true, registryEntriesAfterDispose = 0 });
            }
            root.Children.Clear(); await Layout(); EmptyRegistries(); Guard(); Require(cases.Count == 104 && resetModes.Count == 2, "Incomplete empty-caster run");
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = true, format = "orchard-overlay-empty-caster-native-v1", referenceSha256 = Hash(referenceBytes), librarySha256 = Hash(File.ReadAllBytes(library.Location)), hostSha256 = Hash(File.ReadAllBytes(hostAssembly)), cases, guards, focusEvents = focus, activations, hostVisible = window.Visible, hardwareInputSent = false, cpuBitmapInputs = false, readbacksUsedAsRendererInputs = false, publicDefaultChanged = false, pngExport = ScreenshotPngCodec.Name, referenceCodecSha256 = ScreenshotPngCodec.Sha256, nativeComponentAccepted = false }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(0);
        }
        catch (Exception error)
        {
            try { owner?.Dispose(); } catch { }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = false, stage, error = error.ToString(), cases, guards, focusEvents = focus, activations }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(1);
        }
    }
}
