using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows.Input;
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

internal static class NavigationMenuFixture
{
    private delegate bool Visitor(nint handle, nint value);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor, nint value);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle, out uint process);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint handle);
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node)
    { if (node is FrameworkElement e) yield return e; for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) foreach (var child in Elements(VisualTreeHelper.GetChild(node, i))) yield return child; }
    private sealed class Routing(Action request) : ICommand
    {
        internal bool Allowed = true;
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => Allowed;
        public void Execute(object? parameter) => request();
        internal void Notify() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
    internal static async Task RunAsync(Window window, string output, string hostAssembly, Func<FrameworkElement>? catalogFactory = null)
    {
        var root = (Grid)XamlReader.Load("<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Width='1000' Height='760' Background='{ThemeResource BleeckerBackgroundBrush}'/>");
        root.Clip = new RectangleGeometry { Rect = new Rect(0, 0, 1000, 760) }; window.Content = root; window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(1000, 760));
        var scene = new Grid(); var host = new BleeckerModalHost { Content = scene, Clip = new RectangleGeometry { Rect = new Rect(0, 0, 1000, 760) } }; root.Children.Add(host);
        var guards = 0; var focus = 0; var activations = 0; var stage = "initial";
        var states = new List<object>(); var contracts = new List<object>(); var presence = new List<object>(); var standalone = new List<object>(); var catalog = new List<object>(); var pngs = new List<object>();
        Application.Current.UnhandledException += (_, args) => { File.WriteAllText(output, JsonSerializer.Serialize(new { format = "orchard-navigation-menu-native-v1", passed = false, stage, error = args.Exception.ToString(), states, contracts, presence, standalone, catalog, pngs, guards, focus, activations }, new JsonSerializerOptions { WriteIndented = true })); Environment.Exit(1); };
        root.GotFocus += (_, _) => focus++; window.Activated += (_, a) => { if (a.WindowActivationState != WindowActivationState.Deactivated) activations++; };
        void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        void Guard()
        {
            guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var visible = false;
            EnumWindows((handle, _) => { GetWindowThreadProcessId(handle, out var pid); visible |= pid == Environment.ProcessId && IsWindowVisible(handle); return true; }, 0);
            Require(!visible && !window.Visible && !window.AppWindow.IsVisible && foreground != Environment.ProcessId && focus == 0 && activations == 0, "Navigation Menu probe left its hidden boundary.");
        }
        async Task Layout(int delay = 45)
        {
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(output)!, "navigation-menu-stage.json"), JsonSerializer.Serialize(new { stage, states = states.Count, contracts = contracts.Count, guards, focus, activations }));
            for (var i = 0; i < 5; i++) { var idle = new TaskCompletionSource(); Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => idle.SetResult()), "Dispatcher refused navigation layout."); await idle.Task; root.Measure(new Size(1000, 760)); root.Arrange(new Rect(0, 0, 1000, 760)); root.UpdateLayout(); Guard(); }
            await Task.Delay(delay); root.UpdateLayout(); Guard();
        }
        IEnumerable<FrameworkElement> All() => Elements(root).Concat(VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Where(p => p.Child is not null).SelectMany(p => Elements(p.Child))).Distinct();
        BleeckerOverlaySurface[] Surfaces() => All().OfType<BleeckerOverlaySurface>().Where(s => s.IsLoaded && s.Name == "NavigationMenuSurface").ToArray();
        Button Trigger(BleeckerNavigationMenu menu, string label = "Components") => Elements(menu).OfType<Button>().Single(b => b.DataContext is BleeckerNavigationMenuGroup g && g.Trigger == label);
        HyperlinkButton Link(string label) => All().OfType<HyperlinkButton>().Single(b => b.IsLoaded && b.DataContext is BleeckerNavigationLinkContext c && c.Item.Label == label);
        AutomationPeer Peer(FrameworkElement e) => FrameworkElementAutomationPeer.CreatePeerForElement(e);
        void Invoke(FrameworkElement e) { ((IInvokeProvider)Peer(e).GetPattern(PatternInterface.Invoke)).Invoke(); Guard(); }
        bool Reject(IInvokeProvider provider)
        {
            try { provider.Invoke(); } catch (ElementNotEnabledException) { Guard(); return true; }
            throw new InvalidOperationException("An inactive Navigation Menu provider accepted invocation.");
        }
        HyperlinkButton OriginalLink(BleeckerNavigationMenuLink item) => All().OfType<HyperlinkButton>().Single(b => b.IsLoaded && b.DataContext is BleeckerNavigationLinkContext context && ReferenceEquals(context.Item, item));
        object Box(FrameworkElement e) { var r = e.TransformToVisual(root).TransformBounds(new Rect(0, 0, e.ActualWidth, e.ActualHeight)); return new { x = r.X, y = r.Y, width = r.Width, height = r.Height }; }
        object BrushData(Brush? brush) => brush is SolidColorBrush s ? new { a = s.Color.A, r = s.Color.R, g = s.Color.G, b = s.Color.B, opacity = s.Opacity } : throw new InvalidOperationException("Expected a concrete navigation brush.");
        object[] Inputs(FrameworkElement element) => Elements(element).OfType<TextBox>().Select(input => (object)new { value = input.Text, selectionStart = input.SelectionStart, selectionEnd = input.SelectionStart + input.SelectionLength, box = Box(input), role = Peer(input).GetAutomationControlType().ToString() }).ToArray();
        object Snapshot(BleeckerNavigationMenu menu) => new
        {
            menu = Box(menu),
            entries = Elements(menu).Where(e => e is Button && e.DataContext is BleeckerNavigationMenuGroup || e is HyperlinkButton && e.DataContext is BleeckerNavigationLinkContext).Select(e => new { label = e.DataContext is BleeckerNavigationMenuGroup g ? g.Trigger : ((BleeckerNavigationLinkContext)e.DataContext).Item.Label, box = Box(e), role = Peer(e).GetAutomationControlType().ToString(), inputs = Inputs(e) }).ToArray(),
            surfaces = Surfaces().Select(s => new { box = Box(s), s.Opacity, background = BrushData(s.Background), rows = Elements(s).OfType<HyperlinkButton>().Where(b => b.DataContext is BleeckerNavigationLinkContext).Select(b => new { label = ((BleeckerNavigationLinkContext)b.DataContext).Item.Label, href = ((BleeckerNavigationLinkContext)b.DataContext).Item.Href, external = ((BleeckerNavigationLinkContext)b.DataContext).Item.External, box = Box(b), role = Peer(b).GetAutomationControlType().ToString(), navigateUriUnset = b.NavigateUri is null, inputs = Inputs(b) }).ToArray() }).ToArray()
        };
        async Task<string> Capture(string name)
        {
            Guard(); var bitmap = new RenderTargetBitmap(); await bitmap.RenderAsync(root); Guard(); var pixels = System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());
            using var stream = new InMemoryRandomAccessStream(); var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream); encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight, 96, 96, pixels); await encoder.FlushAsync(); stream.Seek(0);
            using var reader = new DataReader(stream.GetInputStreamAt(0)); await reader.LoadAsync((uint)stream.Size); var bytes = new byte[(int)stream.Size]; reader.ReadBytes(bytes); var file = "navigation-menu-" + name + ".png"; await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!, file), bytes); pngs.Add(new { file, width = bitmap.PixelWidth, height = bitmap.PixelHeight, sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)) }); return file;
        }
        IBleeckerNavigationMenuEntry[] Build(string kind = "basic")
        {
            var home = new BleeckerNavigationMenuLink("#fictional-home", "Home"); var docs = new BleeckerNavigationMenuLink("#fictional-docs", "Documentation");
            var no = kind == "no-description";
            BleeckerNavigationMenuLink[] children = kind == "empty-group" ? [] : [new("#fictional-buttons", kind == "unicode" ? "Straße 東京 e\u0301 العربية" : "Buttons", no ? null : kind == "long-description" ? "A deliberately long fictional description that wraps across several lines without hiding its original text." : kind == "unicode" ? "Fictional multilingual components." : "Reusable fictional buttons."), new("/fictional/forms?kind=one%2Ftwo#fields", "Forms", no ? null : "Fictional input and selection controls."), new("https://example.invalid/library", "External library", no ? null : "Fictional external destination.", true)];
            return kind == "empty" ? [] : kind == "links-only" ? [home, docs] : [home, new BleeckerNavigationMenuGroup("Components", children, kind == "group-href-unused" ? "https://example.invalid/ignored" : "#ignored-group-destination"), new BleeckerNavigationMenuGroup("Layout", [new("#fictional-header", "Header", "Fictional page composition.")]), docs];
        }
        async Task<BleeckerNavigationMenu> Add(string kind = "basic", DataTemplate? template = null)
        {
            scene.Children.Clear(); await Layout(180);
            if (kind is "custom" or "custom-input")
            {
                var content = kind == "custom" ? "<ContentPresenter Content='{Binding Content}'/>" : "<StackPanel Name='CustomContentLayout' Orientation='Horizontal' Spacing='0' UseLayoutRounding='False'><ContentPresenter Content='{Binding Content}'/><TextBox Name='CustomFixtureInput' Text='retained original text' Width='90' Height='18' MinHeight='0' Padding='0' BorderThickness='0' FontSize='12' HorizontalAlignment='Left'/></StackPanel>";
                template = (DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><HyperlinkButton Name='CustomFixtureLink' Style='{Binding Style}' Foreground='{Binding Foreground}' Command='{Binding Command}' AutomationProperties.Name='{Binding Item.Label}'>" + content + "</HyperlinkButton></DataTemplate>");
            }
            var menu = new BleeckerNavigationMenu { Entries = Build(kind), LinkTemplate = template, Width = kind == "narrow" ? 200 : kind == "bottom-right" ? 180 : 680, Margin = new Thickness(kind == "bottom-right" ? 780 : 20, kind == "bottom-right" ? 650 : 20, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, FlowDirection = kind == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
            scene.Children.Add(menu); await Layout(); return menu;
        }
        void PrepareCustomInputs()
        {
            foreach (var panel in All().OfType<StackPanel>().Where(p => p.Name == "CustomContentLayout"))
            {
                panel.Orientation = ((BleeckerNavigationLinkContext)panel.DataContext).IsGrouped ? Orientation.Vertical : Orientation.Horizontal;
                var input = Elements(panel).OfType<TextBox>().Single(); input.SelectionStart = input.Text.Length;
            }
        }
        void Contract(string scheme, string action, object? details = null) => contracts.Add(new { scheme, action, details, passed = true });
        try
        {
            foreach (var scheme in new[] { "Light", "Dark" })
            {
                root.RequestedTheme = Enum.Parse<ElementTheme>(scheme);
                foreach (var kind in new[] { "closed", "empty", "links-only", "basic", "empty-group", "no-description", "long-description", "unicode", "narrow", "rtl", "bottom-right", "group-href-unused", "layout-open", "custom", "custom-input" })
                {
                    stage = scheme + "-" + kind; var menu = await Add(kind);
                    if (kind is not ("closed" or "empty" or "links-only")) { Invoke(Trigger(menu, kind == "layout-open" ? "Layout" : "Components")); await Layout(); }
                    if (kind == "custom-input") { PrepareCustomInputs(); await Layout(); }
                    // Exercise the actual open+hover template combination without
                    // sending pointer input or claiming a physical hover test.
                    if (kind == "basic") Require(VisualStateManager.GoToState(Trigger(menu), "PointerOver", false), "Native trigger has no pointer-over visual state.");
                    states.Add(new { scheme, kind, measurement = Snapshot(menu), file = await Capture(scheme + "-" + kind), sourceRasterParityVerified = false });
                }
                foreach (var label in new[] { "Buttons", "Forms", "External library", "Home" })
                {
                    stage = scheme + "-route-" + label; var menu = await Add(); var original = menu.Entries.ToArray(); var group = (BleeckerNavigationMenuGroup)original[1]; var requested = label == "Home" ? (BleeckerNavigationMenuLink)original[0] : group.Items.Single(i => i.Label == label); var order = new List<string>(); var originalReceived = false;
                    menu.Command = new Routing(() => { Require(((IExpandCollapseProvider)Peer(Trigger(menu)).GetPattern(PatternInterface.ExpandCollapse)).ExpandCollapseState == (label == "Home" ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed), "Grouped dismissal did not precede host routing."); order.Add("command"); });
                    menu.ItemInvoked += (_, item) => { originalReceived = ReferenceEquals(item, requested); order.Add("event"); };
                    Invoke(Trigger(menu)); await Layout(); var link = Link(label); var provider = (IInvokeProvider)Peer(link).GetPattern(PatternInterface.Invoke); Require(link.NavigateUri is null, "Default link launched a browser URI."); provider.Invoke(); await Layout(190);
                    Require(originalReceived && order.SequenceEqual(new[] { "command", "event" }) && ReferenceEquals(menu.Entries.ToArray()[1], group), "Host routing copied or replaced the original caller item.");
                    var retiredRejected = false; if (label != "Home") { try { provider.Invoke(); } catch (ElementNotEnabledException) { retiredRejected = true; } Require(retiredRejected, "Retired grouped provider remained invokable."); }
                    Contract(scheme, "route-" + label, new { href = requested.Href, external = requested.External, originalReceived, order, retiredRejected, groupRemainsOpen = label == "Home" });
                }
                stage = scheme + "-command-denial"; var denied = await Add(); var command = new Routing(() => throw new Exception("Denied routing invoked.")) { Allowed = false }; denied.Command = command; Invoke(Trigger(denied)); await Layout(); var deniedLink = Link("Buttons"); var deniedContext = (BleeckerNavigationLinkContext)deniedLink.DataContext;
                Require(!deniedContext.Command.CanExecute(null) && !Peer(deniedLink).IsEnabled(), "CanExecute denial did not cross the native boundary."); deniedContext.Command.Execute(null); command.Allowed = true; command.Notify(); Require(deniedContext.Command.CanExecute(null), "CanExecute notification did not update the retained renderer."); denied.IsEnabled = false; await Layout(); Require(!deniedContext.Command.CanExecute(null) && Surfaces().Length == 0, "Disabled menu retained live routing."); Contract(scheme, "command-denial-disable");

                stage = scheme + "-live-indices"; var live = await Add(); var values = new ObservableCollection<IBleeckerNavigationMenuEntry>(live.Entries); live.Entries = values; Invoke(Trigger(live)); await Layout(); var originalTrigger = Trigger(live); var originalLink = Link("Buttons"); var first = ((BleeckerNavigationMenuGroup)values[1]).Items.First(); first.Label = "Updated original label"; first.Description = "Updated original description."; await Layout();
                Require(ReferenceEquals(originalTrigger, Trigger(live)) && ReferenceEquals(originalLink, Link(first.Label)), "Live original label/description replaced keyed elements."); values.Insert(0, new BleeckerNavigationMenuGroup("Before", [])); await Layout(190); Require(Surfaces().Length == 0, "Prepending changed numeric active-index ownership."); values.RemoveAt(0); await Layout(); Require(Surfaces().Length == 1, "Numeric source index was silently cleared."); ((BleeckerNavigationMenuGroup)values[1]).Trigger = "Renamed"; await Layout(190); Require(!ReferenceEquals(originalTrigger, Trigger(live, "Renamed")) && Surfaces().Length == 1, "Renaming did not replace its keyed wrapper while retaining active index."); values.Clear(); await Layout(190); Require(Surfaces().Length == 0, "Removing active entries retained a surface."); Contract(scheme, "live-indices-originals");

                stage = scheme + "-custom-content";
                var customTemplate = (DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><StackPanel><HyperlinkButton Name='CustomNavigationLink' Content='{Binding Content}' Style='{Binding Style}' Foreground='{Binding Foreground}'/><TextBox Name='RetainedNavigationInput' Text='retained original text' Width='90' MinHeight='18' FontSize='12'/></StackPanel></DataTemplate>");
                var custom = await Add(template: customTemplate); Invoke(Trigger(custom)); await Layout(); var customLink = Link("Buttons"); var context = (BleeckerNavigationLinkContext)customLink.DataContext; var inputBox = Elements(Surfaces().Single()).OfType<TextBox>().First(); inputBox.Text = "caller retained custom text"; var originalItem = context.Item;
                Require(context.IsGrouped && context.CloseCommand is not null && ((BleeckerNavigationLinkContext)Link("Home").DataContext).CloseCommand is null, "Optional close callback shape changed.");
                originalItem.Label = "Updated custom label"; custom.Refresh(); await Layout(); Require(ReferenceEquals(customLink, Link(originalItem.Label)) && ReferenceEquals(inputBox, Elements(Surfaces().Single()).OfType<TextBox>().First()) && inputBox.Text == "caller retained custom text" && ReferenceEquals(context.Item, originalItem), "Keyed custom input or original item was lost.");
                // A custom renderer can route independently and ignore its optional close callback.
                var customRequests = 0; customLink.Command = new Routing(() => customRequests++); customLink.Command.Execute(null); Require(Surfaces().Single().Opacity == 1 && customRequests == 1, "Custom ignored-close unexpectedly dismissed.");
                customLink.Command = new Routing(() => throw new InvalidOperationException("fictional custom routing failure")); try { customLink.Command.Execute(null); throw new Exception("Custom routing failure was swallowed."); } catch (InvalidOperationException error) when (error.Message == "fictional custom routing failure") { } Require(Surfaces().Single().Opacity == 1, "A custom throw before its optional close dismissed the group.");
                context.CloseCommand!.Execute(null); await Layout(190); Require(!context.Command.CanExecute(null) && Surfaces().Length == 0, "Custom close retained obsolete routing."); Invoke(Trigger(custom)); await Layout(); Require(!ReferenceEquals(customLink, Link(originalItem.Label)) && Elements(Surfaces().Single()).OfType<TextBox>().First().Text == "retained original text", "Unmount retained uncontrolled custom input."); Contract(scheme, "custom-original-retention-close", new { customRequests, originalItemRetained = true, sameInputDuringLiveUpdate = true, resetAfterUnmount = true, directCloseIsNull = true });

                stage = scheme + "-hidden-declaration"; var hidden = await Add(); Invoke(Trigger(hidden)); await Layout(); var hiddenContext = (BleeckerNavigationLinkContext)Link("Buttons").DataContext; hidden.Visibility = Visibility.Collapsed; Require(!hiddenContext.Command.CanExecute(null), "Hidden declaration retained immediate routing eligibility."); await Layout(); Require(Surfaces().Length == 0, "Hidden declaration retained its ported surface."); hidden.Visibility = Visibility.Visible; await Layout(); Invoke(Trigger(hidden)); await Layout(); hiddenContext = (BleeckerNavigationLinkContext)Link("Buttons").DataContext; scene.Visibility = Visibility.Collapsed; Require(!hiddenContext.Command.CanExecute(null), "Hidden declaration ancestor retained immediate routing eligibility."); await Layout(); Require(Surfaces().Length == 0, "Hidden declaration ancestor retained its ported surface."); scene.Visibility = Visibility.Visible; await Layout(); Require(Surfaces().Length == 0, "Revealing a hidden declaration reopened retired presentation."); Contract(scheme, "hidden-declaration-ancestor", new { immediateEligibilityRevoked = true, portedSurfaceRetired = true, revealStaysClosed = true });

                stage = scheme + "-active-modal-isolation";
                var background = await Add(); var enabledProducer = new Border { Tag = true };
                background.SetBinding(Control.IsEnabledProperty, new Binding { Source = enabledProducer, Path = new PropertyPath(nameof(Border.Tag)) });
                var backgroundItem = ((BleeckerNavigationMenuGroup)background.Entries.ElementAt(1)).Items.First();
                var backgroundInvocations = 0; var ownedInvocations = 0; var originalBackgroundDelivered = false; var originalOwnedDelivered = false;
                background.ItemInvoked += (_, item) => { backgroundInvocations++; originalBackgroundDelivered = ReferenceEquals(item, backgroundItem); };
                Invoke(Trigger(background)); await Layout();
                var backgroundLink = OriginalLink(backgroundItem); var backgroundContext = (BleeckerNavigationLinkContext)backgroundLink.DataContext;
                var backgroundProvider = (IInvokeProvider)Peer(backgroundLink).GetPattern(PatternInterface.Invoke);
                var backgroundTriggerProvider = (IInvokeProvider)Peer(Trigger(background)).GetPattern(PatternInterface.Invoke);
                var directProvider = (IInvokeProvider)Peer(OriginalLink((BleeckerNavigationMenuLink)background.Entries.First())).GetPattern(PatternInterface.Invoke);
                var unopened = new BleeckerNavigationMenu { Entries = Build("links-only"), Width = 240, Margin = new Thickness(20, 100, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
                var unopenedEnabledProducer = new Border { Tag = true };
                unopened.SetBinding(Control.IsEnabledProperty, new Binding { Source = unopenedEnabledProducer, Path = new PropertyPath(nameof(Border.Tag)) });
                var unopenedItem = (BleeckerNavigationMenuLink)unopened.Entries.First(); var unopenedInvocations = 0; var unopenedOriginalDelivered = false;
                unopened.ItemInvoked += (_, item) => { unopenedInvocations++; unopenedOriginalDelivered = ReferenceEquals(item, unopenedItem); }; scene.Children.Add(unopened); await Layout();
                var unopenedLink = OriginalLink(unopenedItem); var unopenedContext = (BleeckerNavigationLinkContext)unopenedLink.DataContext;
                var unopenedProvider = (IInvokeProvider)Peer(unopenedLink).GetPattern(PatternInterface.Invoke);
                var owned = new BleeckerNavigationMenu { Entries = Build(), Width = 400 };
                var ownedItem = ((BleeckerNavigationMenuGroup)owned.Entries.ElementAt(1)).Items.First();
                owned.ItemInvoked += (_, item) => { ownedInvocations++; originalOwnedDelivered = ReferenceEquals(item, ownedItem); };
                var upper = new BleeckerModal { Title = "Fictional nested ownership", Content = new TextBlock { Text = "Only this nested modal owns input." } };
                var originalContent = new StackPanel(); originalContent.Children.Add(owned); originalContent.Children.Add(upper);
                var lower = new BleeckerModal { Title = "Fictional navigation ownership", Content = originalContent }; scene.Children.Add(lower);
                await Layout(); Require(root.XamlRoot is { IsHostVisible: false }, "Modal isolation requires a hidden host before opening.");
                lower.IsOpen = true; await Layout(260);
                Require(!backgroundContext.Command.CanExecute(null), $"Background command remained eligible under an active modal (menu enabled={background.IsEnabled}, binding retained={background.GetBindingExpression(Control.IsEnabledProperty) is not null}, modal open={lower.IsOpen}, owned menu loaded={owned.IsLoaded}).");
                Require(background.IsEnabled && background.GetBindingExpression(Control.IsEnabledProperty) is not null, "Modal shield rewrote the caller enabled binding.");
                Require(!unopenedContext.Command.CanExecute(null), "An unopened background menu bypassed modal ownership.");
                var backgroundRejectedProviders = (Reject(backgroundProvider) ? 1 : 0) + (Reject(backgroundTriggerProvider) ? 1 : 0) + (Reject(directProvider) ? 1 : 0) + (Reject(unopenedProvider) ? 1 : 0);
                backgroundContext.Command.Execute(null); backgroundContext.CloseCommand!.Execute(null); unopenedContext.Command.Execute(null);
                Require(backgroundInvocations == 0 && unopenedInvocations == 0 && backgroundLink.IsLoaded, "Blocked custom-renderer commands routed or closed original background content.");
                unopenedEnabledProducer.Tag = false; await Layout(); Require(!unopened.IsEnabled, "Caller enabled producer stopped updating behind a modal."); unopenedEnabledProducer.Tag = true; await Layout();
                Require(unopened.IsEnabled && !unopenedContext.Command.CanExecute(null) && background.IsEnabled && !backgroundContext.Command.CanExecute(null), "Caller enablement bypassed the modal shield.");
                Invoke(Trigger(owned)); await Layout(); Invoke(OriginalLink(ownedItem)); await Layout(190);
                Require(ownedInvocations == 1 && originalOwnedDelivered && backgroundInvocations == 0, "Active modal did not route its original navigation item exclusively.");
                Invoke(Trigger(owned)); await Layout(); var ownedLink = OriginalLink(ownedItem); var ownedContext = (BleeckerNavigationLinkContext)ownedLink.DataContext;
                var ownedProvider = (IInvokeProvider)Peer(ownedLink).GetPattern(PatternInterface.Invoke);
                upper.IsOpen = true; await Layout(260);
                Require(!ownedContext.Command.CanExecute(null) && !backgroundContext.Command.CanExecute(null), "Nested modal retained lower or background command eligibility.");
                var nestedRejectedProviders = (Reject(ownedProvider) ? 1 : 0) + (Reject((IInvokeProvider)Peer(Trigger(owned)).GetPattern(PatternInterface.Invoke)) ? 1 : 0);
                ownedContext.Command.Execute(null); ownedContext.CloseCommand!.Execute(null); Require(ownedInvocations == 1, "Blocked owned commands bypassed the nested modal.");
                upper.IsOpen = false; await Layout(260);
                Require(ReferenceEquals(ownedLink, OriginalLink(ownedItem)) && ReferenceEquals(ownedContext, ownedLink.DataContext) && ownedContext.Command.CanExecute(null) && !backgroundContext.Command.CanExecute(null), "Nested modal restoration lost original navigation state or released the background shield.");
                ownedProvider.Invoke(); await Layout(190); Require(ownedInvocations == 2 && originalOwnedDelivered && backgroundInvocations == 0, "Restored modal navigation did not route its original item exclusively.");
                Invoke(Trigger(owned)); await Layout(); var exitingLink = OriginalLink(ownedItem); var exitingContext = (BleeckerNavigationLinkContext)exitingLink.DataContext; var retiredOwnerProvider = (IInvokeProvider)Peer(exitingLink).GetPattern(PatternInterface.Invoke);
                lower.IsOpen = false; Require(!exitingContext.Command.CanExecute(null), "Closing owner left its retained exit subtree eligible."); var closingOwnerProviderRejected = Reject(retiredOwnerProvider); exitingContext.Command.Execute(null);
                await Layout(260); var ownerRetiredProviderRejected = Reject(retiredOwnerProvider);
                Require(!owned.IsLoaded && ReferenceEquals(lower.Content, originalContent) && ReferenceEquals(backgroundLink, OriginalLink(backgroundItem)) && backgroundContext.Command.CanExecute(null), "Closing the modal retained owned routing or lost original background content.");
                backgroundProvider.Invoke(); await Layout(190);
                unopenedProvider.Invoke(); await Layout(); Require(unopenedInvocations == 1 && unopenedOriginalDelivered, "Unopened menu did not restore original routing after modal close.");
                Require(backgroundInvocations == 1 && originalBackgroundDelivered && ownedInvocations == 2 && background.IsEnabled && background.GetBindingExpression(Control.IsEnabledProperty) is not null, "Modal close did not restore original background routing and binding.");
                Contract(scheme, "active-modal-isolation", new { backgroundRejectedProviders, nestedRejectedProviders, closingOwnerProviderRejected, ownerRetiredProviderRejected, backgroundInvocations, ownedInvocations, unopenedInvocations, originalBackgroundDelivered, originalOwnedDelivered, unopenedOriginalDelivered, blockedCommandsIgnored = true, callerEnabledBindingRetained = true, nestedOriginalStateRestored = true, originalModalContentRetained = true });

                stage = scheme + "-presence"; var present = await Add(); Invoke(Trigger(present)); await Layout(); var surface = Surfaces().Single(); Invoke(Trigger(present)); await Task.Delay(50); Guard(); Require(surface.IsLoaded && surface.Opacity == 0, "Exit removed original surface before 150ms."); await Task.Delay(140); Guard(); Require(!surface.IsLoaded && Surfaces().Length == 0, "Exit retained surface past its removal timer."); Invoke(Trigger(present)); await Layout(); var interrupted = Surfaces().Single(); Invoke(Trigger(present)); await Task.Delay(50); Invoke(Trigger(present)); await Layout(180); Require(ReferenceEquals(interrupted, Surfaces().Single()) && interrupted.Opacity == 1, "Reopening failed to cancel exit."); presence.Add(new { scheme, originalRetainedAt50 = true, retiredBy190 = true, reopenCancelledExit = true, normalMotionVerified = false, passed = true });

                stage = scheme + "-template-unload"; var originalEntries = present.Entries; var retired = (IInvokeProvider)Peer(Link("Buttons")).GetPattern(PatternInterface.Invoke); present.Template = (ControlTemplate)XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='Control'><Border Width='43' Height='29' Background='Purple'/></ControlTemplate>"); await Layout(); Require(Surfaces().Length == 0 && ReferenceEquals(originalEntries, present.Entries), "Template replacement retained a popup or replaced entries."); var rejected = false; try { retired.Invoke(); } catch (ElementNotEnabledException) { rejected = true; } Require(rejected, "Replaced template retained stale provider."); present.ClearValue(Control.TemplateProperty); await Layout(); Invoke(Trigger(present)); await Layout(); scene.Children.Clear(); await Layout(180); scene.Children.Add(present); await Layout(); Require(Surfaces().Length == 0 && ReferenceEquals(originalEntries, present.Entries), "Unload/reload reopened retired state."); Contract(scheme, "template-unload-reload");

                stage = scheme + "-standalone"; scene.Children.Clear(); root.Children.Clear(); await Layout(180); var plain = new BleeckerNavigationMenu { Width = 680, Entries = Build(), Margin = new Thickness(20, 20, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top }; root.Children.Add(plain); await Layout(); Invoke(Trigger(plain)); await Layout(); var popup = VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Single(p => p.Child is Canvas c && c.Name == "NavigationMenuPlane"); Require(popup.IsOpen && popup.ShouldConstrainToRootBounds && !popup.IsLightDismissEnabled, "Standalone navigation lost its scoped native popup."); Invoke(Link("Buttons")); await Layout(190); root.Children.Clear(); await Layout(180); Require(!popup.IsOpen && popup.Child is null, "Standalone popup survived owner unload."); standalone.Add(new { scheme, rootConstrained = true, noNativeLightDismiss = true, unloadedPopupRetired = true, passed = true }); root.Children.Add(host); await Layout();
            }
            if (catalogFactory is not null) foreach (var scheme in new[] { "Light", "Dark" })
            {
                stage = scheme + "-catalog"; scene.Children.Clear(); root.RequestedTheme = Enum.Parse<ElementTheme>(scheme); var fixture = catalogFactory(); scene.Children.Add(fixture); await Layout(180); var menus = Elements(fixture).OfType<BleeckerNavigationMenu>().ToArray(); Require(menus.Length == 5 && menus.All(m => m.IsLoaded), "Registered navigation fixtures did not load."); var demo = menus.Single(m => m.Name == "Default"); Require(Elements(demo).OfType<HyperlinkButton>().Where(b => b.DataContext is BleeckerNavigationLinkContext).All(b => ReferenceEquals(b.Style, demo.TopLinkStyle)), "Top link style did not reach the native link."); Require(Elements(demo).OfType<Button>().Where(b => b.DataContext is BleeckerNavigationMenuGroup).All(b => ReferenceEquals(b.Style, demo.TriggerStyle)), "Trigger style did not reach the native button."); Invoke(Trigger(demo)); await Layout(); Require(ReferenceEquals(Surfaces().Single().Style, demo.SurfaceStyle) && Elements(Surfaces().Single()).OfType<HyperlinkButton>().Where(b => b.DataContext is BleeckerNavigationLinkContext).All(b => ReferenceEquals(b.Style, demo.GroupLinkStyle)), "Surface/grouped link styles did not reach their rendered consumers."); catalog.Add(new { scheme, menus = menus.Length, originalStylesApplied = true, registeredFixture = fixture.GetType().FullName, file = await Capture("catalog-" + scheme) });
            }
            string Hash(string file) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-navigation-menu-native-v1", passed = true, states, contracts, presence, standalone, catalog, pngs, guards, focus, activations, publicApisOnly = true, activateCalled = false, showWindowCalled = false, focusCalled = false, hardwareInputSent = false, physicalInputVerified = false, sourceRasterParityVerified = false, nativeComponentAccepted = false, librarySha256 = Hash(typeof(BleeckerNavigationMenu).Assembly.Location), hostSha256 = Hash(hostAssembly) }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(0);
        }
        catch (Exception error) { await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-navigation-menu-native-v1", passed = false, stage, error = error.ToString(), states, contracts, presence, standalone, catalog, pngs, guards, focus, activations }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(1); }
    }
}
