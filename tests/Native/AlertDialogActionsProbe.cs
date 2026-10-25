using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Testing;

internal static class AlertDialogActionsProbe
{
    private sealed class Command(string name, List<string> trace) : ICommand
    {
        private EventHandler? changed;
        internal bool Enabled = true;
        internal int Executions, Subscriptions;
        internal object? Parameter;
        public event EventHandler? CanExecuteChanged { add { changed += value; Subscriptions++; } remove { changed -= value; Subscriptions--; } }
        public bool CanExecute(object? parameter) => Enabled;
        public void Execute(object? parameter) { Executions++; Parameter = parameter; trace.Add("command:" + name); }
        internal void Raise() => changed?.Invoke(this, EventArgs.Empty);
    }
    private delegate bool Visitor(nint window, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor, nint parameter);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint window);
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node)
    {
        if (node is FrameworkElement element) yield return element;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) foreach (var child in Elements(VisualTreeHelper.GetChild(node, i))) yield return child;
    }
    internal static async Task RunAsync(Window window, string output, string hostPath)
    {
        var background = new Grid(); var host = new BleeckerModalHost { Content = background, Width = 800, Height = 600 }; window.Content = host; window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(800, 600));
        var guards = 0; var focus = 0; var activations = 0; var invokes = 0; var cases = new List<object>(); var failures = new List<object>();
        host.GotFocus += (_, _) => focus++; window.Activated += (_, args) => { if (args.WindowActivationState != WindowActivationState.Deactivated) activations++; };
        void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        void Guard()
        {
            guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var visible = false; EnumWindows((handle, _) => { GetWindowThreadProcessId(handle, out var owner); visible |= owner == Environment.ProcessId && IsWindowVisible(handle); return true; }, 0);
            Require(!visible && !window.Visible && !window.AppWindow.IsVisible && foreground != Environment.ProcessId && focus == 0 && activations == 0, "AlertDialog actions acquired visibility, activation or focus."); Require(host.XamlRoot is null || VisualTreeHelper.GetOpenPopupsForXamlRoot(host.XamlRoot).Count == 0, "AlertDialog actions opened a Popup.");
        }
        async Task Layout()
        {
            var idle = new TaskCompletionSource(); Require(host.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => idle.SetResult()), "Could not drain action layout."); await idle.Task; await Task.Delay(20);
            for (var i = 0; i < 4; i++) { host.Measure(new Size(800, 600)); host.Arrange(new Rect(0, 0, 800, 600)); host.UpdateLayout(); Guard(); await Task.Delay(10); }
        }
        async Task Wait(int milliseconds) { await Task.Delay(milliseconds); Guard(); }
        FrameworkElement? Find(string name) => Elements(host).SingleOrDefault(element => element.Name == name);
        BleeckerButton Button(string name) => (BleeckerButton)(Find(name) ?? throw new InvalidOperationException("Missing action " + name));
        async Task Invoke(string name)
        {
            var action = Elements(Button(name)).OfType<Button>().Single(element => element.Name == "PART_Action"); var provider = (IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(action).GetPattern(PatternInterface.Invoke); provider.Invoke(); invokes++; await Wait(20); Guard();
        }
        foreach (var scheme in new[] { "Light", "Dark" })
        {
            host.RequestedTheme = Enum.Parse<ElementTheme>(scheme); var trace = new List<string>(); var cancelCommand = new Command("cancel", trace); var confirmCommand = new Command("confirm", trace); var parameter = new object(); var canceled = 0; var confirmed = 0;
            var source = new ToggleSwitch { IsOn = true }; var dialog = new BleeckerAlertDialog { Title = "Fictional controlled dialog", Description = "Requests remain local and caller-owned.", CancelCommand = cancelCommand, ConfirmCommand = confirmCommand, CancelCommandParameter = parameter, ConfirmCommandParameter = parameter };
            dialog.CancelRequested += (_, _) => { canceled++; trace.Add("event:cancel"); }; dialog.ConfirmRequested += (_, _) => { confirmed++; trace.Add("event:confirm"); }; dialog.SetBinding(BleeckerAlertDialog.IsOpenProperty, new Binding { Source = source, Path = new PropertyPath(nameof(ToggleSwitch.IsOn)) }); background.Children.Add(dialog); await Layout(); await Wait(260); await Layout();
            bool BoundOpen() => source.IsOn && dialog.IsOpen && dialog.GetBindingExpression(BleeckerAlertDialog.IsOpenProperty) is not null && host.ActiveSurface is not null && host.BackgroundPointerBlocked;
            async Task Case(string name, Func<Task<object>> action)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(output)!); File.WriteAllText(Path.Combine(Path.GetDirectoryName(output)!, "stage.json"), JsonSerializer.Serialize(new { scheme, name, guards, completed = cases.Count }));
                try { var evidence = await action(); Guard(); cases.Add(new { scheme, name, passed = true, evidence }); }
                catch (Exception error) { failures.Add(new { scheme, name, error = error.ToString() }); cases.Add(new { scheme, name, passed = false }); }
            }
            await Case("confirm-native-invoke-requests-once", async () =>
            {
                await Invoke("AlertDialogConfirm"); Require(confirmed == 1 && confirmCommand.Executions == 1 && ReferenceEquals(confirmCommand.Parameter, parameter) && trace.SequenceEqual(new[] { "event:confirm", "command:confirm" }) && BoundOpen(), "Native confirm lost its controlled command/event boundary."); return new { events = 1, executions = 1, originalParameter = true, eventBeforeCommand = true, openBindingRetained = true };
            });
            await Case("cancel-native-invoke-requests-once", async () =>
            {
                await Invoke("AlertDialogCancel"); Require(canceled == 1 && cancelCommand.Executions == 1 && ReferenceEquals(cancelCommand.Parameter, parameter) && BoundOpen(), "Native cancel closed itself or duplicated command/event."); return new { events = 1, executions = 1, originalParameter = true, openBindingRetained = true };
            });
            await Case("command-state-controls-native-action", async () =>
            {
                confirmCommand.Enabled = false; confirmCommand.Raise(); await Layout(); Require(!Button("AlertDialogConfirm").IsEnabled, "CanExecuteChanged did not disable confirm."); var before = confirmed; dialog.Confirm(); Require(confirmed == before, "CanExecute=false emitted a request."); confirmCommand.Enabled = true; confirmCommand.Raise(); await Layout(); Require(Button("AlertDialogConfirm").IsEnabled, "CanExecute restoration did not enable confirm."); await Invoke("AlertDialogConfirm"); Require(confirmed == before + 1 && confirmCommand.Executions == confirmed && BoundOpen(), "Command restoration duplicated/lost its request."); return new { disabledOnRejection = true, rejectedRequestIgnored = true, restoredActionEnabled = true, restoredInvokeOnce = true, openBindingRetained = true };
            });
            await Case("loading-rejects-both-actions-and-window-close", async () =>
            {
                var beforeCancel = canceled; var beforeConfirm = confirmed; dialog.ConfirmLoading = true; await Layout(); Require(!Button("AlertDialogCancel").IsEnabled && !Button("AlertDialogConfirm").IsEnabled && !Button("AlertDialogConfirm").IsLoading, "Loading uses wrong enabled/busy state."); dialog.Cancel(); dialog.Confirm(); var peer = FrameworkElementAutomationPeer.CreatePeerForElement(Find("AlertDialogSurface")!); ((IWindowProvider)peer.GetPattern(PatternInterface.Window)).Close(); Require(canceled == beforeCancel && confirmed == beforeConfirm && BoundOpen(), "Loading emitted a cancellation/confirmation."); dialog.ConfirmLoading = false; await Layout(); return new { bothDisabled = true, manualLoaderNotBusy = true, cancelIgnored = true, confirmIgnored = true, windowCloseIgnored = true, openBindingRetained = true };
            });
            await Case("window-pattern-close-remains-controlled", () =>
            {
                var before = canceled; var peer = FrameworkElementAutomationPeer.CreatePeerForElement(Find("AlertDialogSurface")!); ((IWindowProvider)peer.GetPattern(PatternInterface.Window)).Close(); Require(canceled == before + 1 && cancelCommand.Executions == canceled && BoundOpen(), "Window pattern close bypassed the controlled request."); return Task.FromResult<object>(new { cancelOnce = true, commandOnce = true, openBindingRetained = true });
            });
            await Case("live-label-description-and-variant", async () =>
            {
                dialog.Title = "Updated fictional title"; dialog.Description = ""; dialog.CancelLabel = "Keep local"; dialog.ConfirmLabel = "Continue local"; dialog.Variant = BleeckerAlertDialogVariant.Destructive; await Layout(); var surface = Find("AlertDialogSurface")!; var peer = FrameworkElementAutomationPeer.CreatePeerForElement(surface);
                Require(peer.GetName() == dialog.Title && Microsoft.UI.Xaml.Automation.AutomationProperties.GetDescribedBy(surface).Count == 0 && Button("AlertDialogConfirm").Variant == BleeckerButtonVariant.Destructive && Microsoft.UI.Xaml.Automation.AutomationProperties.GetName(Button("AlertDialogCancel")) == "Keep local" && BoundOpen(), "Live dialog properties failed to reach the rendered consumer."); return new { titleUpdated = true, emptyDescriptionRelationshipRemoved = true, labelsUpdated = true, variantUpdated = true, openBindingRetained = true };
            });
            await Case("close-releases-input-before-removing-paint", async () =>
            {
                source.IsOn = false; Require(!dialog.IsOpen && host.ActiveSurface is null && !host.BackgroundPointerBlocked && Find("AlertDialogOverlay") is not null, "Close did not immediately release ownership and retain paint."); var before = canceled + confirmed; dialog.Cancel(); dialog.Confirm(); Require(before == canceled + confirmed, "Closing paint emitted a request."); await Wait(60); Require(Find("AlertDialogOverlay") is not null, "Closing paint disappeared before its 150ms presence."); await Wait(120); await Layout(); Require(Find("AlertDialogOverlay") is null, "Closing paint outlived presence."); return new { ownershipReleasedImmediately = true, paintRetainedAt60ms = true, removedAfter150ms = true, closingRequestsIgnored = true, callerBindingRetained = dialog.GetBindingExpression(BleeckerAlertDialog.IsOpenProperty) is not null };
            });
            await Case("rapid-reopen-reuses-presence-layer", async () =>
            {
                source.IsOn = true; await Wait(260); await Layout(); var old = Find("AlertDialogOverlay"); source.IsOn = false; await Wait(60); source.IsOn = true; Require(BoundOpen() && ReferenceEquals(old, Find("AlertDialogOverlay")), "Rapid reopen discarded presence or failed immediate ownership."); await Wait(260); await Layout(); Require(Find("AlertDialogSurface")?.Opacity == 1, "Reopened surface did not settle."); return new { originalLayerRetained = true, immediateOwnershipRestored = true, settledOpen = true, openBindingRetained = true };
            });
            var replacement = new Command("replacement", trace);
            await Case("command-replacement-and-shared-subscription", async () =>
            {
                dialog.ConfirmCommand = replacement; await Layout(); Require(confirmCommand.Subscriptions == 0 && replacement.Subscriptions == 1, "Command replacement retained an old subscriber."); dialog.CancelCommand = replacement; await Layout(); Require(cancelCommand.Subscriptions == 0 && replacement.Subscriptions == 1, "A shared command acquired duplicate subscriptions."); var before = replacement.Executions; await Invoke("AlertDialogConfirm"); Require(replacement.Executions == before + 1 && BoundOpen(), "Replacement command did not execute exactly once."); return new { oldCommandsUnsubscribed = true, sharedCommandSubscriptions = 1, replacementInvokedOnce = true, openBindingRetained = true };
            });
            await Case("template-replacement-remounts-controlled-value", async () =>
            {
                var old = Find("AlertDialogOverlay"); dialog.Template = (ControlTemplate)XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'><Grid x:Name='PART_Layout'/></ControlTemplate>"); dialog.ApplyTemplate(); await Layout(); await Wait(260); await Layout(); Require(BoundOpen() && !ReferenceEquals(old, Find("AlertDialogOverlay")), "Template replacement lost caller state or retained retired content."); return new { originalOpenBindingRetained = true, retiredLayerReleased = true, replacementLayerMounted = true };
            });
            await Case("unload-releases-commands-and-reload-reopens", async () =>
            {
                background.Children.Remove(dialog); await Layout(); await Wait(180); Require(replacement.Subscriptions == 0 && host.ActiveSurface is null && !host.BackgroundPointerBlocked && Find("AlertDialogOverlay") is null && source.IsOn && dialog.IsOpen, "Unload leaked resources or changed the caller's open value."); background.Children.Add(dialog); await Layout(); await Wait(260); await Layout(); Require(BoundOpen() && replacement.Subscriptions == 1, "Reload failed to mount the retained caller value."); return new { commandUnsubscribedOnUnload = true, layerReleasedOnUnload = true, callerOpenValueRetained = true, reloadMounted = true, reloadSubscriptions = 1 };
            });
            source.IsOn = false; await Wait(180); background.Children.Remove(dialog); await Layout(); Require(replacement.Subscriptions == 0 && host.ActiveSurface is null, "Action fixture teardown retained a command/layer.");
        }
        Directory.CreateDirectory(Path.GetDirectoryName(output)!); string Hash(string file) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-alert-dialog-actions-v1", passed = failures.Count == 0, cases, failures, guards, focus, activations, softwareNativeInvokes = invokes, librarySha256 = Hash(typeof(BleeckerAlertDialog).Assembly.Location), hostSha256 = Hash(hostPath), architecture = RuntimeInformation.ProcessArchitecture.ToString(), activateCalled = false, showCalled = false, focusCalled = false, hardwareInputSent = false, nativeKeyboardPointerVerified = false }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(failures.Count == 0 ? 0 : 1);
    }
}
