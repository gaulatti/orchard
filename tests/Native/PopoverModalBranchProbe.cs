using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;

namespace Orchard.Testing;

// Real public Popovers in the root portal. This fixture never activates a
// window, calls Focus/TryFocus, or sends keyboard/pointer input.
internal static class PopoverModalBranchProbe
{
    private delegate bool WindowVisitor(nint window, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(WindowVisitor visitor, nint parameter);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint window);
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node)
    {
        if (node is FrameworkElement element) yield return element;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) foreach (var nested in Elements(VisualTreeHelper.GetChild(node, i))) yield return nested;
    }
    internal static async Task RunAsync(Window window, string output, string hostPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        var root = new Grid(); window.Content = root; window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(800, 600));
        var guards = 0; var activations = 0; var focus = 0; var softwareNativeInvokes = 0; var standalonePopupPhases = 0; var allowStandalonePopup = false; var cases = new List<object>(); var failures = new List<object>(); var captures = new List<object>();
        window.Activated += (_, a) => { if (a.WindowActivationState != WindowActivationState.Deactivated) activations++; }; root.GotFocus += (_, _) => focus++;
        void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        void Guard()
        {
            guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var visible = false;
            EnumWindows((handle, _) => { GetWindowThreadProcessId(handle, out var owner); visible |= owner == Environment.ProcessId && IsWindowVisible(handle); return true; }, 0);
            Require(!visible && !window.Visible && !window.AppWindow.IsVisible && foreground != Environment.ProcessId && activations == 0 && focus == 0, "Popover branch probe acquired visibility, activation or focus.");
            Require(VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Count <= (allowStandalonePopup ? 1 : 0), "A hosted Popover escaped the modal portal into a native Popup.");
        }
        async Task Layout()
        {
            for (var i = 0; i < 6; i++)
            {
                var idle = new TaskCompletionSource(); Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => idle.SetResult()), "Could not drain branch layout."); await idle.Task; await Task.Delay(12);
                root.Measure(new Size(800, 600)); root.Arrange(new Rect(0, 0, 800, 600)); root.UpdateLayout(); Guard();
            }
            Require(root.XamlRoot is { IsHostVisible: false, RasterizationScale: 1 } && root.XamlRoot.Size == new Size(800, 600), "Branch probe requires its actual hidden unscaled viewport.");
        }
        Rect Box(FrameworkElement element) => element.TransformToVisual(root).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
        async Task Capture(string file)
        {
            var bitmap = new RenderTargetBitmap(); await bitmap.RenderAsync(root); Guard(); Require(bitmap.PixelWidth == 800 && bitmap.PixelHeight == 600, "Branch capture was rescaled.");
            var buffer = await bitmap.GetPixelsAsync(); var pixels = new byte[buffer.Length]; using (var reader = Windows.Storage.Streams.DataReader.FromBuffer(buffer)) reader.ReadBytes(pixels);
            using var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream(); var encoder = await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId, stream); encoder.SetPixelData(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied, 800, 600, 96, 96, pixels); await encoder.FlushAsync();
            using var pngReader = new Windows.Storage.Streams.DataReader(stream.GetInputStreamAt(0)); await pngReader.LoadAsync((uint)stream.Size); var bytes = new byte[stream.Size]; pngReader.ReadBytes(bytes); await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!, file), bytes);
            captures.Add(new { file, sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)), width = 800, height = 600 });
        }
        foreach (var scheme in new[] { "Light", "Dark" })
        {
            root.RequestedTheme = Enum.Parse<ElementTheme>(scheme);
            var background = new Grid(); var declaration = new Border(); background.Children.Add(declaration);
            var backgroundAction = new Button { Content = "Fictional background popup action" };
            var backgroundPopover = new BleeckerPopover { Name = "BackgroundPopover", Open = true, Trigger = new Button { Content = "Background trigger" }, Content = backgroundAction, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new(16, 16, 0, 0) }; background.Children.Add(backgroundPopover);
            var host = new BleeckerModalHost { Content = background }; root.Children.Add(host); await Layout();
            var originalEditor = new TextBox { Text = "Fictional original edit", Width = 180 }; var enabled = new ToggleSwitch { IsOn = true }; var open = new ToggleSwitch { IsOn = true };
            var originalAction = new Button { Content = "Original modal popup action", Width = 180 }; originalAction.SetBinding(Control.IsEnabledProperty, new Binding { Source = enabled, Path = new(nameof(ToggleSwitch.IsOn)) });
            var nestedAction = new Button { Content = "Nested original action" }; var nestedDeclaration = new Border(); var nestedBody = new StackPanel(); nestedBody.Children.Add(nestedAction); nestedBody.Children.Add(nestedDeclaration);
            var nested = new BleeckerPopover { Name = "NestedPopover", Trigger = new Button { Content = "Nested trigger" }, Content = nestedBody };
            var content = new StackPanel { Width = 220 }; content.Children.Add(originalEditor); content.Children.Add(originalAction); content.Children.Add(nested);
            var trigger = new Button { Content = "Modal trigger", Width = 120, Height = 36 };
            var popover = new BleeckerPopover { Name = "OwnedPopover", Trigger = trigger, Content = content, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new(28, 32, 0, 0) };
            popover.SetBinding(BleeckerPopover.OpenProperty, new Binding { Source = open, Path = new(nameof(ToggleSwitch.IsOn)) });
            var opens = 0; var closes = 0; var requests = 0; popover.Opened += (_, _) => opens++; popover.Closed += (_, _) => closes++; popover.OpenRequested += (_, _) => requests++;
            var childDeclaration = new Border(); var body = new Grid(); body.Children.Add(popover); body.Children.Add(childDeclaration);
            var surface = new ContentControl { Name = "ParentModal", Width = 480, Height = 400, Content = body, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }; var overlay = new Grid(); overlay.Children.Add(surface);
            var parent = host.Attach(declaration, surface, overlay, () => { }); host.Activate(parent); await Layout();
            async Task Case(string name, Func<Task<object>> action)
            {
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(output)!, "stage.json"), JsonSerializer.Serialize(new { scheme, name, guards }));
                try { var evidence = await action(); Guard(); cases.Add(new { scheme, name, passed = true, evidence }); }
                catch (Exception error) { failures.Add(new { scheme, name, error = error.ToString() }); cases.Add(new { scheme, name, passed = false }); File.WriteAllText(Path.Combine(Path.GetDirectoryName(output)!, "failures-in-progress.json"), JsonSerializer.Serialize(failures)); }
            }
            bool Bound() => popover.GetBindingExpression(BleeckerPopover.OpenProperty) is not null && originalAction.GetBindingExpression(Control.IsEnabledProperty) is not null;
            await Case("owned-public-content-and-bindings", () =>
            {
                Require(parent.Layer.Branches.Count == 1 && ReferenceEquals(popover.Content, content) && ReferenceEquals(popover.Trigger, trigger) && Bound() && opens == 1 && closes == 0 && requests == 0, "Public content, binding or presentation identity changed.");
                Require(host.AllowsFocusTarget(originalAction) && host.SequentialTargets().Contains(originalAction), "Owned popup action is outside the modal's sequential targets."); return Task.FromResult<object>(new { branches = 1, originalContent = true, originalTrigger = true, bindings = 2, opens, closes, requests, ownedSequentialTarget = true });
            });
            await Case("native-anchored-placement", () =>
            {
                var anchor = Box(trigger); var actual = Box(parent.Layer.Branches.Single().Root); var expected = BleeckerPopoverModel.Place(new(anchor.X, anchor.Y, anchor.Width, anchor.Height), actual.Width, actual.Height, 800, 600, popover.Side, popover.Align, popover.SideOffset, false, 1);
                Require(actual.Width > 220 && actual.Height > 100 && Math.Abs(actual.X - expected.X) <= 1d / 64 && Math.Abs(actual.Y - expected.Y) <= 1d / 64, "Portal placement differs from the actual trigger/viewport."); return Task.FromResult<object>(new { anchor = new { x = anchor.X, y = anchor.Y, width = anchor.Width, height = anchor.Height }, actual = new { x = actual.X, y = actual.Y, width = actual.Width, height = actual.Height }, expected = new { x = expected.X, y = expected.Y } });
            });
            await Case("background-popup-is-inert", () =>
            {
                var frame = Elements(root).OfType<Border>().Single(b => b.Child is ContentControl c && Elements(c).Contains(backgroundAction));
                Require(!frame.IsHitTestVisible && backgroundAction.IsEnabled && backgroundPopover.IsOpen && !host.AllowsFocusTarget(backgroundAction), "Background popup escaped the active modal or lost original state."); return Task.FromResult<object>(new { frameBlocked = true, originalEnabled = true, logicalOpen = true, focusRejected = true });
            });
            await Case("nested-popup-keeps-nearest-modal-owner", async () =>
            {
                nested.Open = true; await Layout();
                var diagnostic = new { branches = parent.Layer.Branches.Count, branchNames = parent.Layer.Branches.Select(b => new { name = b.Root.Name, hit = b.Frame.IsHitTestVisible }).ToArray(), nestedLoaded = nested.IsLoaded, nestedOpen = nested.IsOpen, actionLoaded = nestedAction.IsLoaded, declarationLoaded = nestedDeclaration.IsLoaded, hostFound = ReferenceEquals(BleeckerModalHost.Find(nested), host), triggerLoaded = ((FrameworkElement)nested.Trigger!).IsLoaded, popovers = Elements(root).OfType<BleeckerPopover>().Select(p => new { p.Name, p.IsLoaded, p.IsOpen, p.ActualWidth, p.ActualHeight }).ToArray(), parts = Elements(nested).Select(p => new { p.Name, type = p.GetType().Name }).ToArray() };
                var flags = typeof(BleeckerPopover).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Where(f => new[] { "root", "popup", "observedRoot", "mountPending", "rendering", "renderPending", "presentationQueued", "modalHost", "modalBranch", "model", "presented", "naturalDirty", "bodySlot", "focusHost" }.Contains(f.Name)).ToDictionary(f => f.Name, f => f.GetValue(nested) is { } value ? value is bool boolean ? boolean.ToString() : value.GetType().Name : "null");
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(output)!, "nested-" + scheme + ".json"), JsonSerializer.Serialize(new { diagnostic, flags, nested.IsEnabled }));
                Require(parent.Layer.Branches.Count == 2 && parent.Layer.Branches.All(b => b.Frame.IsHitTestVisible) && host.AllowsFocusTarget(nestedAction) && host.SequentialTargets().Contains(nestedAction), "Nested popup lost its nearest modal owner: " + JsonSerializer.Serialize(diagnostic));
                return new { branches = 2, sameModalOwner = true, nestedSequentialTarget = true };
            });
            var childSurface = new ContentControl { Name = "ChildModal", Width = 320, Height = 220, Content = new Button { Content = "Child modal action" } }; var childOverlay = new Grid(); childOverlay.Children.Add(childSurface);
            var child = host.Attach(nestedDeclaration, childSurface, childOverlay, () => { });
            await Case("child-modal-shields-owned-popup-branches", async () =>
            {
                host.Activate(child); await Layout(); Require(child.Ancestors.Length == 1 && ReferenceEquals(child.Ancestors[0], parent) && parent.Layer.Branches.All(b => !b.Frame.IsHitTestVisible) && !host.AllowsFocusTarget(originalAction) && !host.AllowsFocusTarget(nestedAction), "A popup branch escaped its inactive modal.");
                Require(popover.IsOpen && nested.IsOpen && originalAction.IsEnabled && Bound(), "Child activation rewrote caller state."); return new { ancestorDepth = 1, branches = 2, allBlocked = true, callerStateRetained = true };
            });
            await Case("inactive-owner-can-retain-controlled-popup", async () =>
            {
                open.IsOn = false; await Layout(); Require(parent.Layer.Branches.Count == 0 && nested.IsOpen, "Closing outer presentation retained an orphan branch or rewrote nested logical state.");
                open.IsOn = true; await Layout(); Require(parent.Layer.Branches.Count == 2 && parent.Layer.Branches.All(b => !b.Frame.IsHitTestVisible) && !host.AllowsFocusTarget(originalAction), "A newly opened inactive branch became interactive."); return new { branches = 2, allBlocked = true, controlledOpen = true, nestedLogicalOpenRetained = true };
            });
            await Case("bound-enabled-state-survives-shield", async () =>
            {
                enabled.IsOn = false; await Layout(); Require(!originalAction.IsEnabled && Bound(), "Caller enabled binding stopped updating."); enabled.IsOn = true; await Layout(); Require(originalAction.IsEnabled && parent.Layer.Branches.All(b => !b.Frame.IsHitTestVisible), "Private shield overwrote caller enabled state."); return new { liveDisabledAndRestored = true, bindingRetained = true, shieldRetained = true };
            });
            await Case("closing-child-restores-parent-branches", async () =>
            {
                host.Deactivate(child); child.Dispose(); await Layout(); Require(ReferenceEquals(host.ActiveSurface, surface) && parent.Layer.Branches.Count == 2 && parent.Layer.Branches.All(b => b.Frame.IsHitTestVisible) && host.AllowsFocusTarget(nestedAction), "Parent popup ownership did not resume.");
                await Capture("owned-" + scheme.ToLowerInvariant() + ".png"); return new { branches = 2, allInteractive = true, nestedTargetRestored = true };
            });
            await Case("controlled-close-is-request-only", async () =>
            {
                var before = requests; popover.Close(); await Layout(); Require(requests == before + 1 && popover.IsOpen && open.IsOn && parent.Layer.Branches.Count == 2 && Bound(), "Controlled close mutated presentation/state before caller approval.");
                nested.Open = false; open.IsOn = false; await Layout(); Require(parent.Layer.Branches.Count == 0 && !popover.IsOpen && !nested.IsOpen && Bound(), "Caller close retained a portal or lost binding."); return new { requestDelta = 1, callerStatePreservedUntilUpdate = true, closedBranches = 0, bindingsRetained = true };
            });
            await Case("reopen-retains-original-edit-and-controls", async () =>
            {
                originalEditor.Text = "Edited fictional value"; open.IsOn = true; await Layout(); Require(parent.Layer.Branches.Count == 1 && ReferenceEquals(popover.Content, content) && originalEditor.Text == "Edited fictional value" && Bound(), "Reopen replaced the original content or edit."); return new { branches = 1, originalContent = true, editRetained = true, bindingsRetained = true };
            });
            await Case("template-replacement-releases-portal", async () =>
            {
                var template = popover.Template; popover.Template = null; await Layout(); Require(parent.Layer.Branches.Count == 0 && popover.IsOpen && Bound(), "Null template retained its portal or changed controlled state.");
                popover.Template = template; await Layout(); Require(parent.Layer.Branches.Count == 1 && originalEditor.Text == "Edited fictional value" && Bound(), "Template remount replaced controls or failed to reattach."); return new { detachedBranches = 0, remountedBranches = 1, editAndBindingsRetained = true };
            });
            await Case("declaration-unload-releases-and-remounts", async () =>
            {
                body.Children.Remove(popover); await Layout(); Require(parent.Layer.Branches.Count == 0 && popover.IsOpen && Bound(), "Unloaded declaration retained a portal or changed logical state."); body.Children.Add(popover); await Layout(); Require(parent.Layer.Branches.Count == 1 && originalEditor.Text == "Edited fictional value", "Declaration remount lost original content."); return new { detachedBranches = 0, remountedBranches = 1, logicalOpenRetained = true, editRetained = true };
            });
            await Case("modal-teardown-restores-background-popup", async () =>
            {
                parent.Dispose(); await Layout(); Require(host.ActiveSurface is null && !host.BackgroundPointerBlocked && !originalAction.IsLoaded && popover.IsOpen && Bound(), "Modal teardown retained ownership or changed caller state.");
                var frame = Elements(root).OfType<Border>().Single(b => b.Child is ContentControl c && Elements(c).Contains(backgroundAction)); Require(frame.IsHitTestVisible && backgroundPopover.IsOpen && host.AllowsFocusTarget(backgroundAction), "Background popup did not resume."); return new { activeModalRemoved = true, ownedContentUnloaded = true, callerStateRetained = true, backgroundInteractive = true };
            });
            await Case("root-unload-releases-background-portal", async () =>
            {
                root.Children.Remove(host); await Layout(); Require(!backgroundAction.IsLoaded && backgroundPopover.IsOpen, "Root unload retained a branch or rewrote its logical state."); root.Children.Add(host); await Layout(); Require(backgroundAction.IsLoaded && backgroundPopover.IsOpen && !host.BackgroundPointerBlocked, "Root remount did not restore original background popup."); return new { contentUnloaded = true, remounted = true, logicalOpenRetained = true };
            });
            await Case("catalog-confirmation-from-original-popup", async () =>
            {
                root.Children.Clear(); await Layout();
                var fixture = new Orchard.Catalog.PopoversFixture(); var fixtureHost = new BleeckerModalHost { Content = fixture }; root.Children.Add(fixtureHost); await Layout();
                var originalPopover = Elements(fixture).OfType<BleeckerPopover>().Single(p => p.Name == "Default"); originalPopover.Open = true; await Layout();
                var editor = Elements(root).OfType<TextBox>().Single(t => t.Text == "Original caller input"); editor.Text = "Edited fictional catalog setting";
                var action = Elements(root).OfType<Button>().Single(b => b.Content is string value && value == "Confirm fictional settings");
                var invoke = FrameworkElementAutomationPeer.CreatePeerForElement(action).GetPattern(PatternInterface.Invoke) as IInvokeProvider ?? throw new InvalidOperationException("Original catalog button has no native invoke provider.");
                invoke.Invoke(); softwareNativeInvokes++; await Task.Delay(260); await Layout();
                var confirmation = Elements(root).OfType<BleeckerAlertDialog>().Single(a => a.Title == "Apply fictional settings?");
                var frame = Elements(root).OfType<Border>().Single(b => b.Child is ContentControl c && Elements(c).Contains(editor));
                Require(confirmation.IsOpen && fixtureHost.ActiveSurface is not null && !frame.IsHitTestVisible && originalPopover.IsOpen && editor.Text == "Edited fictional catalog setting", "The actual catalog popup/confirmation path lost shielding or original edits.");
                await Capture("catalog-confirmation-" + scheme.ToLowerInvariant() + ".png");
                confirmation.Cancel(); await Task.Delay(180); await Layout();
                Require(!confirmation.IsOpen && fixtureHost.ActiveSurface is null && frame.IsHitTestVisible && originalPopover.IsOpen && editor.Text == "Edited fictional catalog setting", "Catalog cancellation did not restore the original popup.");
                return new { nativeButtonInvokes = 1, originalEditRetained = true, backgroundShieldedDuringConfirmation = true, callerCanceled = true, popupRestored = true };
            });
            await Case("transfer-between-native-popup-and-shared-host", async () =>
            {
                root.Children.Clear(); await Layout();
                var editor = new TextBox { Text = "Original transfer edit", Width = 180 }; var toggle = new ToggleSwitch { IsOn = true };
                var moving = new BleeckerPopover { Trigger = new Button { Content = "Transfer trigger" }, Content = editor }; moving.SetBinding(BleeckerPopover.OpenProperty, new Binding { Source = toggle, Path = new(nameof(ToggleSwitch.IsOn)) });
                var movingOpens = 0; var movingCloses = 0; var movingRequests = 0; moving.Opened += (_, _) => movingOpens++; moving.Closed += (_, _) => movingCloses++; moving.OpenRequested += (_, _) => movingRequests++;
                var original = new Grid(); original.Children.Add(moving); allowStandalonePopup = true; root.Children.Add(original); await Layout();
                Require(VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Count == 1 && editor.IsLoaded, "Standalone transfer did not open its native Popup."); standalonePopupPhases++;
                editor.Text = "Edited transfer value"; root.Children.Remove(original); var newHost = new BleeckerModalHost { Content = original }; root.Children.Add(newHost); await Layout();
                Require(VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Count == 0 && editor.IsLoaded && moving.IsOpen && ReferenceEquals(moving.Content, editor), "Transfer into the host lost original presentation/content."); allowStandalonePopup = false;
                Require(movingOpens == 2 && movingCloses == 1 && movingRequests == 0 && moving.GetBindingExpression(BleeckerPopover.OpenProperty) is not null && editor.Text == "Edited transfer value", "A stale native callback or transfer rewrote state/events.");
                allowStandalonePopup = true; root.Children.Remove(newHost); newHost.Content = null; root.Children.Add(original); await Layout();
                Require(VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Count == 1 && editor.IsLoaded && movingOpens == 3 && movingCloses == 2 && movingRequests == 0 && editor.Text == "Edited transfer value", "Transfer out of the host lost native presentation or original edits."); standalonePopupPhases++;
                toggle.IsOn = false; await Layout(); allowStandalonePopup = false;
                Require(VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Count == 0 && movingOpens == 3 && movingCloses == 3 && movingRequests == 0, "Caller close after transfer retained a popup or duplicate event.");
                return new { nativePopupPhases = 2, hostPopupCount = 0, opens = movingOpens, closes = movingCloses, requests = movingRequests, originalContentAndEditRetained = true, bindingRetained = true };
            });
            root.Children.Clear(); await Layout();
        }
        string Hash(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-popover-modal-branch-v1", passed = failures.Count == 0, cases, captures, failures, guards, activations, focus, softwareNativeInvokes, standalonePopupPhases, librarySha256 = Hash(typeof(BleeckerPopover).Assembly.Location), hostSha256 = Hash(hostPath), architecture = RuntimeInformation.ProcessArchitecture.ToString(), activateCalled = false, showCalled = false, focusCalled = false, hardwareInputSent = false, nativeKeyboardPointerVerified = false, sourceRasterParityVerified = false }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(failures.Count == 0 ? 0 : 1);
    }
}
