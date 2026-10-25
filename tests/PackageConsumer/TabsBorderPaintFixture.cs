using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Testing;

// Reuses the catalog's pinned 44-case factory without desktop capture or input.
internal static class TabsBorderPaintFixture
{
    private delegate bool Visitor(nint handle, nint value);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor, nint value);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle, out uint process);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint handle);
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static string Hash(string file) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
    private static IEnumerable<T> Desc<T>(DependencyObject node) where T : FrameworkElement
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
        {
            var child = VisualTreeHelper.GetChild(node, i);
            if (child is T match) yield return match;
            foreach (var descendant in Desc<T>(child)) yield return descendant;
        }
    }
    internal static async Task RunAsync(Window window, string reference, string folder, string hostAssembly)
    {
        Directory.CreateDirectory(folder);
        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(reference));
        var root = new Canvas { Width = 640, Height = 240, UseLayoutRounding = false, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        window.Content = root;
        window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(640, 240));
        var frames = new List<object>(); var pngs = new List<object>(); var guards = 0; var focus = 0; var activations = 0;
        root.GotFocus += (_, _) => focus++;
        window.Activated += (_, e) => { if (e.WindowActivationState != WindowActivationState.Deactivated) activations++; };
        var linkTemplate = (DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Button Style='{Binding Style}' Content='{Binding Content}' Command='{Binding Command}' AutomationProperties.Name='{Binding Label}'/></DataTemplate>");
        void Guard()
        {
            guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var visible = false;
            EnumWindows((handle, _) => { GetWindowThreadProcessId(handle, out var process); visible |= process == Environment.ProcessId && IsWindowVisible(handle); return true; }, 0);
            Require(!visible && !window.Visible && !window.AppWindow.IsVisible && foreground != Environment.ProcessId && focus == 0 && activations == 0 && !root.XamlRoot.IsHostVisible && VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Count == 0, "Tabs probe left its hidden boundary.");
        }
        async Task AwaitPresentationAsync(FrameworkElement element)
        {
            for (var i = 0; i < 3; i++)
            {
                var idle = new TaskCompletionSource();
                Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => idle.SetResult()), "Dispatcher refused Tabs layout.");
                await idle.Task; root.Measure(new Size(640, 240)); root.Arrange(new Rect(0, 0, 640, 240)); root.UpdateLayout(); Guard();
            }
            await Task.Delay(TimeSpan.FromMilliseconds(BleeckerTokens.Durations["control"] + 40));
            root.UpdateLayout(); Guard();
        }
        object Box(FrameworkElement element)
        {
            var point = element.TransformToVisual(root).TransformPoint(new());
            return new { x = point.X, y = point.Y, width = element.ActualWidth, height = element.ActualHeight };
        }
        object Paint(Brush? value)
        {
            if (value is null) return new { r = 0, g = 0, b = 0, a = 0, opacity = 1d };
            Require(value is SolidColorBrush, "Tabs requires solid paint."); var brush = (SolidColorBrush)value;
            return new { r = (int)brush.Color.R, g = (int)brush.Color.G, b = (int)brush.Color.B, a = (int)brush.Color.A, opacity = brush.Opacity };
        }
        async Task CaptureAsync(string file)
        {
            Guard(); var bitmap = new RenderTargetBitmap(); await bitmap.RenderAsync(root); Guard();
            var pixels = System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());
            using var stream = new InMemoryRandomAccessStream(); var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight, 96, 96, pixels);
            await encoder.FlushAsync(); stream.Seek(0); using var reader = new DataReader(stream.GetInputStreamAt(0)); await reader.LoadAsync((uint)stream.Size);
            var bytes = new byte[(int)stream.Size]; reader.ReadBytes(bytes); await File.WriteAllBytesAsync(Path.Combine(folder, file), bytes);
            pngs.Add(new { file, sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)), width = bitmap.PixelWidth, height = bitmap.PixelHeight });
        }
            foreach (var row in document.RootElement.GetProperty("cases").EnumerateArray())
            {
                var theme = Enum.Parse<ElementTheme>(row.GetProperty("scheme").GetString()!);
                root.RequestedTheme = theme;
                root.Background = new SolidColorBrush(theme == ElementTheme.Light ? Microsoft.UI.Colors.White : Windows.UI.Color.FromArgb(255, 13, 24, 33));
                var spec = row.GetProperty("spec");
                var control = new BleeckerTabs { Width = 480, UseLayoutRounding = false };
                if (spec.TryGetProperty("variant", out var variant)) control.Variant = Enum.Parse<BleeckerTabsVariant>(variant.GetString()!, true);
                if (spec.TryGetProperty("size", out var size)) control.Size = size.GetString() switch { "sm" => BleeckerControlSize.Small, "md" => BleeckerControlSize.Medium, "lg" => BleeckerControlSize.Large, _ => throw new InvalidOperationException("Unknown source Tabs size.") };
                if (spec.TryGetProperty("stretch", out var stretch)) control.Stretch = stretch.GetBoolean();
                if (spec.TryGetProperty("activeTab", out var active)) control.ActiveTab = active.GetString();
                if (spec.TryGetProperty("customLink", out var customLink) && customLink.GetBoolean()) control.LinkTemplate = linkTemplate;
                control.Items = new ObservableCollection<BleeckerTabItem>(spec.GetProperty("tabs").EnumerateArray().Select(item => new BleeckerTabItem(
                    item.GetProperty("id").GetString()!, item.GetProperty("label").GetString()!,
                    item.TryGetProperty("href", out var href) ? href.GetString() : null,
                    item.TryGetProperty("external", out var external) && external.GetBoolean(),
                    item.TryGetProperty("panelId", out var panelId) ? panelId.GetString() : null)));
                AutomationProperties.SetName(control, "Component tabs");
                Canvas.SetLeft(control, 20); Canvas.SetTop(control, 20);
                root.Children.Clear(); root.Children.Add(control);
                await AwaitPresentationAsync(control);
                var frame = Desc<Border>(control).Single(border => border.Name == "TabsFrame");
                var buttons = Desc<Button>(control).ToArray();
                var items = buttons.Select(button =>
                {
                    var border = Desc<Border>(button).Single(value => value.Name == "TabBorder");
                    var label = Desc<TextBlock>(button).Single(value => value.Name == "TabLabel");
                    return new
                    {
                        box = Box(button), text = label.Text, foreground = Paint(label.Foreground), background = Paint(border.Background), border = Paint(border.BorderBrush),
                        fontSize = button.FontSize, lineHeight = label.LineHeight, radius = border.CornerRadius.TopLeft,
                        padding = button.Padding, borderThickness = border.BorderThickness, label = Box(label),
                        labelForeground = Paint(label.Foreground), label.FontFamily.Source, label.FontWeight.Weight
                    };
                }).ToArray();
                frames.Add(new { id = spec.GetProperty("id").GetString(), scheme = theme.ToString(), measurement = new { root = new { box = Box(frame), background = Paint(frame.Background), border = Paint(frame.BorderBrush), radius = frame.CornerRadius.TopLeft }, items } });
                if (row.TryGetProperty("file", out var png)) await CaptureAsync(png.GetString()!);
            }
        Guard();
        var identity = new { librarySha256 = Hash(typeof(BleeckerTabs).Assembly.Location), hostSha256 = Hash(hostAssembly) };
        await File.WriteAllTextAsync(Path.Combine(folder, "tabs-reference.json"), JsonSerializer.Serialize(new { passed = true, frames, pngs, guards, focus, activations, identity, referenceSha256 = Hash(reference), hidden = true, nativeInputVerified = false }, new JsonSerializerOptions { WriteIndented = true }));
        await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "tabs-reference", environment = new { displayScale = root.XamlRoot.RasterizationScale, textScale = new Windows.UI.ViewManagement.UISettings().TextScaleFactor }, activateCalled = false, showCalled = false, focusCalled = false, hardwareInputSent = false, nativeComponentAccepted = false }));
        root.Children.Clear(); await AwaitPresentationAsync(root); Guard(); window.Close(); Environment.Exit(0);
    }
}
