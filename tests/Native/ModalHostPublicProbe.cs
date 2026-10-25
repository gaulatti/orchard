using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.System;

namespace Orchard.Testing;

// Real native content, layout and host lifecycle; key plans are inspected,
// never dispatched. No window activation, focus request or hardware input.
internal static class ModalHostPublicProbe
{
    private sealed class SlotSelector(DataTemplate selected) : DataTemplateSelector
    {
        internal int Calls { get; private set; }
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) { Calls++; return selected; }
    }
    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        { var child = VisualTreeHelper.GetChild(root, i); yield return child; foreach (var nested in Descendants(child)) yield return nested; }
    }
    private delegate bool Visitor(nint window, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor, nint parameter);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint window);
    internal static async Task RunAsync(Window window, string output, string hostPath)
    {
        var root = new Grid { Width = 800, Height = 600 }; window.Content = root; window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(800, 600));
        var activations = 0; var focus = 0; var guards = 0; var cases = new List<object>(); var failures = new List<object>();
        window.Activated += (_, a) => { if (a.WindowActivationState != WindowActivationState.Deactivated) activations++; }; root.GotFocus += (_, _) => focus++;
        void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        void Guard()
        {
            guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var visible = false;
            EnumWindows((handle, _) => { GetWindowThreadProcessId(handle, out var owner); visible |= owner == Environment.ProcessId && IsWindowVisible(handle); return true; }, 0);
            Require(!visible && !window.Visible && !window.AppWindow.IsVisible && foreground != Environment.ProcessId && activations == 0 && focus == 0, "Public host acquired visibility, activation or focus.");
            Require(VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Count == 0, "Public host opened a native popup.");
        }
        async Task Layout()
        {
            var idle = new TaskCompletionSource(); Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => idle.SetResult()), "Could not drain public host layout."); await idle.Task; await Task.Delay(20); root.UpdateLayout();
            for (var i = 0; i < 4; i++) { root.Measure(new Size(800, 600)); root.Arrange(new Rect(0, 0, 800, 600)); root.UpdateLayout(); Guard(); await Task.Delay(10); }
            Require(root.XamlRoot is { IsHostVisible: false, RasterizationScale: 1 } && root.XamlRoot.Size == new Size(800, 600), "Public host requires an actual hidden unscaled viewport.");
        }
        foreach (var scheme in new[] { "Light", "Dark" })
        {
            void Stage(string name) => File.WriteAllText(Path.Combine(Path.GetDirectoryName(output)!, "stage.json"), JsonSerializer.Serialize(new { scheme, name, guards, cases, failures }));
            Stage("mount-root");
            root.RequestedTheme = Enum.Parse<ElementTheme>(scheme); var declaration = new Border(); var background = new Grid(); background.Children.Add(declaration);
            var enabled = new ToggleSwitch { IsOn = true }; background.SetBinding(UIElement.IsHitTestVisibleProperty, new Binding { Source = enabled, Path = new PropertyPath(nameof(ToggleSwitch.IsOn)) });
            var original = new Button { Content = "Fictional background" }; original.SetBinding(Control.IsEnabledProperty, new Binding { Source = enabled, Path = new PropertyPath(nameof(ToggleSwitch.IsOn)) }); background.Children.Add(original);
            var contentSource = new Border { Tag = background }; var host = new BleeckerModalHost(); host.SetBinding(ContentControl.ContentProperty, new Binding { Source = contentSource, Path = new PropertyPath(nameof(FrameworkElement.Tag)) });
            root.Children.Add(host); await Layout();
            var first = new Button { Name = "Ordinary", Content = "First" }; var priority = new Button { Name = "Priority", Content = "Priority", TabIndex = 2 }; var disabled = new Button { Name = "Disabled", Content = "Disabled", IsEnabled = false }; var hidden = new Button { Name = "Hidden", Content = "Hidden", Visibility = Visibility.Collapsed };
            var link = new Hyperlink(); link.Inlines.Add(new Run { Text = "Fictional inline action" }); var span = new Span(); span.Inlines.Add(link); var text = new TextBlock(); text.Inlines.Add(span);
            var childDeclaration = new Border(); var body = new StackPanel(); foreach (var element in new UIElement[] { first, priority, disabled, hidden, text, childDeclaration }) body.Children.Add(element);
            var surface = new ContentControl { Name = "Surface", Content = body, Width = 300, Height = 260, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, IsTabStop = true }; var overlay = new Grid(); overlay.Children.Add(surface);
            var dismissals = 0; var parent = host.Attach(declaration, surface, overlay, () => dismissals++); host.Activate(parent); await Layout();
            async Task Case(string name, Func<Task<object>> action)
            {
                Stage(name);
                try { var evidence = await action(); Guard(); cases.Add(new { scheme, name, passed = true, evidence }); }
                catch (Exception error) { failures.Add(new { scheme, name, error = error.ToString() }); cases.Add(new { scheme, name, passed = false }); }
            }
            await Case("root-content-and-caller-bindings", async () =>
            {
                Require(ReferenceEquals(host.Content, background) && host.GetBindingExpression(ContentControl.ContentProperty) is not null && original.GetBindingExpression(Control.IsEnabledProperty) is not null && background.GetBindingExpression(UIElement.IsHitTestVisibleProperty) is not null, "A caller binding/content was replaced.");
                enabled.IsOn = false; await Layout(); Require(!original.IsEnabled && !background.IsHitTestVisible && host.BackgroundPointerBlocked, "Original bound state was overwritten while open."); enabled.IsOn = true; await Layout();
                Require(original.IsEnabled && background.IsHitTestVisible && host.BackgroundPointerBlocked, "Original binding stopped updating while blocked.");
                Require(host.ShouldCancelFocusTarget(original) && !host.ShouldCancelFocusTarget(first) && !host.ShouldCancelFocusTarget(new Button()), "Focus cancellation predicate crossed its actual root boundary.");
                var hostPeer = FrameworkElementAutomationPeer.CreatePeerForElement(host);
                var peerRows = hostPeer.GetChildren().Select(p => new { type = p.GetClassName(), name = p.GetName(), owner = (p as FrameworkElementAutomationPeer)?.Owner?.GetType().Name, ownerName = ((p as FrameworkElementAutomationPeer)?.Owner as FrameworkElement)?.Name, children = p.GetChildren()?.Select(c => new { type = c.GetClassName(), name = c.GetName(), ownerName = ((c as FrameworkElementAutomationPeer)?.Owner as FrameworkElement)?.Name }).ToArray() }).ToArray();
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(output)!, "public-peer-" + scheme + ".json"), JsonSerializer.Serialize(peerRows));
                IEnumerable<AutomationPeer> Peers(AutomationPeer peer) { yield return peer; foreach (var child in peer.GetChildren() ?? []) foreach (var nested in Peers(child)) yield return nested; }
                var names = Peers(hostPeer).Select(peer => peer.GetName()).ToArray(); Require(names.Contains("First") && names.Contains("Priority") && !names.Contains("Fictional background"), "Public host peer bypassed the active core scope."); return new { originalContent = true, bindingsRetained = 3, valuesContinueUpdating = true, privatePointerShield = true, sameRootBackgroundRejected = true, activeAndDetachedTargetsPreserved = true, activePeerScopeRetained = true };
            });
            await Case("sequential-native-buttons-and-inline", () =>
            {
                var targets = host.SequentialTargets(); Require(targets.Count == 3 && ReferenceEquals(targets[0], priority) && ReferenceEquals(targets[1], first) && ReferenceEquals(targets[2], link), "Native sequential targets/order differ.");
                Require(!targets.Contains(surface) && !targets.Contains(disabled) && !targets.Contains(hidden), "Fallback, disabled or hidden element entered sequential order."); return Task.FromResult<object>(new { targets = new[] { "Priority", "Ordinary", "Inline" }, disabledAndHiddenExcluded = true, surfaceExcluded = true });
            });
            await Case("tab-and-shift-tab-plans", () =>
            {
                foreach (var (current, shift, target) in new (DependencyObject?, bool, DependencyObject)[] { (null, false, priority), (null, true, link), (priority, false, first), (first, false, link), (link, false, priority), (priority, true, link), (link, true, first), (first, true, priority) })
                { var plan = host.PlanKey(VirtualKey.Tab, shift, current); Require(plan.Handled && !plan.Dismiss && ReferenceEquals(plan.Target, target), "Tab target plan differs."); }
                return Task.FromResult<object>(new { plans = 8, allHandled = true, nativeKeysSent = false });
            });
            await Case("escape-is-a-controlled-request", () =>
            {
                var plan = host.PlanKey(VirtualKey.Escape, false, first); Require(plan.Handled && plan.Dismiss && plan.Target is null && dismissals == 0 && ReferenceEquals(host.ActiveSurface, surface), "Escape planning mutated controlled state.");
                Require(!host.PlanKey(VirtualKey.Enter, false, first).Handled, "Host consumed unrelated key."); return Task.FromResult<object>(new { escapeHandled = true, requestOnly = true, unrelatedKeyPreserved = true, activeUnchanged = true });
            });
            var branchDeclaration = new Border(); var branchAction = new Button { Name = "Branch", Content = "Branch action", TabIndex = 1 }; var branchRoot = new StackPanel(); branchRoot.Children.Add(branchAction); branchRoot.Children.Add(branchDeclaration); var branch = host.AttachBranch(childDeclaration, branchRoot, new(40, 80)); await Layout();
            await Case("branch-participates-in-ownership-and-order", () =>
            {
                var targets = host.SequentialTargets(); Require(targets.Count == 4 && ReferenceEquals(targets[0], branchAction) && host.AllowsFocusTarget(branchAction) && !host.AllowsFocusTarget(original), "Branch ownership/order differs."); return Task.FromResult<object>(new { targets = 4, branchFirst = true, branchAllowed = true, backgroundRejected = true });
            });
            var childMarker = new Border(); var childBody = new StackPanel(); childBody.Children.Add(childMarker); var childSurface = new ContentControl { Name = "Child", Content = childBody, Width = 200, Height = 140, IsTabStop = true }; var childOverlay = new Grid(); childOverlay.Children.Add(childSurface); var child = host.Attach(branchDeclaration, childSurface, childOverlay, () => { });
            var grandSurface = new ContentControl { Name = "Grandchild", Width = 120, Height = 80, IsTabStop = true }; var grandOverlay = new Grid(); grandOverlay.Children.Add(grandSurface);
            await Layout(); var grandchild = host.Attach(childMarker, grandSurface, grandOverlay, () => { });
            await Case("logical-ancestry-crosses-ported-branches", () =>
            {
                Require(child.Ancestors.SequenceEqual(new[] { parent }) && grandchild.Ancestors.SequenceEqual(new[] { parent, child }), "Ancestry lost a portaled ancestor."); return Task.FromResult<object>(new { childDepth = 1, grandchildDepth = 2, nearestBranchOwner = true });
            });
            await Case("child-first-activation-keeps-descendants-active", async () =>
            {
                host.Deactivate(parent); host.Activate(child); host.Activate(parent); await Layout(); Require(ReferenceEquals(host.ActiveSurface, childSurface), "Late ancestor activation replaced active descendant."); host.Activate(grandchild); await Layout(); Require(ReferenceEquals(host.ActiveSurface, grandSurface) && !host.AllowsFocusTarget(branchAction), "Grandchild did not isolate earlier branches.");
                var plan = host.PlanKey(VirtualKey.Tab, false, null); Require(plan.Handled && ReferenceEquals(plan.Target, grandSurface), "Empty modal lacks surface fallback."); host.Deactivate(grandchild); host.Deactivate(child); Require(ReferenceEquals(host.ActiveSurface, surface), "Closing descendants did not return active parent."); return new { lateAncestorPreservesChild = true, grandchildActive = true, inactiveBranchRejected = true, emptySurfaceFallback = true, parentResumed = true };
            });
            await Case("template-replacement-retains-owned-plane", async () =>
            {
                host.Template = (ControlTemplate)XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'><Grid x:Name='PART_Layout'/></ControlTemplate>"); host.ApplyTemplate(); await Layout();
                Require(ReferenceEquals(host.ActiveSurface, surface) && host.BackgroundPointerBlocked && ReferenceEquals(host.Content, background) && host.GetBindingExpression(ContentControl.ContentProperty) is not null && parent.IsAttached, "Template replacement lost the owned plane/content."); return new { activeSurfaceRetained = true, originalContentRetained = true, contentBindingRetained = true, registrationRetained = true };
            });
            await Case("rtl-root-retains-physical-overlay-viewport", async () =>
            {
                host.FlowDirection = FlowDirection.RightToLeft; await Layout(); Require(ReferenceEquals(BleeckerModalHost.Require(declaration), host) && ReferenceEquals(host.ActiveSurface, surface), "RTL lost its modal host."); host.ClearValue(FrameworkElement.FlowDirectionProperty); await Layout(); return new { fullViewportRetained = true, activeSurfaceRetained = true };
            });
            await Case("outside-declaration-rejected", async () =>
            {
                var outside = new Border(); root.Children.Add(outside); await Layout(); var rejected = false; try { BleeckerModalHost.Require(outside); } catch (InvalidOperationException) { rejected = true; } root.Children.Remove(outside); Require(rejected, "An outside declaration acquired the root host."); return new { rejected };
            });
            await Case("closing-and-detaching-release-background", () =>
            {
                host.Deactivate(parent); Require(host.ActiveSurface is null && !host.BackgroundPointerBlocked && !parent.Layer.Frame.IsHitTestVisible && original.IsEnabled && background.IsHitTestVisible, "Close failed to release the background without repainting.");
                grandchild.Dispose(); child.Dispose(); host.DetachBranch(branch); parent.Dispose(); parent.Dispose(); Require(!parent.IsAttached && !child.IsAttached && !grandchild.IsAttached, "Dispose left registrations alive."); return Task.FromResult<object>(new { backgroundReleased = true, closingFrameInert = true, callerValuesRetained = true, disposalIdempotent = true });
            });
            await Case("constrained-root-rejected-before-taking-content", async () =>
            {
                host.Width = 600; await Layout(); var nextSurface = new Grid(); var nextOverlay = new Grid(); nextOverlay.Children.Add(nextSurface); var rejected = false; try { host.Attach(declaration, nextSurface, nextOverlay, () => { }); } catch (InvalidOperationException) { rejected = true; }
                host.ClearValue(FrameworkElement.WidthProperty); await Layout(); Require(rejected && VisualTreeHelper.GetParent(nextOverlay) is null, "Constrained root took overlay content."); var reusable = host.Attach(declaration, nextSurface, nextOverlay, () => { }); reusable.Dispose(); return new { rejected, overlayReusable = true };
            });
            await Case("transformed-root-rejected-before-taking-content", async () =>
            {
                host.RenderTransform = new ScaleTransform { ScaleX = 1.1 }; await Layout(); var nextSurface = new Grid(); var nextOverlay = new Grid(); nextOverlay.Children.Add(nextSurface); var rejected = false; try { host.Attach(declaration, nextSurface, nextOverlay, () => { }); } catch (InvalidOperationException) { rejected = true; }
                host.ClearValue(UIElement.RenderTransformProperty); await Layout(); Require(rejected && VisualTreeHelper.GetParent(nextOverlay) is null, "Transformed root took overlay content."); var reusable = host.Attach(declaration, nextSurface, nextOverlay, () => { }); reusable.Dispose(); return new { rejected, overlayReusable = true };
            });
            await Case("content-template-and-selector-retain-caller-data", async () =>
            {
                DataTemplate Template(string tag) => (DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Tag='" + tag + "' Text='{Binding}'/></DataTemplate>");
                var template = Template("Explicit"); host.ContentTemplate = template; contentSource.Tag = "Fictional explicit data"; await Layout();
                Require(Descendants(host).OfType<TextBlock>().Any(t => Equals(t.Tag, "Explicit") && t.Text == "Fictional explicit data") && ReferenceEquals(host.ContentTemplate, template), "Explicit content template/data was lost.");
                var selector = new SlotSelector(Template("Selected")); host.ContentTemplate = null; host.ContentTemplateSelector = selector; contentSource.Tag = "Fictional selected data"; await Layout();
                Require(selector.Calls > 0 && Descendants(host).OfType<TextBlock>().Any(t => Equals(t.Tag, "Selected") && t.Text == "Fictional selected data") && ReferenceEquals(host.ContentTemplateSelector, selector), "Content selector/data was lost.");
                host.ContentTemplateSelector = null; contentSource.Tag = background; await Layout(); Require(ReferenceEquals(host.Content, background) && host.GetBindingExpression(ContentControl.ContentProperty) is not null, "Restoring original content lost its binding."); return new { explicitTemplateRendered = true, selectorRendered = true, originalDataRetained = true, contentBindingRetained = true };
            });
            await Case("unload-releases-registry-and-registrations", async () =>
            {
                var nextSurface = new Grid(); var nextOverlay = new Grid(); nextOverlay.Children.Add(nextSurface); var lease = host.Attach(declaration, nextSurface, nextOverlay, () => { }); host.Activate(lease); root.Children.Remove(host); await Layout();
                Require(!lease.IsAttached && host.ActiveSurface is null && !host.BackgroundPointerBlocked, "Unload retained a modal registration."); lease.Dispose(); root.Children.Add(host); await Layout(); Require(ReferenceEquals(BleeckerModalHost.Require(declaration), host), "Reload did not reconnect host registry."); return new { registrationReleased = true, backgroundReleased = true, reloadReconnected = true };
            });
            root.Children.Remove(host); await Layout();
        }
        var catalog = new Orchard.Catalog.MainWindow(); catalog.Activated += (_, a) => { if (a.WindowActivationState != WindowActivationState.Deactivated) activations++; }; ((FrameworkElement)catalog.Content).GotFocus += (_, _) => focus++;
        var catalogRootCases = await catalog.ProbeModalRootAsync(Guard, Path.GetDirectoryName(output)!); Guard();
        Directory.CreateDirectory(Path.GetDirectoryName(output)!); string Hash(string file) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-public-modal-host-v1", passed = failures.Count == 0, scope = "native-root-content-ownership-lifecycle-and-key-planning", cases, catalogRootCases, closedRootRefreshVerified = catalog.ModalRootClosedRefreshVerified, failures, guards, activations, focus, librarySha256 = Hash(typeof(BleeckerModalHost).Assembly.Location), hostSha256 = Hash(hostPath), processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(), activateCalled = false, showCalled = false, focusCalled = false, inputSent = false, actualKeyRoutingVerified = false, actualFocusCancellationVerified = false, nativeModalityVerified = false }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(failures.Count == 0 ? 0 : 1);
    }
}
