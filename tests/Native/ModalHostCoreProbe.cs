using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;

namespace Orchard.Testing;

// Internal layout prototype, not an AlertDialog implementation. No Popup,
// ContentDialog, focus request, activation, visibility or physical input.
internal static class ModalHostCoreProbe
{
    private delegate bool WindowVisitor(nint window, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(WindowVisitor visitor, nint parameter);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint window);
    [Microsoft.UI.Xaml.Data.Bindable] private sealed class Model : INotifyPropertyChanged
    {
        private bool enabled = true, hit = true; private string text = "Original editor";
        public bool Enabled { get => enabled; set { enabled = value; PropertyChanged?.Invoke(this, new(nameof(Enabled))); } }
        public bool Hit { get => hit; set { hit = value; PropertyChanged?.Invoke(this, new(nameof(Hit))); } }
        public string Text { get => text; set { text = value; PropertyChanged?.Invoke(this, new(nameof(Text))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    private static bool Within(DependencyObject? target, DependencyObject root)
    {
        for (var node = target; node is not null; node = VisualTreeHelper.GetParent(node)) if (ReferenceEquals(node, root)) return true;
        return false;
    }
    private static IEnumerable<FrameworkElement> Elements(DependencyObject root)
    {
        if (root is FrameworkElement element) yield return element;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) foreach (var child in Elements(VisualTreeHelper.GetChild(root, i))) yield return child;
    }
    internal static async Task RunAsync(Window window, string reference, string output, string hostPath)
    {
        var root = new Grid { Width = 800, Height = 600, UseLayoutRounding = false }; window.Content = root; window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(800, 600));
        var guards = 0; var activations = 0; var focus = 0; var failures = new List<object>(); var cases = new List<object>(); var captures = new List<object>();
        window.Activated += (_, a) => { if (a.WindowActivationState != WindowActivationState.Deactivated) activations++; }; root.GotFocus += (_, _) => focus++;
        void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        void Guard()
        {
            guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var visible = false;
            EnumWindows((handle, _) => { GetWindowThreadProcessId(handle, out var owner); visible |= owner == Environment.ProcessId && IsWindowVisible(handle); return true; }, 0);
            Require(!visible && !window.Visible && !window.AppWindow.IsVisible && foreground != Environment.ProcessId && activations == 0 && focus == 0, "Modal host prototype acquired visibility, activation or focus.");
        }
        async Task Layout()
        {
            var idle = new TaskCompletionSource(); Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => idle.SetResult()), "Could not drain native style initialization."); await idle.Task; await Task.Delay(20); root.UpdateLayout();
            for (var i = 0; i < 6; i++) { root.Measure(new Size(800, 600)); root.Arrange(new Rect(0, 0, 800, 600)); root.UpdateLayout(); Guard(); await Task.Delay(15); }
            Require(root.XamlRoot is { IsHostVisible: false, RasterizationScale: 1 } && root.XamlRoot.Size == new Size(800, 600), "Modal host requires its actual hidden unscaled viewport.");
            Require(VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Count == 0, "Modal host prototype opened a native popup.");
        }
        object Box(FrameworkElement element) { var b = element.TransformToVisual(root).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight)); return new { x = b.X, y = b.Y, width = b.Width, height = b.Height }; }
        UIElement? Top(Point point) => VisualTreeHelper.FindElementsInHostCoordinates(point, root).FirstOrDefault();
        Directory.CreateDirectory(Path.GetDirectoryName(output)!); var sourceBytes = await File.ReadAllBytesAsync(reference); using var source = JsonDocument.Parse(sourceBytes);
        var rows = source.RootElement.GetProperty("interactions").EnumerateArray().ToArray();
        async Task<byte[]> Capture(string file)
        {
            var bitmap = new RenderTargetBitmap(); var operation = bitmap.RenderAsync(root); var task = operation.AsTask(); if (await Task.WhenAny(task, Task.Delay(2000)) != task) { operation.Cancel(); throw new TimeoutException("Modal host capture exceeded two seconds."); } await task; Guard();
            Require(bitmap.PixelWidth == 800 && bitmap.PixelHeight == 600, "Modal host capture was rescaled.");
            var buffer = await bitmap.GetPixelsAsync(); var pixels = new byte[buffer.Length]; using (var reader = Windows.Storage.Streams.DataReader.FromBuffer(buffer)) reader.ReadBytes(pixels);
            using var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream(); var encoder = await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId, stream); encoder.SetPixelData(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied, 800, 600, 96, 96, pixels); await encoder.FlushAsync();
            using var pngReader = new Windows.Storage.Streams.DataReader(stream.GetInputStreamAt(0)); await pngReader.LoadAsync((uint)stream.Size); var bytes = new byte[stream.Size]; pngReader.ReadBytes(bytes); await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!, file), bytes);
            captures.Add(new { file, sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)), width = 800, height = 600, scope = "complete-native-client-diagnostic", sourceRasterCompared = false }); return pixels;
        }
        foreach (var scheme in new[] { "Light", "Dark" }) foreach (var scenario in new[] { "binding-and-pointer-shield", "branch-and-layer-order", "simultaneous-ancestry", "ancestor-first-teardown", "reopen-before-detach", "replacement-and-branch-release" })
        {
            var samples = new List<object>();
            BleeckerButton? originalAction = null; var currentStage = "create-background";
            try
            {
                root.Children.Clear(); root.RequestedTheme = Enum.Parse<ElementTheme>(scheme); root.Background = new SolidColorBrush(scheme == "Light" ? Microsoft.UI.Colors.White : Windows.UI.Color.FromArgb(255, 24, 37, 51));
                var data = new Model(); var original = new Grid(); var editor = new TextBox { Name = "OriginalEditor", Width = 220, Height = 40, Margin = new(20, 30, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
                var action = new BleeckerButton { Name = "OriginalAction", Content = "Original action", Width = 160, Height = 40, Margin = new(20, 100, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
                originalAction = action;
                var enabledBinding = new Binding { Source = data, Path = new(nameof(Model.Enabled)), Mode = BindingMode.OneWay }; var hitBinding = new Binding { Source = data, Path = new(nameof(Model.Hit)), Mode = BindingMode.OneWay }; var textBinding = new Binding { Source = data, Path = new(nameof(Model.Text)), Mode = BindingMode.TwoWay };
                action.SetBinding(Control.IsEnabledProperty, enabledBinding); action.SetBinding(UIElement.IsHitTestVisibleProperty, hitBinding); editor.SetBinding(TextBox.TextProperty, textBinding);
                original.Children.Add(editor); original.Children.Add(action); currentStage = "original-background-without-host"; root.Children.Add(original); await Layout(); root.Children.Remove(original);
                currentStage = "background-inside-native-host"; var host = new BleeckerModalLayerHostCore { BackgroundContent = original }; root.Children.Add(host); await Layout();
                var attached = new List<BleeckerModalLayerHostCore.Layer>();
                BleeckerModalLayerHostCore.Layer Layer(string name, int index, bool paint = true)
                {
                    var surface = new Grid { Name = name, Width = 300, Height = 160, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new(200 + index * 20, 180 + index * 20, 0, 0), Background = new SolidColorBrush(paint ? index == 0 ? Microsoft.UI.Colors.SteelBlue : Microsoft.UI.Colors.IndianRed : Microsoft.UI.Colors.Transparent) };
                    if (paint) surface.Children.Add(new Button { Name = name + "Action", Content = name + " action", Width = 140, Height = 36 });
                    var overlay = new Grid(); overlay.Children.Add(surface); var layer = host.AttachLayer(surface, overlay); attached.Add(layer); return layer;
                }
                var peer = FrameworkElementAutomationPeer.CreatePeerForElement(host)!;
                IEnumerable<AutomationPeer> PeerTree(AutomationPeer p) { yield return p; foreach (var child in p.GetChildren() ?? []) foreach (var nested in PeerTree(child)) yield return nested; }
                string[] Names() => PeerTree(peer).Select(p => p.GetName()).Where(n => !string.IsNullOrEmpty(n)).ToArray();
                void Snapshot(string stage)
                {
                    Require(ReferenceEquals(host.BackgroundContent, original) || scenario == "replacement-and-branch-release", "Original background was replaced.");
                    Require(ReferenceEquals(action.GetBindingExpression(Control.IsEnabledProperty)?.ParentBinding, enabledBinding) && ReferenceEquals(action.GetBindingExpression(UIElement.IsHitTestVisibleProperty)?.ParentBinding, hitBinding) && ReferenceEquals(editor.GetBindingExpression(TextBox.TextProperty)?.ParentBinding, textBinding), "Modal host replaced an original consumer binding.");
                    samples.Add(new { stage, host = Box(host), active = host.ActiveSurface?.Name, backgroundPointerBlocked = host.BackgroundPointerBlocked, backgroundFocusAllowed = host.AllowsFocusTarget(editor), backgroundHit = Within(Top(new(40, 50)), editor), originalEnabled = action.IsEnabled, originalHitTestVisible = action.IsHitTestVisible, originalText = editor.Text, bindingsRetained = true, peerNames = Names(), layers = attached.Select(l => new { name = l.Surface.Name, active = l.Active, frame = Box(l.Frame), zIndex = Canvas.GetZIndex(l.Frame), pointerAllowed = l.Frame.IsHitTestVisible, branches = l.Branches.Select(b => new { name = b.Root.Name, frame = Box(b.Frame), zIndex = Canvas.GetZIndex(b.Frame), pointerAllowed = b.Frame.IsHitTestVisible }).ToArray() }).ToArray() });
                }
                Snapshot("background-only"); Require(Within(Top(new(40, 50)), editor), "Original editor is not initially hittable.");
                if (scenario == "binding-and-pointer-shield")
                {
                    var before = await Capture(scheme + "-unblocked.png"); var layer = Layer("TransparentSurface", 0, false); host.Activate(layer, [], editor); await Layout(); Snapshot("blocked");
                    Require(host.BackgroundPointerBlocked && !host.AllowsFocusTarget(editor) && !Within(Top(new(40, 50)), editor), "Owned wrapper did not shield background hit testing.");
                    Require(action.IsEnabled && action.IsHitTestVisible && Names().Length == 0, "Shield changed caller state or exposed background peers.");
                    var blocked = await Capture(scheme + "-blocked.png"); Require(before.SequenceEqual(blocked), "Pointer shielding changed original native button/text paint.");
                    data.Enabled = false; data.Hit = false; data.Text = "Updated during modal"; await Layout(); Snapshot("caller-updated-while-blocked"); Require(!action.IsEnabled && !action.IsHitTestVisible && editor.Text == data.Text, "Original bindings stopped updating.");
                    var dynamic = new Button { Content = "Dynamic background", Name = "DynamicBackground", HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom }; original.Children.Add(dynamic); await Layout(); Snapshot("dynamic-background-blocked"); Require(!host.AllowsFocusTarget(dynamic) && !Names().Contains("Dynamic background"), "Dynamic background bypassed the active layer.");
                    host.Deactivate(layer); await Layout(); Snapshot("deactivated-presence-retained"); Require(!host.BackgroundPointerBlocked && host.AllowsFocusTarget(editor) && !action.IsEnabled && !action.IsHitTestVisible, "Closing overwrote current caller state.");
                    data.Enabled = true; data.Hit = true; await Layout(); Require(action.IsEnabled && action.IsHitTestVisible && Within(Top(new(40, 50)), editor), "Background did not resume through its original bindings."); host.DetachLayer(layer); attached.Remove(layer); await Layout(); Snapshot("detached");
                }
                else
                {
                    var parent = Layer("DrawerSurface", 0); var middle = Layer("ModalSurface", 1); var branchRoot = new Button { Name = "ParentBranch", Content = "Parent branch", Width = 170, Height = 60 }; var branch = host.AttachBranch(middle, branchRoot, new(260, 240)); var child = Layer("AlertSurface", 2);
                    var middleOpener = ((Grid)parent.Surface).Children.OfType<Button>().Single(); var alertOpener = ((Grid)middle.Surface).Children.OfType<Button>().Single();
                    if (scenario == "simultaneous-ancestry") { host.Activate(child, [parent.Surface, middle.Surface], alertOpener); host.Activate(middle, [parent.Surface], middleOpener); host.Activate(parent, [], editor); }
                    else { host.Activate(parent, [], editor); host.Activate(middle, [parent.Surface], middleOpener); host.Activate(child, [parent.Surface, middle.Surface], alertOpener); }
                    await Layout(); Snapshot("nested-active-child"); Require(ReferenceEquals(host.ActiveSurface, child.Surface) && Canvas.GetZIndex(parent.Frame) == 50 && Canvas.GetZIndex(middle.Frame) == 51 && Canvas.GetZIndex(child.Frame) == 52 && !branch.Frame.IsHitTestVisible && !host.AllowsFocusTarget(branchRoot), "Native nested stack did not preserve source order/inert branch ownership.");
                    Require(Within(Top(new(280, 260)), child.Surface), "Inactive branch or parent covered the active child.");
                    var sourceId = scenario == "simultaneous-ancestry" ? "simultaneous-original-modal-ancestry" : "ancestor-closes-before-live-child"; var sourceLayers = rows.Single(r => r.GetProperty("scheme").GetString() == scheme && r.GetProperty("id").GetString() == sourceId).GetProperty("samples")[0].GetProperty("layers");
                    var orderedSource = sourceLayers.EnumerateArray().OrderBy(l => int.Parse(l.GetProperty("zIndex").GetString()!)).ToArray();
                    foreach (var (layer, index) in new[] { (parent, 0), (middle, 1), (child, 2) }) Require(Canvas.GetZIndex(layer.Frame).ToString() == orderedSource[index].GetProperty("zIndex").GetString() && layer.Frame.IsHitTestVisible == !orderedSource[index].GetProperty("inert").GetBoolean(), "Native three-layer order differs from the pinned source.");
                    await Capture(scheme + "-" + scenario + "-nested.png");
                    if (scenario == "ancestor-first-teardown")
                    {
                        var removed = host.Deactivate(parent); await Layout(); Snapshot("inactive-parent-closed"); Require(!removed.WasActive && ReferenceEquals(host.ActiveSurface, child.Surface) && Canvas.GetZIndex(middle.Frame) == 50 && Canvas.GetZIndex(child.Frame) == 51, "Ancestor-first closure changed active child or survivor depth.");
                        Require(!host.Deactivate(middle).WasActive, "Removing the middle ancestor restored focus before the live child closed.");
                        var last = host.Deactivate(child); Require(last.WasActive && ReferenceEquals(last.PreviousFocus, editor), "Removed ancestor did not rewire the original restoration target.");
                    }
                    else if (scenario == "reopen-before-detach")
                    {
                        host.Deactivate(child); await Layout(); Snapshot("child-closing"); host.Activate(child, [parent.Surface, middle.Surface], branchRoot); await Layout(); Snapshot("child-reopened"); Require(ReferenceEquals(host.ActiveSurface, child.Surface) && !branch.Frame.IsHitTestVisible, "Reopening lost the child or reenabled its parent's branch."); host.Deactivate(child); host.Deactivate(middle); host.Deactivate(parent);
                    }
                    else
                    {
                        host.Deactivate(child); await Layout(); Snapshot("parent-and-branch-active"); Require(host.AllowsFocusTarget(branchRoot) && branch.Frame.IsHitTestVisible && Canvas.GetZIndex(branch.Frame) == 52 && Within(Top(new(280, 260)), branchRoot), "Parent branch did not regain input ownership.");
                        Require(Names().Contains("Parent branch") && !Names().Contains("AlertSurface action"), "Peer scope exposed a closing child or excluded the active branch.");
                        if (scenario == "replacement-and-branch-release")
                        {
                            var replacement = new Grid(); replacement.Children.Add(new Button { Content = "Replacement background" }); host.BackgroundContent = replacement; await Layout(); Snapshot("replacement-background-blocked"); Require(host.BackgroundPointerBlocked && !host.AllowsFocusTarget(replacement), "Replacing background bypassed the modal shield.");
                            host.DetachBranch(branch); await Layout(); Snapshot("branch-detached"); Require(VisualTreeHelper.GetParent(branchRoot) is null && !host.AllowsFocusTarget(branchRoot), "Detached branch retained its old owner.");
                            host.BackgroundContent = original;
                        }
                        host.Deactivate(middle); host.Deactivate(parent);
                    }
                    await Layout(); Snapshot("all-inactive-paint-retained"); Require(!host.BackgroundPointerBlocked && Within(Top(new(40, 50)), editor), "Closing overlays continued to intercept the original background.");
                    host.DetachLayer(child); attached.Remove(child); host.DetachLayer(middle); attached.Remove(middle); host.DetachLayer(parent); attached.Remove(parent); await Layout(); Snapshot("all-detached"); Require(VisualTreeHelper.GetParent(branchRoot) is null && VisualTreeHelper.GetParent(parent.Overlay) is null && VisualTreeHelper.GetParent(middle.Overlay) is null && VisualTreeHelper.GetParent(child.Overlay) is null, "Modal or branch retained an obsolete visual parent.");
                }
                Guard(); cases.Add(new { scheme, scenario, passed = true, samples });
            }
            catch (Exception error)
            {
                Application.Current.Resources.TryGetValue(typeof(BleeckerButton), out var buttonStyle);
                failures.Add(new { scheme, scenario, stage = currentStage, buttonTemplateNull = originalAction?.Template is null, buttonStyleNull = originalAction?.Style is null, globalButtonStyleTarget = (buttonStyle as Style)?.TargetType.FullName, buttonDefaultStyleKey = originalAction is null ? null : typeof(Control).GetProperty("DefaultStyleKey", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(originalAction)?.ToString(), buttonVisualTree = originalAction is null ? null : Elements(originalAction).Take(18).Select(e => new { e.Name, type = e.GetType().FullName }).ToArray(), error = error.ToString() }); cases.Add(new { scheme, scenario, passed = false, samples });
            }
        }
        string Hash(string file) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-modal-host-core-probe-v1", passed = failures.Count == 0, scope = "hidden-internal-modal-layout-pointer-query-peer-and-binding-prototype", cases, captures, failures, guards, activations, focus, referenceSha256 = Convert.ToHexStringLower(SHA256.HashData(sourceBytes)), librarySha256 = Hash(typeof(BleeckerButton).Assembly.Location), hostSha256 = Hash(hostPath), processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(), osArchitecture = RuntimeInformation.OSArchitecture.ToString(), activateCalled = false, showCalled = false, focusCalled = false, inputSent = false, popupOpened = false, actualFocusCancellationVerified = false, actualKeyboardVerified = false, nativeAlertDialogImplemented = false, nativeModalAcceptanceVerified = false, completeSourceRasterCompared = false }, new JsonSerializerOptions { WriteIndented = true }));
        window.Close(); Environment.Exit(failures.Count == 0 ? 0 : 1);
    }
}
