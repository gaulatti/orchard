using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Orchard;

namespace Orchard.Testing;

internal static class PopoverReferenceFixture
{
    private delegate bool WindowVisitor(nint window, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(WindowVisitor visitor, nint parameter);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint window);
    private sealed class Command(Action action) : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => action();
    }
    [Microsoft.UI.Xaml.Data.Bindable]
    private sealed class Model : INotifyPropertyChanged
    {
        private double width = 160, height = 48; private string value = "Original";
        public double Width { get => width; set { width = value; PropertyChanged?.Invoke(this, new(nameof(Width))); } }
        public double Height { get => height; set { height = value; PropertyChanged?.Invoke(this, new(nameof(Height))); } }
        public string Value { get => value; set { this.value = value; PropertyChanged?.Invoke(this, new(nameof(Value))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    private sealed class Selector(DataTemplate template) : Microsoft.UI.Xaml.Controls.DataTemplateSelector
    {
        internal readonly List<object?> Items = [];
        protected override DataTemplate SelectTemplateCore(object item) { Items.Add(item); return template; }
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) => SelectTemplateCore(item);
    }
    private static double Number(JsonElement spec, string name, double otherwise) => spec.TryGetProperty(name, out var value) ? value.GetDouble() : otherwise;
    private static bool Flag(JsonElement spec, string name) => spec.TryGetProperty(name, out var value) && value.GetBoolean();
    private static object? Brush(Brush? value) => value is SolidColorBrush brush ? new { color = $"#{brush.Color.A:X2}{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2}", opacity = brush.Opacity } : null;
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node)
    {
        if (node is FrameworkElement element) yield return element;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) foreach (var child in Elements(VisualTreeHelper.GetChild(node, i))) yield return child;
    }
    internal static async Task RunAsync(Window window, string referencePath, string output, string hostPath)
    {
        var bytes = await File.ReadAllBytesAsync(referencePath); using var reference = JsonDocument.Parse(bytes);
        var root = new Canvas { Width = 800, Height = 600, UseLayoutRounding = false }; window.Content = root;
        var frames = new List<object>(); var captures = new List<object>(); var checks = new List<object>(); var failures = new List<object>(); var guards = 0; var activations = 0; var focus = 0;
        window.Activated += (_, args) => { if (args.WindowActivationState != WindowActivationState.Deactivated) activations++; }; root.GotFocus += (_, _) => focus++;
        void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        void Guard()
        {
            guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var visible = false;
            EnumWindows((handle, _) => { GetWindowThreadProcessId(handle, out var process); visible |= process == Environment.ProcessId && IsWindowVisible(handle); return true; }, 0);
            Require(!visible && !window.Visible && !window.AppWindow.IsVisible && foreground != Environment.ProcessId && activations == 0 && focus == 0, "Hidden Popover acquired visibility, activation or focus.");
        }
        async Task Layout()
        {
            for (var tick = 0; tick < 6; tick++)
            {
                root.Measure(new Size(root.Width, root.Height)); root.Arrange(new Rect(0, 0, root.Width, root.Height)); root.UpdateLayout(); Guard();
                var idle = new TaskCompletionSource();
                Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => idle.SetResult()), "Could not drain native presentation callbacks.");
                await idle.Task; await Task.Delay(15);
            }
            Require(root.XamlRoot is { IsHostVisible: false, RasterizationScale: 1 } && root.XamlRoot.Size == new Size(root.Width, root.Height), "Popover client was not the actual hidden unscaled source viewport.");
        }
        async Task Resize(double width, double height) { root.Width = width; root.Height = height; window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32((int)width, (int)height)); await Layout(); }
        object Box(FrameworkElement element) { var box = element.TransformToVisual(root).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight)); return new { x = box.X, y = box.Y, width = box.Width, height = box.Height }; }
        async Task Capture(FrameworkElement child, string scheme)
        {
            var bitmap = new Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap(); var operation = bitmap.RenderAsync(child); var task = operation.AsTask();
            if (await Task.WhenAny(task, Task.Delay(2000)) != task) { operation.Cancel(); throw new TimeoutException("Popover child capture exceeded two seconds."); } await task; Guard();
            var tree = Elements(child).Where(e => e is BleeckerOverlaySurface or Border).Select(e => new
            {
                type = e.GetType().Name, e.Name, actual = e.ActualHeight, desired = e.DesiredSize.Height,
                padding = e is Border b ? b.Padding.Top : e is Control c ? c.Padding.Top : 0
            });
            Require(bitmap.PixelWidth == 224 && bitmap.PixelHeight == 78,
                $"Native representative Popover capture differs: pixels={bitmap.PixelWidth}x{bitmap.PixelHeight}, child={child.ActualWidth}x{child.ActualHeight}, tree=" + JsonSerializer.Serialize(tree));
            var pixels = await bitmap.GetPixelsAsync(); var data = new byte[pixels.Length]; using (var reader = Windows.Storage.Streams.DataReader.FromBuffer(pixels)) reader.ReadBytes(data);
            using var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream(); var encoder = await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId, stream);
            encoder.SetPixelData(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied, 224, 78, 96, 96, data); await encoder.FlushAsync();
            using var result = new Windows.Storage.Streams.DataReader(stream.GetInputStreamAt(0)); await result.LoadAsync((uint)stream.Size); var bytes = new byte[stream.Size]; result.ReadBytes(bytes);
            var file = "popover-" + scheme + ".png"; Directory.CreateDirectory(Path.GetDirectoryName(output)!); await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!, file), bytes);
            captures.Add(new { scheme, file, sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)), pixelWidth = bitmap.PixelWidth, pixelHeight = bitmap.PixelHeight, scope = "native-popup-child-only", completeSourceRasterCompared = false });
        }
        Popup? Presented() => VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).SingleOrDefault(popup => popup.Name == "PopoverPopup");
        async Task Case(string id, Func<Task> action)
        {
            try { root.Children.Clear(); await Resize(800, 600); await action(); Guard(); checks.Add(new { id, passed = true }); }
            catch (Exception error) { failures.Add(new { id, error = error.ToString() }); checks.Add(new { id, passed = false }); }
        }
        await Case("source-layout-matrix", async () =>
        {
            foreach (var row in reference.RootElement.GetProperty("cases").EnumerateArray())
            {
                root.Children.Clear(); var spec = row.GetProperty("spec"); var theme = Enum.Parse<ElementTheme>(row.GetProperty("scheme").GetString()!); root.RequestedTheme = theme;
                await Resize(Number(spec, "viewportWidth", 800), Number(spec, "viewportHeight", 600));
                var trigger = new Button { Content = "Open", Width = Number(spec, "triggerWidth", 80), Height = Number(spec, "triggerHeight", 32), MinWidth = 0, MinHeight = 0, IsEnabled = !Flag(spec, "disabled") };
                var content = Flag(spec, "empty") ? null : new Border { Width = Number(spec, "contentWidth", 160), Height = Number(spec, "contentHeight", 60) };
                if (content is not null) { var text = new TextBlock { Text = "Fictional content", LineHeight = 24, LineStackingStrategy = LineStackingStrategy.BlockLineHeight }; BleeckerTextLayout.SetUseReferenceLineBox(text, true); content.Child = text; }
                var owner = new BleeckerPopover { Trigger = trigger, Content = content, DefaultOpen = Flag(spec, "defaultOpen"), FlowDirection = Flag(spec, "rtl") ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, SideOffset = Number(spec, "sideOffset", 4) };
                if (spec.TryGetProperty("side", out var side)) owner.Side = Enum.Parse<BleeckerPopoverSide>(side.GetString()!, true);
                if (spec.TryGetProperty("align", out var align)) owner.Align = Enum.Parse<BleeckerPopoverAlign>(align.GetString()!, true);
                Canvas.SetLeft(owner, Number(spec, "x", 320)); Canvas.SetTop(owner, Number(spec, "y", 240)); root.Children.Add(owner); await Layout();
                var popup = Presented(); var surface = popup?.Child is { } child ? Elements(child).OfType<BleeckerOverlaySurface>().Single() : null;
                Require(ReferenceEquals(owner.Trigger, trigger) && ReferenceEquals(owner.Content, content), "Source fixture copied original caller values.");
                string Internal(string name) => typeof(BleeckerPopover).GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(owner) switch
                {
                    Popup p => $"popup(open={p.IsOpen},childLoaded={(p.Child as FrameworkElement)?.IsLoaded})",
                    FrameworkElement e => $"{e.GetType().Name}(loaded={e.IsLoaded},size={e.ActualWidth}x{e.ActualHeight},desired={e.DesiredSize.Width}x{e.DesiredSize.Height})",
                    { } value => value.ToString()!, _ => "null"
                };
                Require((popup is not null) == owner.IsOpen,
                    $"Logical and presented Popover state differ for {row.GetProperty("id").GetString()}: ownerLoaded={owner.IsLoaded}, triggerLoaded={trigger.IsLoaded}, logical={owner.IsOpen}, root={Internal("root")}, popup={Internal("popup")}, mount={Internal("mountPending")}, queued={Internal("presentationQueued")}, surface={Internal("surface")}.");
                frames.Add(new { id = row.GetProperty("id").GetString(), scheme = theme.ToString(), actualTheme = owner.ActualTheme.ToString(), expanded = owner.IsOpen, disabled = !trigger.IsEnabled, trigger = Box(trigger), popup = surface is null ? null : new { box = Box(surface), content = content is null ? null : Box(content), side = owner.ActualSide.ToString().ToLowerInvariant(), align = owner.ActualAlign.ToString().ToLowerInvariant(), paint = new { background = Brush(surface.Background), border = Brush(surface.BorderBrush), color = Brush(surface.Foreground), radius = surface.CornerRadius.TopLeft, padding = new[] { surface.Padding.Left, surface.Padding.Top, surface.Padding.Right, surface.Padding.Bottom }, borderWidth = surface.BorderThickness.Top, minimum = surface.MinWidth, fontSize = surface.FontSize } } });
                if (row.GetProperty("id").GetString() == "ltr-bottom-center" && popup?.Child is FrameworkElement captureRoot) await Capture(captureRoot, theme.ToString());
            }
        });
        await Case("native-trigger-command-before-request-and-cancellation", async () =>
        {
            var calls = 0; var requests = 0; var owner = new BleeckerPopover { Content = "Fictional content" }; var trigger = new Button { Content = "Original", Command = new Command(() => calls++) }; owner.Trigger = trigger;
            owner.OpenRequested += (_, _) => { Require(calls == 1, "Popover toggled before the original trigger command."); requests++; };
            root.Children.Add(owner); await Layout(); ((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(trigger).GetPattern(PatternInterface.Invoke)).Invoke(); await Layout();
            Require(calls == 1 && requests == 1 && owner.IsOpen && Presented() is not null, "Native original trigger activation did not reach the open request.");
            owner.Open = false; await Layout(); var blocked = 0; owner.OpenChanging += (_, args) => { args.Cancel = true; blocked++; }; owner.Toggle(); await Layout();
            Require(blocked == 1 && requests == 1 && !owner.IsOpen && Presented() is null, "Cancelled transition changed state or emitted a request.");
            ((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(trigger).GetPattern(PatternInterface.Invoke)).Invoke();
            owner.Trigger = new Button { Content = "Replacement trigger" }; await Layout();
            Require(calls == 2 && blocked == 1 && requests == 1 && !owner.IsOpen, "A retired trigger dispatched its queued transition.");
        });
        await Case("controlled-request-and-separate-default-history", async () =>
        {
            var owner = new BleeckerPopover { Trigger = new Button { Content = "Original" }, Content = "Fictional content", Open = false }; var requests = new List<bool>(); owner.OpenRequested += (_, value) => requests.Add(value); root.Children.Add(owner); await Layout();
            owner.Toggle(); await Layout(); Require(!owner.IsOpen && Presented() is null && requests.SequenceEqual(new[] { true }), "Ignored controlled request changed the source value.");
            owner.Open = true; await Layout(); Require(owner.IsOpen && Presented() is not null && requests.Count == 1, "External controlled state did not present without another request.");
            owner.Open = null; await Layout(); Require(!owner.IsOpen && Presented() is null, "Controlled state overwrote separate default history.");
            owner.DefaultOpen = true; await Layout(); Require(!owner.IsOpen, "Later DefaultOpen reset internal history."); owner.Toggle(); await Layout(); Require(owner.IsOpen && requests.SequenceEqual(new[] { true, true }), "Uncontrolled request did not update internal history.");
        });
        await Case("original-edit-state-template-retirement-and-remount", async () =>
        {
            var editor = new TextBox { Width = 160, Text = "Original" }; var trigger = new Button { Content = "Original" }; var owner = new BleeckerPopover { Trigger = trigger, Content = editor, DefaultOpen = true }; root.Children.Add(owner); await Layout(); editor.Text = "Caller edit"; editor.Select(2, 3);
            owner.Side = BleeckerPopoverSide.Top; owner.Align = BleeckerPopoverAlign.End; await Layout(); owner.Close(); await Layout(); owner.Toggle(); await Layout();
            Require(ReferenceEquals(owner.Content, editor) && editor.Text == "Caller edit" && editor.SelectionStart == 2 && editor.SelectionLength == 3, "Caller input state was lost across placement/close/reopen.");
            var previous = Presented(); owner.Template = (ControlTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='ContentControl'><Grid x:Name='PART_Layout'/></ControlTemplate>"); await Layout();
            Require(previous?.IsOpen == false && Presented() is { } current && !ReferenceEquals(previous, current) && ReferenceEquals(owner.Content, editor) && editor.Text == "Caller edit", $"Template retirement lost originals or retained the old popup: oldOpen={previous?.IsOpen}, newPopup={Presented() is not null}, loaded={owner.IsLoaded}, triggerLoaded={trigger.IsLoaded}, logical={owner.IsOpen}, editor={editor.Text}.");
            owner.Template = null; await Layout(); Require(Presented() is null && owner.IsOpen && ReferenceEquals(owner.Content, editor), "Clearing the template retained presentation or changed caller state.");
            owner.ClearValue(Control.TemplateProperty); await Layout(); Require(Presented() is not null && editor.Text == "Caller edit" && editor.SelectionStart == 2 && editor.SelectionLength == 3, "Restoring the default template lost original editor state.");
            root.Children.Clear(); await Layout(); Require(Presented() is null, "Unloaded Popover retained its popup."); root.Children.Add(owner); await Layout(); Require(owner.IsOpen && Presented() is not null && editor.Text == "Caller edit", "Remount reset logical state or original content.");
        });
        await Case("disabled-original-trigger-and-programmatic-state", async () =>
        {
            var trigger = new Button { Content = "Disabled", IsEnabled = false }; var owner = new BleeckerPopover { Trigger = trigger, Content = "Fictional content", Open = true }; root.Children.Add(owner); await Layout();
            Require(owner.IsOpen && Presented() is not null && !trigger.IsEnabled, "Disabled original trigger prevented authoritative programmatic open.");
            owner.IsEnabled = false; await Layout(); Require(Presented() is null && owner.Open == true, "Disabling the native owner mutated controlled state or left its popup interactive."); owner.IsEnabled = true; await Layout(); Require(Presented() is not null, "Re-enabling failed to restore authoritative presentation.");
        });
        await Case("original-template-models-and-live-intrinsic-size", async () =>
        {
            DataTemplate Template(string body) => (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>" + body + "</DataTemplate>");
            var triggerModel = new Model(); var contentModel = new Model();
            var triggerSelector = new Selector(Template("<Button Content='Template trigger' Tag='{Binding}' Width='80' Height='32'/>"));
            var contentSelector = new Selector(Template("<Border Width='{Binding Width}' Height='{Binding Height}' Tag='{Binding}'><TextBox Text='{Binding Value,Mode=TwoWay,UpdateSourceTrigger=PropertyChanged}' FontSize='13'/></Border>"));
            var owner = new BleeckerPopover { Trigger = triggerModel, TriggerTemplateSelector = triggerSelector, Content = contentModel, ContentTemplateSelector = contentSelector, DefaultOpen = true }; root.Children.Add(owner); await Layout();
            var popup = Presented()!; var original = Elements(popup.Child).OfType<Border>().Single(border => ReferenceEquals(border.Tag, contentModel)); var editor = Elements(original).OfType<TextBox>().Single();
            Require(triggerSelector.Items.Count > 0 && triggerSelector.Items.All(item => ReferenceEquals(item, triggerModel)) && contentSelector.Items.Count > 0 && contentSelector.Items.All(item => ReferenceEquals(item, contentModel)), "Selector did not receive the original trigger/content model.");
            contentModel.Width = 340; contentModel.Height = 130; contentModel.Value = "Model edit"; await Layout();
            var current = Presented()!; var surface = Elements(current.Child).OfType<BleeckerOverlaySurface>().Single();
            Require(ReferenceEquals(popup, current) && surface.ActualWidth == 358 && surface.ActualHeight == 148 && original.ActualWidth == 340 && original.ActualHeight == 130 && editor.Text == "Model edit", "Live model sizing or binding did not reach the native popup.");
            editor.Select(2, 3); owner.Side = BleeckerPopoverSide.Left; owner.FontSize = 20; owner.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red); await Resize(640, 600);
            Require(ReferenceEquals(owner.Trigger, triggerModel) && ReferenceEquals(owner.Content, contentModel) && Elements(Presented()!.Child).Contains(original) && editor.SelectionStart == 2 && editor.SelectionLength == 3 && editor.FontSize == 13 && editor.GetBindingExpression(TextBox.TextProperty) is not null, "Placement, typography or viewport changes replaced caller model controls or their explicit values.");
        });
        root.Children.Clear(); await Layout();
        Directory.CreateDirectory(Path.GetDirectoryName(output)!); string Hash(string file) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = failures.Count == 0, scope = "hidden-popover-source-layout-and-contracts", diagnosticOnly = true, frames, captures, checks, failures, processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(), osArchitecture = RuntimeInformation.OSArchitecture.ToString(), referenceSha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)), librarySha256 = Hash(typeof(BleeckerPopover).Assembly.Location), hostSha256 = Hash(hostPath), guardChecks = guards, activations, focus, activateCalled = false, showCalled = false, focusCalled = false, inputSent = false, sourceRasterParityVerified = false, nativeInputFocusVerified = false, visibleRuntimeVerified = false, nativeComponentAccepted = false }, new JsonSerializerOptions { WriteIndented = true }));
        window.Close(); Environment.Exit(failures.Count == 0 ? 0 : 1);
    }
}
