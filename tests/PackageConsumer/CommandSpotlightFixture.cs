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

internal static partial class CommandSpotlightFixture
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
        var root = (Grid)XamlReader.Load("<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{ThemeResource BleeckerBackgroundBrush}'/>");
        var host = new BleeckerModalHost { Content = root, Width = 1000, Height = 960 }; window.Content = host; window.AppWindow.ResizeClient(new(1000, 960));
        var stage = "initial"; var guards = 0; var focus = 0; var activations = 0; var width = 1000d; var height = 960d; var cases = new List<object>(); var contracts = new List<object>(); var styleContracts = new List<object>(); var hotkeyContracts = new List<object>(); var windowsKeyContracts = new List<object>(); var caseSearchContracts = new List<object>(); var readOnlyKeyboardContracts = new List<object>(); var pngs = new List<object>(); var catalogs = new List<object>();
        host.GotFocus += (_, _) => focus++; window.Activated += (_, args) => { if (args.WindowActivationState != WindowActivationState.Deactivated) activations++; };
        void Require(bool condition, string label) { if (!condition) throw new InvalidOperationException(label); }
        void Guard()
        {
            guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var visible = false;
            EnumWindows((h, _) => { GetWindowThreadProcessId(h, out var process); visible |= process == Environment.ProcessId && IsWindowVisible(h); return true; }, 0);
            Require(!visible && !window.Visible && !window.AppWindow.IsVisible && foreground != Environment.ProcessId && focus == 0 && activations == 0, "Spotlight left its hidden background boundary");
            Require(host.XamlRoot is null || VisualTreeHelper.GetOpenPopupsForXamlRoot(host.XamlRoot).Count == 0, "Spotlight opened a native Popup");
        }
        void Fail(Exception error) { File.WriteAllText(output, JsonSerializer.Serialize(new { format = "orchard-command-spotlight-native-v3", passed = false, stage, error = error.ToString(), cases, contracts, styleContracts, hotkeyContracts, windowsKeyContracts, caseSearchContracts, readOnlyKeyboardContracts, pngs, catalogs, guards, focus, activations }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(1); }
        Application.Current.UnhandledException += (_, args) => Fail(args.Exception);
        async Task Layout(int delay = 40)
        {
            File.WriteAllText(Path.Combine(folder, "command-spotlight-stage.json"), JsonSerializer.Serialize(new { stage, cases = cases.Count, contracts = contracts.Count, guards, focus, activations }));
            for (var i = 0; i < 3; i++) { var idle = new TaskCompletionSource(); Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => idle.SetResult()), "Dispatcher stopped"); await idle.Task; host.Measure(new(width, height)); host.Arrange(new(0, 0, width, height)); host.UpdateLayout(); Guard(); }
            if (delay > 0) await Task.Delay(delay); host.UpdateLayout(); Guard();
        }
        void Resize(double w = 1000, double h = 960) { width = w; height = h; host.Width = w; host.Height = h; root.Clip = new RectangleGeometry { Rect = new(0, 0, w, h) }; window.AppWindow.ResizeClient(new((int)w, (int)h)); }
        FrameworkElement Find(string name) => Elements(host).Single(e => e.Name == name);
        object Box(FrameworkElement e) { var r = e.TransformToVisual(host).TransformBounds(new(0, 0, e.ActualWidth, e.ActualHeight)); return new { x = r.X, y = r.Y, width = r.Width, height = r.Height }; }
        object? Paint(Brush? brush)
        {
            if (brush is SolidColorBrush b) return new { a = b.Color.A, r = b.Color.R, g = b.Color.G, b = b.Color.B, opacity = b.Opacity };
            if (brush is ImageBrush { ImageSource: WriteableBitmap bitmap } image && bitmap.PixelWidth == 1 && bitmap.PixelHeight == 1)
            {
                var bgra = new byte[4]; using var stream = bitmap.PixelBuffer.AsStream(); stream.ReadExactly(bgra);
                return new { kind = "premultiplied-bgra8", bgra = bgra.Select(v => (int)v).ToArray(), opacity = image.Opacity };
            }
            return null;
        }
        void Invoke(Button button) => ((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(button).GetPattern(PatternInterface.Invoke)).Invoke();
        void Edit(string text) => ((TextBox)Find("SpotlightSearch")).Text = text;
        object Snapshot(BleeckerCommandSpotlight control)
        {
            var trigger = Elements(control).OfType<Button>().Single(b => b.Name == "SpotlightTrigger"); var editor = Elements(host).OfType<TextBox>().SingleOrDefault(b => b.Name == "SpotlightSearch");
            var rows = Elements(host).OfType<Button>().Where(b => b.Name == "SpotlightAction").Select(row => { var action = (BleeckerCommandSpotlightAction)row.Tag; return new { action.Id, action.Title, action.Description, action.Group, action.Href, action.External, action.Disabled, keywords = action.Keywords?.ToArray() ?? [], shortcut = action.Shortcut?.ToArray() ?? [], original = control.Actions.Any(a => ReferenceEquals(a, action)), selected = action.Id == control.SelectedActionId, row.IsEnabled, row.Opacity, box = Box(row), background = Paint(row.Background), foreground = Paint(row.Foreground), title = Box(Elements(row).OfType<TextBlock>().Single(t => t.Name == "SpotlightActionTitle")) }; }).ToArray();
            return new { control.IsOpen, control.Query, control.SelectedActionId, trigger = new { visible = trigger.Visibility == Visibility.Visible, trigger.IsEnabled, box = Box(trigger), hintVisible = trigger.Visibility == Visibility.Visible && Elements(trigger).Any(e => e.Name == "SpotlightTriggerHint" && e.Visibility == Visibility.Visible), labelVisible = trigger.Visibility == Visibility.Visible && Elements(trigger).Any(e => e.Name == "SpotlightTriggerLabel" && e.Visibility == Visibility.Visible) }, editor = editor is null ? null : new { editor.Text, editor.PlaceholderText, editor.FontSize, editor.IsEnabled, box = Box(editor) }, surface = editor is null ? null : new { box = Box(Find("SpotlightSurface")), modal = AutomationProperties.GetIsDialog(Find("SpotlightSurface")), background = Paint(((Control)Find("SpotlightPaint")).Background) }, groupLabels = Elements(host).OfType<TextBlock>().Where(t => t.Name == "SpotlightGroupLabel").Select(t => AutomationProperties.GetName(t)).ToArray(), rows };
        }
        async Task<string> Capture(string name)
        {
            byte[]? previous = null, bytes = null; var attempts = 0;
            for (; attempts < 6; attempts++) { var image = new RenderTargetBitmap(); await image.RenderAsync(host, (int)width, (int)height); bytes = (await image.GetPixelsAsync()).ToArray(); if (previous is not null && previous.AsSpan().SequenceEqual(bytes)) break; previous = bytes; await Layout(45); }
            Require(attempts < 6, "Spotlight frame did not settle: " + name); var file = name + ".png"; using var stream = new InMemoryRandomAccessStream(); var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream); encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, (uint)width, (uint)height, 96, 96, bytes!); await encoder.FlushAsync(); stream.Seek(0); using (var target = File.Create(Path.Combine(folder, file))) await stream.AsStreamForRead().CopyToAsync(target); pngs.Add(new { file, sha256 = Hash(Path.Combine(folder, file)), stableFrames = 2, attempts = attempts + 1 }); return file;
        }
        string? Optional(JsonElement item, string name) => item.TryGetProperty(name, out var p) ? p.GetString() : null;
        UIElement Icon(string? kind) => (UIElement)XamlReader.Load("<Viewbox xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Width='16' Height='16'><Canvas Width='24' Height='24'><" + (kind == "circle" ? "Ellipse" : "Rectangle") + " Canvas.Left='5' Canvas.Top='5' Width='14' Height='14' Stroke='{ThemeResource BleeckerTextSecondaryBrush}' StrokeThickness='2'/></Canvas></Viewbox>");
        BleeckerCommandSpotlightAction Read(JsonElement item) => new(item.GetProperty("id").GetString()!, item.GetProperty("title").GetString()!) { Description = Optional(item, "description"), Group = Optional(item, "group"), Href = Optional(item, "href"), External = item.TryGetProperty("external", out var external) && external.GetBoolean(), Disabled = item.TryGetProperty("disabled", out var disabled) && disabled.GetBoolean(), Keywords = item.TryGetProperty("keywords", out var keywords) ? keywords.EnumerateArray().Select(k => k.GetString()!).ToArray() : [], Shortcut = item.TryGetProperty("shortcut", out var shortcuts) ? shortcuts.EnumerateArray().Select(k => k.GetString()!).ToArray() : [], Icon = item.TryGetProperty("icon", out var icon) ? Icon(icon.GetString()) : null, OnSelectAsync = item.TryGetProperty("callback", out _) ? () => Task.CompletedTask : null };
        BleeckerCommandSpotlightAction[] Defaults() => source.GetProperty("defaultActions").EnumerateArray().Select(Read).ToArray();
        async Task<BleeckerCommandSpotlight> New(bool? open = null)
        {
            root.Children.Clear(); await Layout(180); Resize(); var control = new BleeckerCommandSpotlight { Actions = Defaults(), Open = open, Margin = new(32), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top }; root.Children.Add(control); await Layout(350); return control;
        }
        try
        {
            await Layout(150);
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                host.RequestedTheme = theme;
                stage = theme + "/hotkeys";
                await RunHotkeyContracts(host, theme, folder, New, Layout, Resize, Capture, hotkeyContracts, windowsKeyContracts, caseSearchContracts, readOnlyKeyboardContracts, Require);
                stage = theme + "/live-styles";
                await RunStyleContracts(root, host, theme, New, Layout, Capture, styleContracts, Require);
                foreach (var c in source.GetProperty("cases").EnumerateArray().Where(c => c.GetProperty("scheme").GetString() == theme.ToString()))
                {
                    var id = c.GetProperty("id").GetString()!; stage = theme + "/" + id; root.Children.Clear(); await Layout(180); var spec = c.GetProperty("spec");
                    Resize(spec.TryGetProperty("viewportWidth", out var vw) ? vw.GetDouble() : 1000, spec.TryGetProperty("viewportHeight", out var vh) ? vh.GetDouble() : 960);
                    var actions = (spec.TryGetProperty("actions", out var a) ? a : source.GetProperty("defaultActions")).EnumerateArray().Select(Read).ToArray();
                    var control = new BleeckerCommandSpotlight { Actions = actions, Open = spec.TryGetProperty("open", out var o) ? o.GetBoolean() : null, FontSize = spec.TryGetProperty("rootFontSize", out var fs) ? fs.GetDouble() : 16, FlowDirection = spec.TryGetProperty("rtl", out var rtl) && rtl.GetBoolean() ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, ShowTrigger = !spec.TryGetProperty("showTrigger", out var show) || show.GetBoolean(), TriggerLabel = Optional(spec, "triggerLabel") ?? "Search", Hotkey = Optional(spec, "hotkey") ?? "k", Placeholder = Optional(spec, "placeholder") ?? "Search commands...", EmptyMessage = Optional(spec, "emptyMessage") ?? "No matching commands.", Margin = new(32), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
                    if (id == "open-custom-dialog") { var resources = new ResourceDictionary { Source = new Uri("ms-appx:///Orchard/Themes/CommandSpotlight.xaml") }; var style = new Style { TargetType = typeof(BleeckerOverlaySurface), BasedOn = (Style)resources["BleeckerCommandSpotlightSurfaceStyle"] }; style.Setters.Add(new Setter(FrameworkElement.MaxWidthProperty, 576d)); style.Setters.Add(new Setter(Control.CornerRadiusProperty, new CornerRadius(0))); control.SurfaceStyle = style; }
                    if (id == "open-custom-overlay") control.OverlayTopOffset = 0;
                    root.Children.Add(control); await Layout(350); if (c.GetProperty("query").ValueKind != JsonValueKind.Null) { Edit(c.GetProperty("query").GetString()!); await Layout(250); }
                    var interaction = c.GetProperty("interaction").GetString(); if (interaction == "row-hover") { var hoveredOriginal = (BleeckerCommandSpotlightAction)Elements(host).OfType<Button>().Where(b => b.Name == "SpotlightAction").ElementAt(1).Tag; control.SelectAction(hoveredOriginal); await Layout(180); }
                    Require(control.IsOpen == (spec.TryGetProperty("open", out var expected) && expected.GetBoolean()), "Source open contract"); Require(control.Query == (c.GetProperty("query").GetString() ?? ""), "Source query crosses native editor");
                    if (width < 640) Require(Elements(control).Single(e => e.Name == "SpotlightTriggerHint").Visibility == Visibility.Collapsed, "Whole narrow shortcut hint must be hidden");
                    var snapshot = Snapshot(control); var file = await Capture("command-spotlight-" + cases.Count + "-" + theme); cases.Add(new { id, theme = theme.ToString(), viewport = new { width, height }, snapshot, file, softwareSelection = interaction == "row-hover", pointerHoverVerified = false, focusVerified = false });
                }
                // Further interaction checks execute through the real public control/native peers.
                stage = theme + "/group-language";
                var localized = await New(true);
                var groupedAction = new BleeckerCommandSpotlightAction("localized", "Fictional local action") { Group = "istanbul" };
                localized.Actions = [groupedAction];
                foreach (var (language, expected) in new[] { ("tr-TR", "İSTANBUL"), ("en-US", "ISTANBUL") })
                {
                    localized.Language = language;
                    await Layout();
                    var groupLabel = Elements(host).OfType<TextBlock>().Single(text => text.Name == "SpotlightGroupLabel");
                    Require(groupLabel.Text == expected && AutomationProperties.GetName(groupLabel) == "istanbul" && groupedAction.Group == "istanbul", "Live group language preserves caller text and automation name");
                    contracts.Add(new { theme = theme.ToString(), id = "group-language-" + language, displayed = groupLabel.Text, automationName = AutomationProperties.GetName(groupLabel), original = groupedAction.Group, passed = true });
                }
                stage = theme + "/contracts"; var current = await New(false); var requests = new List<bool>(); current.OpenRequested += (_, value) => requests.Add(value); Invoke((Button)Elements(current).Single(e => e.Name == "SpotlightTrigger")); await Layout(); Require(!current.IsOpen && requests.SequenceEqual([true]), "Controlled open rejection"); contracts.Add(new { theme = theme.ToString(), id = "controlled-open-rejected", passed = true });
                current = await New(true); Edit("beta"); await Layout(); Require(current.Query == "beta", "Native value provider query"); current.Close(); await Layout(); Require(current.IsOpen && current.Query == "" && ((TextBox)Find("SpotlightSearch")).Text == "", "Rejected close clears actual editor"); contracts.Add(new { theme = theme.ToString(), id = "controlled-close-query-cleared", passed = true });
                current = await New(); var events = new List<string>(); var pending = new TaskCompletionSource(); var original = current.Actions.First(); original.OnSelectAsync = () => { events.Add("own"); return pending.Task; }; current.ActionSelected += (_, value) => { Require(ReferenceEquals(value, original), "Exact original callback object"); events.Add("outer"); }; current.OpenRequested += (_, value) => events.Add(value ? "open" : "close"); current.Show(); await Layout(350); current.InvokeAction(original); Require(!current.IsOpen && events.SequenceEqual(["open", "outer", "own", "close"]), "Original callback order and immediate close"); pending.SetResult(); await Layout(220); contracts.Add(new { theme = theme.ToString(), id = "original-dispatch-immediate-close", passed = true });
                current = await New(); original = current.Actions.First(); var failures = 0; original.OnSelectAsync = () => throw new InvalidOperationException("Original action failure"); current.ActionFailed += (_, failure) => { Require(ReferenceEquals(failure.Action, original), "Original failed object"); failures++; }; current.Show(); await Layout(350); current.InvokeAction(original); await Layout(220); Require(!current.IsOpen && failures == 1 && current.LastActionError?.Message == "Original action failure", "Own failure exposed and closes"); contracts.Add(new { theme = theme.ToString(), id = "own-failure-closes", passed = true });
                current = await New(); original = current.Actions.First(); original.OnSelectAsync = null; original.Href = "/fictional-route?original=Raw%20value#view"; string? href = null; current.NavigationRequested += (_, request) => { href = request.Href; request.Handled = true; }; current.Show(); await Layout(350); current.InvokeAction(original); await Layout(220); Require(href == original.Href && !current.IsOpen, "Original internal href routes only through caller"); contracts.Add(new { theme = theme.ToString(), id = "original-navigation-boundary", passed = true, originalHref = href, effectiveOpen = current.IsOpen });
                stage = theme + "/outer-adds-own"; current = await New(); original = current.Actions.First(); original.OnSelectAsync = null; events = []; var navigationCount = 0;
                current.ActionSelected += (_, value) => { Require(ReferenceEquals(value, original), "Outer receives original before adding callback"); events.Add("outer"); value.OnSelectAsync = () => { events.Add("new-own"); return Task.CompletedTask; }; value.Href = "/must-not-navigate"; };
                current.NavigationRequested += (_, request) => { navigationCount++; request.Handled = true; }; current.OpenRequested += (_, value) => { if (!value) events.Add("close"); };
                current.Show(); await Layout(350); current.InvokeAction(original); await Layout(220); Require(events.SequenceEqual(["outer", "new-own", "close"]) && navigationCount == 0 && !current.IsOpen, "Outer-added own callback must precede current href"); contracts.Add(new { theme = theme.ToString(), id = "outer-adds-current-own", passed = true, events = events.ToArray(), navigationCount, effectiveOpen = current.IsOpen });
                stage = theme + "/outer-removes-own"; current = await New(); original = current.Actions.First(); var oldOwnCount = 0; href = null; original.OnSelectAsync = () => { oldOwnCount++; return Task.CompletedTask; };
                current.ActionSelected += (_, value) => { value.OnSelectAsync = null; value.Href = "/updated-original?label=Raw%20value#view"; }; current.NavigationRequested += (_, request) => { Require(ReferenceEquals(request.Action, original), "Current href retains original object"); href = request.Href; request.Handled = true; };
                current.Show(); await Layout(350); current.InvokeAction(original); await Layout(220); Require(oldOwnCount == 0 && href == "/updated-original?label=Raw%20value#view" && !current.IsOpen, "Outer removal must route the current href without stale callback"); contracts.Add(new { theme = theme.ToString(), id = "outer-removes-own-routes-current-href", passed = true, oldOwnCount, href, effectiveOpen = current.IsOpen });
                stage = theme + "/outer-failure"; current = await New(); original = current.Actions.First(); var ownCount = 0; var outerThrew = false; original.OnSelectAsync = () => { ownCount++; return Task.CompletedTask; }; current.ActionSelected += (_, _) => throw new InvalidOperationException("Original outer failure"); current.Show(); await Layout(350);
                try { current.InvokeAction(original); } catch (InvalidOperationException error) when (error.Message == "Original outer failure") { outerThrew = true; }
                Require(outerThrew && ownCount == 0 && current.IsOpen && current.LastActionError is null, "Outer error must propagate before own callback and close"); contracts.Add(new { theme = theme.ToString(), id = "outer-failure-stops-own-and-close", passed = true, outerThrew, ownCount, effectiveOpen = current.IsOpen });
                stage = theme + "/async-failure"; current = await New(); original = current.Actions.First(); var asyncFailures = 0; original.OnSelectAsync = () => Task.FromException(new InvalidOperationException("Original async failure")); current.ActionFailed += (_, failure) => { Require(ReferenceEquals(failure.Action, original), "Asynchronous original identity"); asyncFailures++; }; current.Show(); await Layout(350); current.InvokeAction(original); await Layout(220);
                Require(asyncFailures == 1 && !current.IsOpen && current.LastActionError?.Message == "Original async failure", "Asynchronous error is observed without delaying close"); contracts.Add(new { theme = theme.ToString(), id = "async-own-failure-closes", passed = true, asyncFailures, error = current.LastActionError?.Message, effectiveOpen = current.IsOpen });
                stage = theme + "/live-memo"; current = await New(true); original = current.Actions.First(); original.Title = "Mutated original alpha"; original.Group = "Mutated group"; original.Disabled = true; await Layout();
                var beforeGroups = Elements(host).OfType<TextBlock>().Where(t => t.Name == "SpotlightGroupLabel").Select(t => AutomationProperties.GetName(t)).ToArray(); var retainedOriginal = ReferenceEquals(current.SelectedAction, original); Require(retainedOriginal && beforeGroups[0] == "Navigation", "Same-array mutation keeps source memoized IDs/group");
                Edit("Mutated"); await Layout(); var afterGroups = Elements(host).OfType<TextBlock>().Where(t => t.Name == "SpotlightGroupLabel").Select(t => AutomationProperties.GetName(t)).ToArray(); Require(current.SelectedAction is null && current.Query == "Mutated" && afterGroups.SequenceEqual(["Mutated group"]), "Query recomputes mutated original group and disabled selection"); contracts.Add(new { theme = theme.ToString(), id = "live-original-memo-query-boundary", passed = true, retainedOriginal, beforeGroups, afterGroups, selectedId = current.SelectedActionId, query = current.Query });
                if (catalogFactory is not null) { stage = theme + "/catalog"; root.Children.Clear(); await Layout(180); Resize(); var fixture = catalogFactory(); root.Children.Add(fixture); await Layout(350); var controls = Elements(fixture).OfType<BleeckerCommandSpotlight>().ToArray(); Require(controls.Length >= 5 && controls.Any(c => !c.IsEnabled) && controls.Any(c => c.FontSize == 32), "Real catalog variants loaded"); await VerifyCatalogStyleCycle(fixture, host, Layout, Require); await VerifyCatalogHotkeyCycle(fixture, Layout, Require); await CommandSpotlightCollectionFixture.VerifyCatalogCycle(fixture, host, Layout, Require); catalogs.Add(new { collectionCycleVerified = true, hotkeyCycleVerified = true, styleCycleVerified = true, theme = theme.ToString(), controls = controls.Length, passed = true, file = await Capture("command-spotlight-catalog-" + theme) }); }
            }
            root.Children.Clear(); await Layout(220); Guard();
            var report = new { format = "orchard-command-spotlight-native-v3", passed = true, librarySha256 = Hash(typeof(BleeckerCommandSpotlight).Assembly.Location), hostSha256 = Hash(hostAssembly), sourceReferenceSha256 = Hash(Path.Combine(folder, "source-reference.json")), cases, contracts, styleContracts, hotkeyContracts, windowsKeyContracts, caseSearchContracts, readOnlyKeyboardContracts, catalogs, pngs, guards, focusEvents = focus, activations, hostVisible = window.Visible, hardwareInputSent = false, externalBrowserOpened = false, clipboardModified = false, physicalInputVerified = false, nativeArm64RuntimeVerified = false, nativeComponentAccepted = false, rasterizationScale = host.XamlRoot.RasterizationScale };
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(0);
        }
        catch (Exception error) { Fail(error); }
    }
}
