using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Testing;

internal static class AuthShellFixture
{
    private delegate bool Visitor(nint handle, nint value);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor, nint value);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle, out uint process);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint handle);
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node) { if (node is FrameworkElement e) yield return e; for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) foreach (var child in Elements(VisualTreeHelper.GetChild(node, i))) yield return child; }
    private static string Hash(string file) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
    internal static async Task RunAsync(Window window, string output, string hostAssembly, Func<FrameworkElement>? catalogFactory = null)
    {
        var folder = Path.GetDirectoryName(output)!; using var document = JsonDocument.Parse(await File.ReadAllBytesAsync(Path.Combine(folder, "source-reference.json"))); var source = document.RootElement;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        var root = (Grid)XamlReader.Load("<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' UseLayoutRounding='False' Background='{ThemeResource BleeckerBackgroundBrush}'/>");
        var host = new BleeckerModalHost { Content = root, Width = 1440, Height = 760 }; window.Content = host; window.AppWindow.ResizeClient(new(1440, 760));
        var stage = "initial"; var guards = 0; var focus = 0; var activations = 0; var width = 1440d; var height = 760d; var cases = new List<object>(); var contracts = new List<object>(); var pngs = new List<object>(); var catalogs = new List<object>();
        var callers = new Dictionary<string, Grid>(); BleeckerAuthShell? current = null; ScrollViewer? pageViewport = null;
        host.GotFocus += (_, _) => focus++; window.Activated += (_, args) => { if (args.WindowActivationState != WindowActivationState.Deactivated) activations++; };
        void Require(bool value, string label) { if (!value) throw new InvalidOperationException(label); }
        void Guard()
        {
            guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var visible = false;
            EnumWindows((h, _) => { GetWindowThreadProcessId(h, out var process); visible |= process == Environment.ProcessId && IsWindowVisible(h); return true; }, 0);
            Require(!visible && !window.Visible && !window.AppWindow.IsVisible && foreground != Environment.ProcessId && focus == 0 && activations == 0, "AuthShell left its hidden background boundary");
            Require(host.XamlRoot is null || VisualTreeHelper.GetOpenPopupsForXamlRoot(host.XamlRoot).Count == 0, "AuthShell opened a native Popup");
        }
        void Fail(Exception error) { File.WriteAllText(output, JsonSerializer.Serialize(new { format = "orchard-auth-shell-native-v1", passed = false, stage, error = error.ToString(), cases, contracts, pngs, catalogs, guards, focus, activations }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(1); }
        Application.Current.UnhandledException += (_, args) => Fail(args.Exception);
        async Task Layout(int delay = 35)
        {
            File.WriteAllText(Path.Combine(folder, "auth-shell-stage.json"), JsonSerializer.Serialize(new { stage, cases = cases.Count, contracts = contracts.Count, guards, focus, activations }));
            for (var i = 0; i < 3; i++) { var idle = new TaskCompletionSource(); Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => idle.SetResult()), "Dispatcher stopped"); await idle.Task; host.Measure(new(width, height)); host.Arrange(new(0, 0, width, height)); host.UpdateLayout(); Guard(); }
            if (delay > 0) await Task.Delay(delay); host.UpdateLayout(); Guard();
        }
        void Resize(double w = 1440, double h = 760) { width = w; height = h; host.Width = w; host.Height = h; root.Clip = new RectangleGeometry { Rect = new(0, 0, w, h) }; window.AppWindow.ResizeClient(new((int)w, (int)h)); }
        FrameworkElement Find(string name) => Elements(current!).Single(e => e.Name == name);
        object Box(FrameworkElement e) { var r = e.TransformToVisual(host).TransformBounds(new(0, 0, e.ActualWidth, e.ActualHeight)); return new { x = r.X, y = r.Y, width = r.Width, height = r.Height }; }
        object? Paint(Brush? brush) => brush is SolidColorBrush b ? new { a = b.Color.A, r = b.Color.R, g = b.Color.G, b = b.Color.B, opacity = b.Opacity } : null;
        void Invoke(Button button) => ((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(button).GetPattern(PatternInterface.Invoke)).Invoke();
        async Task<string> Capture(string name)
        {
            byte[]? previous = null, bytes = null; var attempts = 0;
            for (; attempts < 6; attempts++) { var image = new RenderTargetBitmap(); await image.RenderAsync(host, (int)width, (int)height); bytes = (await image.GetPixelsAsync()).ToArray(); if (previous is not null && previous.AsSpan().SequenceEqual(bytes)) break; previous = bytes; await Layout(45); }
            Require(attempts < 6, "AuthShell frame did not settle: " + name); var file = name + ".png"; using var stream = new InMemoryRandomAccessStream(); var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream); encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, (uint)width, (uint)height, 96, 96, bytes!); await encoder.FlushAsync(); stream.Seek(0); using (var target = File.Create(Path.Combine(folder, file))) await stream.AsStreamForRead().CopyToAsync(target); pngs.Add(new { file, sha256 = Hash(Path.Combine(folder, file)), stableFrames = 2, attempts = attempts + 1 }); return file;
        }
        string? Text(JsonElement s, string key) => s.TryGetProperty(key, out var v) ? v.GetString() : null;
        double Number(JsonElement s, string key, double fallback) => s.TryGetProperty(key, out var v) ? v.GetDouble() : fallback;
        Grid Caller(string slot, double h)
        {
            var grid = new Grid { Name = "Caller_" + slot, Height = h, UseLayoutRounding = false, Background = new SolidColorBrush(Windows.UI.Color.FromArgb(20, slot == "aside" ? (byte)255 : (byte)100, slot == "aside" ? (byte)255 : (byte)120, slot == "aside" ? (byte)255 : (byte)140)) };
            var title = new TextBlock { Text = slot == "content" ? "Original fictional access content" : "Original " + slot, VerticalAlignment = VerticalAlignment.Top, UseLayoutRounding = false };
            var input = new TextBox { Text = "Original " + slot, FontSize = 12, Height = 24, MinHeight = 0, Padding = new(0), BorderThickness = new(0), Background = null, VerticalAlignment = VerticalAlignment.Top, Margin = new(0, 24, 0, 0), UseLayoutRounding = false }; AutomationProperties.SetName(input, "Original " + slot + " editor");
            var button = new Button { Content = "Original " + slot + " action", FontSize = 12, Height = 24, MinHeight = 0, Padding = new(0), BorderThickness = new(0), Background = null, VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Left, Margin = new(0, 48, 0, 0), UseLayoutRounding = false };
            grid.Children.Add(title); grid.Children.Add(input); grid.Children.Add(button); grid.SizeChanged += (_, _) => grid.Clip = new RectangleGeometry { Rect = new(0, 0, grid.ActualWidth, grid.ActualHeight) }; callers[slot] = grid; return grid;
        }
        DataTemplate EmptyTemplate() => (DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Grid/></DataTemplate>");
        object? Slot(JsonElement s, string slot, double h) => Text(s, slot) switch { "none" => null, "zero" => 0, "empty" => "", "false" => false, "empty-array" => null, _ => Caller(slot, h) };
        async Task<BleeckerAuthShell> New(JsonElement spec = default, Action<BleeckerAuthShell>? configure = null)
        {
            if (pageViewport is not null) pageViewport.Content = null; root.Children.Clear(); await Layout(); Resize(spec.ValueKind == JsonValueKind.Undefined ? 1440 : Number(spec, "width", 1440), spec.ValueKind == JsonValueKind.Undefined ? 760 : Number(spec, "height", 760)); callers.Clear();
            if (spec.ValueKind == JsonValueKind.Undefined) spec = JsonSerializer.SerializeToElement(new { layout = "split", side = "start" });
            var c = new BleeckerAuthShell { Layout = Text(spec, "layout") == "split" ? BleeckerAuthShellLayout.Split : BleeckerAuthShellLayout.Centered, Side = Text(spec, "side") == "start" ? BleeckerAuthShellSide.Start : BleeckerAuthShellSide.End, AsideLabel = "Original membership note", FontSize = Number(spec, "rootFont", 16), FlowDirection = spec.TryGetProperty("rtl", out var rtl) && rtl.GetBoolean() ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
            c.Brand = Slot(spec, "brand", Number(spec, "brandHeight", 40)); c.Footer = Slot(spec, "footer", Number(spec, "footerHeight", 32)); if (c.Layout == BleeckerAuthShellLayout.Split) c.Aside = Slot(spec, "aside", Number(spec, "asideHeight", 160));
            c.Content = Text(spec, "children") switch { "none" => null, "zero" => 0, _ => Caller("content", Number(spec, "contentHeight", 220)) };
            if (Text(spec, "brand") == "empty-array") c.BrandTemplate = EmptyTemplate(); if (Text(spec, "footer") == "empty-array") c.FooterTemplate = EmptyTemplate(); if (Text(spec, "aside") == "empty-array") c.AsideTemplate = EmptyTemplate();
            if (spec.TryGetProperty("style", out _)) c.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 10, 20, 30));
            configure?.Invoke(c); AutomationProperties.SetName(c, "Original access shell"); current = c; pageViewport = (ScrollViewer)XamlReader.Load("<ScrollViewer xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' IsTabStop='False' Padding='0' BorderThickness='0' UseLayoutRounding='False' HorizontalScrollMode='Disabled' HorizontalScrollBarVisibility='Disabled' VerticalScrollMode='Enabled' VerticalScrollBarVisibility='Hidden' HorizontalContentAlignment='Stretch' VerticalContentAlignment='Top' ZoomMode='Disabled'/>"); pageViewport.Content = c; root.Children.Add(pageViewport); await Layout(100); return c;
        }
        object Snapshot(BleeckerAuthShell c)
        {
            object Region(string name) => new { box = Box(Find(name)) };
            object? Optional(string name) { var element = Elements(c).SingleOrDefault(e => e.Name == name && e.Visibility == Visibility.Visible); return element is null ? null : new { box = Box(element) }; }
            var mainPeer = FrameworkElementAutomationPeer.CreatePeerForElement(c); var aside = Elements(c).OfType<Panel>().SingleOrDefault(e => e.Name == "AuthShellAside"); var footer = Elements(c).OfType<ContentControl>().Single(e => e.Name == "AuthShellFooter"); var asidePeer = aside is null ? null : FrameworkElementAutomationPeer.CreatePeerForElement(aside); var footerPeer = FrameworkElementAutomationPeer.CreatePeerForElement(footer);
            var circleRows = Elements(c).OfType<Microsoft.UI.Xaml.Shapes.Ellipse>().Select(e => new { box = Box(e), hitTestVisible = e.IsHitTestVisible, accessibilityView = AutomationProperties.GetAccessibilityView(e).ToString(), strokeThickness = e.StrokeThickness, stroke = Paint(e.Stroke) }).ToArray();
            var connected = Elements(c).ToHashSet(); var originalCallers = callers.Where(pair => connected.Contains(pair.Value)).ToDictionary(pair => pair.Key, pair => new { box = Box(pair.Value), value = Elements(pair.Value).OfType<TextBox>().Single().Text, fontFamily = pair.Value.Children.OfType<TextBlock>().Single().FontFamily.Source, fontSize = pair.Value.Children.OfType<TextBlock>().Single().FontSize, connected = true, visible = pair.Key != "aside" || aside?.Visibility == Visibility.Visible });
            return new { main = new { box = Box(c), foreground = Paint(c.Foreground), landmark = mainPeer.GetLandmarkType().ToString(), name = mainPeer.GetName() }, access = new { box = Box(Find("AuthShellAccess")), background = Paint(((Panel)Find("AuthShellAccess")).Background) }, inner = Region("AuthShellInner"), brand = Optional("AuthShellBrand"), content = Region("AuthShellContent"), contentInner = new { box = Box(Find("AuthShellContentInner")), text = Elements(c).OfType<TextBlock>().Single(e => e.Name == "AuthShellBodyText").Text }, footer = footer.Visibility == Visibility.Visible ? new { box = Box(footer), foreground = Paint(footer.Foreground), fontSize = footer.FontSize, landmark = footerPeer.GetLandmarkType().ToString(), localizedLandmark = footerPeer.GetLocalizedLandmarkType() } : null, aside = aside is null ? null : new { box = Box(aside), visible = aside.Visibility == Visibility.Visible, background = Paint(aside.Background), foreground = Paint(c.AsideForeground), name = AutomationProperties.GetName(aside), landmark = asidePeer?.GetLandmarkType().ToString(), localizedLandmark = asidePeer?.GetLocalizedLandmarkType() }, circles = circleRows, caller = originalCallers };
        }
        try
        {
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                root.RequestedTheme = theme; await Layout();
                foreach (var row in source.GetProperty("cases").EnumerateArray().Where(c => !Environment.GetCommandLineArgs().Contains("--auth-shell-contracts-only") && c.GetProperty("scheme").GetString() == theme.ToString()))
                {
                    var id = row.GetProperty("id").GetString()!; stage = theme + "/" + id; var c = await New(row.GetProperty("spec")); var file = await Capture(theme + "-" + id); cases.Add(new { theme = theme.ToString(), id, viewport = new { width, height }, snapshot = Snapshot(c), file });
                }
                stage = theme + "/retained-side-resize"; var control = await New(); var access = Find("AuthShellAccess"); var brand = callers["brand"]; var body = callers["content"]; var footer = callers["footer"]; var aside = callers["aside"]; foreach (var pair in callers) Elements(pair.Value).OfType<TextBox>().Single().Text = "Typed " + pair.Key;
                control.Side = BleeckerAuthShellSide.End; await Layout(); Resize(639); await Layout(); var hiddenAsideMounted = Elements(control).Contains(aside) && Find("AuthShellAside").Visibility == Visibility.Collapsed; Resize(1280); await Layout();
                var sameAccess = ReferenceEquals(access, Find("AuthShellAccess")); var sameBrand = Elements(control).Contains(brand); var sameFooter = Elements(control).Contains(footer); var sameAside = Elements(control).Contains(aside); var typedValuesRetained = callers.All(p => Elements(p.Value).OfType<TextBox>().Single().Text == "Typed " + p.Key); Require(sameAccess && sameBrand && sameFooter && sameAside && hiddenAsideMounted && typedValuesRetained, stage); contracts.Add(new { theme = theme.ToString(), id = "retained-side-resize", passed = true, sameAccess, sameBrand, sameFooter, sameAside, hiddenAsideMounted, typedValuesRetained });
                stage = theme + "/templated-aside-remount"; var originalModel = new Model(); control = await New(configure: c => { c.AsideTemplate = ModelTemplate(); c.Aside = originalModel; }); await Layout(); var oldRoot = Find("OriginalModelRoot"); var oldEditor = (TextBox)Find("OriginalModelEditor"); oldEditor.Text = "Typed aside"; control.Layout = BleeckerAuthShellLayout.Centered; await Layout(); var oldDetached = !Elements(control).Contains(oldRoot); control.Layout = BleeckerAuthShellLayout.Split; await Layout(); var newRoot = Find("OriginalModelRoot"); var differentRoot = !ReferenceEquals(oldRoot, newRoot); var resetEditor = ((TextBox)Find("OriginalModelEditor")).Text == "Original model editor"; var sameOriginalModel = ReferenceEquals(control.Aside, originalModel); Require(oldDetached && differentRoot && resetEditor && sameOriginalModel, stage); contracts.Add(new { theme = theme.ToString(), id = "templated-aside-remount", passed = true, oldDetached, differentRoot, resetEditor, sameOriginalModel });
                stage = theme + "/original-selector-model"; originalModel = new Model(); var selector = new Selector(originalModel, ModelTemplate()); control = await New(configure: c => { c.Content = originalModel; c.ContentTemplateSelector = selector; }); await Layout(); oldRoot = Find("OriginalModelRoot"); control.Side = BleeckerAuthShellSide.End; await Layout(); originalModel.Label = "Updated original model"; await Layout(); var receivedOriginal = selector.OriginalSelections > 0 && !selector.Wrong && ReferenceEquals(((FrameworkElement)Find("OriginalModelRoot")).Tag, originalModel); var sameTemplateRoot = ReferenceEquals(oldRoot, Find("OriginalModelRoot")); var liveBindingUpdated = ((TextBlock)Find("OriginalModelLabel")).Text == "Updated original model"; Require(receivedOriginal && sameTemplateRoot && liveBindingUpdated, stage + $": receivedOriginal={receivedOriginal}, seen={selector.Seen}, wrong={selector.Wrong}, sameTemplateRoot={sameTemplateRoot}, liveBindingUpdated={liveBindingUpdated}, actualLabel={((TextBlock)Find("OriginalModelLabel")).Text}, items={string.Join("|", selector.Items)}"); contracts.Add(new { theme = theme.ToString(), id = "original-selector-model", passed = true, receivedOriginal, sameTemplateRoot, liveBindingUpdated, nullInitializationSelections = selector.NullSelections, originalSelections = selector.OriginalSelections });
                stage = theme + "/template-replacement"; oldRoot = Find("OriginalModelRoot"); control.ContentTemplate = ModelTemplate(); control.ContentTemplateSelector = null; await Layout(); Require(!ReferenceEquals(oldRoot, Find("OriginalModelRoot")) && !Elements(control).Contains(oldRoot), stage); contracts.Add(new { theme = theme.ToString(), id = "template-replacement", passed = true });
                stage = theme + "/caller-ui-removal"; control = await New(); body = callers["content"]; control.Content = "Replacement scalar"; await Layout(); var detached = !Elements(control).Contains(body); control.Content = body; await Layout(); Require(detached && ReferenceEquals(control.Content, body) && Elements(control).Contains(body), stage); contracts.Add(new { theme = theme.ToString(), id = "caller-ui-removal", passed = true });
                stage = theme + "/slot-truthiness"; control = await New(); control.Brand = 0; control.Footer = false; control.Aside = ""; control.Content = 0; await Layout(); Require(Find("AuthShellBrand").Visibility == Visibility.Collapsed && Find("AuthShellFooter").Visibility == Visibility.Collapsed && !Elements(control).Any(e => e.Name == "AuthShellAside") && ((TextBlock)Find("AuthShellBodyText")).Text == "0", stage); control.AsideTemplate = EmptyTemplate(); control.BrandTemplate = EmptyTemplate(); await Layout(); Require(Find("AuthShellBrand").Visibility == Visibility.Visible && Find("AuthShellAside").Visibility == Visibility.Visible, stage); contracts.Add(new { theme = theme.ToString(), id = "slot-truthiness", passed = true });
                stage = theme + "/landmarks-and-order"; control = await New(); var mainPeer = FrameworkElementAutomationPeer.CreatePeerForElement(control); var asidePeer = FrameworkElementAutomationPeer.CreatePeerForElement(Find("AuthShellAside")); var order = Elements(control).OfType<TextBox>().Select(AutomationProperties.GetName).ToArray(); Require(mainPeer.GetLandmarkType() == AutomationLandmarkType.Main && asidePeer.GetName() == "Original membership note" && asidePeer.GetLocalizedLandmarkType() == "complementary" && order.SequenceEqual(new[] { "Original brand editor", "Original content editor", "Original footer editor", "Original aside editor" }), stage); contracts.Add(new { theme = theme.ToString(), id = "landmarks-and-order", passed = true, order });
                stage = theme + "/original-local-action"; control = await New(); var invocations = 0; var button = callers["content"].Children.OfType<Button>().Single(); button.Click += (_, _) => invocations++; Invoke(button); await Layout(); Require(invocations == 1, stage); contracts.Add(new { theme = theme.ToString(), id = "original-local-action", passed = true, invocations });
                stage = theme + "/disabled-native-action"; control.IsEnabled = false; await Layout(); invocations = 0; var rejected = false; try { Invoke(button); } catch (Exception error) when (error.HResult == unchecked((int)0x80040200) || error.GetType().Name == "ElementNotEnabledException") { rejected = true; } await Layout(); Require(rejected && !button.IsEnabled && invocations == 0, stage); contracts.Add(new { theme = theme.ToString(), id = "disabled-native-action", passed = true, rejected, invocations });
                stage = theme + "/theme-font-retention"; control = await New(); body = callers["content"]; var editor = Elements(body).OfType<TextBox>().Single(); editor.Text = "Typed theme value"; root.RequestedTheme = theme == ElementTheme.Light ? ElementTheme.Dark : ElementTheme.Light; control.FontSize = 32; control.FlowDirection = FlowDirection.RightToLeft; await Layout(); Require(Elements(control).Contains(body) && editor.Text == "Typed theme value", stage); root.RequestedTheme = theme; await Layout(); contracts.Add(new { theme = theme.ToString(), id = "theme-font-retention", passed = true });
                stage = theme + "/unload-reload-retention"; pageViewport!.Content = null; await Layout(); pageViewport.Content = control; await Layout(); Require(Elements(control).Contains(body) && editor.Text == "Typed theme value" && Elements(control).Count(e => e.Name == "AuthShellAccess") == 1, stage); contracts.Add(new { theme = theme.ToString(), id = "unload-reload-retention", passed = true });
                stage = theme + "/live-native-brushes"; control = await New(); var brush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 13, 27, 41)); var style = new Style { TargetType = typeof(BleeckerAuthShell) }; style.Setters.Add(new Setter(Control.BackgroundProperty, brush)); control.Style = style; control.AccessBackground = brush; control.AsideBackground = brush; await Layout(); var callerStyleRetained = ReferenceEquals(control.Background, brush) && ReferenceEquals(((Panel)Find("AuthShellAccess")).Background, brush) && ReferenceEquals(((Panel)Find("AuthShellAside")).Background, brush); Require(callerStyleRetained, stage); contracts.Add(new { theme = theme.ToString(), id = "live-native-brushes", passed = true, callerStyleRetained });
                if (catalogFactory is not null) { stage = theme + "/catalog"; root.Children.Clear(); await Layout(); Resize(); var fixture = catalogFactory(); root.Children.Add(fixture); await Layout(200); var controls = Elements(fixture).OfType<BleeckerAuthShell>().ToArray(); var split = controls.Any(c => c.Layout == BleeckerAuthShellLayout.Split); var centered = controls.Any(c => c.Layout == BleeckerAuthShellLayout.Centered); var disabled = controls.Any(c => !c.IsEnabled); Require(controls.Length >= 3 && split && centered && disabled, stage); catalogs.Add(new { theme = theme.ToString(), passed = true, controls = controls.Length, split, centered, disabled, file = await Capture("AuthShellCatalog-" + theme) }); }
            }
            root.Children.Clear(); await Layout(); Guard(); var report = new { format = "orchard-auth-shell-native-v1", passed = true, librarySha256 = Hash(typeof(BleeckerAuthShell).Assembly.Location), hostSha256 = Hash(hostAssembly), sourceReferenceSha256 = Hash(Path.Combine(folder, "source-reference.json")), cases, contracts, catalogs, pngs, guards, focusEvents = focus, activations, hostVisible = window.Visible, hardwareInputSent = false, authenticationImplemented = false, externalBrowserOpened = false, clipboardModified = false, physicalInputVerified = false, nativeArm64RuntimeVerified = false, nativeComponentAccepted = false, rasterizationScale = host.XamlRoot.RasterizationScale };
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(0);
        }
        catch (Exception error) { Fail(error); }
    }
    private static DataTemplate ModelTemplate() => (DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><StackPanel x:Name='OriginalModelRoot' Tag='{Binding}' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'><TextBlock x:Name='OriginalModelLabel' Text='{Binding Label}'/><TextBox x:Name='OriginalModelEditor' Text='Original model editor'/></StackPanel></DataTemplate>");
    [Microsoft.UI.Xaml.Data.Bindable]
    private sealed class Model : INotifyPropertyChanged
    {
        private string label = "Original model";
        public string Label { get => label; set { label = value; PropertyChanged?.Invoke(this, new(nameof(Label))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    private sealed partial class Selector(object original, DataTemplate template) : DataTemplateSelector
    {
        internal int Seen { get; private set; }
        internal List<string> Items { get; } = [];
        internal int NullSelections { get; private set; }
        internal int OriginalSelections { get; private set; }
        internal bool Wrong { get; private set; }
        protected override DataTemplate SelectTemplateCore(object item) { Seen++; Items.Add(item is null ? "<null>" : (ReferenceEquals(item, original) ? "original:" : "unexpected:") + item.GetType().Name); if (item is null) NullSelections++; else if (ReferenceEquals(item, original)) OriginalSelections++; else Wrong = true; return template; }
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) => SelectTemplateCore(item);
    }
}
