using System.Collections.ObjectModel;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.System;

namespace Orchard.Testing;

internal static class MenubarFixture
{
    private delegate bool Visitor(nint handle, nint value);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor, nint value);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle, out uint process);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint handle);
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node)
    { if (node is FrameworkElement e) yield return e; for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) foreach (var child in Elements(VisualTreeHelper.GetChild(node, i))) yield return child; }

    private sealed class PeerAccess(FrameworkElement owner) : FrameworkElementAutomationPeer(owner)
    {
        private static PeerAccess? resolver;
        internal static AutomationPeer? Resolve(IRawElementProviderSimple provider) => (resolver ??= new(new Border())).PeerFromProvider(provider);
    }
    private static object ProviderIdentity(IRawElementProviderSimple provider)
    {
        var peer = PeerAccess.Resolve(provider);
        return new { resolved = peer is not null, role = peer?.GetAutomationControlType().ToString(), name = peer?.GetName() };
    }

    private static object PeerTree(AutomationPeer? peer, int depth = 0)
    {
        if (peer is null) return new { missing = true };
        var children = peer.GetChildren()?.ToArray() ?? [];
        return new
        {
            role = peer.GetAutomationControlType().ToString(), name = peer.GetName(), className = peer.GetClassName(),
            control = peer.IsControlElement(), content = peer.IsContentElement(), childCount = children.Length,
            truncated = depth >= 6 && children.Length > 0,
            controlled = peer.GetControlledPeers()?.Select(controlled => new { role = controlled.GetAutomationControlType().ToString(), name = controlled.GetName() }).ToArray() ?? [],
            selection = peer.GetPattern(PatternInterface.SelectionItem) is ISelectionItemProvider selection ? new { selected = selection.IsSelected, containerAvailable = selection.SelectionContainer is not null, container = selection.SelectionContainer is { } container ? ProviderIdentity(container) : null } : null,
            group = peer.GetPattern(PatternInterface.Selection) is ISelectionProvider group ? new { multiple = group.CanSelectMultiple, required = group.IsSelectionRequired, selected = group.GetSelection().Select(ProviderIdentity).ToArray() } : null,
            children = depth >= 6 ? [] : children.Select(child => PeerTree(child, depth + 1)).ToArray()
        };
    }

    internal static async Task RunAsync(Window window, string output, string hostAssembly, Func<FrameworkElement>? catalogFactory = null)
    {
        var root = (Grid)XamlReader.Load("<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Width='900' Height='760' Background='{ThemeResource BleeckerBackgroundBrush}'/>");
        root.Clip = new RectangleGeometry { Rect = new Rect(0, 0, 900, 760) }; window.Content = root; window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(900, 760));
        var guards = 0; var focus = 0; var activations = 0; var stage = "initial";
        var states = new List<object>(); var contracts = new List<object>(); var pngs = new List<object>(); var events = new List<object>(); var presence = new List<object>(); var surfaceMotion = new List<object>(); var rowMotion = new List<object>(); var rowMotionCaptures = new List<object>(); var rowPaintOwnership = new List<object>(); var standalone = new List<object>(); var keyboard = new List<object>(); var radioSelection = new List<object>(); var peerTrees = new List<object>(); var groupContracts = new List<object>();
        string? lastRecordedStage = null;
        Application.Current.UnhandledException += (_, args) =>
        {
            var nativeMessage = args.Message;
            File.WriteAllText(output, JsonSerializer.Serialize(new { format = "orchard-menubar-native-v1", passed = false, stage, nativeMessage, error = args.Exception.ToString(), states, contracts, presence, surfaceMotion, rowMotion, rowMotionCaptures, rowPaintOwnership, keyboard, radioSelection, peerTrees, groupContracts, standalone, pngs, guards, focus, activations }, new JsonSerializerOptions { WriteIndented = true }));
            Environment.Exit(1);
        };
        var scene = new Grid(); var host = new BleeckerModalHost { Content = scene, Clip = new RectangleGeometry { Rect = new Rect(0, 0, 900, 760) } }; root.Children.Add(host);
        root.GotFocus += (_, _) => focus++; window.Activated += (_, args) => { if (args.WindowActivationState != WindowActivationState.Deactivated) activations++; };
        void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        void Guard()
        {
            guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var visible = false;
            EnumWindows((handle, _) => { GetWindowThreadProcessId(handle, out var pid); visible |= pid == Environment.ProcessId && IsWindowVisible(handle); return true; }, 0);
            Require(!visible && !window.Visible && !window.AppWindow.IsVisible && foreground != Environment.ProcessId && focus == 0 && activations == 0, "Menubar probe left its hidden boundary.");
        }
        async Task Layout(int delay = 260)
        {
            if (lastRecordedStage != stage) { lastRecordedStage = stage; File.WriteAllText(Path.Combine(Path.GetDirectoryName(output)!, "menubar-stage.json"), JsonSerializer.Serialize(new { stage, states = states.Count, contracts = contracts.Count, presence = presence.Count, standalone = standalone.Count, guards, focus, activations })); }
            for (var i = 0; i < 5; i++) { var idle = new TaskCompletionSource(); Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => idle.SetResult()), "Dispatcher refused Menubar layout."); await idle.Task; root.Measure(new Size(900, 760)); root.Arrange(new Rect(0, 0, 900, 760)); root.UpdateLayout(); Guard(); }
            await Task.Delay(delay); root.UpdateLayout(); Guard();
        }
        IEnumerable<FrameworkElement> All() => Elements(root).Concat(VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Where(p => p.Child is not null).SelectMany(p => Elements(p.Child))).Distinct();
        Button Trigger(BleeckerMenubar bar, string label) => Elements(bar).OfType<Button>().Single(b => b.DataContext is BleeckerMenubarMenu menu && menu.Trigger == label);
        Button Item(string label) => All().OfType<Button>().Single(b => b.IsLoaded && b.DataContext is BleeckerMenubarItem item && item.Label == label);
        AutomationPeer Peer(FrameworkElement e) => FrameworkElementAutomationPeer.CreatePeerForElement(e);
        void Invoke(Button button) { ((IInvokeProvider)Peer(button).GetPattern(PatternInterface.Invoke)).Invoke(); Guard(); }
        void Expand(Button button) { ((IExpandCollapseProvider)Peer(button).GetPattern(PatternInterface.ExpandCollapse)).Expand(); Guard(); }
        IInvokeProvider Provider(Button button) => (IInvokeProvider)Peer(button).GetPattern(PatternInterface.Invoke);
        bool Reject(IInvokeProvider provider) { try { provider.Invoke(); } catch (ElementNotEnabledException) { Guard(); return true; } throw new InvalidOperationException("An inactive Menubar provider accepted invocation."); }
        Button OriginalRow(BleeckerMenubarItem item) => All().OfType<Button>().Single(b => b.IsLoaded && ReferenceEquals(b.DataContext, item));
        BleeckerOverlaySurface[] Surfaces() => All().OfType<BleeckerOverlaySurface>().Where(s => s.IsLoaded && s.Name is "MenubarRootSurface" or "MenubarSubmenuSurface").ToArray();
        object Box(FrameworkElement e) { var r = e.TransformToVisual(root).TransformBounds(new Rect(0, 0, e.ActualWidth, e.ActualHeight)); return new { x = r.X, y = r.Y, width = r.Width, height = r.Height }; }
        object BrushData(Brush? brush) => brush is SolidColorBrush s ? new { a = s.Color.A, r = s.Color.R, g = s.Color.G, b = s.Color.B, opacity = s.Opacity } : throw new InvalidOperationException("Expected a concrete Menubar brush.");
        double EffectiveOpacity(BleeckerOverlaySurface surface) => surface.Opacity * ((Grid)surface.Parent).Opacity;
        object Snapshot(BleeckerMenubar bar) => new
        {
            bar = Box(bar), menusRetained = bar.Menus is not null,
            triggers = Elements(bar).OfType<Button>().Where(b => b.DataContext is BleeckerMenubarMenu).Select(b => new { label = ((BleeckerMenubarMenu)b.DataContext).Trigger, box = Box(b), role = Peer(b).GetAutomationControlType().ToString(), activeBackgroundOpacity = ((SolidColorBrush)Elements(b).OfType<Border>().Single(e => e.Name == "MenuPaint").Background).Opacity, expanded = ((IExpandCollapseProvider)Peer(b).GetPattern(PatternInterface.ExpandCollapse)).ExpandCollapseState.ToString() }).ToArray(),
            surfaces = Surfaces().Select(s => new { name = s.Name, box = Box(s), Opacity = EffectiveOpacity(s), background = BrushData(s.Background), rows = Elements(s).OfType<Button>().Where(b => b.DataContext is BleeckerMenubarItem).Select(b => new { label = ((BleeckerMenubarItem)b.DataContext).Label, box = Box(b), enabled = Peer(b).IsEnabled(), role = Peer(b).GetAutomationControlType().ToString(), name = Peer(b).GetName(), activeBackgroundOpacity = ((SolidColorBrush)Elements(b).OfType<Border>().Single(e => e.Name == "MenuPaint").Background).Opacity, expanded = b.DataContext is BleeckerMenubarSubmenu ? ((IExpandCollapseProvider)Peer(b).GetPattern(PatternInterface.ExpandCollapse)).ExpandCollapseState.ToString() : null, isChecked = b.DataContext is BleeckerMenubarCheckbox ? ((IToggleProvider)Peer(b).GetPattern(PatternInterface.Toggle)).ToggleState.ToString() : b.DataContext is BleeckerMenubarRadioItem ? ((ISelectionItemProvider)Peer(b).GetPattern(PatternInterface.SelectionItem)).IsSelected.ToString() : null }).ToArray() }).ToArray(),
            events = events.ToArray()
        };
        async Task<string> Capture(string name)
        {
            Guard(); var bitmap = new RenderTargetBitmap(); await bitmap.RenderAsync(root); Guard(); var pixels = System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());
            using var stream = new InMemoryRandomAccessStream(); var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream); encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight, 96, 96, pixels); await encoder.FlushAsync(); stream.Seek(0);
            using var reader = new DataReader(stream.GetInputStreamAt(0)); await reader.LoadAsync((uint)stream.Size); var bytes = new byte[(int)stream.Size]; reader.ReadBytes(bytes); var file = "menubar-" + name + ".png"; await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!, file), bytes); pngs.Add(new { file, width = bitmap.PixelWidth, height = bitmap.PixelHeight, sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)) }); return file;
        }
        BleeckerMenubar? current = null;
        bool checkedValue = false, accept = false, throwAction = false; string radioValue = "a", labelValue = "New fictional draft";
        BleeckerMenubarMenu[] Build(bool empty = false, bool rich = false) => [new("File", empty ? [] : [
            new BleeckerMenubarAction(labelValue, () => { events.Add("new"); if (throwAction) throw new InvalidOperationException("fictional menubar callback failed"); }, rich ? XamlReader.Load("<Viewbox xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Width='16' Height='16'><Canvas Width='24' Height='24'><Path Data='M12,5 L12,19 M5,12 L19,12' Stroke='{ThemeResource BleeckerTextSecondaryBrush}' StrokeThickness='2' StrokeStartLineCap='Round' StrokeEndLineCap='Round'/></Canvas></Viewbox>") : null, shortcut: "Ctrl+N"),
            new BleeckerMenubarAction("Unavailable action", () => events.Add("disabled"), isDisabled: true), new BleeckerMenubarSeparator(),
            new BleeckerMenubarCheckbox("Show fictional guides", checkedValue, value => { events.Add(new { key = "checked", value }); if (accept) { checkedValue = value; current!.Menus = Build(); } }, shortcut: "Ctrl+G"),
            new BleeckerMenubarRadioGroup(radioValue, value => { events.Add(new { key = "radio", value }); if (accept) { radioValue = value; current!.Menus = Build(); } }, [new("Fictional Alpha mode", "a"), new("Fictional Beta mode", "b", shortcut: "Alt+B"), new("Unavailable mode", "c", true)]), new BleeckerMenubarSeparator(),
            new BleeckerMenubarSubmenu("More fictional choices", [new BleeckerMenubarAction("Nested original action", () => events.Add("nested")), new BleeckerMenubarSubmenu("Deeper fictional choices", [new BleeckerMenubarAction("Deep original action", () => events.Add("deep"))])]),
            new BleeckerMenubarSubmenu("Unavailable submenu", [new BleeckerMenubarAction("Disabled parent child", () => events.Add("disabled-parent-child"))], true)
        ]), new("Edit", [new BleeckerMenubarAction("Edit original action", () => events.Add("edit"))])];
        async Task<BleeckerMenubar> Add(string kind = "basic")
        {
            scene.Children.Clear(); await Layout(180); events.Clear(); checkedValue = kind == "checked-beta"; radioValue = kind == "checked-beta" ? "b" : "a"; accept = false; throwAction = false;
            labelValue = kind == "unicode" ? "Straße 東京 é العربية" : kind == "long-label" ? "A fictional action with a deliberately long label across the menu" : "New fictional draft";
            current = new BleeckerMenubar { Width = kind == "narrow" ? 140 : kind == "bottom-right" ? 180 : 360, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(kind == "bottom-right" ? 680 : 20, kind == "bottom-right" ? 650 : 20, 0, 0), Menus = kind == "empty-bar" ? [] : Build(kind == "empty-menu", kind == "rich-action"), FlowDirection = kind == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
            AutomationProperties.SetName(current, "Original fictional menu bar"); scene.Children.Add(current); await Layout(); return current;
        }
        (bool Handled, Button? Target) Navigate(BleeckerMenubar bar, Button source, VirtualKey key)
        {
            try
            {
                var method = typeof(BleeckerMenubar).GetMethod("NavigateMenu", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new InvalidOperationException("Menubar has no keyboard controller.");
                var result = (System.Runtime.CompilerServices.ITuple)method.Invoke(bar, [source, key])!;
                return ((bool)result[0]!, (Button?)result[1]);
            }
            catch (TargetInvocationException error) when (error.InnerException is not null)
            { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
        }
        try
        {
            foreach (var scheme in new[] { "Light", "Dark" })
            {
                root.RequestedTheme = Enum.Parse<ElementTheme>(scheme);
                stage = scheme + "-active-modal-isolation";
                var background = await Add(); var backgroundCalls = 0; var ownedCalls = 0;
                var backgroundAction = new BleeckerMenubarAction("Original background action", () => backgroundCalls++);
                BleeckerMenubarMenu[] backgroundMenus = [new("Background", [backgroundAction])]; var producer = new Border { Tag = backgroundMenus };
                background.SetBinding(BleeckerMenubar.MenusProperty, new Binding { Source = producer, Path = new PropertyPath(nameof(Border.Tag)) }); await Layout();
                var unopened = new BleeckerMenubar { Width = 240, Margin = new Thickness(20, 100, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Menus = [new("Unopened", [])] }; scene.Children.Add(unopened); await Layout();
                Invoke(Trigger(background, "Background")); await Layout(); var backgroundRow = OriginalRow(backgroundAction);
                var backgroundProvider = Provider(backgroundRow); var backgroundTrigger = Provider(Trigger(background, "Background")); var unopenedTrigger = Provider(Trigger(unopened, "Unopened"));
                var ownedAction = new BleeckerMenubarAction("Original owned action", () => ownedCalls++);
                var owned = new BleeckerMenubar { Width = 360, Menus = [new("Owned", [ownedAction])] };
                var upper = new BleeckerModal { Title = "Fictional nested menu ownership", Content = new TextBlock { Text = "Only the nested modal owns input." } };
                var originalContent = new StackPanel(); originalContent.Children.Add(owned); originalContent.Children.Add(upper);
                var lower = new BleeckerModal { Title = "Fictional menu ownership", Content = originalContent }; scene.Children.Add(lower); await Layout();
                Require(root.XamlRoot is { IsHostVisible: false }, "Menubar modal checks require a hidden host."); lower.IsOpen = true; await Layout(260);
                Require(!Peer(backgroundRow).IsEnabled() && !Peer(Trigger(background, "Background")).IsEnabled() && !Peer(Trigger(unopened, "Unopened")).IsEnabled(), $"Background Menubar providers remained eligible (bar enabled={background.IsEnabled}, binding retained={background.GetBindingExpression(BleeckerMenubar.MenusProperty) is not null}, modal open={lower.IsOpen}, owned loaded={owned.IsLoaded}).");
                var backgroundProviderRejections = (Reject(backgroundProvider) ? 1 : 0) + (Reject(backgroundTrigger) ? 1 : 0) + (Reject(unopenedTrigger) ? 1 : 0);
                Invoke(Trigger(owned, "Owned")); await Layout(); Invoke(OriginalRow(ownedAction)); await Layout(190); Require(ownedCalls == 1 && backgroundCalls == 0, "Active modal did not invoke only its original menu action.");
                Invoke(Trigger(owned, "Owned")); await Layout(); var ownedRow = OriginalRow(ownedAction); var ownedProvider = Provider(ownedRow);
                upper.IsOpen = true; await Layout(260); var nestedProviderRejections = (Reject(ownedProvider) ? 1 : 0) + (Reject(Provider(Trigger(owned, "Owned"))) ? 1 : 0);
                upper.IsOpen = false; await Layout(260); Require(ReferenceEquals(ownedRow, OriginalRow(ownedAction)) && ReferenceEquals(ownedRow.DataContext, ownedAction), "Nested modal close replaced original owned menu content.");
                ownedProvider.Invoke(); await Layout(190); Require(ownedCalls == 2 && backgroundCalls == 0, "Nested modal restoration routed an extra or missing original callback.");
                Invoke(Trigger(owned, "Owned")); await Layout(); var retiringRow = OriginalRow(ownedAction); var retiringProvider = Provider(retiringRow);
                lower.IsOpen = false; var closingProviderRejections = (Reject(retiringProvider) ? 1 : 0) + (Reject(Provider(Trigger(owned, "Owned"))) ? 1 : 0); await Layout(260);
                var retiredOwnerProviderRejected = Reject(retiringProvider); Require(!retiringRow.IsLoaded && ReferenceEquals(lower.Content, originalContent), "Closing the modal retained owned rows or replaced original content.");
                Require(ReferenceEquals(backgroundRow, OriginalRow(backgroundAction)) && ReferenceEquals(background.Menus, backgroundMenus) && background.GetBindingExpression(BleeckerMenubar.MenusProperty) is not null, "Modal lifecycle replaced original background menu data, rows or binding.");
                backgroundProvider.Invoke(); await Layout(190); unopenedTrigger.Invoke(); await Layout(); Require(Surfaces().Length == 1 && Peer(Trigger(unopened, "Unopened")).IsEnabled(), "Unopened trigger did not restore after modal close."); unopenedTrigger.Invoke(); await Layout(190);
                Require(backgroundCalls == 1 && ownedCalls == 2, "Inactive Menubar providers invoked an original callback.");
                contracts.Add(new { scheme, action = "active-modal-isolation", passed = true, backgroundProviderRejections, nestedProviderRejections, closingProviderRejections, retiredOwnerProviderRejected, backgroundCalls, ownedCalls, originalBackgroundRowsAndBindingRetained = true, nestedOriginalRowRetained = true, unopenedTriggerRestored = true, originalModalContentRetained = true });
                stage = scheme + "-radio-selection-provider";
                var radioBar = await Add(); Invoke(Trigger(radioBar, "File")); await Layout();
                ISelectionItemProvider Selection(string label) => (ISelectionItemProvider)Peer(Item(label)).GetPattern(PatternInterface.SelectionItem);
                var alphaButton = Item("Fictional Alpha mode");
                var alpha = Selection("Fictional Alpha mode"); var beta = Selection("Fictional Beta mode"); var unavailable = Selection("Unavailable mode");
                async Task RadioCase(string id, Action action, string? expectedError = null, int expectedCalls = 0, string expectedValue = "a", int expectedSurfaces = 1, bool expectedCallerRetained = true)
                {
                    stage = scheme + "-radio-provider-" + id;
                    var original = radioBar.Menus; string? error = null;
                    try { action(); } catch (Exception failure) { error = failure.GetType().Name; }
                    await Layout(190);
                    var selected = new[] { ("a", alpha), ("b", beta) }.Where(value => value.Item2.IsSelected).Select(value => value.Item1).ToArray();
                    var callerRetained = ReferenceEquals(original, radioBar.Menus);
                    Require(error == expectedError && events.Count == expectedCalls && radioValue == expectedValue && Surfaces().Length == expectedSurfaces && callerRetained == expectedCallerRetained && selected.SequenceEqual(expectedValue is "a" or "b" ? new[] { expectedValue } : []), "Radio provider contract failed: " + id);
                    radioSelection.Add(new { scheme, id, error, callbacks = events.Count, value = radioValue, selected, surfaces = Surfaces().Length, callerRetained, passed = true });
                }
                await RadioCase("selected-select", alpha.Select);
                await RadioCase("selected-add", alpha.AddToSelection);
                await RadioCase("unselected-remove", beta.RemoveFromSelection);
                await RadioCase("conflicting-add", beta.AddToSelection, nameof(InvalidOperationException));
                await RadioCase("selected-remove", alpha.RemoveFromSelection, nameof(InvalidOperationException));
                foreach (var method in new[] { "Select", "Add", "Remove" })
                    await RadioCase("disabled-unselected-" + method, method == "Select" ? unavailable.Select : method == "Add" ? unavailable.AddToSelection : unavailable.RemoveFromSelection, nameof(ElementNotEnabledException));
                alphaButton.IsEnabled = false;
                foreach (var method in new[] { "Select", "Add", "Remove" })
                    await RadioCase("disabled-selected-" + method, method == "Select" ? alpha.Select : method == "Add" ? alpha.AddToSelection : alpha.RemoveFromSelection, nameof(ElementNotEnabledException));
                alphaButton.IsEnabled = true; scene.Children.Clear(); await Layout(190);
                foreach (var method in new[] { "Select", "Add", "Remove" })
                    await RadioCase("unloaded-" + method, method == "Select" ? alpha.Select : method == "Add" ? alpha.AddToSelection : alpha.RemoveFromSelection, nameof(ElementNotEnabledException), expectedSurfaces: 0);
                foreach (var accepted in new[] { false, true }) foreach (var addEmpty in new[] { false, true })
                {
                    radioBar = await Add(); accept = accepted;
                    if (addEmpty) { radioValue = "unknown"; radioBar.Menus = Build(); }
                    Invoke(Trigger(radioBar, "File")); await Layout(); alpha = Selection("Fictional Alpha mode"); beta = Selection("Fictional Beta mode");
                    await RadioCase((addEmpty ? "add-empty-" : "select-") + accepted, addEmpty ? beta.AddToSelection : beta.Select, expectedCalls: 1, expectedValue: accepted ? "b" : addEmpty ? "unknown" : "a", expectedSurfaces: 0, expectedCallerRetained: !accepted);
                }
                radioBar = await Add(); Invoke(Trigger(radioBar, "File")); await Layout(); alpha = Selection("Fictional Alpha mode"); beta = Selection("Fictional Beta mode");
                await RadioCase("invoke-selected", () => Invoke(Item("Fictional Alpha mode")), expectedCalls: 1, expectedSurfaces: 0);
                radioBar = await Add(); radioBar.Menus = [new("File", [new BleeckerMenubarRadioGroup("a", _ => { events.Add("throwing-radio"); throw new InvalidOperationException("Original fictional radio callback failed."); }, [new("Fictional Alpha mode", "a"), new("Fictional Beta mode", "b")])])];
                Invoke(Trigger(radioBar, "File")); await Layout(); alpha = Selection("Fictional Alpha mode"); beta = Selection("Fictional Beta mode");
                await RadioCase("throwing-select", beta.Select, nameof(InvalidOperationException), expectedCalls: 1);
                foreach (var kind in new[] { "closed", "empty-bar", "basic", "rich-action", "checked-beta", "empty-menu", "edit-menu", "submenu", "deep-submenu", "disabled-submenu", "unicode", "long-label", "narrow", "rtl", "bottom-right" })
                {
                    stage = scheme + "-" + kind; var bar = await Add(kind);
                    if (kind is not "closed" and not "empty-bar") { Invoke(Trigger(bar, kind == "edit-menu" ? "Edit" : "File")); await Layout(); }
                    if (kind is "submenu" or "deep-submenu") { Expand(Item("More fictional choices")); await Layout(); }
                    if (kind == "deep-submenu") { Expand(Item("Deeper fictional choices")); await Layout(); }
                    var disabledInvocationsRejected = 0;
                    if (kind == "disabled-submenu")
                    {
                        foreach (var name in new[] { "Unavailable submenu", "Unavailable action", "Unavailable mode" })
                            try { if (name == "Unavailable submenu") Expand(Item(name)); else Invoke(Item(name)); throw new Exception("Disabled request unexpectedly succeeded."); } catch (ElementNotEnabledException) { disabledInvocationsRejected++; }
                        Require(disabledInvocationsRejected == 3 && events.Count == 0, "Disabled request invoked an original callback.");
                    }
                    Require(Peer(bar).GetAutomationControlType() == AutomationControlType.MenuBar && Peer(bar).GetName() == "Original fictional menu bar", "Caller menu name or role changed.");
                    if (kind is "closed" or "basic" or "submenu" or "deep-submenu")
                        peerTrees.Add(new { scheme, kind, bar = PeerTree(Peer(bar)), surfaces = Surfaces().Select(surface => new { surface = surface.Name, tree = PeerTree(FrameworkElementAutomationPeer.CreatePeerForElement(surface)), menu = PeerTree(surface.Content is FrameworkElement content ? FrameworkElementAutomationPeer.CreatePeerForElement(content) : null), rows = Elements(surface).OfType<Button>().Where(button => button.DataContext is BleeckerMenubarItem).Select(button => PeerTree(Peer(button))).ToArray() }).ToArray() });
                    var file = await Capture(stage); states.Add(new { scheme, kind, measurement = Snapshot(bar), file, disabledInvocationsRejected, sourceRasterParityVerified = false });
                }
                foreach (var accepted in new[] { false, true }) foreach (var action in new[] { "checkbox", "radio", "action", "nested", "deep" })
                {
                    stage = scheme + "-" + action + "-" + accepted; var bar = await Add(); accept = accepted; var original = bar.Menus; Invoke(Trigger(bar, "File")); await Layout();
                    if (action is "nested" or "deep") { Expand(Item("More fictional choices")); await Layout(); }
                    if (action == "deep") { Expand(Item("Deeper fictional choices")); await Layout(); }
                    var name = action switch { "checkbox" => "Show fictional guides", "radio" => "Fictional Beta mode", "nested" => "Nested original action", "deep" => "Deep original action", _ => "New fictional draft" };
                    var button = Item(name); var originalItem = button.DataContext; var originalProvider = (IInvokeProvider)Peer(button).GetPattern(PatternInterface.Invoke); Invoke(button); await Layout(190);
                    Require(events.Count == 1 && Surfaces().Length == 0, "Original action did not invoke exactly once and close.");
                    var retiredInvokeRejected = false;
                    try { originalProvider.Invoke(); } catch (ElementNotEnabledException) { retiredInvokeRejected = true; }
                    Require(retiredInvokeRejected && events.Count == 1, "Retired menu row invoked its original callback again.");
                    Invoke(Trigger(bar, "File")); await Layout();
                    Require(((IToggleProvider)Peer(Item("Show fictional guides")).GetPattern(PatternInterface.Toggle)).ToggleState == (accepted && action == "checkbox" ? ToggleState.On : ToggleState.Off), "Caller checked ownership changed.");
                    Require(((ISelectionItemProvider)Peer(Item("Fictional Beta mode")).GetPattern(PatternInterface.SelectionItem)).IsSelected == (accepted && action == "radio"), "Caller radio ownership changed.");
                    Require(accepted && action is "checkbox" or "radio" || ReferenceEquals(original, bar.Menus), "Refused request replaced caller menus.");
                    contracts.Add(new { scheme, action, accepted, retiredInvokeRejected, originalActionObjectRetained = ReferenceEquals(originalItem, button.DataContext), events = events.ToArray(), reopened = Snapshot(bar), passed = true });
                }
                stage = scheme + "-live-collections";
                var live = await Add(); var originalMenus = new ObservableCollection<BleeckerMenubarMenu>(Build()); live.Menus = originalMenus; await Layout(); var fileTrigger = Trigger(live, "File"); Invoke(fileTrigger); await Layout();
                originalMenus[0] = new("File", [new BleeckerMenubarAction("Updated original label", () => events.Add("updated"))]); await Layout();
                Require(ReferenceEquals(fileTrigger, Trigger(live, "File")) && ReferenceEquals(Item("Updated original label").DataContext, originalMenus[0].Items[0]), "Live entries lost original data or keyed trigger.");
                originalMenus.Insert(0, new("Before", [new BleeckerMenubarAction("Prepended original action", () => events.Add("before"))])); await Layout();
                Require(((IExpandCollapseProvider)Peer(Trigger(live, "Before")).GetPattern(PatternInterface.ExpandCollapse)).ExpandCollapseState == ExpandCollapseState.Expanded, "Active index became key ownership.");
                contracts.Add(new { scheme, action = "live-collections", originalCollectionRetained = ReferenceEquals(live.Menus, originalMenus), originalTriggerRetained = ReferenceEquals(fileTrigger, Trigger(live, "File")), passed = true });
                stage = scheme + "-throwing-callback"; var throwing = await Add(); throwAction = true; Invoke(Trigger(throwing, "File")); await Layout();
                try { Invoke(Item("New fictional draft")); throw new Exception("Original callback error was swallowed."); } catch (InvalidOperationException error) when (error.Message == "fictional menubar callback failed") { }
                Require(events.Count == 1 && Surfaces().Length == 1, "Throwing callback did not retain open root."); contracts.Add(new { scheme, action = "throwing-callback", after = Snapshot(throwing), passed = true });
                stage = scheme + "-disable-unload"; throwing.IsEnabled = false; await Layout(); Require(Surfaces().Length == 0, "Disabled Menubar retained its presentation.");
                throwing.IsEnabled = true; await Layout(); Invoke(Trigger(throwing, "File")); await Layout(); var retainedMenus = throwing.Menus; scene.Children.Clear(); await Layout(180); Require(!throwing.IsLoaded && Surfaces().Length == 0, "Unloaded Menubar retained a surface.");
                scene.Children.Add(throwing); await Layout(); Require(ReferenceEquals(throwing.Menus, retainedMenus) && Surfaces().Length == 0, "Reload reopened retired state or replaced caller data.");
                Invoke(Trigger(throwing, "File")); await Layout(); var priorTemplate = throwing.ReadLocalValue(Control.TemplateProperty);
                throwing.Template = (ControlTemplate)XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='Control'><Border Width='43' Height='29' Background='Purple'/></ControlTemplate>"); await Layout();
                Require(Surfaces().Length == 0 && ReferenceEquals(throwing.Menus, retainedMenus), "Replacement template retained a popup or replaced caller data.");
                if (ReferenceEquals(priorTemplate, DependencyProperty.UnsetValue)) throwing.ClearValue(Control.TemplateProperty); else throwing.SetValue(Control.TemplateProperty, priorTemplate);
                await Layout(); Require(Surfaces().Length == 0 && Trigger(throwing, "File").IsLoaded, "Restoring the default template reopened retired state.");
                contracts.Add(new { scheme, action = "disable-unload-reload", originalMenusRetained = ReferenceEquals(throwing.Menus, retainedMenus), templateRetirementVerified = true, passed = true });

                stage = scheme + "-presence"; var presenceBar = await Add(); Invoke(Trigger(presenceBar, "File")); await Layout(); var originalSurface = Surfaces().Single();
                Invoke(Trigger(presenceBar, "File")); await Task.Delay(50); Guard(); Require(originalSurface.IsLoaded && EffectiveOpacity(originalSurface) < 1, "Source exit presence dropped its original surface too early or never began fading.");
                await Task.Delay(140); Guard(); Require(!originalSurface.IsLoaded && Surfaces().Length == 0, "Source exit presence retained a retired surface.");
                Invoke(Trigger(presenceBar, "File")); await Layout(); var interruptedSurface = Surfaces().Single(); Invoke(Trigger(presenceBar, "File")); await Task.Delay(50); Invoke(Trigger(presenceBar, "File")); await Layout(180);
                Require(ReferenceEquals(interruptedSurface, Surfaces().Single()) && EffectiveOpacity(interruptedSurface) == 1, "Reopening did not cancel the original exit timer.");
                presence.Add(new { scheme, closingOriginalSurfaceRetainedAt50 = true, retiredBy190 = true, reopenCancelledOriginalExit = true, normalMotionVerified = false, passed = true });

                stage = scheme + "-controlled-surface-motion"; var motionBar = await Add(); Invoke(Trigger(motionBar, "File")); await Layout();
                var motionSurface = Surfaces().Single(); var motionRoot = (Grid)motionSurface.Parent; var transform = (CompositeTransform)motionRoot.RenderTransform;
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var rootMenus = (IDictionary)typeof(BleeckerMenubar).GetField("roots", flags)!.GetValue(motionBar)!;
                var rootMenu = rootMenus["File"]!; var level = rootMenu.GetType().GetField("level", flags)!.GetValue(rootMenu)!;
                void Call(string method, params object[] args) => level.GetType().GetMethod(method, flags)!.Invoke(level, args);
                Call("StopSurfaceMotion");
                var carrier = new Border { Opacity = .6 }; var callerTransform = new ScaleTransform { ScaleX = .9, ScaleY = .9 };
                motionSurface.SetBinding(UIElement.OpacityProperty, new Binding { Source = carrier, Path = new PropertyPath("Opacity"), Mode = BindingMode.OneWay });
                var originalBinding = motionSurface.GetBindingExpression(UIElement.OpacityProperty); motionSurface.RenderTransform = callerTransform;
                var histories = new List<object>();
                void Transition(bool visible, double time, bool animate, params double[] times)
                {
                    Call("RetargetSurfaceMotion", visible, time, animate); var samples = new List<object>();
                    foreach (var at in times) { Call("PaintSurfaceMotion", at); Guard(); samples.Add(new { time = at, opacity = motionRoot.Opacity, scaleX = transform.ScaleX, scaleY = transform.ScaleY, translateY = transform.TranslateY }); }
                    histories.Add(new { visible, time, animate, samples });
                }
                Transition(false, 0, false, 0);
                Transition(true, 20, true, 20, 75, 130, 240);
                Transition(false, 300, true, 300, 350);
                Transition(true, 360, true, 360, 380, 420, 580);
                Transition(false, 600, true, 600, 650, 749);
                Transition(false, 800, false, 800);
                Transition(true, 820, false, 820);
                carrier.Opacity = .4; await Layout(0);
                var bindingRetained = ReferenceEquals(originalBinding, motionSurface.GetBindingExpression(UIElement.OpacityProperty)); var transformRetained = ReferenceEquals(motionSurface.RenderTransform, callerTransform);
                surfaceMotion.Add(new { scheme, histories, originalSurfaceRetained = ReferenceEquals(motionSurface, Surfaces().Single()), callerOpacityBindingRetained = bindingRetained, callerTransformRetained = transformRetained, callerOpacity = motionSurface.Opacity, callerSourceOpacity = carrier.Opacity, controlledClock = true, normalRealtimeMotionVerified = false });
                Require(bindingRetained && motionSurface.Opacity == carrier.Opacity && transformRetained, $"Menu motion changed caller state: binding={bindingRetained}, target={motionSurface.Opacity:R}, source={carrier.Opacity:R}, transform={transformRetained}.");

                stage = scheme + "-row-motion";
                foreach (var reduced in new[] { false, true }) foreach (var role in new[] { "trigger", "action", "checkbox", "radio", "submenu" })
                foreach (var kind in role == "trigger" ? new[] { "hover", "leave", "open", "close" } : new[] { "hover", "leave" })
                {
                    var bar = await Add(); var trigger = Trigger(bar, "File");
                    if (role != "trigger" || kind == "close") { Invoke(trigger); await Layout(); }
                    var button = role == "trigger" ? trigger : Item(role switch { "action" => "New fictional draft", "checkbox" => "Show fictional guides", "radio" => "Fictional Alpha mode", _ => "More fictional choices" });
                    var paintFlags = BindingFlags.Instance | BindingFlags.NonPublic;
                    void PaintCall(string name, params object[] args) => button.GetType().GetMethod(name, paintFlags)!.Invoke(button, args);
                    void Hover(bool value) { button.GetType().GetField("hovered", paintFlags)!.SetValue(button, value); button.GetType().GetMethod("UpdateVisualState", paintFlags)!.Invoke(button, null); }
                    object PaintSnapshot()
                    {
                        var chrome = Elements(button).OfType<Border>().Single(e => e.Name == "MenuPaint"); var presenter = Elements(button).OfType<ContentPresenter>().Single(e => e.Name == "Presenter");
                        object Color(Brush paint) { var b = (SolidColorBrush)paint; return new { color = $"#{b.Color.A:X2}{b.Color.R:X2}{b.Color.G:X2}{b.Color.B:X2}", opacity = b.Opacity }; }
                        return new { background = Color(chrome.Background), foreground = Color(presenter.Foreground), rendering = (bool)button.GetType().GetField("rowRendering", paintFlags)!.GetValue(button)! };
                    }
                    // Freeze only the private paint clock; normal UIA scenarios
                    // above still exercise the complete public callback path.
                    PaintCall("UnobserveRowPalette"); PaintCall("StopRowPaint");
                    Hover(kind == "leave"); PaintCall("RetargetRowPaint", 0d, false); PaintCall("StopRowPaint"); var before = PaintSnapshot();
                    if (kind is "open" or "close") Invoke(trigger); else Hover(kind == "hover");
                    PaintCall("StopRowPaint"); PaintCall("RetargetRowPaint", 0d, !reduced); PaintCall("StopRowPaint");
                    var samples = new List<object>();
                    foreach (var milliseconds in reduced ? new[] { 47.5, 95d, 142.5, 190d, 220d } : new[] { 0d, 47.5, 95d, 142.5, 190d, 220d })
                    {
                        PaintCall("PaintRowAt", milliseconds); Guard(); samples.Add(new { milliseconds, paint = PaintSnapshot() });
                        if (!reduced && role == "trigger" && kind == "open" && milliseconds == 95)
                            rowMotionCaptures.Add(new { scheme, file = await Capture("row-motion-" + scheme + "-95") });
                    }
                    rowMotion.Add(new { scheme, reduced, role, kind, before, samples, controlledClock = true, inputRoutingVerified = false });
                    PaintCall("ObserveRowPalette"); PaintCall("RequestRowPaint", false);
                }

                stage = scheme + "-row-paint-ownership"; var ownedBar = await Add(); var ownedButton = Trigger(ownedBar, "File");
                var donor = new TextBlock { Foreground = new SolidColorBrush(Microsoft.UI.Colors.Purple) }; var donorBackground = new Border { Background = new SolidColorBrush(Microsoft.UI.Colors.OrangeRed) { Opacity = .6 } };
                ownedButton.SetBinding(Control.ForegroundProperty, new Binding { Source = donor, Path = new PropertyPath("Foreground"), Mode = BindingMode.OneWay });
                ownedButton.SetBinding(Control.BackgroundProperty, new Binding { Source = donorBackground, Path = new PropertyPath("Background"), Mode = BindingMode.OneWay });
                var fgBinding = ownedButton.GetBindingExpression(Control.ForegroundProperty); var bgBinding = ownedButton.GetBindingExpression(Control.BackgroundProperty); await Layout();
                var ownedChrome = Elements(ownedButton).OfType<Border>().Single(e => e.Name == "MenuPaint");
                ((SolidColorBrush)donorBackground.Background).Opacity = .35; ((SolidColorBrush)donor.Foreground).Color = Microsoft.UI.Colors.Green; await Layout();
                Require(((SolidColorBrush)ownedChrome.Background).Opacity == ((SolidColorBrush)donorBackground.Background).Opacity, "Live caller background opacity did not reach menu paint.");
                Require(((SolidColorBrush)Elements(ownedButton).OfType<ContentPresenter>().Single(e => e.Name == "Presenter").Foreground).Color == ((SolidColorBrush)donor.Foreground).Color, "Live caller foreground color did not reach menu paint.");
                var gradient = new LinearGradientBrush(); gradient.GradientStops.Add(new() { Color = Microsoft.UI.Colors.Red, Offset = 0 }); gradient.GradientStops.Add(new() { Color = Microsoft.UI.Colors.Blue, Offset = 1 }); donorBackground.Background = gradient; await Layout();
                Require(ReferenceEquals(ownedChrome.Background, gradient), "Menu paint replaced the original non-color brush.");
                var originalTemplate = ownedButton.Template;
                ownedButton.Template = (ControlTemplate)XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='Button'><Border Width='61' Height='31' Background='{TemplateBinding Background}'><ContentPresenter Content='{TemplateBinding Content}'/></Border></ControlTemplate>"); await Layout();
                Require(!Elements(ownedButton).Any(e => e.Name == "MenuPaint"), "Caller button template acquired a library paint part.");
                ownedButton.Template = originalTemplate; await Layout();
                Require(ReferenceEquals(ownedButton.GetBindingExpression(Control.ForegroundProperty), fgBinding) && ReferenceEquals(ownedButton.GetBindingExpression(Control.BackgroundProperty), bgBinding), "Menu paint replaced original caller bindings.");
                var paintFlagsOwned = BindingFlags.Instance | BindingFlags.NonPublic; scene.Children.Clear(); await Layout();
                Require(((ICollection)ownedButton.GetType().GetField("rowBrushTokens", paintFlagsOwned)!.GetValue(ownedButton)!).Count == 0 && !(bool)ownedButton.GetType().GetField("rowPaletteObserved", paintFlagsOwned)!.GetValue(ownedButton)! && !(bool)ownedButton.GetType().GetField("rowRendering", paintFlagsOwned)!.GetValue(ownedButton)!, "Unloaded menu paint retained observers or rendering callbacks.");
                scene.Children.Add(ownedBar); await Layout();
                Require(ReferenceEquals(Elements(ownedButton).OfType<Border>().Single(e => e.Name == "MenuPaint").Background, gradient), "Reload lost the original gradient brush.");
                rowPaintOwnership.Add(new { scheme, originalBindingsRetained = true, liveBrushMutationVerified = true, nonColorBrushIdentityRetained = true, callerTemplateRetained = true, reloadVerified = true, unloadedObserversReleased = true, passed = true });

                foreach (var flow in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
                {
                    stage = scheme + "-keyboard-" + flow;
                    var bar = await Add(); bar.FlowDirection = flow; await Layout();
                    var forward = flow == FlowDirection.LeftToRight ? VirtualKey.Right : VirtualKey.Left;
                    var back = flow == FlowDirection.LeftToRight ? VirtualKey.Left : VirtualKey.Right;
                    async Task Key(string id, Button source, VirtualKey key, string? expectedTarget, bool expectedHandled = true)
                    {
                        var result = Navigate(bar, source, key); await Layout();
                        var target = result.Target is null ? null : Peer(result.Target).GetName();
                        Require(result.Handled == expectedHandled && target == expectedTarget, $"Keyboard {id}: expected {expectedHandled}/{expectedTarget}, got {result.Handled}/{target}.");
                        keyboard.Add(new { scheme, flow = flow.ToString(), id, key = key.ToString(), from = Peer(source).GetName(), target, expectedTarget, handled = result.Handled, expectedHandled, callbackCount = events.Count, passed = true });
                    }
                    var file = Trigger(bar, "File"); var edit = Trigger(bar, "Edit");
                    Require(Elements(bar).OfType<StackPanel>().Single(panel => panel.Name == "PART_Triggers").TabFocusNavigation == KeyboardNavigationMode.Once, "Default Menubar is not one Tab navigation group.");
                    await Key("closed-next-root", file, forward, "Edit");
                    await Key("closed-wrap-root", edit, forward, "File");
                    await Key("root-home", edit, VirtualKey.Home, "File");
                    await Key("root-end", file, VirtualKey.End, "Edit");
                    await Key("closed-escape", file, VirtualKey.Escape, null, false);
                    edit.IsEnabled = false; await Key("skip-disabled-root", file, forward, "File"); edit.IsEnabled = true;
                    await Key("open-first", file, VirtualKey.Down, "New fictional draft");
                    await Key("open-next-root", file, forward, "Edit");
                    await Key("open-previous-root", edit, back, "File");
                    await Key("unhandled-letter", Item("New fictional draft"), VirtualKey.N, null, false);
                    await Key("skip-disabled-and-separator", Item("New fictional draft"), VirtualKey.Down, "Show fictional guides");
                    await Key("enter-radio-group", Item("Show fictional guides"), VirtualKey.Down, "Fictional Alpha mode");
                    await Key("radio-group-next", Item("Fictional Alpha mode"), VirtualKey.Down, "Fictional Beta mode");
                    await Key("leave-radio-group", Item("Fictional Beta mode"), VirtualKey.Down, "More fictional choices");
                    await Key("item-home", Item("Show fictional guides"), VirtualKey.Home, "New fictional draft");
                    await Key("wrap-up", Item("New fictional draft"), VirtualKey.Up, "More fictional choices");
                    await Key("wrap-down", Item("More fictional choices"), VirtualKey.Down, "New fictional draft");
                    await Key("item-end", Item("New fictional draft"), VirtualKey.End, "More fictional choices");
                    await Key("disabled-action", Item("Unavailable action"), VirtualKey.Enter, null, false);
                    await Key("disabled-submenu", Item("Unavailable submenu"), forward, null, false);
                    Require(events.Count == 0, "Navigation invoked an original caller action.");
                    await Key("enter-submenu", Item("More fictional choices"), forward, "Nested original action");
                    await Key("submenu-next", Item("Nested original action"), VirtualKey.Down, "Deeper fictional choices");
                    await Key("enter-deep", Item("Deeper fictional choices"), VirtualKey.Enter, "Deep original action");
                    await Key("escape-deep", Item("Deep original action"), VirtualKey.Escape, "Deeper fictional choices");
                    await Key("back-submenu", Item("Deeper fictional choices"), back, "More fictional choices");
                    await Key("escape-root", Item("More fictional choices"), VirtualKey.Escape, "File");
                    Require(Surfaces().Length == 0, "Escape retained the root menu after its exit interval.");
                    await Key("open-last", file, VirtualKey.Up, "More fictional choices");
                    await Key("space-submenu", Item("More fictional choices"), VirtualKey.Space, "Nested original action");
                    await Key("leaf-next-root", Item("Nested original action"), forward, "Edit original action");
                    await Key("leaf-previous-root", Item("Edit original action"), back, "New fictional draft");
                    await Key("tab-exit", Item("New fictional draft"), VirtualKey.Tab, "File", false);
                    Require(Surfaces().Length == 0, "Tab did not close the menu controller.");
                    await Key("enter-root", file, VirtualKey.Enter, "New fictional draft");
                    var retired = Item("New fictional draft");
                    await Key("activate-action", retired, VirtualKey.Enter, "File");
                    await Key("retired-row", retired, VirtualKey.Enter, null, false);
                    Require(events.Count == 1 && Surfaces().Length == 0, "Keyboard action did not invoke exactly once and close.");
                    foreach (var accepted in new[] { false, true }) foreach (var choice in new[] { "checkbox", "radio" })
                    {
                        bar = await Add(); bar.FlowDirection = flow; accept = accepted; file = Trigger(bar, "File");
                        Navigate(bar, file, VirtualKey.Down); await Layout();
                        await Key(choice + "-" + accepted, Item(choice == "checkbox" ? "Show fictional guides" : "Fictional Beta mode"), VirtualKey.Space, "File");
                        Require(events.Count == 1 && checkedValue == (choice == "checkbox" && accepted) && radioValue == (choice == "radio" && accepted ? "b" : "a") && Surfaces().Length == 0, "Keyboard changed original caller selection ownership.");
                    }
                    bar = await Add("empty-menu"); bar.FlowDirection = flow; file = Trigger(bar, "File");
                    await Key("empty-menu", file, VirtualKey.Down, "File");
                    await Key("empty-menu-close", file, VirtualKey.Escape, "File");
                    scene.Children.Clear(); await Layout(); await Key("unloaded-owner", file, VirtualKey.Down, null, false);
                }
                stage = scheme + "-automation-groups";
                var groupBar = await Add(); string firstValue = "a"; var secondOptions = new ObservableCollection<BleeckerMenubarRadioItem> { new("Second X", "x"), new("Second Y", "y") };
                var firstOptions = new[] { new BleeckerMenubarRadioItem("First A", "a"), new BleeckerMenubarRadioItem("First B", "b") };
                BleeckerMenubarMenu[] GroupMenus(bool second = true) => [new("File", second ? [new BleeckerMenubarRadioGroup(firstValue, value => { events.Add("first:" + value); firstValue = value; groupBar.Menus = GroupMenus(); }, firstOptions), new BleeckerMenubarSeparator(), new BleeckerMenubarRadioGroup("x", value => events.Add("second:" + value), secondOptions)] : [new BleeckerMenubarRadioGroup(firstValue, _ => { }, firstOptions)])];
                groupBar.Menus = GroupMenus(); Invoke(Trigger(groupBar, "File")); await Layout();
                AutomationPeer Container(string name) => PeerAccess.Resolve(((ISelectionItemProvider)Peer(Item(name)).GetPattern(PatternInterface.SelectionItem)).SelectionContainer ?? throw new InvalidOperationException("Radio item has no selection container.")) ?? throw new InvalidOperationException("Selection container cannot resolve to its peer.");
                string[] Selected(AutomationPeer peer) => ((ISelectionProvider)peer.GetPattern(PatternInterface.Selection)).GetSelection().Select(provider => PeerAccess.Resolve(provider)?.GetName() ?? throw new InvalidOperationException("Selected provider cannot resolve.")).ToArray();
                void Groups(string id, params AutomationPeer[] groups)
                {
                    foreach (var group in groups) Require(group.GetAutomationControlType() == AutomationControlType.Group && !((ISelectionProvider)group.GetPattern(PatternInterface.Selection)).CanSelectMultiple, "Radio selection container is not a single-selection group.");
                    groupContracts.Add(new { scheme, id, groups = groups.Select(group => new { role = group.GetAutomationControlType().ToString(), names = group.GetChildren()?.Select(peer => peer.GetName()).ToArray() ?? [], selected = Selected(group), enabled = group.IsEnabled() }).ToArray(), callbacks = events.Count, passed = true });
                }
                var firstGroup = Container("First A"); var secondGroup = Container("Second X");
                Require(Selected(firstGroup).SequenceEqual(new[] { "First A" }) && Selected(secondGroup).SequenceEqual(new[] { "Second X" }), "Separate radio groups merged their selections."); Groups("separate", firstGroup, secondGroup);
                ((ISelectionItemProvider)Peer(Item("First B")).GetPattern(PatternInterface.SelectionItem)).Select(); await Layout(190); Invoke(Trigger(groupBar, "File")); await Layout();
                Require(Selected(firstGroup).SequenceEqual(new[] { "First B" }) && Selected(secondGroup).SequenceEqual(new[] { "Second X" }) && events.Count == 1, "Selecting first group changed the second group."); Groups("select-first", firstGroup, secondGroup);
                var originalSecond = Item("Second X"); var originalSecondItem = originalSecond.DataContext; secondOptions.Move(0, 1); await Layout();
                Require(ReferenceEquals(originalSecond, Item("Second X")) && ReferenceEquals(originalSecondItem, Item("Second X").DataContext) && (secondGroup.GetChildren()?.Select(peer => peer.GetName()) ?? []).SequenceEqual(new[] { "Second Y", "Second X" }), "Live group reorder replaced an original row or lost source order."); Groups("reorder", firstGroup, secondGroup);
                groupBar.Menus = GroupMenus(false); await Layout(); var removedRejected = false; try { ((ISelectionItemProvider)Peer(originalSecond).GetPattern(PatternInterface.SelectionItem)).Select(); } catch (ElementNotEnabledException) { removedRejected = true; }
                Require(removedRejected && !secondGroup.IsEnabled() && (secondGroup.GetChildren()?.Count ?? 0) == 0 && Selected(secondGroup).Length == 0 && events.Count == 1, "Removed group retained active provider state."); Groups("removed", firstGroup, secondGroup);
                var firstRow = Item("First B"); ((IExpandCollapseProvider)Peer(Trigger(groupBar, "File")).GetPattern(PatternInterface.ExpandCollapse)).Collapse(); await Layout(50);
                var closingRejected = false; try { Peer(firstRow).SetFocus(); } catch (ElementNotEnabledException) { closingRejected = true; }
                Require(closingRejected && !firstGroup.IsEnabled() && Selected(firstGroup).Length == 0 && !Peer(firstRow).IsKeyboardFocusable() && (Peer(Trigger(groupBar, "File")).GetControlledPeers()?.Count ?? 0) == 0, "Closing menu retained active metadata or focus eligibility."); Groups("closing", firstGroup, secondGroup);
                ((IExpandCollapseProvider)Peer(Trigger(groupBar, "File")).GetPattern(PatternInterface.ExpandCollapse)).Expand(); await Layout();
                Require(Selected(firstGroup).SequenceEqual(new[] { "First B" }) && Peer(Trigger(groupBar, "File")).GetControlledPeers().Single().GetAutomationControlType() == AutomationControlType.Menu, "Reopening did not restore current menu/group metadata."); Groups("reopened", firstGroup);
                stage = scheme + "-standalone-popup"; scene.Children.Clear(); root.Children.Clear(); await Layout(180); throwAction = false; accept = false; checkedValue = false; radioValue = "a"; events.Clear();
                var plain = new BleeckerMenubar { Menus = Build(), Width = 360, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(20, 20, 0, 0) }; current = plain; root.Children.Add(plain); await Layout();
                Invoke(Trigger(plain, "File")); await Layout(); var nativePopup = VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Single(p => p.Child is Canvas canvas && canvas.Name == "MenubarPlane");
                Require(nativePopup.IsOpen && nativePopup.ShouldConstrainToRootBounds && !nativePopup.IsLightDismissEnabled && Surfaces().Length == 1, "Standalone Menubar did not use its scoped native popup.");
                Invoke(Item("Show fictional guides")); await Layout(190); Require(events.Count == 1 && Surfaces().Length == 0, "Standalone selection request did not invoke and dismiss.");
                root.Children.Clear(); await Layout(180); Require(!nativePopup.IsOpen && nativePopup.Child is null && !plain.IsLoaded, "Standalone unload retained native popup ownership.");
                standalone.Add(new { scheme, originalCallbackCount = events.Count, rootConstrained = true, noNativeLightDismiss = true, unloadedPopupRetired = true, sourceRasterParityVerified = false, passed = true });
                root.Children.Add(host); await Layout();
            }
            var catalog = new List<object>();
            if (catalogFactory is not null) foreach (var scheme in new[] { "Light", "Dark" })
            {
                scene.Children.Clear(); root.RequestedTheme = Enum.Parse<ElementTheme>(scheme); var fixture = catalogFactory(); scene.Children.Add(fixture); await Layout(180); var bars = Elements(fixture).OfType<BleeckerMenubar>().ToArray(); Require(bars.Length == 4 && bars.All(b => b.IsLoaded), "Actual catalog Menubars did not load.");
                var demo = bars.Single(b => b.Name == "Default");
                Require(Elements(demo).OfType<Button>().Where(b => b.DataContext is BleeckerMenubarMenu).All(b => ReferenceEquals(b.Style, demo.TriggerStyle)), "Catalog trigger style did not reach its native buttons.");
                Invoke(Trigger(demo, "File")); await Layout(); var rootSurface = Surfaces().Single();
                Require(ReferenceEquals(rootSurface.Style, demo.SurfaceStyle) && Elements(rootSurface).OfType<Button>().Where(b => b.DataContext is BleeckerMenubarItem).All(b => ReferenceEquals(b.Style, demo.ItemStyle)), "Catalog surface/item styles did not reach their native elements.");
                Expand(Item("More fictional choices")); await Layout(); Require(ReferenceEquals(Surfaces().Single(s => s.Name == "MenubarSubmenuSurface").Style, demo.SubmenuSurfaceStyle), "Catalog submenu style did not reach its native surface.");
                catalog.Add(new { scheme, bars = bars.Length, originalStylesApplied = true, file = await Capture("catalog-" + scheme), registeredFixture = fixture.GetType().FullName });
            }
            string Hash(string file) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-menubar-native-v1", passed = true, states, contracts, presence, surfaceMotion, rowMotion, rowMotionCaptures, rowPaintOwnership, keyboard, radioSelection, peerTrees, groupContracts, standalone, catalog, pngs, guards, focus, activations, activateCalled = false, showWindowCalled = false, focusCalled = false, hardwareInputSent = false, physicalInputVerified = false, sourceRasterParityVerified = false, nativeComponentAccepted = false, publicApisOnly = false, interactionScenariosUsePublicApis = false, selectionScenariosUsePublicApis = true, keyboardControllerUsesPrivateMethod = true, keyboardRoutedEventsVerified = false, keyboardFocusMovementVerified = false, controlledClockUsesPrivateRenderer = true, librarySha256 = Hash(typeof(BleeckerMenubar).Assembly.Location), hostSha256 = Hash(hostAssembly) }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(0);
        }
        catch (Exception error) { await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-menubar-native-v1", passed = false, stage, error = error.ToString(), states, contracts, presence, surfaceMotion, rowMotion, rowMotionCaptures, rowPaintOwnership, keyboard, radioSelection, peerTrees, groupContracts, standalone, pngs, guards, focus, activations }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(1); }
    }
}
