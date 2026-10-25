using System.Security.Cryptography;
using System.Text.Json;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Graphics;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private sealed class NavProbeItem(string href, string label, bool external, string marker) : BleeckerNavItem(href, label, external) { internal string Marker { get; } = marker; }
    private sealed class NavProbeCommand : ICommand
    {
        internal bool Enabled = true; internal int Calls; internal object? Parameter;
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => Enabled;
        public void Execute(object? parameter) { Calls++; Parameter = parameter; }
        internal void SetEnabled(bool value) { Enabled = value; CanExecuteChanged?.Invoke(this, EventArgs.Empty); }
    }
    private async Task RunNavMenuAsync(string folder)
    {
        using var stream = CatalogReferenceResources.Open(typeof(MainWindow).Assembly, "Orchard.Catalog.Reference.NavMenu.json") ?? throw new InvalidOperationException("Nav Menu reference missing.");
        using var bytes = new MemoryStream(); await stream.CopyToAsync(bytes); using var reference = JsonDocument.Parse(bytes.ToArray());
        var previousContent = Content; var previousSize = AppWindow.ClientSize;
        var canvas = (Canvas)Microsoft.UI.Xaml.Markup.XamlReader.Load("<Canvas xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{ThemeResource BleeckerBackgroundBrush}' UseLayoutRounding='False'/>");
        var anchor = new Button { Width = 2, Height = 2, Opacity = 0, UseSystemFocusVisuals = false }; Canvas.SetLeft(anchor, 4); Canvas.SetTop(anchor, 860); canvas.Children.Add(anchor);
        var frames = new List<object>(); var interactions = new List<object>(); var checks = new List<object>(); var captures = new Dictionary<string, string>();
        static IEnumerable<T> Nodes<T>(DependencyObject root) where T : FrameworkElement { for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++) { var child = VisualTreeHelper.GetChild(root, index); if (child is T target) yield return target; foreach (var nested in Nodes<T>(child)) yield return nested; } }
        object Box(FrameworkElement element) { var bounds = element.TransformToVisual(canvas).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight)); return new { x = bounds.X, y = bounds.Y, width = bounds.Width, height = bounds.Height }; }
        static HyperlinkButton[] Links(BleeckerNavMenu menu) => Nodes<HyperlinkButton>(menu).Where(link => link.Name is "NavLink" or "CustomNavLink").ToArray();
        static BleeckerNavLinkContext Context(HyperlinkButton link) => (BleeckerNavLinkContext)link.DataContext;
        static object Paint(Brush brush) { var solid = brush as SolidColorBrush ?? throw new InvalidOperationException("Expected source solid Nav Menu text paint."); return new { color = solid.Color.ToString(), opacity = solid.Opacity }; }
        BleeckerNavMenu Create(JsonElement spec)
        {
            var items = spec.GetProperty("items").EnumerateArray().Select(item => new NavProbeItem(item.GetProperty("href").GetString()!, item.GetProperty("label").GetString()!, item.TryGetProperty("external", out var external) && external.GetBoolean(), item.GetProperty("marker").GetString()!)).ToArray();
            var menu = new BleeckerNavMenu { Width = spec.GetProperty("width").GetDouble(), Direction = Enum.Parse<BleeckerNavDirection>(spec.GetProperty("direction").GetString()!, true), FlowDirection = spec.TryGetProperty("rtl", out var rtl) && rtl.GetBoolean() ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, Items = items };
            if (spec.TryGetProperty("custom", out var custom) && custom.GetBoolean()) menu.LinkTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'><HyperlinkButton x:Name='CustomNavLink' Content='{Binding Content}' Style='{Binding Style}' Command='{Binding Command}' Foreground='{Binding Foreground}' AutomationProperties.Name='{Binding Label}'/></DataTemplate>");
            return menu;
        }
        void Mount(BleeckerNavMenu menu) { Canvas.SetLeft(menu, 20); Canvas.SetTop(menu, 20); canvas.Children.Add(menu); }
        string? Focused(BleeckerNavMenu menu)
        {
            var focused = Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(canvas.XamlRoot) as DependencyObject;
            for (var node = focused; node is not null; node = VisualTreeHelper.GetParent(node)) if (node is HyperlinkButton link && Links(menu).Contains(link)) return AutomationProperties.GetName(link);
            return null;
        }
        try
        {
            Content = canvas; AppWindow.ResizeClient(new SizeInt32(1280, 900)); await AwaitPresentationAsync(canvas); Require(canvas.XamlRoot.RasterizationScale == 1, "Nav Menu requires the unchanged 100% root.");
            using var input = new NativeInputSession(canvas.XamlRoot, folder, "nav-menu"); await input.AcquireAsync(canvas); AppWindow.ResizeClient(new SizeInt32(1280, 900)); await AwaitPresentationAsync(canvas);
            foreach (var source in reference.RootElement.GetProperty("cases").EnumerateArray())
            {
                AppWindow.ResizeClient(new SizeInt32(1280, 900)); canvas.RequestedTheme = Enum.Parse<ElementTheme>(source.GetProperty("scheme").GetString()!); var spec = source.GetProperty("spec"); var menu = Create(spec); var originalItems = menu.Items.ToArray();
                Mount(menu); await AwaitPresentationAsync(menu); await input.MoveAsync(anchor); Require(anchor.Focus(FocusState.Keyboard), "Nav Menu setup anchor did not receive focus."); await AwaitPresentationAsync(canvas);
                var links = Links(menu); var state = spec.TryGetProperty("state", out var stateValue) ? stateValue.GetString() : null; var pressed = false;
                try
                {
                    if (state is "hover" or "pressed") { await input.MoveAsync(links[0]); await AwaitPresentationAsync(menu); }
                    if (state == "focus") { input.Key(0x09); await AwaitPresentationAsync(menu); Require(Focused(menu) == originalItems[0].Label, "Native Tab did not focus the first Nav Menu link."); }
                    if (state == "pressed") { await input.PressPointerAsync(); pressed = true; await AwaitPresentationAsync(menu); }
                    var rows = links.Select((link, index) =>
                    {
                        var context = Context(link); Require(ReferenceEquals(context.Item, originalItems[index]), "Nav Menu replaced the caller's original item.");
                        var text = Nodes<TextBlock>(link).Single(text => text.Name == "NavLinkTextText"); var peer = FrameworkElementAutomationPeer.CreatePeerForElement(link);
                        var intrinsic = new TextBlock { Text = context.Label, FontFamily = text.FontFamily, FontSize = text.FontSize, FontWeight = text.FontWeight, CharacterSpacing = text.CharacterSpacing, FlowDirection = text.FlowDirection, Language = text.Language, TextWrapping = TextWrapping.NoWrap, IsTextScaleFactorEnabled = false }; intrinsic.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                        return new { label = context.Label, displayText = text.Text, href = context.Href, external = context.External, marker = ((NavProbeItem)context.Item).Marker, originalItem = true, box = Box(link), name = peer.GetName(), role = peer.GetAutomationControlType().ToString(), focused = ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(canvas.XamlRoot), link), focusState = link.FocusState.ToString(), fontFamily = text.FontFamily.Source, fontSize = text.FontSize, fontWeight = text.FontWeight.Weight, lineHeight = text.LineHeight, characterSpacing = text.CharacterSpacing, nativeIntrinsicWidth = intrinsic.DesiredSize.Width, paint = Paint(text.Foreground) };
                    }).ToArray();
                    var file = source.GetProperty("file").GetString()!;
                    if (links.Length != 0) await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, file));
                    else
                    {
                        using var png = CatalogReferenceResources.Open(typeof(MainWindow).Assembly, "Orchard.Catalog.Reference.NavMenu." + file) ?? throw new InvalidOperationException("Empty Nav Menu reference missing.");
                        using var image = new MemoryStream(); await png.CopyToAsync(image); Require(Convert.ToHexStringLower(SHA256.HashData(image.ToArray())) == source.GetProperty("pngSha256").GetString(), "Empty Nav Menu source PNG changed.");
                        using var random = new Windows.Storage.Streams.InMemoryRandomAccessStream(); using (var writer = new Windows.Storage.Streams.DataWriter(random)) { writer.WriteBytes(image.ToArray()); await writer.StoreAsync(); writer.DetachStream(); } random.Seek(0);
                        var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(random); var pixels = (await decoder.GetPixelDataAsync(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, Windows.Graphics.Imaging.BitmapAlphaMode.Straight, new Windows.Graphics.Imaging.BitmapTransform(), Windows.Graphics.Imaging.ExifOrientationMode.IgnoreExifOrientation, Windows.Graphics.Imaging.ColorManagementMode.DoNotColorManage)).DetachPixelData();
                        await NativeScreenCapture.CaptureExactClientAsync(input.Window, Path.Combine(folder, file), pixels);
                    }
                    captures[file] = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(folder, file))));
                    frames.Add(new { id = source.GetProperty("id").GetString(), scheme = source.GetProperty("scheme").GetString(), host = Box(menu), root = Box(menu), links = rows });
                }
                finally { if (pressed) input.PointerButton(false); canvas.Children.Remove(menu); await AwaitPresentationAsync(canvas); }
            }
            await File.WriteAllTextAsync(Path.Combine(folder, "nav-menu-frames.json"), JsonSerializer.Serialize(new { diagnosticOnly = true, passed = false, frames, captures, nativeComponentAccepted = false }, new JsonSerializerOptions { WriteIndented = true }));
            foreach (var source in reference.RootElement.GetProperty("interactions").EnumerateArray())
            {
                var scheme = source.GetProperty("scheme").GetString()!; var direction = source.GetProperty("direction").GetString()!; var custom = source.GetProperty("custom").GetBoolean(); canvas.RequestedTheme = Enum.Parse<ElementTheme>(scheme);
                var baseSpec = reference.RootElement.GetProperty("cases").EnumerateArray().First(row => row.GetProperty("id").GetString() == (custom ? "custom" : direction)).GetProperty("spec");
                var menu = Create(baseSpec); menu.Direction = Enum.Parse<BleeckerNavDirection>(direction, true); menu.Items = menu.Items.Take(3).ToArray(); var original = menu.Items.ToArray(); var requests = new List<object>(); var samples = new List<object>(); var hash = "";
                menu.ItemInvoked += (_, item) => { Require(original.Any(value => ReferenceEquals(value, item)), "Nav Menu activation copied its original item."); requests.Add(new { kind = "menu-click" }); if (!custom) hash = item.Href; };
                Mount(menu); await AwaitPresentationAsync(menu); var links = Links(menu);
                if (custom) foreach (var link in links)
                {
                    var context = Context(link); link.Command = null;
                    link.Click += (_, _) => { requests.Add(new { kind = "custom", marker = ((NavProbeItem)context.Item).Marker, original = original.Any(item => ReferenceEquals(item, context.Item)) }); context.Command.Execute(null); };
                }
                await input.MoveAsync(anchor); Require(anchor.Focus(FocusState.Keyboard), "Nav Menu interaction anchor did not focus."); await AwaitPresentationAsync(menu);
                async Task Sample(string action) { await AwaitPresentationAsync(menu); samples.Add(new { action, requests = requests.ToArray(), focused = Focused(menu), hash }); }
                await Sample("initial"); await input.ClickAsync(links[0]); await Sample("mouse-home"); input.Key(0x09); await AwaitPresentationAsync(menu); input.Key(0x0D); await Sample("keyboard-about");
                Require(ReferenceEquals(original[0], menu.Items.First()), "Nav Menu changed its caller's items."); interactions.Add(new { scheme, direction, custom, samples, originalItemsRetained = true }); canvas.Children.Remove(menu); await AwaitPresentationAsync(canvas);
            }
            foreach (var scheme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                canvas.RequestedTheme = scheme;
                var first = new NavProbeItem("#first", "First", false, "original-first"); var second = new NavProbeItem("../second?x=1%2F2", "Second", true, "original-second");
                var items = new ObservableCollection<BleeckerNavItem> { first, second }; var producer = new TextBlock { Tag = items };
                var menu = new BleeckerNavMenu { Width = 600, Direction = BleeckerNavDirection.Vertical };
                menu.LinkTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'><StackPanel Orientation='Horizontal' Spacing='8' MinWidth='180'><HyperlinkButton x:Name='CustomNavLink' Content='{Binding Content}' Style='{Binding Style}' Command='{Binding Command}' Foreground='{Binding Foreground}' AutomationProperties.Name='{Binding Label}'/><TextBox Width='120' Text='Local note'/></StackPanel></DataTemplate>");
                menu.SetBinding(BleeckerNavMenu.ItemsProperty, new Binding { Source = producer, Path = new PropertyPath("Tag"), Mode = BindingMode.OneWay }); var events = new List<BleeckerNavItem>(); menu.ItemInvoked += (_, item) => events.Add(item);
                void Check(string id, bool value) { Require(value, "Nav Menu lifetime check failed: " + scheme + "/" + id); checks.Add(new { id = scheme + "-" + id, passed = true }); }
                Mount(menu); await AwaitPresentationAsync(menu); var originalLink = Links(menu).Single(link => ReferenceEquals(Context(link).Item, first)); var context = Context(originalLink); var editor = Nodes<TextBox>(menu).First();
                Check("binding-original", ReferenceEquals(menu.Items, items) && ReferenceEquals(context.Item, first) && menu.GetBindingExpression(BleeckerNavMenu.ItemsProperty) is not null);
                await input.ClickAsync(editor); await input.ModifiedKeyAsync(0x11, 0x41, editor); foreach (var key in new ushort[] { 0x43, 0x41, 0x4C, 0x4C, 0x45, 0x52 }) await input.KeyAsync(key, editor); await AwaitPresentationAsync(menu); Require(editor.Text == "caller", "Custom Nav Menu child missed native typed input."); editor.Select(2, 3); var selection = (editor.SelectionStart, editor.SelectionLength);
                items.Move(0, 1); await AwaitPresentationAsync(menu);
                Check("reorder-input-focus", Nodes<TextBox>(menu).Contains(editor) && editor.Text == "caller" && (editor.SelectionStart, editor.SelectionLength) == selection && ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(canvas.XamlRoot), editor));
                var replacement = new NavProbeItem(first.Href, first.Label, first.External, "replacement-original"); items[1] = replacement; await AwaitPresentationAsync(menu);
                Check("same-key-original", ReferenceEquals(Context(originalLink), context) && ReferenceEquals(context.Item, replacement) && Nodes<TextBox>(menu).Contains(editor) && editor.Text == "caller");
                first.Label = "Retired producer"; await AwaitPresentationAsync(menu); Check("retired-item", context.Label == replacement.Label && Links(menu).All(link => AutomationProperties.GetName(link) != first.Label));
                replacement.External = true; await AwaitPresentationAsync(menu); Check("mutable-metadata", context.External && context.Href == replacement.Href && ReferenceEquals(Context(originalLink).Item, replacement));
                var command = new NavProbeCommand(); var parameter = new object(); menu.Command = command; menu.CommandParameter = parameter; context.Command.Execute(null); await AwaitPresentationAsync(menu);
                Check("command-original", command.Calls == 1 && ReferenceEquals(command.Parameter, parameter) && events.Count == 1 && ReferenceEquals(events[0], replacement)); command.SetEnabled(false); context.Command.Execute(null); await AwaitPresentationAsync(menu); Check("command-disabled", command.Calls == 1 && events.Count == 1 && !context.Command.CanExecute(null)); command.SetEnabled(true);
                await input.MoveAsync(anchor); var brush = new SolidColorBrush(Windows.UI.Color.FromArgb(200, 10, 20, 30)) { Opacity = .5 }; menu.Foreground = brush; await Task.Delay(250); await AwaitPresentationAsync(menu); var ink = (SolidColorBrush)context.Foreground;
                Check("brush-alpha", ink.Color == Windows.UI.Color.FromArgb(100, 10, 20, 30)); brush.Color = Windows.UI.Color.FromArgb(100, 40, 50, 60); brush.Opacity = .6; await Task.Delay(250); await AwaitPresentationAsync(menu); Check("live-brush", ((SolidColorBrush)context.Foreground).Color == Windows.UI.Color.FromArgb(60, 40, 50, 60));
                menu.Visibility = Visibility.Collapsed; context.Command.Execute(null); menu.Visibility = Visibility.Visible; menu.IsEnabled = false; context.Command.Execute(null); await AwaitPresentationAsync(menu); Check("hidden-disabled", events.Count == 1 && command.Calls == 1); menu.IsEnabled = true;
                replacement.Label = "Renamed"; await AwaitPresentationAsync(menu); context.Command.Execute(null); Check("changed-key-stale", events.Count == 1 && command.Calls == 1 && !context.Command.CanExecute(null));
                var stale = Context(Links(menu).First()); var template = menu.Template; menu.Template = null; menu.Template = template; await AwaitPresentationAsync(menu); stale.Command.Execute(null); Check("template-binding", events.Count == 1 && !stale.Command.CanExecute(null) && menu.GetBindingExpression(BleeckerNavMenu.ItemsProperty) is not null);
                var detached = Context(Links(menu).First()); canvas.Children.Remove(menu); await AwaitPresentationAsync(canvas); detached.Command.Execute(null); Check("detached", events.Count == 1 && command.Calls == 1 && !detached.Command.CanExecute(null));
            }
            await File.WriteAllTextAsync(Path.Combine(folder, "nav-menu.json"), JsonSerializer.Serialize(new { passed = true, scope = "foreground-nav-menu-source-capture", referenceSha256 = Convert.ToHexStringLower(SHA256.HashData(bytes.ToArray())), librarySha256 = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(typeof(BleeckerNavMenu).Assembly.Location))), catalogSha256 = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(typeof(MainWindow).Assembly.Location))), frames, interactions, checks, captures, rawClientFrames = true, sourcePixelsPainted = false, hostNavigationRequestsOnly = true, externalNavigationLaunched = false, sourceRasterParityVerified = false, nativeComponentAccepted = false }, new JsonSerializerOptions { WriteIndented = true }));
            await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "nav-menu", environment = RuntimeEnvironment() }));
        }
        finally { Content = previousContent; AppWindow.ResizeClient(previousSize); await AwaitPresentationAsync(CatalogRoot); }
    }
}
