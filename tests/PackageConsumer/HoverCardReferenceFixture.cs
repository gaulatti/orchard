using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;

namespace Orchard.Testing;

// Hidden renderer/ownership diagnostics. Reflection supplies source hover/time
// inputs; it does not prove that native pointer or keyboard events work.
internal static class HoverCardReferenceFixture
{
    private delegate bool WindowVisitor(nint window, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(WindowVisitor visitor, nint parameter);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint window);
    private sealed class Command(Action action) : ICommand { public bool CanExecute(object? parameter) => true; public void Execute(object? parameter) => action(); public event EventHandler? CanExecuteChanged { add { } remove { } } }
    [Microsoft.UI.Xaml.Data.Bindable] private sealed class Model : INotifyPropertyChanged
    {
        private double width = 160, height = 60; private string value = "Original";
        public double Width { get => width; set { width = value; PropertyChanged?.Invoke(this, new(nameof(Width))); } }
        public double Height { get => height; set { height = value; PropertyChanged?.Invoke(this, new(nameof(Height))); } }
        public string Value { get => value; set { this.value = value; PropertyChanged?.Invoke(this, new(nameof(Value))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    private sealed class Selector(DataTemplate template) : DataTemplateSelector
    {
        internal readonly List<object> Items = [];
        protected override DataTemplate SelectTemplateCore(object item) { Items.Add(item); return template; }
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) => SelectTemplateCore(item);
    }
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node)
    {
        if (node is FrameworkElement element) yield return element;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) foreach (var child in Elements(VisualTreeHelper.GetChild(node, i))) yield return child;
    }
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static object ModelOf(BleeckerHoverCard owner) => typeof(BleeckerHoverCard).GetField("model", Private)!.GetValue(owner)!;
    private static void Hover(BleeckerHoverCard owner, bool value, double time, bool motion)
    {
        var model = ModelOf(owner); model.GetType().GetMethod("SetHovered", Private)!.Invoke(model, [value, time, motion]);
    }
    private static void Present(BleeckerHoverCard owner, double time) => typeof(BleeckerHoverCard).GetMethod("Present", Private)!.Invoke(owner, [time]);
    private static object? Brush(Brush? value) => value is SolidColorBrush brush ? new { color = $"#{brush.Color.A:X2}{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2}", opacity = brush.Opacity } : null;
    internal static async Task RunAsync(Window window, string referencePath, string output, string hostPath)
    {
        var referenceBytes = await File.ReadAllBytesAsync(referencePath); using var reference = JsonDocument.Parse(referenceBytes);
        var root = new Canvas { Width = 800, Height = 600, Background = new SolidColorBrush(Microsoft.UI.Colors.White), UseLayoutRounding = false }; window.Content = root;
        var frames = new List<object>(); var timelines = new List<object>(); var captures = new List<object>(); var checks = new List<object>(); var failures = new List<object>(); var guards = 0; var activations = 0; var focus = 0;
        window.Activated += (_, args) => { if (args.WindowActivationState != WindowActivationState.Deactivated) activations++; }; root.GotFocus += (_, _) => focus++;
        void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        void Guard()
        {
            guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var visible = false;
            EnumWindows((handle, _) => { GetWindowThreadProcessId(handle, out var process); visible |= process == Environment.ProcessId && IsWindowVisible(handle); return true; }, 0);
            Require(!visible && !window.Visible && !window.AppWindow.IsVisible && foreground != Environment.ProcessId && activations == 0 && focus == 0, "Hidden HoverCard acquired visibility, activation or focus.");
        }
        async Task Layout()
        {
            for (var tick = 0; tick < 6; tick++)
            {
                root.Measure(new Size(root.Width, root.Height)); root.Arrange(new Rect(0, 0, root.Width, root.Height)); root.UpdateLayout(); Guard();
                var idle = new TaskCompletionSource(); Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => idle.SetResult()), "Could not drain native HoverCard presentation callbacks."); await idle.Task; await Task.Delay(15);
            }
            Require(root.XamlRoot is { IsHostVisible: false, RasterizationScale: 1 } && root.XamlRoot.Size == new Size(root.Width, root.Height), "HoverCard requires its actual hidden unscaled viewport.");
            Require(VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Count == 0, "HoverCard introduced a popup.");
        }
        async Task Resize(double width, double height) { root.Width = width; root.Height = height; window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32((int)width, (int)height)); await Layout(); }
        object Box(FrameworkElement element) { var b = element.TransformToVisual(root).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight)); return new { x = b.X, y = b.Y, width = b.Width, height = b.Height }; }
        async Task Case(string id, Func<Task> action)
        {
            try { root.Children.Clear(); await Resize(800, 600); await action(); Guard(); checks.Add(new { id, passed = true }); }
            catch (Exception error) { failures.Add(new { id, error = error.ToString() }); checks.Add(new { id, passed = false }); }
        }
        async Task Capture(string name)
        {
            var bitmap = new RenderTargetBitmap(); var operation = bitmap.RenderAsync(root); var task = operation.AsTask(); if (await Task.WhenAny(task, Task.Delay(2000)) != task) { operation.Cancel(); throw new TimeoutException("HoverCard complete-client capture exceeded two seconds."); } await task; Guard();
            Require(bitmap.PixelWidth == root.Width && bitmap.PixelHeight == root.Height, "HoverCard complete-client capture was rescaled.");
            var buffer = await bitmap.GetPixelsAsync(); var pixels = new byte[buffer.Length]; using (var reader = Windows.Storage.Streams.DataReader.FromBuffer(buffer)) reader.ReadBytes(pixels);
            using var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream(); var encoder = await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId, stream);
            encoder.SetPixelData(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied, (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight, 96, 96, pixels); await encoder.FlushAsync();
            using var result = new Windows.Storage.Streams.DataReader(stream.GetInputStreamAt(0)); await result.LoadAsync((uint)stream.Size); var bytes = new byte[stream.Size]; result.ReadBytes(bytes);
            var file = name + ".png"; Directory.CreateDirectory(Path.GetDirectoryName(output)!); await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!, file), bytes);
            captures.Add(new { name, file, sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)), width = bitmap.PixelWidth, height = bitmap.PixelHeight, scope = "complete-native-client", completeSourceRasterCompared = false });
        }
        BleeckerOverlaySurface Surface(BleeckerHoverCard owner) => Elements(owner).OfType<BleeckerOverlaySurface>().Single();
        FrameworkElement Tooltip(BleeckerHoverCard owner) => Elements(owner).Single(e => e.Name == "HoverCardTooltip");
        await Case("source-geometry-and-paint-matrix", async () =>
        {
            foreach (var row in reference.RootElement.GetProperty("cases").EnumerateArray())
            {
                root.Children.Clear(); var spec = row.GetProperty("spec"); var m = row.GetProperty("measurement"); double N(string name, double fallback) => spec.TryGetProperty(name, out var value) ? value.GetDouble() : fallback; bool F(string name) => spec.TryGetProperty(name, out var value) && value.GetBoolean();
                root.RequestedTheme = Enum.Parse<ElementTheme>(row.GetProperty("scheme").GetString()!); await Resize(N("viewportWidth", 800), N("viewportHeight", 600));
                var trigger = new Button { Content = "Hover", Width = N("triggerWidth", 80), Height = N("triggerHeight", 32), MinWidth = 0, MinHeight = 0, IsEnabled = !F("disabled") };
                var content = F("empty") ? null : new Border { Width = N("contentWidth", 160), Height = N("contentHeight", 60), Child = new TextBlock { Text = "Fictional content" } };
                var owner = new BleeckerHoverCard { Trigger = trigger, Content = content, FlowDirection = F("rtl") ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
                if (spec.TryGetProperty("side", out var side)) owner.Side = Enum.Parse<BleeckerHoverCardSide>(side.GetString()!, true);
                if (F("custom")) { var style = new Style(typeof(BleeckerOverlaySurface)); style.Setters.Add(new Setter(FrameworkElement.WidthProperty, 384d)); owner.SurfaceStyle = style; owner.Padding = new Thickness(24); owner.CornerRadius = new CornerRadius(0); }
                // The source fixture's external RTL inline sibling shifts its
                // wrapper. Supply that frozen anchor; test the control's own
                // flow size, surface and original content independently.
                var anchor = m.GetProperty("wrapper"); var host = new Canvas { Width = 140, Height = 110, UseLayoutRounding = false }; Canvas.SetLeft(host, anchor.GetProperty("x").GetDouble()); Canvas.SetTop(host, anchor.GetProperty("y").GetDouble()); host.Children.Add(owner); root.Children.Add(host);
                if (F("clip")) host.Clip = new RectangleGeometry { Rect = new Rect(0, 0, 140, 110) };
                await Layout(); Hover(owner, F("hover"), 0, false); Present(owner, 1000); await Layout(); var surface = Surface(owner); var tooltip = Tooltip(owner);
                Require(ReferenceEquals(owner.Trigger, trigger) && ReferenceEquals(owner.Content, content), "HoverCard copied caller controls.");
                Require(owner.DesiredSize == trigger.DesiredSize, "Inline surface affected trigger flow size.");
                Require(FrameworkElementAutomationPeer.CreatePeerForElement(tooltip).GetAutomationControlType() == AutomationControlType.ToolTip, "Native surface lost tooltip semantics.");
                frames.Add(new { id = row.GetProperty("id").GetString(), scheme = row.GetProperty("scheme").GetString(), actualTheme = owner.ActualTheme.ToString(), trigger = Box(trigger), wrapper = Box(owner), surface = Box(surface), content = content is null ? null : Box(content), hovered = owner.IsHovered, opacity = tooltip.Opacity, hitTestVisible = tooltip.IsHitTestVisible, disabled = !trigger.IsEnabled, originalValuesRetained = true, flowUnchanged = true, role = "tooltip", paint = new { background = Brush(surface.Background), border = Brush(surface.BorderBrush), color = Brush(surface.Foreground), radius = surface.CornerRadius.TopLeft, padding = new[] { surface.Padding.Left, surface.Padding.Top, surface.Padding.Right, surface.Padding.Bottom }, borderWidth = surface.BorderThickness.Top, fontSize = surface.FontSize } });
                var id = row.GetProperty("id").GetString(); if (id is "ltr-bottom-open" or "ltr-bottom-closed" or "ltr-top-open" or "clipped") await Capture(row.GetProperty("scheme").GetString() + "-" + id);
            }
        });
        await Case("original-action-and-disabled-trigger", async () =>
        {
            var calls = 0; var trigger = new Button { Content = "Original action", Command = new Command(() => calls++) }; var owner = new BleeckerHoverCard { Trigger = trigger, Content = "Original body" }; root.Children.Add(owner); await Layout();
            ((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(trigger).GetPattern(PatternInterface.Invoke)).Invoke(); await Layout(); Require(calls == 1 && !owner.IsHovered && Tooltip(owner).Opacity == 0, "Original action was intercepted or toggled hover.");
            trigger.IsEnabled = false; Hover(owner, true, 0, false); Present(owner, 1000); await Layout(); Require(!trigger.IsEnabled && owner.IsHovered && Tooltip(owner).IsHitTestVisible, "Disabled original trigger lost independent hover presentation.");
        });
        await Case("native-hit-query-and-sibling-layer", async () =>
        {
            var trigger = new Button { Content = "Hover", Width = 80, Height = 32, MinWidth = 0, MinHeight = 0 };
            var owner = new BleeckerHoverCard { Trigger = trigger, Content = new Border { Width = 160, Height = 60 } }; Canvas.SetLeft(owner, 320); Canvas.SetTop(owner, 240); root.Children.Add(owner);
            var sibling = new Border { Width = 400, Height = 120, Background = new SolidColorBrush(Microsoft.UI.Colors.Blue) }; Canvas.SetLeft(sibling, 200); Canvas.SetTop(sibling, 280); root.Children.Add(sibling); await Layout();
            async Task PointAt(double x, double y, bool expected)
            {
                typeof(BleeckerHoverCard).GetMethod("UpdateHoverAtPoint", Private)!.Invoke(owner, [new Point(x, y)]);
                var clock = (System.Diagnostics.Stopwatch)typeof(BleeckerHoverCard).GetField("clock", Private)!.GetValue(owner)!; Present(owner, clock.Elapsed.TotalMilliseconds + 1000); await Layout();
                Require(owner.IsHovered == expected && Tooltip(owner).IsHitTestVisible == expected, $"Native point query disagreed at {x},{y}.");
            }
            Require(Canvas.GetZIndex(owner) == 50, "Native surface lost its default sibling layer.");
            await PointAt(360, 256, true); await PointAt(460, 330, true); await PointAt(360, 276, false); await PointAt(460, 330, false);
            trigger.IsEnabled = false; await PointAt(360, 256, true); await PointAt(460, 330, true);
            Canvas.SetZIndex(owner, -1); await PointAt(460, 330, false); Require(Canvas.GetZIndex(owner) == -1, "HoverCard overwrote caller stacking.");
        });
        await Case("original-editor-and-template-lifetime", async () =>
        {
            var editor = new TextBox { Width = 160, Text = "Original", FontSize = 13 }; var owner = new BleeckerHoverCard { Trigger = new Button { Content = "Original" }, Content = editor }; root.Children.Add(owner); await Layout(); Hover(owner, true, 0, false); Present(owner, 1000); await Layout(); editor.Text = "Caller edit"; editor.Select(2, 3);
            owner.Side = BleeckerHoverCardSide.Top; owner.FontSize = 20; await Layout(); Hover(owner, false, 0, false); Present(owner, 1000); await Layout(); Require(ReferenceEquals(FocusManager.FindFirstFocusableElement(Tooltip(owner)), editor), "Closed original editor left focus traversal.");
            var old = Tooltip(owner); owner.Template = (ControlTemplate)XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='ContentControl'><Grid x:Name='PART_Layout' Background='Transparent' UseLayoutRounding='False'/></ControlTemplate>"); await Layout();
            Require(!Elements(owner).Contains(old) && ReferenceEquals(owner.Content, editor) && editor.Text == "Caller edit" && editor.SelectionStart == 2 && editor.SelectionLength == 3 && editor.FontSize == 13, "Template replacement lost original editor state or retained its old host.");
            owner.Template = null; await Layout(); Require(!Elements(owner).Any(e => e.Name == "HoverCardTooltip"), "Null template retained the old tooltip."); owner.ClearValue(Control.TemplateProperty); await Layout(); root.Children.Clear(); await Layout(); root.Children.Add(owner); await Layout(); Require(!owner.IsHovered && editor.Text == "Caller edit" && editor.SelectionStart == 2 && editor.SelectionLength == 3, "Remount lost caller edits or retained stale hover.");
        });
        await Case("original-model-selector-and-live-size", async () =>
        {
            DataTemplate Template(string body) => (DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>" + body + "</DataTemplate>");
            var triggerModel = new Model(); var contentModel = new Model(); var triggerSelector = new Selector(Template("<Button Tag='{Binding}' Content='Original model' Width='80' Height='32'/>")); var bodySelector = new Selector(Template("<Border Width='{Binding Width}' Height='{Binding Height}' Tag='{Binding}'><TextBox Text='{Binding Value,Mode=TwoWay,UpdateSourceTrigger=PropertyChanged}' FontSize='13'/></Border>"));
            var owner = new BleeckerHoverCard { Trigger = triggerModel, TriggerTemplateSelector = triggerSelector, Content = contentModel, ContentTemplateSelector = bodySelector }; root.Children.Add(owner); await Layout(); Hover(owner, true, 0, false); Present(owner, 1000); await Layout();
            var original = Elements(owner).OfType<Border>().Single(e => ReferenceEquals(e.Tag, contentModel)); var editor = Elements(original).OfType<TextBox>().Single();
            Require(triggerSelector.Items.Count > 0 && triggerSelector.Items.All(v => ReferenceEquals(v, triggerModel)) && bodySelector.Items.Count > 0 && bodySelector.Items.All(v => ReferenceEquals(v, contentModel)), "Selector did not receive original models.");
            contentModel.Height = 130; contentModel.Value = "Model edit"; await Layout(); Require(Surface(owner).ActualHeight == 164 && editor.Text == "Model edit", "Live model size/value did not reach the surface."); editor.Select(2, 3); owner.Side = BleeckerHoverCardSide.Top; owner.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red); await Resize(640, 600);
            Require(Elements(owner).Contains(original) && editor.SelectionStart == 2 && editor.SelectionLength == 3 && editor.GetBindingExpression(TextBox.TextProperty) is not null && editor.FontSize == 13, "Placement or paint replaced original model controls/bindings.");
        });
        await Case("native-presentation-source-timelines", async () =>
        {
            foreach (var row in reference.RootElement.GetProperty("transitions").EnumerateArray())
            {
                root.Children.Clear(); root.RequestedTheme = Enum.Parse<ElementTheme>(row.GetProperty("scheme").GetString()!); var owner = new BleeckerHoverCard { Trigger = new Button { Content = "Hover", Width = 80, Height = 32, MinWidth = 0, MinHeight = 0 }, Content = new Border { Width = 160, Height = 60 }, Side = Enum.Parse<BleeckerHoverCardSide>(row.GetProperty("side").GetString()!, true) }; Canvas.SetLeft(owner, 320); Canvas.SetTop(owner, 240); root.Children.Add(owner); await Resize(800, 600);
                var entering = row.GetProperty("kind").GetString() == "enter"; if (!entering) Hover(owner, true, -1000, false); Hover(owner, entering, 0, !row.GetProperty("reduced").GetBoolean()); var samples = new List<object>();
                foreach (var sample in row.GetProperty("samples").EnumerateArray()) { var time = sample.GetProperty("milliseconds").GetDouble(); Present(owner, time); root.UpdateLayout(); Guard(); samples.Add(new { milliseconds = time, opacity = Tooltip(owner).Opacity, hitTestVisible = Tooltip(owner).IsHitTestVisible, surface = Box(Surface(owner)) }); }
                timelines.Add(new { scheme = row.GetProperty("scheme").GetString(), reduced = row.GetProperty("reduced").GetBoolean(), side = row.GetProperty("side").GetString(), kind = row.GetProperty("kind").GetString(), samples });
            }
        });
        root.Children.Clear(); await Layout(); Directory.CreateDirectory(Path.GetDirectoryName(output)!); string Hash(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = failures.Count == 0, scope = "hidden-hover-card-source-renderer-and-contracts", diagnosticOnly = true, stateInjection = "private-production-model-and-presentation-clock", frames, timelines, captures, checks, failures, guards, activations, focus, referenceSha256 = Convert.ToHexStringLower(SHA256.HashData(referenceBytes)), librarySha256 = Hash(typeof(BleeckerHoverCard).Assembly.Location), hostSha256 = Hash(hostPath), processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(), osArchitecture = RuntimeInformation.OSArchitecture.ToString(), activateCalled = false, showCalled = false, focusCalled = false, inputSent = false, sourceRasterParityVerified = false, nativeInputFocusVerified = false, visibleRuntimeVerified = false, nativeComponentAccepted = false }, new JsonSerializerOptions { WriteIndented = true }));
        window.Close(); Environment.Exit(failures.Count == 0 ? 0 : 1);
    }
}
