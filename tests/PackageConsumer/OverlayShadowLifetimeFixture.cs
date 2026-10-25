using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Testing;

internal static class OverlayShadowLifetimeFixture
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private delegate bool Visitor(nint handle, nint value);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor, nint value);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle, out uint process);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint handle);
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    internal static async Task RunAsync(Window window, string output, string mode, string hostAssembly)
    {
        Require(mode is "default" or "near" or "far" or "combined" or "near-rectangle" or "combined-rectangle" or "far-empty" or "combined-empty", "Unknown lifetime renderer mode");
        var isCombined = mode is "combined" or "combined-rectangle" or "combined-empty";
        var emptyCaster = mode is "far-empty" or "combined-empty";
        var initialWidth = emptyCaster ? 40 : 200;
        var library = typeof(BleeckerOverlaySurface).Assembly; var componentType = typeof(BleeckerOverlaySurface);
        var ownerType = mode == "default" ? null : library.GetType(isCombined ? "Orchard.BleeckerCombinedShadowLayer" : (mode is "near" or "near-rectangle") ? "Orchard.BleeckerNearShadowLayer" : "Orchard.BleeckerFarShadowLayer", true)!;
        var root = new Canvas { Width = 520, Height = 440, UseLayoutRounding = false, Background = new SolidColorBrush(Microsoft.UI.Colors.White), Clip = new RectangleGeometry { Rect = new(0, 0, 520, 440) } };
        window.Content = root; window.AppWindow.ResizeClient(new(520, 440));
        var parent = new Canvas { Width = 520, Height = 440 }; root.Children.Add(parent);
        var donor = new TextBlock { Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(128, 48, 96, 144)) };
        var component = new BleeckerOverlaySurface { Width = initialWidth, Height = 120, CornerRadius = new(mode is "near-rectangle" or "combined-rectangle" || emptyCaster ? 0 : 14), BorderThickness = new(1), BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Transparent), Opacity = .75, UseLayoutRounding = false };
        component.SetBinding(Control.BackgroundProperty, new Binding { Source = donor, Path = new("Foreground"), Mode = BindingMode.OneWay });
        Canvas.SetLeft(component, 160); Canvas.SetTop(component, 140); parent.Children.Add(component);
        var folder = Path.GetDirectoryName(output)!; var guards = 0; var focus = 0; var activations = 0; var stage = "initial"; object? current = null;
        var bindings = new List<object>(); var suspensions = new List<object>(); object? reparent = null; object? final = null; object? resourceReset = null; IDisposable? owner = null;
        root.GotFocus += (_, _) => focus++;
        window.Activated += (_, e) => { if (e.WindowActivationState != WindowActivationState.Deactivated) activations++; };
        JsonElement PublicState() => JsonSerializer.SerializeToElement(new { loaded = component.IsLoaded, resources = ((System.Collections.ICollection)componentType.GetField("shadowResources", Private)!.GetValue(component)!).Count, layers = ((System.Collections.ICollection)componentType.GetField("shadowLayers", Private)!.GetValue(component)!).Count, visual = componentType.GetField("shadowVisual", Private)!.GetValue(component) is not null, visibilityObservers = (componentType.GetField("visibilityObservers", Private)?.GetValue(component) as System.Collections.ICollection)?.Count });
        JsonElement? OwnerState() => owner is null ? null : JsonSerializer.SerializeToElement(ownerType!.GetMethod("Snapshot", Private)!.Invoke(owner, null));
        int Registry(string type) => (library.GetType(type, true)!.GetField("devices", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null) as System.Collections.IDictionary)?.Count ?? 0;
        void PaintRetained() => Require(component.GetBindingExpression(Control.BackgroundProperty) is not null && ReferenceEquals(component.Background, donor.Foreground) && component.Opacity == .75, "Lifetime transition changed caller binding, brush or opacity");
        void Guard()
        {
            guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var visible = false;
            EnumWindows((h, _) => { GetWindowThreadProcessId(h, out var p); visible |= p == Environment.ProcessId && IsWindowVisible(h); return true; }, 0);
            Require(!visible && !window.Visible && !window.AppWindow.IsVisible && foreground != Environment.ProcessId && focus == 0 && activations == 0, "Lifetime fixture left hidden boundary");
            Require(root.XamlRoot is null || VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Count == 0, "Lifetime fixture opened a popup");
        }
        async Task Layout()
        {
            for (var i = 0; i < 3; i++) { var idle = new TaskCompletionSource(); Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => idle.SetResult()), "Lifetime dispatcher unavailable"); await idle.Task; root.Measure(new(520, 440)); root.Arrange(new(0, 0, 520, 440)); root.UpdateLayout(); Guard(); }
            await Task.Delay(20);
        }
        IDisposable Attach() => (IDisposable)Activator.CreateInstance(ownerType!, Private, null, isCombined ? [component, new Vector4(2, 3, 5, 15) / 255, new Vector4(7, 15, 21, 71) / 255] : (mode is "near" or "near-rectangle") ? [component, new Vector2(0, 4), new Vector4(2, 3, 5, 15) / 255] : [component, new Vector4(7, 15, 21, 71) / 255], null)!;
        void SetCandidateVisibility(bool visible)
        {
            if (owner is null) return;
            if (isCombined)
            {
                foreach (var slot in new[] { "near", "far" })
                {
                    var child = ownerType!.GetField(slot, Private)!.GetValue(owner)!;
                    ((Microsoft.UI.Composition.SpriteVisual)child.GetType().GetField("sprite", Private)!.GetValue(child)!).IsVisible = visible;
                }
            }
            else ((Microsoft.UI.Composition.SpriteVisual)ownerType!.GetField("sprite", Private)!.GetValue(owner)!).IsVisible = visible;
        }
        async Task<(byte[] Pixels, object Record)> Capture(string id)
        {
            await Layout(); byte[]? last = null; byte[]? pixels = null; var attempt = 0;
            for (; attempt < 6; attempt++) { var bitmap = new RenderTargetBitmap(); await bitmap.RenderAsync(root, 520, 440); pixels = (await bitmap.GetPixelsAsync()).ToArray(); Require(bitmap.PixelWidth == 520 && bitmap.PixelHeight == 440 && pixels.Length == 520 * 440 * 4, "Incomplete lifetime frame"); if (last is not null && last.AsSpan().SequenceEqual(pixels)) break; last = pixels; await Layout(); }
            Require(attempt < 6 && pixels is not null, "Unstable lifetime frame"); var raw = pixels!; var file = id + ".bgra8"; var pngFile = id + ".png"; await File.WriteAllBytesAsync(Path.Combine(folder, file), raw);
            using var stream = new InMemoryRandomAccessStream(); var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream); encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, 520, 440, 96, 96, ScreenshotPngCodec.EncodeBgra(raw)); await encoder.FlushAsync(); stream.Seek(0); using (var destination = File.Create(Path.Combine(folder, pngFile))) await stream.AsStreamForRead().CopyToAsync(destination);
            Guard(); return (raw, new { file, sha256 = Hash(raw), pngFile, pngSha256 = Hash(File.ReadAllBytes(Path.Combine(folder, pngFile))), width = 520, height = 440, stableFrames = 2, attempts = attempt + 1 });
        }
        try
        {
            await Layout(); Require(component.IsLoaded && root.XamlRoot?.RasterizationScale == 1, "Lifetime fixture requires a loaded scale-one root");
            if (ownerType is not null) owner = Attach(); await Layout(); PaintRetained();
            var gradient = new LinearGradientBrush { StartPoint = new(0, 0), EndPoint = new(1, 0) }; gradient.GradientStops.Add(new() { Color = Windows.UI.Color.FromArgb(90, 48, 96, 144), Offset = 0 }); gradient.GradientStops.Add(new() { Color = Windows.UI.Color.FromArgb(210, 210, 80, 30), Offset = 1 });
            Brush[] paints = [new SolidColorBrush(Windows.UI.Color.FromArgb(128, 48, 96, 144)), gradient, new SolidColorBrush(Microsoft.UI.Colors.Transparent)];
            for (var paint = 0; paint < paints.Length; paint++)
            {
                stage = "binding-" + paint; donor.Foreground = paints[paint]; await Layout(); PaintRetained(); var on = await Capture(stage + "-on");
                SetCandidateVisibility(false);
                var off = await Capture(stage + "-off"); var interior = 0; for (var y = 155; y < 245; y++) for (var x = 175; x < (emptyCaster ? 185 : 345); x++) { var at = (y * 520 + x) * 4; Require(on.Pixels.AsSpan(at, 4).SequenceEqual(off.Pixels.AsSpan(at, 4)), "Shadow attachment changed bound interior pixels"); interior++; }
                SetCandidateVisibility(true);
                bindings.Add(new { id = stage, on = on.Record, off = off.Record, interiorPixels = interior, bindingRetained = true, backgroundReferenceRetained = true, opacityRetained = true });
            }
            donor.Foreground = paints[0]; await Layout(); var baseline = await Capture("visible-baseline"); var savedTemplate = component.Template;
            if (mode == "near-rectangle")
            {
                stage = "rectangle-profile-invalidation"; var before = OwnerState()!.Value;
                var graph = ownerType!.GetField("container", Private)!.GetValue(owner); var brush = ownerType.GetField("brush", Private)!.GetValue(owner);
                var oldKernel = ((Microsoft.Graphics.Canvas.Effects.PixelShaderEffect)ownerType.GetField("encoded", Private)!.GetValue(owner)!).Source1;
                ownerType.GetMethod("InvalidateGeneratedResources", Private)!.Invoke(owner, null); await Layout(); PaintRetained();
                var after = OwnerState()!.Value; var frame = await Capture("rectangle-profile-regenerated");
                var newKernel = ((Microsoft.Graphics.Canvas.Effects.PixelShaderEffect)ownerType.GetField("encoded", Private)!.GetValue(owner)!).Source1;
                Require(!ReferenceEquals(oldKernel, newKernel) && ReferenceEquals(graph, ownerType.GetField("container", Private)!.GetValue(owner)) && ReferenceEquals(brush, ownerType.GetField("brush", Private)!.GetValue(owner)), "Rectangle invalidation failed to regenerate its profile or replaced the loaded graph");
                Require(after.GetProperty("shared").GetProperty("generation").GetInt32() == before.GetProperty("shared").GetProperty("generation").GetInt32() + 1 && baseline.Pixels.AsSpan().SequenceEqual(frame.Pixels), "Rectangle invalidation changed pixels or missed its generation");
                resourceReset = new { before, after, frame = frame.Record, profileRegenerated = true, graphRetained = true, bindingRetained = true, actualDeviceLossSimulated = false };
            }
            var scenarios = new List<string> { "self-collapsed", "parent-collapsed", "root-collapsed", "zero-width", "zero-height", "empty-template" }; if (owner is not null) scenarios.Add("host-hidden");
            foreach (var scenario in scenarios)
            {
                stage = scenario; var beforePublic = PublicState(); var beforeOwner = OwnerState(); var host = componentType.GetField("shadowHost", Private)!.GetValue(component) as Grid;
                if (scenario == "self-collapsed") component.Visibility = Visibility.Collapsed;
                else if (scenario == "parent-collapsed") parent.Visibility = Visibility.Collapsed;
                else if (scenario == "root-collapsed") root.Visibility = Visibility.Collapsed;
                else if (scenario == "zero-width") component.Width = 0;
                else if (scenario == "zero-height") component.Height = 0;
                else if (scenario == "empty-template") { component.Template = (ControlTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='ContentControl'><Grid /></ControlTemplate>"); component.ApplyTemplate(); }
                else { Require(host is not null, "Missing template host"); host!.Visibility = Visibility.Collapsed; }
                await Layout(); PaintRetained(); var hiddenPublic = PublicState(); var hiddenOwner = OwnerState(); current = new { scenario, beforePublic, beforeOwner, hiddenPublic, hiddenOwner };
                if (scenario != "host-hidden") Require(hiddenPublic.GetProperty("resources").GetInt32() == 0 && !hiddenPublic.GetProperty("visual").GetBoolean(), "Hidden public component retained shadow resources");
                if (hiddenOwner.HasValue) Require(!hiddenOwner.Value.GetProperty("disposed").GetBoolean() && hiddenOwner.Value.GetProperty("graphResourceCount").GetInt32() == 0, "Suspended internal owner retained graph resources or was disposed");
                if (scenario == "self-collapsed") component.Visibility = Visibility.Visible;
                else if (scenario == "parent-collapsed") parent.Visibility = Visibility.Visible;
                else if (scenario == "root-collapsed") root.Visibility = Visibility.Visible;
                else if (scenario == "zero-width") component.Width = initialWidth;
                else if (scenario == "zero-height") component.Height = 120;
                else if (scenario == "empty-template") { component.Template = savedTemplate; component.ApplyTemplate(); }
                else host!.Visibility = Visibility.Visible;
                await Layout(); PaintRetained(); var resumed = await Capture("resumed-" + scenario); Require(baseline.Pixels.AsSpan().SequenceEqual(resumed.Pixels), "Lifetime resume changed visible pixels");
                suspensions.Add(new { scenario, beforePublic, beforeOwner, hiddenPublic, hiddenOwner, resumedPublic = PublicState(), resumedOwner = OwnerState(), frame = resumed.Record, bindingRetained = true, visiblePixelsRestored = true, osSettingsChanged = false });
            }
            stage = "reparent"; var oldParent = parent; oldParent.Children.Remove(component); await Layout(); var removedPublic = PublicState(); var removedOwner = OwnerState(); Require(!component.IsLoaded && removedPublic.GetProperty("resources").GetInt32() == 0, "Removed public component retained resources"); if (removedOwner.HasValue) Require(removedOwner.Value.GetProperty("disposed").GetBoolean(), "Removed internal owner was not disposed"); owner = null;
            parent = new Canvas { Width = 520, Height = 440, Visibility = Visibility.Collapsed }; root.Children.Add(parent); parent.Children.Add(component); await Layout(); var reparentHidden = PublicState(); Require(reparentHidden.GetProperty("resources").GetInt32() == 0, "Reparented hidden component allocated shadow resources"); parent.Visibility = Visibility.Visible; await Layout(); if (ownerType is not null) owner = Attach(); await Layout(); PaintRetained(); var moved = await Capture("reparent-resumed"); Require(baseline.Pixels.AsSpan().SequenceEqual(moved.Pixels), "Reparent changed visible pixels"); oldParent.Visibility = Visibility.Collapsed; await Layout(); var formerChanged = await Capture("former-parent-collapsed"); Require(moved.Pixels.AsSpan().SequenceEqual(formerChanged.Pixels), "Former parent visibility changed the moved component");
            reparent = new { removedPublic, removedOwner, reparentHidden, resumedPublic = PublicState(), resumedOwner = OwnerState(), resumed = moved.Record, formerParentChanged = formerChanged.Record, oldParentNoLongerAffectsPixels = true, bindingRetained = true };
            parent.Children.Remove(component); await Layout(); final = new { component = PublicState(), owner = OwnerState(), nearRegistry = Registry("Orchard.BleeckerNearShadowDevice"), farRegistry = Registry("Orchard.BleeckerFarShadowDevice") }; Require(Registry("Orchard.BleeckerNearShadowDevice") == 0 && Registry("Orchard.BleeckerFarShadowDevice") == 0, "Lifetime cleanup leaked cache registries"); Guard();
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = true, format = "orchard-overlay-shadow-lifetime-native-v3", mode, librarySha256 = Hash(File.ReadAllBytes(library.Location)), hostSha256 = Hash(File.ReadAllBytes(hostAssembly)), baseline = baseline.Record, resourceReset, bindings, suspensions, reparent, final, guards, focusEvents = focus, activations, hostVisible = window.Visible, hardwareInputSent = false, osSettingsChanged = false, pngExport = ScreenshotPngCodec.Name, referenceCodecSha256 = ScreenshotPngCodec.Sha256, nativeComponentAccepted = false }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(0);
        }
        catch (Exception error)
        {
            try { owner?.Dispose(); } catch { }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = false, stage, mode, error = error.ToString(), current, bindings, suspensions, reparent, final, guards, focusEvents = focus, activations }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(1);
        }
    }
}
