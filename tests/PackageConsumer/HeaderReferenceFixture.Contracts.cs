using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Orchard;

namespace Orchard.Testing;

internal static partial class HeaderReferenceFixture
{
    [Microsoft.UI.Xaml.Data.Bindable]
    private sealed class Model : INotifyPropertyChanged
    {
        private string value = "Original";
        public string Value { get => value; set { this.value = value; PropertyChanged?.Invoke(this, new(nameof(Value))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    private sealed class Selector(DataTemplate template) : DataTemplateSelector
    {
        internal readonly List<object?> Items = [];
        protected override DataTemplate SelectTemplateCore(object item) { Items.Add(item); return template; }
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) => SelectTemplateCore(item);
    }
    private sealed class Command : ICommand
    {
        private volatile bool enabled = true;
        internal int Executions;
        internal object? Parameter;
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => enabled;
        public void Execute(object? parameter) { Executions++; Parameter = parameter; }
        internal void Enable(bool value) { enabled = value; CanExecuteChanged?.Invoke(this, EventArgs.Empty); }
    }
    private static DataTemplate Template(string body) => (DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>" + body + "</DataTemplate>");
    internal static async Task RunContractsAsync(Window window, string compositionReferencePath, string reportPath, string hostPath)
    {
        var compositionBytes = await File.ReadAllBytesAsync(compositionReferencePath);
        using var compositionReference = JsonDocument.Parse(compositionBytes);
        var root = new Canvas { Width = 640, Height = 900, UseLayoutRounding = false }; window.Content = root; window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(640, 900));
        var handle = WinRT.Interop.WindowNative.GetWindowHandle(window); var guards = 0; var activations = 0; var focus = 0; var checks = new List<object>(); var failures = new List<object>(); var scrollEvidence = new List<object>(); var scrollOperations = new List<object>(); var compositionEvidence = new List<object>();
        window.Activated += (_, args) => { if (args.WindowActivationState != WindowActivationState.Deactivated) activations++; }; root.GotFocus += (_, _) => focus++;
        void Require(bool passed, string message) { if (!passed) throw new InvalidOperationException(message); }
        void Guard() { guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); Require(!window.Visible && !window.AppWindow.IsVisible && !IsWindowVisible(handle) && foreground != Environment.ProcessId && activations == 0 && focus == 0, "Hidden Header contracts acquired visibility, activation or focus."); }
        async Task Layout() { root.Clip = new RectangleGeometry { Rect = new Rect(0, 0, root.Width, 900) }; for (var index = 0; index < 5; index++) { root.Measure(new(root.Width, 900)); root.Arrange(new(0, 0, root.Width, 900)); root.UpdateLayout(); Guard(); await Task.Delay(15); } Require(root.XamlRoot is { IsHostVisible: false, RasterizationScale: 1 } && root.XamlRoot.Size.Width == root.Width && root.XamlRoot.Size.Height == 900, "Hidden Header viewport differs from its actual client."); }
        async Task Resize(double width) { root.Width = width; window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32((int)width, 900)); await Layout(); }
        async Task<object> ScrollPresentation(BleeckerHeader header, string id, bool present, Point? probe = null)
        {
            var point = probe ?? new Point(1100, 30); var width = (int)root.Width;
            FrameworkElement captureTarget = id.StartsWith("app-shell-", StringComparison.Ordinal) ? Elements(root).OfType<BleeckerAppShell>().Single() : root;
            var captureOrigin = captureTarget.TransformToVisual(root).TransformPoint(new Point());
            Require(captureOrigin.X == 0 && captureOrigin.Y == 0 && captureTarget.ActualWidth == root.Width && captureTarget.ActualHeight == 900, "Capture target is not the complete client composition.");
            var surface = Part(header, "HeaderSurface");
            bool OwnsTopHit() { var first = VisualTreeHelper.FindElementsInHostCoordinates(point, root).FirstOrDefault(); return first is not null && Elements(surface).Contains(first); }
            var hits = VisualTreeHelper.FindElementsInHostCoordinates(point, root).ToArray(); var hit = hits.Contains(surface);
            Require(hit == present && OwnsTopHit() == present, "Native Header does not own the source's topmost hit: " + JsonSerializer.Serialize(new { id, hit, hits = hits.Take(8).Select(element => new { type = element.GetType().Name, name = (element as FrameworkElement)?.Name }) }));
            var logo = Elements(header).OfType<BleeckerBrandLockup>().Single(); var decode = System.Diagnostics.Stopwatch.StartNew();
            while (logo.LogoSource is not null && !logo.IsLogoLoaded) { Require(decode.ElapsedMilliseconds < 5000, "Header composition logo did not decode."); await Layout(); }
            async Task<byte[]> Encode(byte[] pixels, int pixelWidth, int pixelHeight)
            {
                using var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
                var encoder = await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId, stream);
                encoder.SetPixelData(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied, (uint)pixelWidth, (uint)pixelHeight, 96, 96, pixels);
                await encoder.FlushAsync(); using var reader = new Windows.Storage.Streams.DataReader(stream.GetInputStreamAt(0)); await reader.LoadAsync((uint)stream.Size);
                var bytes = new byte[stream.Size]; reader.ReadBytes(bytes); return bytes;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
            var renderFrames = new List<object>(); string? previousHash = null; byte[]? finalPng = null, finalData = null; var lastWidth = 0; var lastHeight = 0;
            bool Visible(FrameworkElement element)
            {
                for (DependencyObject? node = element; node is not null; node = VisualTreeHelper.GetParent(node)) { if (node is FrameworkElement { Visibility: Visibility.Collapsed }) return false; if (ReferenceEquals(node, captureTarget)) return true; }
                return false;
            }
            // Dark logo effects retain an off-screen XAML decoder. Natural RTB
            // bounds include that source image; a native client capture clips it.
            // Extract the client from measured tree coordinates, without scaling.
            for (var attempt = 0; attempt < 6; attempt++)
            {
                await Layout(); var appShellCapture = id.StartsWith("app-shell-", StringComparison.Ordinal);
                var rectangles = Elements(captureTarget).Where(element => Visible(element) && element.ActualWidth > 0 && element.ActualHeight > 0).Select(element => element.TransformToVisual(captureTarget).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight))).ToArray();
                var measuredMinX = appShellCapture ? Math.Min(0, rectangles.Min(box => box.Left)) : 0;
                var measuredMinY = appShellCapture ? Math.Min(0, rectangles.Min(box => box.Top)) : 0;
                var cropX = checked(-(int)Math.Floor(measuredMinX)); var cropY = checked(-(int)Math.Floor(measuredMinY));
                var bitmap = new Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap();
                Require(root.ActualWidth == width && root.ActualHeight == 900 && root.XamlRoot.RasterizationScale == 1, "Capture would rescale the actual client.");
                var operation = appShellCapture ? bitmap.RenderAsync(captureTarget) : bitmap.RenderAsync(captureTarget, width, 900); var render = operation.AsTask();
                if (await Task.WhenAny(render, Task.Delay(2000)) != render) { operation.Cancel(); throw new TimeoutException("Hidden Header rendering exceeded two seconds."); }
                await render; Guard(); lastWidth = bitmap.PixelWidth; lastHeight = bitmap.PixelHeight;
                Require(lastWidth >= cropX + width && lastHeight >= cropY + 900, "Native bitmap does not contain its measured client viewport.");
                var buffer = await bitmap.GetPixelsAsync(); var data = new byte[buffer.Length]; using (var reader = Windows.Storage.Streams.DataReader.FromBuffer(buffer)) reader.ReadBytes(data);
                var rawPng = await Encode(data, lastWidth, lastHeight); var frameFile = id + "-render-" + attempt + ".png";
                await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(reportPath)!, frameFile), rawPng);
                var viewportData = new byte[width * 900 * 4];
                for (var row = 0; row < 900; row++) Buffer.BlockCopy(data, ((row + cropY) * lastWidth + cropX) * 4, viewportData, row * width * 4, width * 4);
                if (appShellCapture && cropX > 0) Require(data[((500 + cropY) * lastWidth + cropX - 1) * 4 + 3] == 0, "Measured client extraction starts inside painted content.");
                var png = await Encode(viewportData, width, 900); var digest = Convert.ToHexStringLower(SHA256.HashData(png));
                renderFrames.Add(new { file = frameFile, pngSha256 = Convert.ToHexStringLower(SHA256.HashData(rawPng)), pixelWidth = lastWidth, pixelHeight = lastHeight, measuredMinX, measuredMinY, cropX, cropY, viewportWidth = width, viewportHeight = 900, viewportSha256 = digest, requestedWidth = appShellCapture ? 0 : width, requestedHeight = appShellCapture ? 0 : 900, actualRootWidth = root.ActualWidth, actualRootHeight = root.ActualHeight, rasterizationScale = root.XamlRoot.RasterizationScale, captureTarget = captureTarget.GetType().Name, captureWidth = captureTarget.ActualWidth, captureHeight = captureTarget.ActualHeight, captureX = captureOrigin.X, captureY = captureOrigin.Y });
                if (previousHash == digest) { finalPng = png; finalData = viewportData; break; }
                previousHash = digest;
            }
            if (finalPng is null || finalData is null)
            {
                var visual = Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.GetElementVisual(root);
                var diagnostic = JsonSerializer.Serialize(new { id, lastWidth, lastHeight, root.ActualWidth, root.ActualHeight, viewport = root.XamlRoot.Size, windowX = window.AppWindow.Position.X, windowY = window.AppWindow.Position.Y, visualX = visual.Offset.X, visualY = visual.Offset.Y, renderFrames }, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(reportPath)!, id + "-render-diagnostic.json"), diagnostic);
                throw new InvalidOperationException("Header did not produce two stable client-sized frames: " + diagnostic);
            }
            if (id.StartsWith("app-shell-", StringComparison.Ordinal) && Enumerable.Range(0, finalData.Length / 4).Any(index => finalData[index * 4 + 3] != 255))
            {
                var boxes = Elements(root).Select(element => new { type = element.GetType().Name, element.Name, element.Visibility, element.IsLoaded, box = element.TransformToVisual(root).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight)), visualX = Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.GetElementVisual(element).Offset.X }).OrderBy(row => row.box.Left).Take(30).ToArray();
                var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Select(popup => new { popup.IsOpen, popup.HorizontalOffset, popup.VerticalOffset, child = popup.Child?.GetType().Name }).ToArray();
                await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(reportPath)!, id + "-padding-diagnostic.json"), JsonSerializer.Serialize(new { id, boxes, popups }, new JsonSerializerOptions { WriteIndented = true }));
                throw new InvalidOperationException("AppShell capture contains transparent padding instead of the actual client.");
            }
            var pixel = ((int)point.Y * width + (int)point.X) * 4; var alpha = finalData[pixel + 3];
            Require((alpha > 0) == present && OwnsTopHit() == present, "Native Header ancestor paint or topmost hit differs after rendering.");
            var file = id + ".png"; await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(reportPath)!, file), finalPng); Guard();
            return new { id, file, pngSha256 = Convert.ToHexStringLower(SHA256.HashData(finalPng)), pixelWidth = width, pixelHeight = 900, probeX = point.X, probeY = point.Y, hit, ownsTopHit = OwnsTopHit(), alpha, rgba = new int[] { finalData[pixel + 2], finalData[pixel + 1], finalData[pixel], alpha }, renderFrames, present };
        }
        BleeckerHeader NewHeader() => new() { Brand = new() { Href = "#brand", BrandName = "Fictional library" }, Navigation = new ObservableCollection<BleeckerNavItem> { new Item { Href = "#home", Label = "Home", Marker = "original-home" }, new Item { Href = "#devices", Label = "Devices", Marker = "original-devices" } } };
        async Task<Host> Mount(BleeckerHeader header, double height = 2000, double top = 0)
        {
            var host = new Host(header, new Border { Height = 1600 }) { Width = root.Width, Height = height, UseLayoutRounding = false }; host.Mount(); Canvas.SetTop(host, top); root.Children.Add(host); await Layout(); return host;
        }
        FrameworkElement Part(BleeckerHeader header, string name) => Elements(header).First(element => element.Name == name);
        BleeckerNavLinkContext Context(BleeckerHeader header, bool mobile = false) => (BleeckerNavLinkContext)Elements(Part(header, mobile ? "HeaderMobileNavigation" : "HeaderDesktopNavigation")).OfType<HyperlinkButton>().First(link => link.DataContext is BleeckerNavLinkContext).DataContext;
        async Task Case(string id, Func<Task> action)
        {
            try
            {
                root.Children.Clear();
                root = new Canvas { Width = 640, Height = 900, UseLayoutRounding = false }; root.GotFocus += (_, _) => focus++; window.Content = root;
                await Resize(640); await action(); Guard(); checks.Add(new { id, passed = true });
            }
            catch (Exception error) { failures.Add(new { id, error = error.ToString() }); checks.Add(new { id, passed = false }); }
        }
        foreach (var scheme in new[] { ElementTheme.Light, ElementTheme.Dark }) foreach (var custom in new[] { false, true })
        {
            await Case("caller-history-" + scheme + "-" + custom, async () =>
            {
                root.RequestedTheme = scheme; var header = NewHeader(); var desktop = new TextBox { Width = 120, Height = 32, MinHeight = 0, Text = "Original" }; var mobile = new TextBox { Width = 140, Height = 32, MinHeight = 0, Text = "Original" }; header.Actions = desktop; header.MobileActions = mobile;
                if (custom) header.LinkTemplate = Template("<HyperlinkButton Content='{Binding Content}' Command='{Binding Command}' Style='{Binding Style}' Foreground='{Binding Foreground}' Tag='{Binding Item}'/>");
                await Mount(header); header.ToggleNavigation(); await Task.Delay(230); await Layout(); mobile.Text = "Caller text"; mobile.Select(2, 3); await Layout();
                await Resize(1280); Require(header.IsMenuOpen, "Desktop resize reset open state."); await Resize(640); Require(header.IsMenuOpen, "Narrow resize lost open state.");
                var original = header.Navigation.ToArray(); header.Navigation = original.Reverse().ToArray(); await Layout();
                Require(ReferenceEquals(header.Actions, desktop) && ReferenceEquals(header.MobileActions, mobile) && mobile.Text == "Caller text" && mobile.SelectionStart == 2 && mobile.SelectionLength == 3 && desktop.Text == "Original", "Caller identity/edit/selection lost across viewport and source reorder.");
                BleeckerNavItem? requested = null; header.ItemInvoked += (_, item) => requested = item; var context = Context(header, true); Require(ReferenceEquals(context.Item, original[1]), "Link renderer copied the original reordered item."); context.Command.Execute(null); await Layout();
                Require(ReferenceEquals(requested, original[1]) && !header.IsMenuOpen && mobile.Text == "Caller text" && mobile.SelectionStart == 2 && mobile.SelectionLength == 3, "Mobile original-item request failed to close without losing caller state.");
            });
        }
        await Case("model-template-selector-original-data", async () =>
        {
            var header = NewHeader(); var model = new Model(); var template = Template("<TextBox Text='{Binding Value,Mode=TwoWay,UpdateSourceTrigger=PropertyChanged}' Tag='{Binding}' Width='140' Height='32' MinHeight='0'/>"); var selector = new Selector(template); header.MobileActions = model; header.MobileActionsTemplateSelector = selector; await Mount(header); header.ToggleNavigation(); await Task.Delay(230); await Layout();
            var editor = Elements(Part(header, "HeaderMobileActions")).OfType<TextBox>().Single(); Require(selector.Items.Any(item => ReferenceEquals(item, model)) && ReferenceEquals(editor.Tag, model), "Selector/template did not receive original caller data: " + JsonSerializer.Serialize(new { items = selector.Items.Select(item => new { type = item?.GetType().FullName, original = ReferenceEquals(item, model) }), tagType = editor.Tag?.GetType().FullName, contextType = editor.DataContext?.GetType().FullName, editor.Text, tagOriginal = ReferenceEquals(editor.Tag, model), presenters = Elements(Part(header, "HeaderMobileActions")).OfType<ContentPresenter>().Select(presenter => new { type = presenter.Content?.GetType().FullName, original = ReferenceEquals(presenter.Content, model) }) })); editor.Text = "Edited"; await Layout(); Require(model.Value == "Edited", "Two-way slot binding did not update original model."); model.Value = "Updated"; await Layout(); Require(editor.Text == "Updated", "Live original model did not reach slot template."); header.Navigation = header.Navigation.Reverse().ToArray(); await Layout(); Require(ReferenceEquals(editor, Elements(Part(header, "HeaderMobileActions")).OfType<TextBox>().Single()), "Unrelated navigation update recreated the caller editor.");
        });
        await Case("shared-renderer-original-item-command-and-brand", async () =>
        {
            var header = NewHeader(); var command = new Command(); var parameter = new object(); header.Command = command; header.CommandParameter = parameter; header.LinkTemplate = Template("<HyperlinkButton Content='{Binding Content}' Command='{Binding Command}' Style='{Binding Style}' Foreground='{Binding Foreground}' Tag='{Binding Item}'/>"); var originalBrand = header.Brand; BleeckerHeaderBrand? brandRequest = null; header.BrandInvoked += (_, brand) => brandRequest = brand; await Mount(header);
            var brandLink = Elements(header).OfType<HyperlinkButton>().First(link => link.DataContext is BleeckerNavLinkContext context && context.Item.Href == "#brand"); var context = (BleeckerNavLinkContext)brandLink.DataContext; Require(context.Item.Label == originalBrand.BrandName && context.Href == originalBrand.Href, "Shared brand renderer lost constructed source metadata."); context.Command.Execute(null); Require(command.Executions == 1 && ReferenceEquals(command.Parameter, parameter) && ReferenceEquals(brandRequest, originalBrand), "Brand routing lost original model or explicit command parameter.");
            var item = Context(header, true).Item; BleeckerNavItem? request = null; header.ItemInvoked += (_, value) => request = value; header.ToggleNavigation(); await Task.Delay(230); await Layout(); Context(header, true).Command.Execute(null); Require(command.Executions == 2 && ReferenceEquals(command.Parameter, parameter) && ReferenceEquals(request, item), "Shared navigation renderer lost original item/explicit parameter.");
        });
        await Case("worker-command-notification-disabled-and-retired", async () =>
        {
            var header = NewHeader(); var command = new Command(); header.Command = command; await Mount(header); header.ToggleNavigation(); await Task.Delay(230); await Layout(); var context = Context(header, true); var brand = Elements(header).OfType<HyperlinkButton>().First(link => link.DataContext is BleeckerNavLinkContext value && value.Href == "#brand");
            await Task.Run(() => command.Enable(false)); await Layout(); Require(!context.Command.CanExecute(null) && !brand.IsEnabled, "Worker command change did not disable both original navigation and brand."); context.Command.Execute(null); Require(command.Executions == 0, "Disabled request executed."); await Task.Run(() => command.Enable(true)); await Layout(); Require(context.Command.CanExecute(null) && brand.IsEnabled, "Worker command re-enable did not reach current actions.");
            header.IsEnabled = false; Require(!context.Command.CanExecute(null), "Disabled Header retained an executable link."); header.IsEnabled = true; root.Children.Clear(); await Layout(); Require(!context.Command.CanExecute(null), "Unloaded Header retained an executable action."); await Task.Run(() => command.Enable(false)); await Layout(); Require(command.Executions == 0, "Retired command produced a host request.");
        });
        await Case("template-replacement-preserves-editors-and-retires-links", async () =>
        {
            var header = NewHeader(); var desktop = new TextBox { Width = 120, Height = 32, MinHeight = 0, Text = "Desktop" }; var mobile = new TextBox { Width = 140, Height = 32, MinHeight = 0, Text = "Caller text" }; header.Actions = desktop; header.MobileActions = mobile; await Mount(header); header.ToggleNavigation(); await Task.Delay(230); await Layout(); mobile.Select(2, 3); var retired = Context(header, true);
            header.Template = (ControlTemplate)XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Control'><Grid x:Name='PART_Header' FlowDirection='LeftToRight' UseLayoutRounding='False' HorizontalAlignment='Left' VerticalAlignment='Top'/></ControlTemplate>"); header.ApplyTemplate(); await Layout();
            Require(header.IsMenuOpen && ReferenceEquals(header.Actions, desktop) && ReferenceEquals(header.MobileActions, mobile) && mobile.Text == "Caller text" && mobile.SelectionStart == 2 && mobile.SelectionLength == 3, "Template replacement lost caller/native menu state."); Require(!retired.Command.CanExecute(null), "Replaced template retained an executable request."); header.Template = null; await Layout(); Require(!ContextOrNull(header)?.Command.CanExecute(null) ?? true, "Null template retained a mounted action.");
        });
        await Case("bound-brand-and-explicit-null-logo", async () =>
        {
            var header = NewHeader(); var model = new Model(); BindingOperations.SetBinding(header.Brand, BleeckerHeaderBrand.BrandNameProperty, new Binding { Source = model, Path = new PropertyPath(nameof(Model.Value)), Mode = BindingMode.OneWay }); header.Brand.LogoSource = null; await Mount(header); model.Value = "Updated brand"; await Layout(); var content = (BleeckerBrandLockup)Part(header, "HeaderBrandContent"); Require(content.BrandName == "Updated brand" && content.LogoSource is null, "Brand binding or explicit null source was replaced.");
        });
        await Case("mobile-actions-use-source-conditional-truthiness", async () =>
        {
            var header = NewHeader(); header.Navigation = new[] { new BleeckerNavItem("#one", "One"), new BleeckerNavItem("#two", "Two"), new BleeckerNavItem("#three", "Three"), new BleeckerNavItem("#four", "Four") }; await Mount(header);
            foreach (var value in new object?[] { null, false, "", 0, 0u, 0ul, double.NaN }) { header.MobileActions = value; await Layout(); Require(Part(header, "HeaderMobileContent").ActualHeight == 208, "Falsy mobile action created a slot/gap."); }
            header.MobileActions = true; await Layout(); Require(Part(header, "HeaderMobileContent").ActualHeight == 228, "Truthy empty React child lost its conditional wrapper/gap.");
        });
        await Case("live-navigation-producer-and-replacement", async () =>
        {
            var header = NewHeader(); var original = (ObservableCollection<BleeckerNavItem>)header.Navigation; await Mount(header); header.ToggleNavigation(); await Task.Delay(230); await Layout(); Require(Part(header, "HeaderMobileContent").ActualHeight == 120, "Initial navigation content differs.");
            var addition = new Item { Href = "#worker", Label = "Worker-published original", Marker = "worker-original" }; await Task.Run(() => original.Add(addition)); await Layout(); Require(Part(header, "HeaderMobileContent").ActualHeight == 164, "Worker collection publication did not update Header's containing layout.");
            addition.Label = string.Join(" ", Enumerable.Repeat("Fictional component navigation", 12)); await Layout(); Require(Part(header, "HeaderMobileContent").ActualHeight > 164, "Live item wrapping did not reach the mobile surface.");
            var retired = Context(header, true); var replacement = new ObservableCollection<BleeckerNavItem> { new("#new-one", "New one"), new("#new-two", "New two") }; header.Navigation = replacement; await Layout(); var obsolete = new Item { Href = "#obsolete", Label = "Obsolete publication" }; await Task.Run(() => original.Add(obsolete)); await Layout(); Require(Part(header, "HeaderMobileContent").ActualHeight == 120 && ReferenceEquals(Context(header, true).Item, replacement[0]) && !retired.Command.CanExecute(null), "Retired navigation producer or request changed the replacement surface.");
        });
        foreach (var position in new[] { BleeckerHeaderPosition.Fixed, BleeckerHeaderPosition.Sticky, BleeckerHeaderPosition.Static })
        {
            await Case("actual-scroll-viewer-" + position, async () =>
            {
                await Resize(1280); var header = NewHeader(); header.Position = position; var host = new Host(header, new Border { Height = 1600 }) { Width = 1280, Height = 2000 }; host.Mount(); var content = new Canvas { Width = 1280, Height = 2400 }; Canvas.SetTop(host, 120); content.Children.Add(host); var viewer = new ScrollViewer { Width = 1280, Height = 900, Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Hidden, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, HorizontalScrollMode = ScrollMode.Disabled, VerticalScrollMode = ScrollMode.Enabled, IsTabStop = false }; root.Children.Add(viewer); await Layout(); Require(viewer.ChangeView(null, 140, null, true), "Native ScrollViewer refused source scroll."); await Layout();
                Require(Math.Abs(viewer.VerticalOffset - 140) < .000001, "Native scroll did not reach requested offset."); var surface = Part(header, "HeaderSurface"); var y = surface.TransformToVisual(root).TransformPoint(new Point()).Y; Require(Math.Abs(y - (position == BleeckerHeaderPosition.Static ? -20 : 0)) < .000001, "Actual native scroll positioning differs from source."); Require(header.DesiredSize.Height == (position == BleeckerHeaderPosition.Fixed ? 0 : 73), "Native flow contribution differs from source.");
                var bounds = FrameworkElementAutomationPeer.CreatePeerForElement(header).GetBoundingRectangle(); var physical = new FrameworkElementAutomationPeer(surface).GetBoundingRectangle(); var clippedHeight = position == BleeckerHeaderPosition.Static ? 53 : 73; Require(bounds == physical && bounds.Width == 1280 && bounds.Height == clippedHeight, "Header automation bounds do not expose its visible physical surface: " + JsonSerializer.Serialize(new { bounds, physical })); scrollEvidence.Add(await ScrollPresentation(header, position.ToString().ToLowerInvariant(), true));
            });
        }
        await Case("actual-sticky-parent-bottom", async () =>
        {
            await Resize(1280); var header = NewHeader(); header.Position = BleeckerHeaderPosition.Sticky; var host = new Host(header, new Border { Height = 1600 }) { Width = 1280, Height = 240 }; host.Mount(); var content = new Canvas { Width = 1280, Height = 2400 }; Canvas.SetTop(host, 120); content.Children.Add(host); var viewer = new ScrollViewer { Width = 1280, Height = 900, Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Hidden, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, IsTabStop = false }; root.Children.Add(viewer); await Layout(); Require(viewer.ChangeView(null, 500, null, true), "Native parent-boundary scroll refused."); await Layout(); var y = Part(header, "HeaderSurface").TransformToVisual(root).TransformPoint(new Point()).Y; Require(Math.Abs(y + 213) < .000001, "Sticky Header escaped its actual containing parent."); scrollEvidence.Add(await ScrollPresentation(header, "sticky-parent-end", false));
        });
        async Task Scroll(ScrollView view, double offset, string id)
        {
            var completed = new HashSet<int>();
            void Completed(ScrollView sender, ScrollingScrollCompletedEventArgs args) => completed.Add(args.CorrelationId);
            view.ScrollCompleted += Completed;
            try
            {
                var correlation = view.ScrollTo(0, offset, new ScrollingScrollOptions(ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore));
                var deadline = System.Diagnostics.Stopwatch.StartNew();
                while (!completed.Contains(correlation) || Math.Abs(view.VerticalOffset - offset) > .000001)
                {
                    Require(deadline.ElapsedMilliseconds < 5000, "Native ScrollView did not complete its requested scroll: " + JsonSerializer.Serialize(new { correlation, completed, offset, view.VerticalOffset, view.ExtentHeight, view.ViewportHeight }));
                    await Layout();
                }
                await Layout();
                scrollOperations.Add(new { id, correlation, completed = completed.Contains(correlation), requestedOffset = offset, actualOffset = view.VerticalOffset, view.ExtentHeight, view.ViewportHeight });
            }
            finally { view.ScrollCompleted -= Completed; }
        }
        foreach (var position in new[] { BleeckerHeaderPosition.Fixed, BleeckerHeaderPosition.Sticky, BleeckerHeaderPosition.Static })
        {
            await Case("actual-scroll-view-" + position, async () =>
            {
                await Resize(1280); var header = NewHeader(); header.Position = position;
                var host = new Host(header, new Border { Height = 1600 }) { Width = 1280, Height = 2000 }; host.Mount();
                var content = new Canvas { Width = 1280, Height = 2400 }; Canvas.SetTop(host, 120); content.Children.Add(host);
                var view = new ScrollView { Width = 1280, Height = 900, Content = content, VerticalScrollBarVisibility = ScrollingScrollBarVisibility.Hidden, HorizontalScrollBarVisibility = ScrollingScrollBarVisibility.Hidden, HorizontalScrollMode = ScrollingScrollMode.Disabled, VerticalScrollMode = ScrollingScrollMode.Enabled, ZoomMode = ScrollingZoomMode.Disabled, IsTabStop = false };
                root.Children.Add(view); await Layout(); await Scroll(view, 140, "scroll-view-" + position.ToString().ToLowerInvariant());
                var surface = Part(header, "HeaderSurface"); var y = surface.TransformToVisual(root).TransformPoint(new Point()).Y;
                Require(Math.Abs(y - (position == BleeckerHeaderPosition.Static ? -20 : 0)) < .000001, "Actual ScrollView positioning differs from source: " + JsonSerializer.Serialize(new { position, y, view.VerticalOffset }));
                Require(header.DesiredSize.Height == (position == BleeckerHeaderPosition.Fixed ? 0 : 73), "ScrollView flow contribution differs from source.");
                var bounds = FrameworkElementAutomationPeer.CreatePeerForElement(header).GetBoundingRectangle(); var physical = new FrameworkElementAutomationPeer(surface).GetBoundingRectangle();
                Require(bounds == physical && bounds.Width == 1280 && bounds.Height == (position == BleeckerHeaderPosition.Static ? 53 : 73), "ScrollView banner bounds do not expose the clipped physical surface.");
                scrollEvidence.Add(await ScrollPresentation(header, "scroll-view-" + position.ToString().ToLowerInvariant(), true));
                await Scroll(view, 0, "scroll-view-" + position.ToString().ToLowerInvariant() + "-return");
                y = surface.TransformToVisual(root).TransformPoint(new Point()).Y;
                Require(Math.Abs(y - (position == BleeckerHeaderPosition.Fixed ? 0 : 120)) < .000001, "ScrollView return scroll retained a stale position.");
            });
        }
        await Case("actual-scroll-view-sticky-parent-bottom", async () =>
        {
            await Resize(1280); var header = NewHeader(); header.Position = BleeckerHeaderPosition.Sticky;
            var host = new Host(header, new Border { Height = 1600 }) { Width = 1280, Height = 240 }; host.Mount();
            var content = new Canvas { Width = 1280, Height = 2400 }; Canvas.SetTop(host, 120); content.Children.Add(host);
            var view = new ScrollView { Width = 1280, Height = 900, Content = content, VerticalScrollBarVisibility = ScrollingScrollBarVisibility.Hidden, HorizontalScrollBarVisibility = ScrollingScrollBarVisibility.Hidden, ZoomMode = ScrollingZoomMode.Disabled, IsTabStop = false };
            root.Children.Add(view); await Layout(); await Scroll(view, 500, "scroll-view-sticky-parent-end");
            var y = Part(header, "HeaderSurface").TransformToVisual(root).TransformPoint(new Point()).Y;
            Require(Math.Abs(y + 213) < .000001, "Sticky Header escaped its actual ScrollView containing parent: " + y);
            scrollEvidence.Add(await ScrollPresentation(header, "scroll-view-sticky-parent-end", false));
        });
        await Case("reparent-scroll-owner-and-live-position", async () =>
        {
            await Resize(1280); var header = NewHeader(); header.Position = BleeckerHeaderPosition.Sticky;
            var editor = new TextBox { Width = 140, Height = 32, MinHeight = 0, Text = "Original" }; header.MobileActions = editor;
            var host = new Host(header, new Border { Height = 1600 }) { Width = 1280, Height = 2000 }; host.Mount();
            var oldContent = new Canvas { Width = 1280, Height = 2400 }; Canvas.SetTop(host, 120); oldContent.Children.Add(host);
            var oldViewer = new ScrollViewer { Width = 1280, Height = 900, Content = oldContent, VerticalScrollBarVisibility = ScrollBarVisibility.Hidden, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, IsTabStop = false };
            root.Children.Add(oldViewer); await Layout(); Require(oldViewer.ChangeView(null, 140, null, true), "Initial owner refused scroll."); await Layout(); header.ToggleNavigation(); editor.Text = "Retained original"; editor.Select(2, 3);
            oldContent.Children.Remove(host); await Layout();
            var content = new Canvas { Width = 1280, Height = 2400 }; content.Children.Add(host);
            var view = new ScrollView { Width = 1280, Height = 900, Content = content, VerticalScrollBarVisibility = ScrollingScrollBarVisibility.Hidden, HorizontalScrollBarVisibility = ScrollingScrollBarVisibility.Hidden, ZoomMode = ScrollingZoomMode.Disabled, IsTabStop = false };
            root.Children.Add(view); await Layout(); await Scroll(view, 150, "scroll-view-reparented");
            Require(oldViewer.ChangeView(null, 500, null, true), "Old scroll owner refused its independent scroll."); await Layout();
            double Y() => Part(header, "HeaderSurface").TransformToVisual(root).TransformPoint(new Point()).Y;
            Require(Math.Abs(Y()) < .000001, "Retired scroll owner changed the reparented Header.");
            header.Position = BleeckerHeaderPosition.Static; await Layout(); Require(Math.Abs(Y() + 30) < .000001 && header.DesiredSize.Height == 73, "Live static position retained sticky translation.");
            header.Position = BleeckerHeaderPosition.Fixed; await Layout(); Require(Math.Abs(Y()) < .000001 && header.DesiredSize.Height == 0, "Live fixed position retained static flow.");
            header.Position = BleeckerHeaderPosition.Sticky; await Layout(); Require(Math.Abs(Y()) < .000001 && header.DesiredSize.Height == 73, "Live sticky position did not reattach its current owner.");
            Require(header.IsMenuOpen && ReferenceEquals(header.MobileActions, editor) && editor.Text == "Retained original" && editor.SelectionStart == 2 && editor.SelectionLength == 3, "Scroll reparenting or live positioning discarded caller/menu state.");
            scrollEvidence.Add(await ScrollPresentation(header, "scroll-view-reparented", true));
        });
        foreach (var scheme in new[] { ElementTheme.Light, ElementTheme.Dark })
        {
            await Case("app-shell-header-composition-" + scheme, async () =>
            {
                await Resize(1280); root.RequestedTheme = scheme; var header = NewHeader();
                var editor = new TextBox { Width = 160, Height = 32, MinHeight = 0, Text = "Original body", HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
                var footer = new Border { Height = 32 };
                var shell = new BleeckerAppShell { Width = 1280, Height = 900, MinHeight = 0, Header = header, Content = editor, Footer = footer };
                root.Children.Add(shell); await Layout();
                FrameworkElement ShellPart(string name) => Elements(shell).Single(element => element.Name == name);
                double Y(FrameworkElement element) => element.TransformToVisual(root).TransformPoint(new Point()).Y;
                Require(header.DesiredSize.Height == 0 && Y(Part(header, "HeaderSurface")) == 0 && Y(ShellPart("AppShellMain")) == 0 && Y(ShellPart("AppShellContent")) == 72 && Y(footer) == 868, "Fixed Header/AppShell composition doubled the header offset or changed footer flow.");
                async Task CaptureComposition(string id)
                {
                    var source = compositionReference.RootElement.GetProperty("cases").EnumerateArray().Single(row => row.GetProperty("id").GetString() == id && row.GetProperty("scheme").GetString() == scheme.ToString());
                    var elements = new Dictionary<string, FrameworkElement> { ["header"] = Part(header, "HeaderSurface"), ["main"] = ShellPart("AppShellMain"), ["content"] = ShellPart("AppShellContent"), ["body"] = editor, ["footer"] = footer };
                    var boxes = new Dictionary<string, object>();
                    foreach (var (name, element) in elements)
                    {
                        var box = element.TransformToVisual(root).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
                        var expected = source.GetProperty("measurement").GetProperty(name);
                        Require(Math.Abs(box.X - expected.GetProperty("x").GetDouble()) < .000001 && Math.Abs(box.Y - expected.GetProperty("y").GetDouble()) < .000001 && Math.Abs(box.Width - expected.GetProperty("width").GetDouble()) < .000001 && Math.Abs(box.Height - expected.GetProperty("height").GetDouble()) < .000001, "Native Header/AppShell differs from source " + scheme + "/" + id + "/" + name + ": " + box);
                        boxes[name] = new { x = box.X, y = box.Y, width = box.Width, height = box.Height };
                    }
                    var point = source.GetProperty("measurement").GetProperty("point");
                    scrollEvidence.Add(await ScrollPresentation(header, "app-shell-" + scheme.ToString().ToLowerInvariant() + "-" + id, true, new Point(point.GetProperty("x").GetDouble(), point.GetProperty("y").GetDouble())));
                    compositionEvidence.Add(new { id, scheme = scheme.ToString(), actualTheme = header.ActualTheme.ToString(), measurement = boxes, menuOpen = header.IsMenuOpen, flowHeight = header.DesiredSize.Height, originalBody = ReferenceEquals(shell.Content, editor), text = editor.Text, selectionStart = editor.SelectionStart, selectionLength = editor.SelectionLength });
                }
                editor.Text = "Original body edited"; editor.Select(2, 3); await CaptureComposition("wide"); shell.Width = 640; await Resize(640); await CaptureComposition("narrow");
                header.ToggleNavigation(); await Task.Delay(230); await Layout();
                Require(header.IsMenuOpen && Y(ShellPart("AppShellContent")) == 72 && Y(footer) == 868, "AppShell mobile overlay changed normal flow.");
                await CaptureComposition("narrow-open");
                header.ToggleNavigation(); await Task.Delay(230); await Layout(); shell.Width = 1280; await Resize(1280);
                Require(ReferenceEquals(shell.Header, header) && ReferenceEquals(shell.Content, editor) && editor.Text == "Original body edited" && editor.SelectionStart == 2 && editor.SelectionLength == 3, "Header/AppShell viewport change replaced caller content.");
                bool OwnsTopHit()
                {
                    var hit = VisualTreeHelper.FindElementsInHostCoordinates(new Point(1240, 30), root).FirstOrDefault();
                    return hit is not null && Elements(Part(header, "HeaderSurface")).Contains(hit);
                }
                Require(Canvas.GetZIndex(ShellPart("AppShellHeader")) == 50 && OwnsTopHit(), "AppShell wrapper did not inherit the original Header's default stacking.");
                Canvas.SetZIndex(header, -1); await Layout(); Require(Canvas.GetZIndex(ShellPart("AppShellHeader")) == -1 && !OwnsTopHit(), "Lowering caller stacking did not reach the slot wrapper.");
                Canvas.SetZIndex(header, 75); await Layout(); Require(Canvas.GetZIndex(ShellPart("AppShellHeader")) == 75 && OwnsTopHit(), "Raising caller stacking did not restore the Header above main.");
                var replacement = new Border { Height = 0 }; Canvas.SetZIndex(replacement, 3); shell.Header = replacement; await Layout(); Canvas.SetZIndex(header, 99); await Layout();
                Require(Canvas.GetZIndex(ShellPart("AppShellHeader")) == 3 && !OwnsTopHit(), "Retired Header stacking producer changed its replacement slot.");
                var model = new Model(); shell.HeaderTemplate = Template("<Grid Name='CallerStackingRoot' Canvas.ZIndex='7' Height='0' Tag='{Binding}'/>"); shell.Header = model; await Layout();
                var templateRoot = Elements(ShellPart("AppShellHeader")).Single(element => element.Name == "CallerStackingRoot"); Require(ReferenceEquals(templateRoot.Tag, model) && Canvas.GetZIndex(ShellPart("AppShellHeader")) == 7, "Templated original root stacking/data did not cross the slot wrapper.");
                Canvas.SetZIndex(templateRoot, 8); await Layout(); Require(Canvas.GetZIndex(ShellPart("AppShellHeader")) == 8, "Live template-root stacking did not reach its wrapper.");
                shell.Header = null; shell.HeaderTemplate = null; await Layout(); Canvas.SetZIndex(templateRoot, 42); await Layout(); Require(Canvas.GetZIndex(ShellPart("AppShellHeader")) == 0, "Empty slot retained stacking or an obsolete template-root notification.");
                Canvas.SetZIndex(footer, 11); await Layout(); Require(Canvas.GetZIndex(ShellPart("AppShellFooter")) == 11, "Original footer stacking did not reach its slot wrapper.");
                shell.Header = header; await Layout(); Require(Canvas.GetZIndex(ShellPart("AppShellHeader")) == 99 && OwnsTopHit() && ReferenceEquals(shell.Content, editor) && editor.Text == "Original body edited", "Restored original Header lost current stacking or caller body state.");
                scrollEvidence.Add(await ScrollPresentation(header, "app-shell-" + scheme.ToString().ToLowerInvariant() + "-restored", true));
                var retainedHeaderSlot = ShellPart("AppShellHeader"); root.Children.Clear(); await Layout();
                Require(Canvas.GetZIndex(retainedHeaderSlot) == 99, "Unloading changed the retired slot's cached stacking.");
                Canvas.SetZIndex(header, 101); await Layout(); Require(Canvas.GetZIndex(retainedHeaderSlot) == 99, "Unloaded original root still published stacking changes.");
            });
        }
        await Case("caller-paint-identity-live-alpha-and-retired-producers", async () =>
        {
            var background = new SolidColorBrush(Windows.UI.Color.FromArgb(200, 15, 50, 90)) { Opacity = .43 };
            var border = new SolidColorBrush(Windows.UI.Color.FromArgb(180, 220, 90, 10)) { Opacity = .37 };
            var originalBackgroundOpacity = background.Opacity; var originalBorderOpacity = border.Opacity;
            var header = NewHeader(); header.Background = background; header.BorderBrush = border; var host = await Mount(header);
            Border BackgroundPart() => (Border)Part(header, "HeaderBackground");
            Border RulePart() => (Border)Part(header, "HeaderBottomRule");
            void Encoded(Border part, byte alpha, byte red, byte green, byte blue)
            {
                Require(part.Background is SolidColorBrush brush && brush.Opacity == 1 && brush.Color == Windows.UI.Color.FromArgb(alpha, red, green, blue), "Actual Header paint differs from its byte-alpha contract.");
            }
            Require(ReferenceEquals(header.Background, background) && ReferenceEquals(header.BorderBrush, border) && background.Opacity == originalBackgroundOpacity && border.Opacity == originalBorderOpacity && background.Color.A == 200 && border.Color.A == 180, "Header changed caller brush identity or values.");
            Encoded(BackgroundPart(), 86, 15, 50, 90); Encoded(RulePart(), 67, 220, 90, 10);
            background.Opacity = .61; border.Color = Windows.UI.Color.FromArgb(127, 50, 100, 150); await Layout();
            Encoded(BackgroundPart(), 122, 15, 50, 90); Encoded(RulePart(), 47, 50, 100, 150);
            var replacement = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 30, 60, 90)) { Opacity = .5 };
            var replacementRule = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 40, 80, 120)) { Opacity = .25 };
            header.Background = replacement; header.BorderBrush = replacementRule; await Layout(); background.Opacity = .1; border.Opacity = .9; await Layout();
            Encoded(BackgroundPart(), 128, 30, 60, 90); Encoded(RulePart(), 64, 40, 80, 120);
            var retiredBackground = BackgroundPart(); var retiredRule = RulePart();
            header.Template = (ControlTemplate)XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Control'><Grid x:Name='PART_Header' UseLayoutRounding='False'/></ControlTemplate>"); await Layout();
            replacement.Opacity = .2; replacementRule.Color = Windows.UI.Color.FromArgb(128, 40, 80, 120); await Layout();
            Require(!ReferenceEquals(BackgroundPart(), retiredBackground) && ReferenceEquals(header.Background, replacement) && ReferenceEquals(header.BorderBrush, replacementRule), "Template replacement lost caller paint or retained the old surface.");
            Encoded(BackgroundPart(), 51, 30, 60, 90); Encoded(RulePart(), 32, 40, 80, 120);
            Encoded(retiredBackground, 128, 30, 60, 90); Encoded(retiredRule, 64, 40, 80, 120);
            var gradient = new LinearGradientBrush(); gradient.GradientStops.Add(new GradientStop { Color = Microsoft.UI.Colors.Red }); gradient.GradientStops.Add(new GradientStop { Color = Microsoft.UI.Colors.Blue, Offset = 1 });
            header.Background = gradient; header.BorderBrush = null; await Layout(); gradient.Opacity = .3;
            Require(ReferenceEquals(BackgroundPart().Background, gradient) && RulePart().Background is null && ReferenceEquals(header.Background, gradient), "Non-solid or null caller paint was substituted.");
            header.Background = replacement; header.BorderBrush = replacementRule; await Layout(); var unloadedBackground = BackgroundPart(); var unloadedRule = RulePart(); root.Children.Clear(); await Layout();
            replacement.Opacity = .8; replacementRule.Opacity = .2; await Layout();
            Encoded(unloadedBackground, 51, 30, 60, 90); Encoded(unloadedRule, 32, 40, 80, 120);
            root.Children.Add(host); await Layout(); Encoded(BackgroundPart(), 204, 30, 60, 90); Encoded(RulePart(), 26, 40, 80, 120);
        });
        BleeckerNavLinkContext? ContextOrNull(BleeckerHeader header) => Elements(header).OfType<HyperlinkButton>().Select(link => link.DataContext as BleeckerNavLinkContext).FirstOrDefault(context => context is not null);
        string Hash(string file) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!); await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(new { passed = failures.Count == 0, diagnosticOnly = true, scope = "hidden-header-public-contracts", checks, failures, scrollEvidence, scrollOperations, compositionEvidence, compositionReferenceSha256 = Convert.ToHexStringLower(SHA256.HashData(compositionBytes)), guardChecks = guards, activations, focus, activateCalled = false, showCalled = false, focusCalled = false, inputSent = false, processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(), osArchitecture = RuntimeInformation.OSArchitecture.ToString(), hostSha256 = Hash(hostPath), librarySha256 = Hash(typeof(BleeckerHeader).Assembly.Location), actualNativeScrollVerified = scrollEvidence.Count == 17 && scrollOperations.Count == 8 && compositionEvidence.Count == 6, nativeInputFocusVerified = false, visibleRuntimeVerified = false, sourceRasterParityVerified = false, nativeComponentAccepted = false }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(failures.Count == 0 ? 0 : 1);
    }
}
