using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Testing;

// SDK-boundary investigation only. Transient ShowAt deliberately takes no
// focus. The fixture owns an unactivated hidden window and touches no OS state.
internal static class MenuPortalPrimitiveProbe
{
    private sealed class MarkerMenu : MenuFlyout
    {
        internal MenuFlyoutPresenter? OriginalPresenter;
        internal MarkerMenu()
        {
            var style = new Style(typeof(MenuFlyoutPresenter));
            style.Setters.Add(new Setter(Control.TemplateProperty, (ControlTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='MenuFlyoutPresenter'><Grid><ItemsPresenter/></Grid></ControlTemplate>")));
            MenuFlyoutPresenterStyle = style;
        }
        protected override Control CreatePresenter()
        {
            OriginalPresenter = (MenuFlyoutPresenter)base.CreatePresenter();
            return OriginalPresenter;
        }
    }
    private sealed class ProbeCommand(Action<object?> execute) : System.Windows.Input.ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute(parameter);
    }
    private delegate bool WindowVisitor(nint window, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(WindowVisitor visitor, nint parameter);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint window);
    internal static async Task RunAsync(Window window, string output)
    {
        var root = new Grid(); window.Content = root; window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(800, 600)); var original = new Button { Content = "Fictional menu trigger", Width = 180, Height = 40, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new(20) }; var host = new BleeckerModalHost { Content = original }; root.Children.Add(host);
        var guards = 0; var focus = 0; var activations = 0; var clicks = 0; var opens = 0; var closes = 0; var snapshots = new List<object>();
        window.Activated += (_, a) => { if (a.WindowActivationState != WindowActivationState.Deactivated) activations++; }; root.GotFocus += (_, _) => focus++;
        void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        void Guard()
        {
            guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var visible = false; EnumWindows((handle, _) => { GetWindowThreadProcessId(handle, out var owner); visible |= owner == Environment.ProcessId && IsWindowVisible(handle); return true; }, 0);
            var isolated = !visible && !window.Visible && !window.AppWindow.IsVisible && foreground != Environment.ProcessId && focus == 0 && activations == 0;
            if (!isolated) File.WriteAllText(Path.Combine(Path.GetDirectoryName(output)!, "guard-failure.json"), JsonSerializer.Serialize(new { guards, ownVisibleWindow = visible, windowVisible = window.Visible, appWindowVisible = window.AppWindow.IsVisible, ownForeground = foreground == Environment.ProcessId, focus, activations }));
            Require(isolated, "Menu primitive acquired visibility, activation or focus.");
        }
        async Task Layout() { for (var i = 0; i < 6; i++) { var idle = new TaskCompletionSource(); Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => idle.SetResult()), "Menu primitive could not drain."); await idle.Task; await Task.Delay(15); root.Measure(new Size(800, 600)); root.Arrange(new Rect(0, 0, 800, 600)); root.UpdateLayout(); Guard(); } }
        void Stage(string name) => File.WriteAllText(Path.Combine(Path.GetDirectoryName(output)!, "stage.json"), JsonSerializer.Serialize(new { name, guards }));
        var events = new List<string>(); var commands = 0; var nestedClicks = 0; var parameter = new object();
        var menu = new MarkerMenu { AreOpenCloseAnimationsEnabled = false, ShouldConstrainToRootBounds = true };
        var action = new MenuFlyoutItem { Text = "Fictional original action", CommandParameter = parameter, Command = new ProbeCommand(p => { Require(ReferenceEquals(p, parameter), "Command parameter identity changed."); commands++; events.Add("command"); }) };
        action.Click += (_, _) => { clicks++; events.Add("click"); };
        var checkbox = new ToggleMenuFlyoutItem { Text = "Fictional original checkbox" };
        var sub = new MenuFlyoutSubItem { Text = "Fictional original submenu" }; var nested = new MenuFlyoutItem { Text = "Fictional original nested action" }; nested.Click += (_, _) => nestedClicks++; sub.Items.Add(nested);
        menu.Items.Add(action); menu.Items.Add(checkbox); menu.Items.Add(sub); menu.Opened += (_, _) => opens++; menu.Closed += (_, _) => { closes++; events.Add("closed"); }; original.Flyout = menu;
        var restorations = new List<Action>();
        void Move(Popup popup, FrameworkElement declaration, Point position)
        {
            var presenter = (MenuFlyoutPresenter)popup.Child;
            var slot = (Grid)VisualTreeHelper.GetChild(presenter, 0); var rows = (ItemsPresenter)slot.Children.Single(); slot.Children.Remove(rows);
            popup.IsLightDismissEnabled = false; popup.IsHitTestVisible = false;
            presenter.Width = presenter.Height = presenter.MinWidth = presenter.MinHeight = 0;
            var canvas = new Canvas { Width = 800, Height = 600, Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent) }; canvas.Children.Add(rows); Canvas.SetLeft(rows, position.X); Canvas.SetTop(rows, position.Y); var branch = host.AttachPopupBranch(declaration, canvas, new Point());
            restorations.Add(() => { if (host.IsBranchAttached(branch)) host.DetachBranch(branch); canvas.Children.Clear(); slot.Children.Add(rows); foreach (var property in new[] { FrameworkElement.WidthProperty, FrameworkElement.HeightProperty, FrameworkElement.MinWidthProperty, FrameworkElement.MinHeightProperty }) presenter.ClearValue(property); });
        }
        void Restore() { foreach (var restore in restorations.AsEnumerable().Reverse()) restore(); restorations.Clear(); }
        async Task<Popup> Open()
        {
            ((FlyoutBase)menu).ShowAt(original, new FlyoutShowOptions { ShowMode = FlyoutShowMode.Transient }); await Layout();
            var popup = VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Single(p => p.Child is MenuFlyoutPresenter); Move(popup, original, new Point(20, 64)); await Layout();
            Require(menu.IsOpen && action.IsLoaded && checkbox.IsLoaded && sub.IsLoaded && ReferenceEquals(menu.Items[0], action), "Original native items did not survive the item-surface move."); return popup;
        }
        void Invoke(MenuFlyoutItem item) => ((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(item).GetPattern(PatternInterface.Invoke)).Invoke();
        Stage("load-root"); await Layout();
        Stage("command-and-native-dismissal"); var rootPopup = await Open(); Invoke(action); await Layout();
        Require(!menu.IsOpen && !rootPopup.IsOpen && clicks == 1 && commands == 1 && opens == 1 && closes == 1, "Original command/dismissal contract failed."); Require(events.SequenceEqual(new[] { "click", "command", "closed" }), "Native event/command order changed."); snapshots.Add(new { stage = "command", menu.IsOpen, opens, closes, clicks, commands, events = events.ToArray() }); Restore(); await Layout();
        Stage("reopen-and-checkbox-dismissal"); rootPopup = await Open(); ((IToggleProvider)FrameworkElementAutomationPeer.CreatePeerForElement(checkbox).GetPattern(PatternInterface.Toggle)).Toggle(); await Layout();
        Require(checkbox.IsChecked && !menu.IsOpen && !rootPopup.IsOpen && opens == 2 && closes == 2, "Original checkbox state/dismissal contract failed."); snapshots.Add(new { stage = "checkbox", menu.IsOpen, checkbox.IsChecked, opens, closes }); Restore(); await Layout();
        // UIA submenu expansion failed the earlier background isolation probe.
        // Keep that boundary deferred; do not cancel focus or weaken isolation.
        Stage("inherited-hide"); rootPopup = await Open(); Require(checkbox.IsChecked, "Original checkbox state did not persist across reopen."); ((FlyoutBase)menu).Hide(); await Layout(); Require(!menu.IsOpen && !rootPopup.IsOpen && opens == 3 && closes == 3, "Inherited Hide contract failed."); snapshots.Add(new { stage = "inherited-hide", menu.IsOpen, checkbox.IsChecked, opens, closes, nestedClicks }); Restore(); await Layout();
        Stage("cleanup"); root.Children.Clear(); await Layout();
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-menu-portal-primitive-v2", passed = true, primitiveContractPassed = true, snapshots, guards, focus, activations, activateCalled = false, showWindowCalled = false, focusCalled = false, hardwareInputSent = false, transientFlyoutRequested = true, menuAdapterImplemented = false, nativeFocusVerified = false, sourceGeometryVerified = false, inactiveModalGuardVerified = false, submenuRuntimeVerified = false, submenuExpansionDeferred = true }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(0);
    }
}
