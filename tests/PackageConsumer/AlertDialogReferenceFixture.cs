using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Testing;

internal static class AlertDialogReferenceFixture
{
    private delegate bool Visitor(nint window, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor, nint parameter);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint window);
    private static IEnumerable<FrameworkElement> Elements(DependencyObject root)
    {
        if (root is FrameworkElement element) yield return element;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) foreach (var child in Elements(VisualTreeHelper.GetChild(root, i))) yield return child;
    }
    internal static async Task RunAsync(Window window, string reference, string output, string hostPath)
    {
        using var source = JsonDocument.Parse(await File.ReadAllBytesAsync(reference)); var rows = new List<object>(); var failures = new List<object>(); var guards = 0; var focus = 0; var activations = 0;
        var background = (Grid)XamlReader.Load("<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{ThemeResource BleeckerBackgroundBrush}'/>"); var host = new BleeckerModalHost { Content = background, Width = 800, Height = 600 }; window.Content = host; window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(800, 600));
        host.GotFocus += (_, _) => focus++; window.Activated += (_, a) => { if (a.WindowActivationState != WindowActivationState.Deactivated) activations++; };
        var folder = Path.GetDirectoryName(output)!; Directory.CreateDirectory(folder); double width = 800, height = 600;
        void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        void Guard()
        {
            guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var visible = false; EnumWindows((handle, _) => { GetWindowThreadProcessId(handle, out var owner); visible |= owner == Environment.ProcessId && IsWindowVisible(handle); return true; }, 0);
            Require(!visible && !window.Visible && !window.AppWindow.IsVisible && foreground != Environment.ProcessId && focus == 0 && activations == 0, "AlertDialog probe acquired visibility, activation or focus."); Require(host.XamlRoot is null || VisualTreeHelper.GetOpenPopupsForXamlRoot(host.XamlRoot).Count == 0, "AlertDialog opened a native Popup.");
        }
        async Task Layout()
        {
            var idle = new TaskCompletionSource(); Require(host.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => idle.SetResult()), "Could not drain AlertDialog layout."); await idle.Task; await Task.Delay(20);
            for (var i = 0; i < 4; i++) { host.Measure(new Size(width, height)); host.Arrange(new Rect(0, 0, width, height)); host.UpdateLayout(); Guard(); await Task.Delay(10); }
            Require(host.XamlRoot is { IsHostVisible: false, RasterizationScale: 1 } && host.XamlRoot.Size == new Size(width, height), "AlertDialog probe requires its actual hidden unscaled viewport.");
        }
        async Task Wait(double milliseconds) { var watch = System.Diagnostics.Stopwatch.StartNew(); while (watch.Elapsed.TotalMilliseconds < milliseconds) { await Task.Delay(10); Guard(); } }
        FrameworkElement Find(string name) => Elements(host).Single(e => e.Name == name);
        object Box(FrameworkElement element) { var b = element.TransformToVisual(host).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight)); return new { x = b.X, y = b.Y, width = b.Width, height = b.Height }; }
        object? ForegroundPaint(Brush? brush) => brush is SolidColorBrush b ? new { rgba = new[]{(int)b.Color.R,b.Color.G,b.Color.B,b.Color.A}, opacity = b.Opacity } : null;
        object[] ButtonForegrounds(BleeckerButton cancel,BleeckerButton confirm)
        {
            var values=new List<object>();
            void Add(BleeckerButton owner,string role,string name)
            {
                var action=Elements(owner).OfType<Button>().Single(b=>b.Name=="PART_Action");
                var presenter=Elements(action).OfType<ContentPresenter>().First();
                var label=Elements(owner).OfType<TextBlock>().Single(t=>t.Name==name);
                values.Add(new{role,enabled=action.IsEnabled,foreground=ForegroundPaint(presenter.Foreground),actual=ForegroundPaint(label.Foreground),box=Box(label),button=Box(action),presenterName=presenter.Name});
                if(role=="confirm-label")
                {
                    var loader=Elements(owner).OfType<Microsoft.UI.Xaml.Shapes.Path>().Single(p=>p.Name=="AlertDialogLoaderPath");
                    var view=Elements(owner).OfType<Viewbox>().Single(v=>Elements(v).Any(e=>ReferenceEquals(e,loader)));
                    values.Add(new{role="confirm-loader",enabled=action.IsEnabled,visible=view.Visibility==Visibility.Visible,foreground=ForegroundPaint(presenter.Foreground),actual=ForegroundPaint(loader.Stroke),box=Box(view),button=Box(action),presenterName=presenter.Name});
                }
            }
            Add(cancel,"cancel-label","AlertDialogCancelLabelText");Add(confirm,"confirm-label","AlertDialogConfirmLabelText");return values.ToArray();
        }
        static string Text(JsonElement spec, string key, string fallback) => spec.TryGetProperty(key, out var value) && value.ValueKind != JsonValueKind.Null ? value.GetString()! : fallback;
        foreach (var row in source.RootElement.GetProperty("cases").EnumerateArray())
        {
            var id = row.GetProperty("id").GetString()!; var scheme = row.GetProperty("scheme").GetString()!; var spec = row.GetProperty("spec"); File.WriteAllText(Path.Combine(folder, "stage.json"), JsonSerializer.Serialize(new { scheme, id, guards, completed = rows.Count }));
            width = spec.TryGetProperty("viewportWidth", out var w) ? w.GetDouble() : 800; height = spec.TryGetProperty("viewportHeight", out var h) ? h.GetDouble() : 600; host.Width = width; host.Height = height; host.RequestedTheme = Enum.Parse<ElementTheme>(scheme); window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32((int)width, (int)height));
            Canvas? pattern = null;
            if (spec.TryGetProperty("pattern", out var patterned) && patterned.GetBoolean()) { pattern = new Canvas(); for (var x = 30; x < width; x += 32) { var stripe = (Border)XamlReader.Load("<Border xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{ThemeResource BleeckerSeaBrush}'/>"); stripe.Width = 2; stripe.Height = height; Canvas.SetLeft(stripe, x); pattern.Children.Add(stripe); } background.Children.Add(pattern); }
            var dialog = new BleeckerAlertDialog { Title = Text(spec, "title", "Fictional confirmation"), Description = spec.TryGetProperty("description", out var d) ? d.ValueKind == JsonValueKind.Null ? null : d.GetString() : "Review the fictional local component example before continuing.", CancelLabel = Text(spec, "cancelLabel", "Cancel"), ConfirmLabel = Text(spec, "confirmLabel", "Continue"), ConfirmLoading = spec.TryGetProperty("confirmLoading", out var loading) && loading.GetBoolean(), Variant = Text(spec, "variant", "default") == "destructive" ? BleeckerAlertDialogVariant.Destructive : BleeckerAlertDialogVariant.Default, FlowDirection = spec.TryGetProperty("rtl", out var rtl) && rtl.GetBoolean() ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
            if (spec.TryGetProperty("custom", out var custom) && custom.GetBoolean()) { var style = new Style(typeof(BleeckerOverlaySurface)) { BasedOn = (Style)new ResourceDictionary { Source = new Uri("ms-appx:///Orchard/Themes/AlertDialog.xaml") }["BleeckerAlertDialogSurfaceStyle"] }; style.Setters.Add(new Setter(FrameworkElement.MaxWidthProperty, 512d)); style.Setters.Add(new Setter(Control.CornerRadiusProperty, new CornerRadius(0))); dialog.SurfaceStyle = style; }
            background.Children.Add(dialog); await Layout(); dialog.IsOpen = true; await Wait(260); await Layout();
            try
            {
                var surface = Find("AlertDialogSurface"); var paint = (BleeckerOverlaySurface)Find("AlertDialogPaintSurface"); var view = (ScrollView)Find("AlertDialogViewport"); var title = Find("AlertDialogTitle"); var description = Find("AlertDialogDescription"); var cancel = (BleeckerButton)Find("AlertDialogCancel"); var confirm = (BleeckerButton)Find("AlertDialogConfirm"); var icon = (Border)Find("AlertDialogIcon");
                var peer = FrameworkElementAutomationPeer.CreatePeerForElement(surface); var provider = (IWindowProvider)peer.GetPattern(PatternInterface.Window); var describedBy = AutomationProperties.GetDescribedBy(surface); Require(peer.IsDialog() && provider.IsModal && provider.IsTopmost, "AlertDialog lost its native modal metadata.");
                var file = "native-" + scheme + "-" + id + ".png"; await CaptureAsync(host, Path.Combine(folder, file)).WaitAsync(TimeSpan.FromSeconds(3)); Guard();
                var scrollbarParts = Elements(view).Where(element => new[] { "PART_VerticalScrollBar", "VerticalRoot", "VerticalSmallDecrease", "VerticalThumb", "VerticalSmallIncrease" }.Contains(element.Name)).Select(element => new { element.Name, box = Box(element), element.Visibility, element.Opacity, height = double.IsFinite(element.Height) ? element.Height : (double?)null, margin = element.Margin.ToString(), enabled = (element as Control)?.IsEnabled }).ToArray();
                rows.Add(new { id, scheme, passed = true, surface = Box(surface), overlay = Box(Find("AlertDialogOverlay")), inner = Box(Find("AlertDialogInner")), header = Box(Find("AlertDialogHeader")), icon = Box(icon), iconSvg = Box((FrameworkElement)icon.Child), title = Box(title), description = description.Visibility == Visibility.Visible ? Box(description) : null, footer = Box(Find("AlertDialogFooter")), cancel = Box(cancel), confirm = Box(confirm), titleText = Elements(title).OfType<TextBlock>().First().Text, descriptionText = description.Visibility == Visibility.Visible ? Elements(description).OfType<TextBlock>().First().Text : null, cancelLabel = AutomationProperties.GetName(cancel), confirmLabel = AutomationProperties.GetName(confirm), cancelEnabled = cancel.IsEnabled, confirmEnabled = confirm.IsEnabled, confirmBusy = confirm.IsLoading, buttonForegrounds = ButtonForegrounds(cancel,confirm), surfaceOpacity = surface.Opacity, titleRelationship = ReferenceEquals(AutomationProperties.GetLabeledBy(surface), Elements(title).OfType<TextBlock>().First()), descriptionRelationships = describedBy.Count, modal = provider.IsModal, role = peer.GetAutomationControlType().ToString(), radius = paint.CornerRadius.TopLeft, viewport = new { width = view.ViewportWidth, height = view.ViewportHeight, extentWidth = view.ExtentWidth, extentHeight = view.ExtentHeight, horizontal = view.HorizontalOffset, vertical = view.VerticalOffset }, scrollbarParts, capture = new { file, sha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(folder, file)))) } });
            }
            catch (Exception error) { failures.Add(new { id, scheme, error = error.ToString() }); rows.Add(new { id, scheme, passed = false }); }
            dialog.IsOpen = false; await Wait(170); background.Children.Remove(dialog); if (pattern is not null) background.Children.Remove(pattern); await Layout(); Require(!Elements(host).Any(element => element.Name == "AlertDialogOverlay") && VisualTreeHelper.FindElementsInHostCoordinates(new Point(8, 8), host).Any(element => ReferenceEquals(element, background)), "AlertDialog close retained an overlay or blocked the original background.");
        }
        string Hash(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-alert-dialog-native-v1", passed = failures.Count == 0, cases = rows, failures, guards, focus, activations, referenceSha256 = Hash(reference), librarySha256 = Hash(typeof(BleeckerAlertDialog).Assembly.Location), hostSha256 = Hash(hostPath), architecture = RuntimeInformation.ProcessArchitecture.ToString(), activateCalled = false, showCalled = false, focusCalled = false, physicalInputSent = false, nativeInputVerified = false, sourceRasterParityVerified = false }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(failures.Count == 0 ? 0 : 1);
    }
    private static async Task CaptureAsync(UIElement visual, string path)
    {
        var bitmap = new RenderTargetBitmap(); await bitmap.RenderAsync(visual); var pixels = await bitmap.GetPixelsAsync(); var bytes = new byte[checked((int)pixels.Length)]; using (var reader = DataReader.FromBuffer(pixels)) reader.ReadBytes(bytes);
        using var stream = new InMemoryRandomAccessStream(); var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream); encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight, 96, 96, bytes); await encoder.FlushAsync();
        var encoded = new byte[checked((int)stream.Size)]; using var saved = new DataReader(stream.GetInputStreamAt(0)); await saved.LoadAsync((uint)encoded.Length); saved.ReadBytes(encoded); await File.WriteAllBytesAsync(path, encoded);
    }
}
