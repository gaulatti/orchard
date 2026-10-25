using System.Collections.ObjectModel;
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
using Orchard;
using Windows.Foundation;

namespace PackageConsumer;

// This consumer uses public component/native APIs only. It never focuses,
// activates or shows a window, sends hardware input, or opens a native Popup.
internal static class SelectModalRootFixture
{
    private delegate bool WindowVisitor(nint window, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(WindowVisitor visitor, nint parameter);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint window);
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node) { if (node is FrameworkElement element) yield return element; for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) foreach (var child in Elements(VisualTreeHelper.GetChild(node, i))) yield return child; }
    private sealed class Table : IBleeckerDataTableFilterSource, IBleeckerDataTableFilterColumn, IBleeckerDataTablePaginationSource
    {
        public List<object?> Filters { get; } = []; public List<int> Sizes { get; } = [];
        public event EventHandler? Changed;
        public int ColumnFilterCount => 1; public object? GlobalFilter => null; public IBleeckerDataTableFilterColumn? GetColumn(string id) => id == "library" ? this : null; public object? GetFilterValue() => "a";
        public void SetFilterValue(object? value) => Filters.Add(value); public void ResetColumnFilters() => SetFilterValue(null);
        public int PageIndex { get; private set; } public int PageSize => 10; public int PageCount => 10; public int FilteredRowCount => 100; public int FilteredSelectedRowCount => 0; public bool CanPreviousPage => PageIndex > 0; public bool CanNextPage => PageIndex < 9;
        public void SetPageIndex(int value) { PageIndex = value; Changed?.Invoke(this, EventArgs.Empty); } public void SetPageSize(int value) => Sizes.Add(value); public void PreviousPage() => SetPageIndex(PageIndex - 1); public void NextPage() => SetPageIndex(PageIndex + 1);
    }
    internal static async Task RunAsync(Window window, string output)
    {
        var root = (Grid)XamlReader.Load("<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{ThemeResource BleeckerBackgroundBrush}'/>"); window.Content = root; window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(900, 760));
        var guards = 0; var focus = 0; var activations = 0; var invokes = 0; var cases = new List<object>(); window.Activated += (_, a) => { if (a.WindowActivationState != WindowActivationState.Deactivated) activations++; }; root.GotFocus += (_, _) => focus++;
        void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        void Guard()
        {
            guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var visible = false; EnumWindows((handle, _) => { GetWindowThreadProcessId(handle, out var owner); visible |= owner == Environment.ProcessId && IsWindowVisible(handle); return true; }, 0);
            Require(!visible && !window.Visible && !window.AppWindow.IsVisible && foreground != Environment.ProcessId && activations == 0 && focus == 0, "Public Select consumer acquired visibility, activation or focus."); Require(VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Count == 0, "Public Select consumer opened a native Popup input site.");
        }
        async Task Layout() { for (var i = 0; i < 6; i++) { var idle = new TaskCompletionSource(); Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => idle.SetResult()), "Consumer Select layout could not drain."); await idle.Task; await Task.Delay(12); root.Measure(new Size(900, 760)); root.Arrange(new Rect(0, 0, 900, 760)); root.UpdateLayout(); Guard(); } Require(root.XamlRoot is { IsHostVisible: false, RasterizationScale: 1 } && root.XamlRoot.Size == new Size(900, 760), "Consumer Select requires an actual hidden unscaled viewport."); }
        foreach (var scheme in new[] { "Light", "Dark" }) foreach (var recipe in new[] { "select", "header", "filter", "pagination" })
        {
            root.RequestedTheme = Enum.Parse<ElementTheme>(scheme); var choices = new ObservableCollection<BleeckerSelectOption> { new("a", "Fictional Alpha"), new("b", "Fictional Beta"), new("disabled", "Fictional disabled", true) }; var table = new Table(); var value = new TextBox { Text = "a" }; var enabled = new ToggleSwitch { IsOn = true }; var requests = new List<BleeckerSelectRequestedEventArgs>(); Control control;
            if (recipe is "select" or "header") { var select = recipe == "header" ? new BleeckerHeaderSelect() : new BleeckerSelect(); select.Options = choices; select.SetBinding(BleeckerSelect.ValueProperty, new Binding { Source = value, Path = new(nameof(TextBox.Text)) }); select.ValueRequested += (_, e) => requests.Add(e); control = select; }
            else if (recipe == "filter") control = new BleeckerDataTableSelectFilter { Table = table, ColumnId = "library", Options = choices }; else control = new BleeckerDataTablePagination { Table = table, PageSizeOptions = new ObservableCollection<int> { 10, 20, 30 } };
            control.SetBinding(Control.IsEnabledProperty, new Binding { Source = enabled, Path = new(nameof(ToggleSwitch.IsOn)) }); if (recipe != "header") control.Width = recipe == "pagination" ? 780 : 240;
            var dialog = new BleeckerAlertDialog { Title = "Fictional package menu confirmation", Description = "Original choice producers remain caller-owned." }; dialog.CancelRequested += (_, _) => dialog.IsOpen = false;
            var body = new StackPanel { Width = recipe == "pagination" ? 800 : 260, Spacing = 12 }; body.Children.Add(control); body.Children.Add(dialog);
            var popover = new BleeckerPopover { Trigger = new Button { Content = "Fictional package choice panel" }, Content = body, Open = true }; var parentRequests = 0; popover.OpenRequested += (_, _) => parentRequests++; var host = new BleeckerModalHost { Content = popover }; root.Children.Add(host); await Layout();
            var chooser = Elements(control).OfType<ItemsControl>().Single(c => FrameworkElementAutomationPeer.CreatePeerForElement(c)?.GetAutomationControlType() == AutomationControlType.ComboBox); var peer = (IExpandCollapseProvider)FrameworkElementAutomationPeer.CreatePeerForElement(chooser).GetPattern(PatternInterface.ExpandCollapse); peer.Expand(); await Layout();
            var popup = Elements(root).OfType<Border>().Single(b => b.Name == "PopupBorder"); Require(popup.IsLoaded && VisualTreeHelper.GetParent(popup) is Canvas && peer.ExpandCollapseState == ExpandCollapseState.Expanded, "Public package Select did not mount through the shared plane."); var target = chooser.Items.OfType<Button>().First(i => recipe == "pagination" ? (int)i.Tag == 20 : i.Tag is BleeckerSelectOption option && ReferenceEquals(option, choices[1])); Require(target.IsLoaded, "Public package option was not rendered.");
            var canvas = (Canvas)VisualTreeHelper.GetParent(popup); var frame = (Border)VisualTreeHelper.GetParent(canvas); var invoke = FrameworkElementAutomationPeer.CreatePeerForElement(target).GetPattern(PatternInterface.Invoke) as IInvokeProvider ?? throw new InvalidOperationException("Public option has no native invoke provider.");
            dialog.IsOpen = true; await Task.Delay(260); await Layout(); Require(!frame.IsHitTestVisible && peer.ExpandCollapseState == ExpandCollapseState.Expanded && control.IsEnabled, "Newer modal did not inert the original open menu."); invoke.Invoke(); invokes++; await Layout(); Require(requests.Count == 0 && table.Filters.Count == 0 && table.Sizes.Count == 0, "Inactive option emitted a producer request."); dialog.Cancel(); await Task.Delay(180); await Layout(); Require(frame.IsHitTestVisible && peer.ExpandCollapseState == ExpandCollapseState.Expanded, "Menu did not resume after the public confirmation.");
            invoke.Invoke(); invokes++; await Layout(); Require(peer.ExpandCollapseState == ExpandCollapseState.Collapsed && popover.IsOpen && parentRequests == 0 && control.GetBindingExpression(Control.IsEnabledProperty) is not null, "Native option invocation retained its menu or dismissed the parent popup.");
            if (recipe is "select" or "header") Require(requests.Count == 1 && requests[0].Value == "b" && ReferenceEquals(requests[0].Option, choices[1]) && ((BleeckerSelect)control).Value == "a" && ((BleeckerSelect)control).GetBindingExpression(BleeckerSelect.ValueProperty) is not null, "Public selection lost its original option/binding or rewrote the controlled value."); else if (recipe == "filter") Require(table.Filters.Count == 1 && Equals(table.Filters[0], "b") && Equals(table.GetFilterValue(), "a"), "Public filter rewrote its original column."); else Require(table.Sizes.SequenceEqual(new[] { 20 }) && table.PageSize == 10, "Public page-size chooser rewrote its original table.");
            peer.Expand(); await Layout(); enabled.IsOn = false; await Layout(); Require(peer.ExpandCollapseState == ExpandCollapseState.Collapsed && !control.IsEnabled, "Disabling retained the package menu."); enabled.IsOn = true; await Layout(); peer.Expand(); await Layout(); root.Children.Remove(host); await Layout(); Require(!target.IsLoaded && peer.ExpandCollapseState == ExpandCollapseState.Collapsed && popover.IsOpen, "Package host unload retained an option or rewrote caller open state.");
            cases.Add(new { scheme, recipe, passed = true, evidence = new { sharedPortalMounted = true, inactiveMenuBlocked = true, inactiveInvokeIgnored = true, menuResumed = true, originalProducerRequest = true, controlledValueRetained = true, callerBindingsRetained = true, parentPopupPreserved = true, disableReleasedMenu = true, unloadReleasedMenu = true, nativeInvokes = 2 } });
        }
        string Hash(string p) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(p)));
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-package-select-modal-root-v1", passed = true, cases, guards, focus, activations, softwareNativeInvokes = invokes, publicApisOnly = true, librarySha256 = Hash(typeof(BleeckerSelect).Assembly.Location), hostSha256 = Hash(typeof(App).Assembly.Location), architecture = RuntimeInformation.ProcessArchitecture.ToString(), activateCalled = false, showCalled = false, focusCalled = false, hardwareInputSent = false, physicalInputVerified = false, sourceRasterParityVerified = false }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(0);
    }
}
