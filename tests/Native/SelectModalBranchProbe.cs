using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;

namespace Orchard.Testing;

internal static class SelectModalBranchProbe
{
    private delegate bool WindowVisitor(nint window, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(WindowVisitor visitor, nint parameter);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint window);
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node)
    {
        if (node is FrameworkElement element) yield return element;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) foreach (var child in Elements(VisualTreeHelper.GetChild(node, i))) yield return child;
    }
    private sealed class LegacyTable : IBleeckerDataTableFilterSource, IBleeckerDataTableFilterColumn, IBleeckerDataTablePaginationSource
    {
        public object? FilterValue { get; private set; } = "a";
        public List<object?> FilterRequests { get; } = []; public List<int> PageRequests { get; } = [];
        public event EventHandler? Changed;
        public int ColumnFilterCount => FilterValue is null ? 0 : 1; public object? GlobalFilter => null;
        public IBleeckerDataTableFilterColumn? GetColumn(string id) => id == "library" ? this : null;
        public object? GetFilterValue() => FilterValue;
        public void SetFilterValue(object? value) { FilterRequests.Add(value); }
        public void ResetColumnFilters() => SetFilterValue(null);
        public int PageIndex { get; private set; } public int PageSize { get; private set; } = 10;
        public int PageCount => 10; public int FilteredRowCount => 100; public int FilteredSelectedRowCount => 0;
        public bool CanPreviousPage => PageIndex > 0; public bool CanNextPage => PageIndex < PageCount - 1;
        public void SetPageIndex(int value) { PageIndex = value; Changed?.Invoke(this, EventArgs.Empty); }
        public void SetPageSize(int value) { PageRequests.Add(value); }
        public void PreviousPage() => SetPageIndex(PageIndex - 1); public void NextPage() => SetPageIndex(PageIndex + 1);
    }
    internal static async Task RunAsync(Window window, string reference, string output, string hostPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(output)!); var sourceBytes = await File.ReadAllBytesAsync(reference); using var source = JsonDocument.Parse(sourceBytes);
        var root = (Grid)XamlReader.Load("<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{ThemeResource BleeckerBackgroundBrush}'/>"); window.Content = root; window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(900, 760));
        var guards = 0; var focus = 0; var activations = 0; var invokes = 0; var frames = new List<object>(); var cases = new List<object>(); var compositionCases = new List<object>(); var failures = new List<object>(); var captures = new List<object>();
        window.Activated += (_, a) => { if (a.WindowActivationState != WindowActivationState.Deactivated) activations++; }; root.GotFocus += (_, _) => focus++;
        void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        void Guard()
        {
            guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var visible = false; EnumWindows((handle, _) => { GetWindowThreadProcessId(handle, out var owner); visible |= owner == Environment.ProcessId && IsWindowVisible(handle); return true; }, 0);
            Require(!visible && !window.Visible && !window.AppWindow.IsVisible && foreground != Environment.ProcessId && activations == 0 && focus == 0, "Select probe acquired visibility, activation or focus.");
            Require(VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Count == 0, "Hosted Select opened a native Popup input site.");
        }
        async Task Layout()
        {
            for (var i = 0; i < 6; i++) { var idle = new TaskCompletionSource(); Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => idle.SetResult()), "Select layout could not drain."); await idle.Task; await Task.Delay(12); root.Measure(new Size(900, 760)); root.Arrange(new Rect(0, 0, 900, 760)); root.UpdateLayout(); Guard(); }
            Require(root.XamlRoot is { IsHostVisible: false, RasterizationScale: 1 } && root.XamlRoot.Size == new Size(900, 760), "Select probe requires its actual hidden unscaled viewport.");
        }
        object Box(FrameworkElement e) { var b = e.TransformToVisual(root).TransformBounds(new Rect(0, 0, e.ActualWidth, e.ActualHeight)); return new { x = b.X, y = b.Y, width = b.Width, height = b.Height }; }
        async Task Capture(string file)
        {
            var bitmap = new RenderTargetBitmap(); await bitmap.RenderAsync(root); Guard(); Require(bitmap.PixelWidth == 900 && bitmap.PixelHeight == 760, "Select capture was rescaled."); var buffer = await bitmap.GetPixelsAsync(); var pixels = new byte[buffer.Length]; using (var reader = Windows.Storage.Streams.DataReader.FromBuffer(buffer)) reader.ReadBytes(pixels);
            using var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream(); var encoder = await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId, stream); encoder.SetPixelData(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied, 900, 760, 96, 96, pixels); await encoder.FlushAsync(); using var pngReader = new Windows.Storage.Streams.DataReader(stream.GetInputStreamAt(0)); await pngReader.LoadAsync((uint)stream.Size); var bytes = new byte[stream.Size]; pngReader.ReadBytes(bytes); await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!, file), bytes); captures.Add(new { file, sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)), width = 900, height = 760 });
        }
        IExpandCollapseProvider Peer(BleeckerSelectSurface e) => (IExpandCollapseProvider)FrameworkElementAutomationPeer.CreatePeerForElement(e).GetPattern(PatternInterface.ExpandCollapse);
        foreach (var row in source.RootElement.GetProperty("openCases").EnumerateArray())
        {
            var scheme = row.GetProperty("scheme").GetString()!; var spec = row.GetProperty("spec"); var id = row.GetProperty("id").GetString()!; File.WriteAllText(Path.Combine(Path.GetDirectoryName(output)!, "stage.json"), JsonSerializer.Serialize(new { scheme, id, guards }));
            root.RequestedTheme = Enum.Parse<ElementTheme>(scheme); var choices = spec.GetProperty("options").EnumerateArray().Select(o => new BleeckerSelectOption { Value = o.GetProperty("value").GetString()!, Label = o.GetProperty("label").GetString()!, IsDisabled = o.TryGetProperty("disabled", out var disabled) && disabled.GetBoolean() }).ToArray();
            var header = spec.GetProperty("header").GetBoolean(); BleeckerSelect control = header ? new BleeckerHeaderSelect() : new BleeckerSelect(); control.Options = choices; control.Value = spec.GetProperty("value").GetString()!; control.Size = spec.GetProperty("size").GetString() switch { "sm" => BleeckerControlSize.Small, "lg" => BleeckerControlSize.Large, _ => BleeckerControlSize.Medium }; if (!header) control.Width = spec.GetProperty("width").GetDouble(); control.Margin = new(20, spec.GetProperty("top").GetDouble(), 0, 0); control.HorizontalAlignment = HorizontalAlignment.Left; control.VerticalAlignment = VerticalAlignment.Top;
            var host = new BleeckerModalHost { Content = control }; root.Children.Add(host); await Layout(); var surface = Elements(control).OfType<BleeckerSelectSurface>().Single(); Peer(surface).Expand(); await Layout();
            var popup = Elements(root).OfType<Border>().Single(b => b.Name == "PopupBorder"); var items = surface.Items.OfType<BleeckerSelectItem>().ToArray();
            var diagnostic = new { surface.IsDropDownOpen, surface.IsLoaded, surface.IsEnabled, rootAvailable = surface.XamlRoot is not null, hostFound = ReferenceEquals(BleeckerModalHost.Find(surface), host), popupLoaded = popup.IsLoaded, popupParent = VisualTreeHelper.GetParent(popup)?.GetType().Name, items = items.Select(i => new { i.IsLoaded, i.IsEnabled, parent = VisualTreeHelper.GetParent(i)?.GetType().Name }).ToArray(), presenters = Elements(root).OfType<ItemsPresenter>().Select(p => new { p.IsLoaded, p.ActualWidth, p.ActualHeight, children = VisualTreeHelper.GetChildrenCount(p) }).ToArray(), visualParts = Elements(root).Where(e => e.Name is "PART_Listbox" or "ScrollViewer" or "PopupSurface").Select(e => new { e.Name, e.IsLoaded, e.ActualWidth, e.ActualHeight }).ToArray() };
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(output)!, "mount-" + frames.Count + ".json"), JsonSerializer.Serialize(diagnostic));
            Require(surface.IsDropDownOpen && popup.IsLoaded && items.All(i => i.IsLoaded), "Select popup/options did not mount in the shared root: " + JsonSerializer.Serialize(diagnostic));
            frames.Add(new { id, scheme, trigger = Box(surface), popup = Box(popup), items = items.Select(i => new { box = Box(i), text = i.Content?.ToString(), selected = surface.SelectionMatches(i), disabled = !i.IsEnabled }).ToArray(), expanded = Peer(surface).ExpandCollapseState.ToString(), nativePopupCount = 0, originalOptionsRetained = ReferenceEquals(control.Options, choices) });
            if (frames.Count is 1 or 16 or 17 or 32) await Capture("select-open-" + frames.Count + ".png");
            Peer(surface).Collapse(); await Layout(); root.Children.Clear(); await Layout();
        }
        foreach (var scheme in new[] { "Light", "Dark" }) foreach (var recipe in new[] { "select", "header", "filter", "pagination" })
        {
            root.RequestedTheme = Enum.Parse<ElementTheme>(scheme); var choices = new ObservableCollection<BleeckerSelectOption> { new() { Value = "a", Label = "Fictional Alpha" }, new() { Value = "b", Label = "Fictional Beta" }, new() { Value = "disabled", Label = "Fictional disabled", IsDisabled = true } }; var table = new LegacyTable(); var value = new TextBox { Text = "a" }; var enabled = new ToggleSwitch { IsOn = true }; var requests = new List<BleeckerSelectRequestedEventArgs>();
            Control control;
            if (recipe is "select" or "header") { var select = recipe == "header" ? new BleeckerHeaderSelect() : new BleeckerSelect(); select.Options = choices; select.SetBinding(BleeckerSelect.ValueProperty, new Binding { Source = value, Path = new(nameof(TextBox.Text)) }); select.ValueRequested += (_, e) => requests.Add(e); control = select; }
            else if (recipe == "filter") control = new BleeckerDataTableSelectFilter { Table = table, ColumnId = "library", Options = choices };
            else control = new BleeckerDataTablePagination { Table = table, PageSizeOptions = new ObservableCollection<int> { 10, 20, 30 } };
            control.SetBinding(Control.IsEnabledProperty, new Binding { Source = enabled, Path = new(nameof(ToggleSwitch.IsOn)) }); control.HorizontalAlignment = HorizontalAlignment.Left; control.VerticalAlignment = VerticalAlignment.Top; control.Margin = new(24, 30, 0, 0); if (recipe != "header") control.Width = recipe == "pagination" ? 780 : 240;
            var background = new Grid(); var declaration = new Border(); background.Children.Add(declaration); var original = new Button { Content = "Fictional outside action" }; background.Children.Add(original); var host = new BleeckerModalHost { Content = background }; root.Children.Add(host); await Layout();
            var parentBody = new Grid(); parentBody.Children.Add(control); var childDeclaration = new Border(); parentBody.Children.Add(childDeclaration); var parentSurface = new ContentControl { Content = parentBody, Width = 850, Height = 500 }; var parentOverlay = new Grid(); parentOverlay.Children.Add(parentSurface); var parent = host.Attach(declaration, parentSurface, parentOverlay, () => { }); host.Activate(parent); await Layout(); var chooser = Elements(control).OfType<BleeckerSelectSurface>().Single(); var peer = Peer(chooser); peer.Expand(); await Layout();
            async Task Case(string name, Func<Task<object>> action)
            {
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(output)!, "stage.json"), JsonSerializer.Serialize(new { scheme, recipe, name, guards }));
                try { var e = await action(); Guard(); cases.Add(new { scheme, recipe, name, passed = true, evidence = e }); } catch (Exception error) { failures.Add(new { scheme, recipe, name, error = error.ToString() }); cases.Add(new { scheme, recipe, name, passed = false }); File.WriteAllText(Path.Combine(Path.GetDirectoryName(output)!, "failures-in-progress.json"), JsonSerializer.Serialize(failures)); }
            }
            await Case("nearest-modal-and-outside-shield", () =>
            {
                Require(parent.Layer.Branches.Count == 1 && parent.Layer.Branches[0].Root is Canvas canvas && canvas.ActualWidth == 900 && canvas.ActualHeight == 760 && parent.Layer.Branches[0].Frame.IsHitTestVisible, "Owned Select has no full-viewport private shield.");
                var item = chooser.Items.OfType<BleeckerSelectItem>().First(); var hit = VisualTreeHelper.FindElementsInHostCoordinates(new Point(890, 750), root).FirstOrDefault(); Require(ReferenceEquals(hit, parent.Layer.Branches[0].Root) && host.AllowsFocusTarget(item) && host.SequentialTargets().Contains(item) && control.GetBindingExpression(Control.IsEnabledProperty) is not null, "Select shield/ownership did not reach the native hit query/targets."); return Task.FromResult<object>(new { branches = 1, fullViewportShield = true, outsideHitIsShield = true, ownedSequentialTarget = true, callerBindingRetained = true });
            });
            await Case("live-options-retain-original-rows-and-portal", async () =>
            {
                var popup = Elements(root).OfType<Border>().Single(b => b.Name == "PopupBorder"); var originalRow = chooser.Items.OfType<BleeckerSelectItem>().First(i => recipe == "pagination" ? (int)i.Tag == 20 : i.Tag is BleeckerSelectOption o && ReferenceEquals(o, choices[1]));
                if (recipe == "pagination")
                {
                    var sizes = (ObservableCollection<int>)((BleeckerDataTablePagination)control).PageSizeOptions; sizes.Insert(0, 5); sizes.Move(1, 3); await Layout(); Require(chooser.Items.OfType<BleeckerSelectItem>().Select(i => (int)i.Tag).SequenceEqual(new[] { 5, 20, 30, 10 }), "Open page-size producer order was not rendered.");
                }
                else
                {
                    var beta = choices[1]; choices[0].Label = "Renamed fictional Alpha"; choices.Add(new("c", "Fictional Gamma")); choices.Move(1, 2); await Layout(); Require(chooser.Items.Contains(originalRow) && ReferenceEquals(originalRow.Tag, beta) && originalRow.IsLoaded && originalRow.Content?.ToString() == beta.Label, "Open choices replaced the original keyed row."); choices.Move(2, 1); await Layout();
                    var display = (IValueProvider)FrameworkElementAutomationPeer.CreatePeerForElement(chooser).GetPattern(PatternInterface.Value); Require(display.Value == "Renamed fictional Alpha", "Open trigger did not render the original producer's label update.");
                }
                Require(chooser.IsDropDownOpen && chooser.Items.Contains(originalRow) && originalRow.IsLoaded && ReferenceEquals(popup, Elements(root).OfType<Border>().Single(b => b.Name == "PopupBorder")) && requests.Count == 0 && table.FilterRequests.Count == 0 && table.PageRequests.Count == 0, "Live update remounted the portal, lost rows or emitted a request.");
                return new { originalRowRetained = true, producerOrderAndLabelRendered = true, portalRetained = true, noSelectionRequest = true };
            });
            var childSurface = new ContentControl { Content = new Button { Content = "Child fictional action" }, Width = 300, Height = 200 }; var childOverlay = new Grid(); childOverlay.Children.Add(childSurface); var child = host.Attach(childDeclaration, childSurface, childOverlay, () => { });
            await Case("child-modal-inerts-open-menu-and-stale-action", async () =>
            {
                var item = chooser.Items.OfType<BleeckerSelectItem>().Last(i => i.IsEnabled); chooser.QueueCommit(item); host.Activate(child); await Layout(); Require(chooser.IsDropDownOpen && parent.Layer.Branches.All(b => !b.Frame.IsHitTestVisible) && !host.AllowsFocusTarget(item) && requests.Count == 0 && table.FilterRequests.Count == 0 && table.PageRequests.Count == 0, "Inactive menu consumed its queued selection or escaped its parent modal."); return new { retainedOpen = true, allBlocked = true, staleRequestIgnored = true, originalEnabled = control.IsEnabled };
            });
            await Case("resume-and-software-native-selection", async () =>
            {
                host.Deactivate(child); child.Dispose(); await Layout(); Require(parent.Layer.Branches.All(b => b.Frame.IsHitTestVisible) && chooser.IsDropDownOpen, "Menu did not resume after the child modal.");
                var option = chooser.Items.OfType<BleeckerSelectItem>().First(i => recipe == "pagination" ? (int)i.Tag == 20 : i.Tag is BleeckerSelectOption o && ReferenceEquals(o, choices[1]));
                var invoke = FrameworkElementAutomationPeer.CreatePeerForElement(option).GetPattern(PatternInterface.Invoke) as IInvokeProvider ?? throw new InvalidOperationException("Original Select option has no native invoke provider."); invoke.Invoke(); invokes++; await Layout();
                Require(!chooser.IsDropDownOpen && parent.Layer.Branches.Count == 0 && control.GetBindingExpression(Control.IsEnabledProperty) is not null, "Native option invocation did not close/release its menu.");
                if (recipe is "select" or "header") Require(requests.Count == 1 && ReferenceEquals(requests[0].Option, choices[1]) && requests[0].Value == "b" && ((BleeckerSelect)control).Value == "a" && ((BleeckerSelect)control).GetBindingExpression(BleeckerSelect.ValueProperty) is not null, "Native request lost the original option or rewrote controlled value.");
                else if (recipe == "filter") Require(table.FilterRequests.Count == 1 && Equals(table.FilterRequests[0], "b") && Equals(table.FilterValue, "a"), "Filter request rewrote its original column.");
                else Require(table.PageRequests.SequenceEqual(new[] { 20 }) && table.PageSize == 10, "Page-size request rewrote its original table.");
                return new { nativeInvokes = 1, originalProducerRequest = true, controlledValueRetained = true, menuReleased = true };
            });
            await Case("disable-template-and-unload-release", async () =>
            {
                peer.Expand(); await Layout(); enabled.IsOn = false; await Layout(); Require(!chooser.IsDropDownOpen && parent.Layer.Branches.Count == 0 && !control.IsEnabled && control.GetBindingExpression(Control.IsEnabledProperty) is not null, "Disabling rewrote a binding or retained the menu."); enabled.IsOn = true; await Layout(); peer.Expand(); await Layout();
                var template = control.Template; control.Template = null; await Layout(); Require(parent.Layer.Branches.Count == 0 && !chooser.IsDropDownOpen, "Template replacement retained the old menu."); control.Template = template; await Layout(); chooser = Elements(control).OfType<BleeckerSelectSurface>().Single(); peer = Peer(chooser); peer.Expand(); await Layout();
                parentBody.Children.Remove(control); await Layout(); Require(parent.Layer.Branches.Count == 0 && !chooser.IsDropDownOpen, "Unloading retained a menu branch."); parentBody.Children.Add(control); await Layout(); chooser = Elements(control).OfType<BleeckerSelectSurface>().Single(); peer = Peer(chooser); peer.Expand(); await Layout(); Require(chooser.IsDropDownOpen && parent.Layer.Branches.Count == 1, "Remount did not restore the original recipe."); peer.Collapse(); await Layout(); return new { disabledReleased = true, bindingRetained = true, templateReleased = true, unloadedReleased = true, remountOpened = true };
            });
            parent.Dispose(); root.Children.Clear(); await Layout();
        }
        foreach (var scheme in new[] { "Light", "Dark" })
        {
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(output)!, "stage.json"), JsonSerializer.Serialize(new { scheme, name = "catalog-nested-select-scope", guards })); root.RequestedTheme = Enum.Parse<ElementTheme>(scheme);
            var fixture = new Orchard.Catalog.SelectsFixture(); var host = new BleeckerModalHost { Content = fixture }; root.Children.Add(host); await Layout(); var popover = Elements(root).OfType<BleeckerPopover>().Single(p => p.Name == "InPopover"); var popoverRequests = 0; popover.OpenRequested += (_, _) => popoverRequests++; popover.Open = true; await Layout();
            var nested = Elements(root).OfType<BleeckerSelect>().Single(p => p.Name == "Nested"); var chooser = Elements(nested).OfType<BleeckerSelectSurface>().Single(); Peer(chooser).Expand(); await Layout(); var target = chooser.Items.OfType<BleeckerSelectItem>().Single(i => i.Tag is BleeckerSelectOption o && o.Value == "b");
            var canvas = Elements(root).OfType<Canvas>().Single(c => c.Width == 900 && c.Height == 760); var outside = Elements(fixture).OfType<BleeckerSelect>().First(s => s.Name == "Medium");
            Require(BleeckerPopupScope.Contains(root.XamlRoot, (DependencyObject)popover.Content, target) && BleeckerPopupScope.Contains(root.XamlRoot, (DependencyObject)popover.Content, canvas) && !BleeckerPopupScope.AllowsInput(root.XamlRoot, outside), "The Select portal escaped its parent popup's logical containment or focus scope.");
            var confirmation = Elements(root).OfType<BleeckerAlertDialog>().Single(a => a.Title == "Fictional nested confirmation"); confirmation.IsOpen = true; await Task.Delay(260); await Layout(); Require(chooser.IsDropDownOpen && host.ActiveSurface is not null && BleeckerPopupScope.AllowsInput(root.XamlRoot, host.ActiveSurface) && !host.AllowsFocusTarget(target), "Inactive Select trap blocked the newer modal or lost its menu."); confirmation.IsOpen = false; await Task.Delay(180); await Layout(); Require(!BleeckerPopupScope.AllowsInput(root.XamlRoot, outside) && chooser.IsDropDownOpen, "Select focus scope did not resume after modal close.");
            var invoke = FrameworkElementAutomationPeer.CreatePeerForElement(target).GetPattern(PatternInterface.Invoke) as IInvokeProvider ?? throw new InvalidOperationException("Nested option has no native invoke provider."); invoke.Invoke(); invokes++; await Layout(); Require(nested.Value == "b" && !chooser.IsDropDownOpen && popover.IsOpen && popoverRequests == 0 && BleeckerPopupScope.AllowsInput(root.XamlRoot, outside), "The actual catalog request failed or dismissed its original parent popup.");
            await Capture("catalog-nested-" + scheme.ToLowerInvariant() + ".png"); compositionCases.Add(new { scheme, name = "catalog-nested-select-scope", passed = true, evidence = new { logicalPortalContainment = true, outsideTrapActive = true, newerModalAllowed = true, olderTrapResumed = true, originalChoiceAccepted = true, parentPopupPreserved = true, trapReleased = true, nativeInvokes = 1 } }); root.Children.Clear(); await Layout();
        }
        string Hash(string p) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(p)));
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-select-modal-branch-v1", passed = failures.Count == 0, frames, cases, compositionCases, captures, failures, guards, focus, activations, softwareNativeInvokes = invokes, referenceSha256 = Convert.ToHexStringLower(SHA256.HashData(sourceBytes)), librarySha256 = Hash(typeof(BleeckerSelect).Assembly.Location), hostSha256 = Hash(hostPath), architecture = RuntimeInformation.ProcessArchitecture.ToString(), activateCalled = false, showCalled = false, focusCalled = false, hardwareInputSent = false, physicalInputVerified = false, sourceInitialFocusPaintVerified = false, sourceRasterParityVerified = false }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(failures.Count == 0 ? 0 : 1);
    }
}
