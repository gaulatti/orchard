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

internal static class CommandSpotlightSurfaceFixture
{
    private delegate bool Visitor(nint handle, nint value);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor, nint value);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle, out uint process);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint handle);
    private static IEnumerable<DependencyObject> Walk(DependencyObject node) { yield return node; for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) foreach (var child in Walk(VisualTreeHelper.GetChild(node, i))) yield return child; }
    internal static async Task RunAsync(Window window, string output, string referencePath, string hostAssembly)
    {
        var referenceBytes = await File.ReadAllBytesAsync(referencePath); using var document = JsonDocument.Parse(referenceBytes); var reference = document.RootElement;
        var inputs = reference.GetProperty("cases").EnumerateArray().Select(c => c.Clone()).ToArray();
        if (reference.GetProperty("format").GetString() != "orchard-command-spotlight-surface-reference-v1" || inputs.Length != 378) throw new InvalidOperationException("Unexpected selected-fill reference");
        var width = 720; var height = 840; var root = new Canvas { Width = width, Height = height }; var host = new BleeckerModalHost { Content = root, Width = width, Height = height }; window.Content = host; window.AppWindow.ResizeClient(new(width, height)); var folder = Path.GetDirectoryName(output)!;
        var guards = 0; var focus = 0; var activations = 0; var stage = "initial"; var cases = new List<object>(); var images = new List<object>(); var liveCases = new List<object>();
        host.GotFocus += (_, _) => focus++; window.Activated += (_, a) => { if (a.WindowActivationState != WindowActivationState.Deactivated) activations++; };
        void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        void Guard()
        {
            guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var visible = false; EnumWindows((h, _) => { GetWindowThreadProcessId(h, out var p); visible |= p == Environment.ProcessId && IsWindowVisible(h); return true; }, 0);
            Require(!visible && !window.Visible && !window.AppWindow.IsVisible && foreground != Environment.ProcessId && focus == 0 && activations == 0, "Selected-fill probe left hidden boundary");
            Require(host.XamlRoot is null || VisualTreeHelper.GetOpenPopupsForXamlRoot(host.XamlRoot).Count == 0, "Selected-fill probe opened native Popup");
        }
        async Task Layout()
        {
            await File.WriteAllTextAsync(Path.Combine(folder, "surface-stage.json"), JsonSerializer.Serialize(new { stage, guards }));
            for (var i = 0; i < 3; i++) { var idle = new TaskCompletionSource(); Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => idle.SetResult()), "Dispatcher refused selected-fill layout"); await idle.Task; host.Measure(new(width, height)); host.Arrange(new(0, 0, width, height)); host.UpdateLayout(); Guard(); } await Task.Delay(40);
        }
        async Task<(byte[] Pixels, object Image)> Capture(string file)
        {
            byte[]? previous = null, pixels = null; var attempts = 0;
            for (; attempts < 6; attempts++) { var bitmap = new RenderTargetBitmap(); await bitmap.RenderAsync(host, width, height); pixels = (await bitmap.GetPixelsAsync()).ToArray(); if (previous is not null && previous.AsSpan().SequenceEqual(pixels)) break; previous = pixels; await Layout(); }
            Require(attempts < 6, "Selected-fill frame did not settle"); using var stream = new InMemoryRandomAccessStream(); var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream); encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, (uint)width, (uint)height, 96, 96, pixels!); await encoder.FlushAsync(); stream.Seek(0); using (var target = File.Create(Path.Combine(folder, file))) await stream.AsStreamForRead().CopyToAsync(target);
            object image = new { file, width, height, sha256 = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(folder, file)))), stableFrames = 2, attempts = attempts + 1 }; images.Add(image); return (pixels!, image);
        }
        static byte[] ReadPixel(WriteableBitmap bitmap) { var bytes = new byte[4]; using var stream = bitmap.PixelBuffer.AsStream(); stream.ReadExactly(bytes); return bytes; }
        try
        {
            var encode = typeof(BleeckerCommandSpotlight).Assembly.GetType("Orchard.BleeckerOklabFill", true)!.GetMethod("Encode", BindingFlags.Static | BindingFlags.NonPublic)!;
            foreach (var input in inputs)
            {
                var rgb = input.GetProperty("rgb"); var value = ((byte R, byte G, byte B, byte A))encode.Invoke(null, [rgb[0].GetByte(), rgb[1].GetByte(), rgb[2].GetByte(), input.GetProperty("sourceAlphaByte").GetByte(), input.GetProperty("opacity").GetDouble()])!;
                var bitmap = new WriteableBitmap(1, 1); using (var pixels = bitmap.PixelBuffer.AsStream()) pixels.Write([value.B, value.G, value.R, value.A]); bitmap.Invalidate(); var background = input.GetProperty("background"); var tile = new Grid { Width = 40, Height = 40, Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, background[0].GetByte(), background[1].GetByte(), background[2].GetByte())) }; tile.Children.Add(new Border { Background = new ImageBrush { ImageSource = bitmap, Stretch = Stretch.Fill } }); var index = root.Children.Count; Canvas.SetLeft(tile, index % 18 * 40); Canvas.SetTop(tile, index / 18 * 40); root.Children.Add(tile);
            }
            stage = "atlas"; await Layout(); var atlas = await Capture("surface-atlas.png");
            for (var i = 0; i < inputs.Length; i++) { var input = inputs[i]; var x = i % 18 * 40 + 20; var y = i / 18 * 40 + 20; var at = (y * width + x) * 4; int[] pixel = [atlas.Pixels[at + 2], atlas.Pixels[at + 1], atlas.Pixels[at], atlas.Pixels[at + 3]]; var expected = input.GetProperty("expected").EnumerateArray().Select(v => v.GetInt32()).ToArray(); Require(pixel.SequenceEqual(expected), "Product fill codec differs from browser: " + input.GetProperty("id").GetString()); cases.Add(new { id = input.GetProperty("id").GetString(), index = i, x, y, pixel, expected }); }
            root.Children.Clear(); width = 1000; height = 960; host.Width = width; host.Height = height; window.AppWindow.ResizeClient(new(width, height)); var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                host.RequestedTheme = theme; int[] surface = theme == ElementTheme.Light ? [255, 255, 255] : [24, 37, 51]; var page = new Grid { Background = new SolidColorBrush(theme == ElementTheme.Light ? Microsoft.UI.Colors.White : Windows.UI.Color.FromArgb(255, 13, 24, 33)) }; var action = new BleeckerCommandSpotlightAction("original", "Fictional original") { Group = "Actions" }; var control = new BleeckerCommandSpotlight { Open = true, Margin = new(32), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Actions = new[] { action } }; page.Children.Add(control); host.Content = page; await Layout(); await Task.Delay(350); await Layout();
                var slots = (Dictionary<string, TextBlock>)typeof(BleeckerCommandSpotlight).GetField("palette", flags)!.GetValue(control)!; var slot = slots["SelectedBackground"]; var row = Walk(host).OfType<Button>().Single(b => b.Name == "SpotlightAction"); var render = (ImageBrush)row.Background; var raster = (WriteableBitmap)render.ImageSource; var defaultBrush = (SolidColorBrush)slot.Foreground;
                async Task Live(string id, SolidColorBrush source, bool bound, bool retained = true)
                {
                    stage = theme + "/" + id; await Layout(); var frame = await Capture("surface-" + theme + "-" + id + ".png"); var point = row.TransformToVisual(host).TransformPoint(new(row.ActualWidth / 2 + 100, 20)); var x = (int)Math.Floor(point.X); var y = (int)Math.Floor(point.Y); var at = (y * width + x) * 4; int[] pixel = [frame.Pixels[at + 2], frame.Pixels[at + 1], frame.Pixels[at], frame.Pixels[at + 3]]; var color = source.Color;
                    var matches = inputs.Where(c => c.GetProperty("rgb")[0].GetByte() == color.R && c.GetProperty("rgb")[1].GetByte() == color.G && c.GetProperty("rgb")[2].GetByte() == color.B && c.GetProperty("sourceAlphaByte").GetByte() == color.A && (float)c.GetProperty("opacity").GetDouble() == source.Opacity && c.GetProperty("background").EnumerateArray().Select(v => v.GetInt32()).SequenceEqual(surface)).ToArray(); Require(matches.Length > 0, "No source oracle for live selected fill"); var expected = matches[0].GetProperty("expected").EnumerateArray().Select(v => v.GetInt32()).ToArray(); Require(matches.All(c => c.GetProperty("expected").EnumerateArray().Select(v => v.GetInt32()).SequenceEqual(expected)), "Conflicting source oracles"); Require(pixel.SequenceEqual(expected), "Live selected fill differs from source: " + id);
                    Require(ReferenceEquals(slot.Foreground, source) && (!bound || slot.GetBindingExpression(TextBlock.ForegroundProperty) is not null), "Semantic brush or binding lost"); Require(ReferenceEquals(control.Actions.Single(), action), "Original action replaced"); Require(ReferenceEquals(row.Background, render) && ReferenceEquals(render.ImageSource, raster), "Selected raster replaced during update");
                    liveCases.Add(new { theme = theme.ToString(), id, image = frame.Image, x, y, pixel, referenceCaseId = matches[0].GetProperty("id").GetString(), expected, sourceColor = new[] { color.A, color.R, color.G, color.B }, sourceOpacity = source.Opacity, semanticSourceRetained = true, bindingRequired = bound, bindingRetained = !bound || slot.GetBindingExpression(TextBlock.ForegroundProperty) is not null, originalActionRetained = true, sameRasterRetained = retained, passed = true });
                }
                await Live("default", defaultBrush, false);
                var donor = new TextBlock(); var boundBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 44, 87, 132)) { Opacity = .1 }; donor.Foreground = boundBrush; slot.SetBinding(TextBlock.ForegroundProperty, new Binding { Source = donor, Path = new("Foreground"), Mode = BindingMode.OneWay }); await Live("bound", boundBrush, true);
                boundBrush.Color = Microsoft.UI.Colors.White; await Live("color-mutated", boundBrush, true); boundBrush.Color = Windows.UI.Color.FromArgb(128, 255, 255, 255); await Live("alpha-mutated", boundBrush, true); boundBrush.Opacity = .5; await Live("opacity-mutated", boundBrush, true);
                var replacement = new SolidColorBrush(Windows.UI.Color.FromArgb(32, 44, 87, 132)) { Opacity = .3 }; donor.Foreground = replacement; await Live("replacement", replacement, true); boundBrush.Color = Microsoft.UI.Colors.Black; boundBrush.Opacity = 1; await Live("old-source-quiet", replacement, true);
                var retainedBytes = ReadPixel(raster); typeof(BleeckerCommandSpotlight).GetMethod("Refresh", flags)!.Invoke(control, null); await Live("refresh-retains-raster", replacement, true); Require(ReadPixel(raster).SequenceEqual(retainedBytes), "Unchanged refresh changed selected pixel data");
                control.Open = false; await Task.Delay(300); await Layout(); Require(render.ImageSource is null, "Detached selected fill retained its image source"); replacement.Color = Microsoft.UI.Colors.White; replacement.Opacity = .5; await Layout(); Require(ReadPixel(raster).SequenceEqual(retainedBytes), "Detached selected fill observes old source mutations"); liveCases.Add(new { theme = theme.ToString(), id = "closed-quiet", imageSourceReleased = true, retiredBitmapUnchanged = true, passed = true });
                var retired = render; control.Open = true; await Task.Delay(350); await Layout(); row = Walk(host).OfType<Button>().Single(b => b.Name == "SpotlightAction"); render = (ImageBrush)row.Background; raster = (WriteableBitmap)render.ImageSource; Require(!ReferenceEquals(render, retired), "Reopen reused disposed selected fill"); await Live("reopened", replacement, true, false); host.Content = root; await Layout();
            }
            Guard(); string Hash(string p) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(p)));
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-command-spotlight-surface-native-v1", passed = true, referenceSha256 = Convert.ToHexStringLower(SHA256.HashData(referenceBytes)), librarySha256 = Hash(typeof(BleeckerCommandSpotlight).Assembly.Location), hostSha256 = Hash(hostAssembly), cases, images, liveCases, guards, focusEvents = focus, activations, hostVisible = window.Visible, hardwareInputSent = false, nativeComponentAccepted = false }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(0);
        }
        catch (Exception error) { await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = false, stage, error = error.ToString(), cases, images, liveCases, guards, focusEvents = focus, activations }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(1); }
    }
}
