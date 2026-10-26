using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Testing;

internal static class SeparatorFixture
{
    private delegate bool Visitor(nint handle, nint value);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor, nint value);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle, out uint process);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint handle);
    private static string Hash(string file) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node)
    {
        if (node is FrameworkElement element) yield return element;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
            foreach (var child in Elements(VisualTreeHelper.GetChild(node, i))) yield return child;
    }

    internal static async Task RunAsync(Window window, string output, string hostAssembly, Func<FrameworkElement>? catalogFactory = null)
    {
        var states = new List<object>(); var contracts = new List<object>(); var catalogs = new List<object>();
        var stage = "initial"; var guards = 0; var focus = 0; var activations = 0;
        void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        try
        {
            var root = new Canvas { Width = 400, Height = 160, Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 68, 85, 102)) };
            window.Content = root; window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(400, 160));
            root.GotFocus += (_, _) => focus++;
            window.Activated += (_, e) => { if (e.WindowActivationState != WindowActivationState.Deactivated) activations++; };
            void Guard()
            {
                guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var visible = false;
                EnumWindows((h, _) => { GetWindowThreadProcessId(h, out var p); visible |= p == Environment.ProcessId && IsWindowVisible(h); return true; }, 0);
                Require(!visible && !window.Visible && !window.AppWindow.IsVisible && foreground != Environment.ProcessId && focus == 0 && activations == 0, "Separator probe left its hidden boundary.");
            }
            async Task Layout()
            {
                for (var i = 0; i < 3; i++) { root.Measure(new Size(400, 160)); root.Arrange(new Rect(0, 0, 400, 160)); root.UpdateLayout(); Guard(); await Task.Delay(16); }
                await NativeRenderReadiness.RenderAsync(root); Guard();
            }
            object Box(FrameworkElement element)
            {
                var r = element.TransformToVisual(root).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
                return new { x = r.X, y = r.Y, width = r.Width, height = r.Height };
            }
            async Task<object> Capture(string file)
            {
                Guard(); var bitmap = new RenderTargetBitmap(); await bitmap.RenderAsync(root); Guard();
                Require(bitmap.PixelWidth == 400 && bitmap.PixelHeight == 160, "Separator capture dimensions changed.");
                var bytes = System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());
                using var stream = new InMemoryRandomAccessStream(); var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, 400, 160, 96, 96, bytes); await encoder.FlushAsync(); stream.Seek(0);
                using var reader = new DataReader(stream); await reader.LoadAsync((uint)stream.Size); var png = new byte[(int)stream.Size]; reader.ReadBytes(png);
                await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!, file), png);
                return new { file, sha256 = Convert.ToHexStringLower(SHA256.HashData(png)) };
            }
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                root.RequestedTheme = theme;
                foreach (var flow in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
                foreach (var orientation in new[] { Orientation.Horizontal, Orientation.Vertical })
                foreach (var decorative in new[] { true, false })
                foreach (var enabled in new[] { true, false })
                {
                    stage = $"{theme}/{flow}/{orientation}/{decorative}/{enabled}";
                    root.Children.Clear(); var host = new Grid { Width = 320, Height = 64 }; Canvas.SetLeft(host, 24); Canvas.SetTop(host, 24);
                    var control = new BleeckerSeparator { Orientation = orientation, Decorative = decorative, IsEnabled = enabled, FlowDirection = flow, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
                    AutomationProperties.SetName(control, "Fictional workspace sections"); host.Children.Add(control); root.Children.Add(host); await Layout();
                    Require(control.IsLoaded && !control.IsTabStop && control.Opacity == 1, "Separator lost its loaded, noninteractive or undimmed contract.");
                    Require(control.ActualWidth == (orientation == Orientation.Horizontal ? 320 : 1) && control.ActualHeight == (orientation == Orientation.Horizontal ? 1 : 64), "Separator default geometry differs from the pinned source.");
                    var peer = FrameworkElementAutomationPeer.CreatePeerForElement(control); var brush = (SolidColorBrush)control.Background; var line = Elements(control).OfType<Border>().Single();
                    Require(peer.IsControlElement() == !decorative && peer.IsContentElement() == !decorative && peer.GetAutomationControlType() == AutomationControlType.Separator && peer.GetName() == "Fictional workspace sections", "Separator semantics differ.");
                    Require(peer.GetOrientation() == (orientation == Orientation.Horizontal ? AutomationOrientation.Horizontal : AutomationOrientation.Vertical), "Separator automation orientation differs.");
                    Require(brush.Color == (theme == ElementTheme.Dark ? Microsoft.UI.Colors.White : Microsoft.UI.Colors.Black) && Math.Abs(brush.Opacity - (theme == ElementTheme.Dark ? .1 : .05)) < .000001 && ReferenceEquals(line.Background, brush), $"Separator rendered source paint differs: color={brush.Color}, opacity={brush.Opacity:R}, renderedBrushRetained={ReferenceEquals(line.Background, brush)}.");
                    var image = await Capture($"separator-{theme}-{flow}-{orientation}-{decorative}-{enabled}.png");
                    states.Add(new { theme = theme.ToString(), flow = flow.ToString(), orientation = orientation.ToString(), decorative, enabled, box = Box(control), line = Box(line), color = new[] { (int)brush.Color.R, brush.Color.G, brush.Color.B, brush.Color.A }, brush.Opacity, tabStop = control.IsTabStop, controlElement = peer.IsControlElement(), contentElement = peer.IsContentElement(), role = peer.GetAutomationControlType().ToString(), name = peer.GetName(), image });
                    host.Width = 137; host.Height = 48; control.Orientation = orientation == Orientation.Horizontal ? Orientation.Vertical : Orientation.Horizontal; control.Decorative = !decorative; await Layout();
                    Require(ReferenceEquals(Elements(control).OfType<Border>().Single(), line) && control.ActualWidth == (control.Orientation == Orientation.Horizontal ? 137 : 1) && control.ActualHeight == (control.Orientation == Orientation.Horizontal ? 1 : 48) && peer.IsControlElement() == decorative, "Live layout or semantic changes replaced the line or retained stale state.");
                }
                root.Children.Clear(); var width = new Border { Tag = 137d }; var height = new Border { Tag = 3d }; var paint = new Border { Tag = new SolidColorBrush(Microsoft.UI.Colors.Coral) };
                var bound = new BleeckerSeparator { Decorative = false };
                bound.SetBinding(FrameworkElement.WidthProperty, new Binding { Source = width, Path = new PropertyPath("Tag") });
                bound.SetBinding(FrameworkElement.HeightProperty, new Binding { Source = height, Path = new PropertyPath("Tag") });
                bound.SetBinding(Control.BackgroundProperty, new Binding { Source = paint, Path = new PropertyPath("Tag") }); root.Children.Add(bound); await Layout();
                void CallerValues(string id)
                {
                    Require(bound.ActualWidth == (double)width.Tag && bound.ActualHeight == (double)height.Tag && ReferenceEquals(bound.Background, paint.Tag) && ReferenceEquals(Elements(bound).OfType<Border>().Single().Background, paint.Tag), "Separator replaced caller dimensions or rendered background.");
                    Require(bound.GetBindingExpression(FrameworkElement.WidthProperty) is not null && bound.GetBindingExpression(FrameworkElement.HeightProperty) is not null && bound.GetBindingExpression(Control.BackgroundProperty)is not null, "Separator replaced caller bindings.");
                    contracts.Add(new { theme = theme.ToString(), id, passed = true, originalBrushRetained = true, bindingsRetained = true, box = Box(bound) });
                }
                CallerValues("caller-bindings"); bound.Orientation = Orientation.Vertical; width.Tag = 5d; height.Tag = 64d; paint.Tag = new SolidColorBrush(Microsoft.UI.Colors.Teal); await Layout(); CallerValues("live-caller-values");
                var template = bound.Template; bound.Template = null; await Layout(); bound.Template = template; await Layout(); CallerValues("retemplate");
                root.Children.Remove(bound); await Layout(); Require(!bound.IsLoaded, "Separator did not unload."); width.Tag = 7d; height.Tag = 48d; root.Children.Add(bound); await Layout(); CallerValues("remount");
                if (catalogFactory is not null)
                {
                    root.Children.Clear(); var fixture = catalogFactory(); root.Children.Add(fixture); await Layout(); var controls = Elements(fixture).OfType<BleeckerSeparator>().ToArray();
                    Require(controls.Length == 4 && controls.All(c => c.IsLoaded && !c.IsTabStop && c.ActualWidth > 0 && c.ActualHeight > 0), "Registered Separator catalog variants are missing.");
                    Require(controls.Any(c => c.Orientation == Orientation.Vertical) && controls.Any(c => !c.Decorative), "Registered Separator catalog lost orientation or semantic variants.");
                    catalogs.Add(new { theme = theme.ToString(), controls = controls.Length, passed = true });
                }
            }
            Guard(); await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-separator-native-v1", passed = true, states, contracts, catalogs, guards, focus, activations, rasterizationScale = root.XamlRoot.RasterizationScale, processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(), osArchitecture = RuntimeInformation.OSArchitecture.ToString(), hardwareInputSent = false, osSettingsChanged = false, nativeARM64RuntimeVerified = false, displayScalingVerified = false, nativeComponentAccepted = false, librarySha256 = Hash(typeof(BleeckerSeparator).Assembly.Location), hostSha256 = Hash(hostAssembly) }, new JsonSerializerOptions { WriteIndented = true }));
            window.Close(); Environment.Exit(0);
        }
        catch (Exception error)
        {
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-separator-native-v1", passed = false, stage, error = error.ToString(), states, contracts, guards, focus, activations, librarySha256 = Hash(typeof(BleeckerSeparator).Assembly.Location), hostSha256 = Hash(hostAssembly) })); Environment.Exit(1);
        }
    }
}
