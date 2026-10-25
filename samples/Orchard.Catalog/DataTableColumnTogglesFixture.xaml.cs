using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Windowing;
using Windows.Foundation;
using Windows.Graphics;

namespace Orchard.Catalog;

public sealed partial class DataTableColumnTogglesFixture : UserControl
{
    private readonly ColumnVisibilityTable preview = new();
    public DataTableColumnTogglesFixture()
    { InitializeComponent(); Preview.Table = preview; preview.Changed += (_, _) => Cells.Text = preview.VisibleCells; Cells.Text = preview.VisibleCells; }
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    { for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++) { var child = VisualTreeHelper.GetChild(root, index); if (child is T result) yield return result; foreach (var item in Descendants<T>(child)) yield return item; } }
    private static Button Trigger(BleeckerDataTableColumnToggle view) => Descendants<Button>(view).Single(button => button.Name == "PART_Trigger");
    private static IExpandCollapseProvider Provider(Button trigger) => (IExpandCollapseProvider)FrameworkElementAutomationPeer.CreatePeerForElement(trigger).GetPattern(PatternInterface.ExpandCollapse);
    private static bool IsOpen(BleeckerDataTableColumnToggle view) => Provider(Trigger(view)).ExpandCollapseState == ExpandCollapseState.Expanded;
    private static Rect Bounds(FrameworkElement element, UIElement root) => element.TransformToVisual(root).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
    private async Task ReadyAsync(Func<bool> condition)
    { var timer = System.Diagnostics.Stopwatch.StartNew(); while (!condition() && timer.ElapsedMilliseconds < 2000) { UpdateLayout(); await Task.Delay(16); } Require(condition(), "Column picker readiness exceeded its original two-second bound."); }

    internal async Task VerifySourceAsync(BleeckerColorScheme scheme, string folder)
    {
        using var stream = CatalogReferenceResources.Open(typeof(DataTableColumnTogglesFixture).Assembly, "Orchard.Catalog.Reference.DataTableColumnToggle.json")!; using var buffer = new MemoryStream(); await stream.CopyToAsync(buffer); var bytes = buffer.ToArray(); using var reference = JsonDocument.Parse(bytes);
        var root = (Grid)XamlRoot.Content; var app = AppWindow.GetFromWindowId(XamlRoot.ContentIslandEnvironment.AppWindowId); var originalSize = app.ClientSize; var reports = new List<object>(); var failures = new List<string>(); var focusDifferences = new List<object>();
        try
        {
            foreach (var sample in reference.RootElement.GetProperty("cases").EnumerateArray().Where(item => item.GetProperty("scheme").GetString() == scheme.ToString()))
            {
                var id = sample.GetProperty("id").GetString()!; var props = sample.GetProperty("props");
                var initial = props.TryGetProperty("visibility", out var visibility) ? visibility.EnumerateObject().ToDictionary(pair => pair.Name, pair => pair.Value.GetBoolean()) : null;
                var a = new ColumnVisibilityTable(props.TryGetProperty("kind", out var kind) ? kind.GetString()! : "basic", emptyRows: props.TryGetProperty("rows", out var count) && count.GetInt32() == 0, initial: initial); var b = new ColumnVisibilityTable(second: true); var active = a;
                var width = sample.GetProperty("viewportWidth").GetInt32(); var hostWidth = sample.GetProperty("hostWidth").GetDouble(); var top = sample.GetProperty("top").GetDouble(); var rtl = sample.GetProperty("direction").GetString() == "rtl";
                var view = new BleeckerDataTableColumnToggle { Table = active, FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, HorizontalAlignment = rtl ? HorizontalAlignment.Left : HorizontalAlignment.Right };
                if (props.TryGetProperty("label", out var label)) view.Label = label.GetString();
                var layout = new Grid { Width = hostWidth, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(rtl ? width - 16 - hostWidth : 16, top, 0, 0) }; layout.Children.Add(view);
                var outsideClicks = 0; var outside = new Button { Name = "Outside", Content = "Outside", HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, top + 52, 0, 0) }; outside.Click += (_, _) => outsideClicks++;
                var probe = new TextBlock { Text = active.VisibleCells, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(16, top + 92, 16, 0), VerticalAlignment = VerticalAlignment.Top };
                EventHandler changed = (sender, _) => { if (ReferenceEquals(sender, active)) probe.Text = active.VisibleCells; }; a.Changed += changed; b.Changed += changed;
                var host = new Grid { Background = root.Background }; Grid.SetRowSpan(host, root.RowDefinitions.Count); Grid.SetColumnSpan(host, root.ColumnDefinitions.Count); host.Children.Add(layout); host.Children.Add(outside); host.Children.Add(probe); root.Children.Add(host);
                using var input = new NativeInputSession(XamlRoot, folder, $"column-toggle-{id}-{scheme}");
                try
                {
                    await ReadyAsync(() => view.IsLoaded && a.Subscribers == 2); view.ApplyTemplate(); await MainWindow.AwaitPresentationAsync(view); await input.AcquireAsync(outside); outsideClicks = 0;
                    app.ResizeClient(new SizeInt32(width, 745)); await ReadyAsync(() => XamlRoot.Size.Width == width && XamlRoot.Size.Height == 745); await MainWindow.AwaitPresentationAsync(view);
                    Popup Popup() => VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Single(item => item.Child is FrameworkElement child && child.Name == "PART_Dialog");
                    BleeckerOverlaySurface Surface() => Descendants<BleeckerOverlaySurface>(Popup().Child).Single(item => item.Name == "PART_Surface");
                    BleeckerCheckbox[] Checks() => Descendants<BleeckerCheckbox>(Popup().Child).ToArray();
                    string Focus(object? node) => node switch { BleeckerCheckbox check when check.IsLoaded && check.Tag is IBleeckerDataTableVisibilityColumn column => "column:" + column.Id, _ when ReferenceEquals(node, Trigger(view)) => "trigger", FrameworkElement { Name: "PART_Dialog" } => "dialog", _ when ReferenceEquals(node, outside) => "outside", null or Microsoft.UI.Xaml.Controls.Primitives.Popup => "BODY", FrameworkElement { IsLoaded: false } => "BODY", _ => node.GetType().Name };
                    var index = 0;
                    foreach (var point in sample.GetProperty("snapshots").EnumerateArray())
                    {
                        var phase = point.GetProperty("phase").GetString()!; var expected = point.GetProperty("state"); var errors = new List<string>(); object? receipt = null;
                        switch (phase)
                        {
                            case "closed": break;
                            case "open": await input.ClickAsync(Trigger(view)); break;
                            case "click-first": var row = Descendants<ContentControl>(Surface()).First(item => item.Name == "PART_ColumnRow"); await input.ClickAsync(row); break;
                            case "space": receipt = await PairAsync(input, 0x20, false); break;
                            case "tab": receipt = await PairAsync(input, 0x09, false); break;
                            case "reverse-tab": receipt = await PairAsync(input, 0x09, true); break;
                            case "escape": receipt = await PairAsync(input, 0x1B, false); break;
                            case "outside": await input.ClickAsync(outside); break;
                            case "external-status-off": var next = new Dictionary<string, bool>(active.Visibility) { ["status"] = false }; active.ExternalVisibility(next); break;
                            case "rename": view.Label = "Fields now"; break;
                            case "replace": active = b; view.Table = b; probe.Text = b.VisibleCells; break;
                            case "old-notification": a.ExternalVisibility(new Dictionary<string, bool> { ["title"] = false, ["status"] = false }); break;
                            case "hide-eligibility": active.EnableHiding = false; break;
                            case "restore-eligibility": active.EnableHiding = true; break;
                            default: throw new InvalidOperationException("Unknown frozen column action.");
                        }
                        var open = expected.GetProperty("popupOpen").GetBoolean(); await ReadyAsync(() => IsOpen(view) == open); await MainWindow.AwaitPresentationAsync(view);
                        if (open) { await ReadyAsync(() => Descendants<BleeckerOverlaySurface>(Popup().Child).Any(item => item.Name == "PART_Surface" && item.IsLoaded)); await MainWindow.AwaitPresentationAsync(Surface()); }
                        void Equal(bool value, string message) { if (!value) errors.Add(message); }
                        void Near(double actual, double wanted, string message) => Equal(double.IsFinite(actual) && Math.Abs(actual - wanted) <= 1d / 32, $"{message}: {actual:R}/{wanted:R}");
                        var focused = FocusManager.GetFocusedElement(XamlRoot); var sourceFocus = expected.GetProperty("focused").GetString();
                        var nativeContainerFocus = open && sourceFocus == "BODY" && ReferenceEquals(focused, Popup().Child);
                        if (nativeContainerFocus) focusDifferences.Add(new { id, phase, sourceFocus, nativeFocus = Focus(focused), reason = "Windows retains the existing dialog as its live focus container after column removal." });
                        if (phase != "closed") Equal(Focus(focused) == sourceFocus || nativeContainerFocus, "Focused role differs: " + Focus(focused) + "/" + sourceFocus);
                        Equal(ReferenceEquals(view.Table, active) && probe.Text == active.VisibleCells && active.VisibleCells == expected.GetProperty("visibleCells").GetString(), "Original table/cell projection differs.");
                        Equal(a.Requests.Count + b.Requests.Count == expected.GetProperty("events").GetArrayLength() && outsideClicks == expected.GetProperty("outsideClicks").GetInt32(), "Visibility or outside-action callback count differs.");
                        Equal(input.OwnsForeground, "Catalog lost its own foreground input boundary.");
                        Equal(view.Label == expected.GetProperty("label").GetString(), "Controlled trigger label differs.");
                        Equal(a.Requests.All(request => a.Columns.Any(column => ReferenceEquals(column, request.Column))) && b.Requests.All(request => b.Columns.Any(column => ReferenceEquals(column, request.Column))), "A visibility request replaced its original column.");
                        object? geometry = null;
                        if (open)
                        {
                            var surface = Surface(); var surfaceBox = Bounds(surface, root); var triggerBox = Bounds(Trigger(view), root); var sourceBox = expected.GetProperty("popup").GetProperty("box"); var scroller = Descendants<ScrollViewer>(surface).Single(item => item.Name == "PART_Viewport");
                            Near(surfaceBox.Width, sourceBox.GetProperty("width").GetDouble(), "Popup width"); Near(surfaceBox.Height, sourceBox.GetProperty("height").GetDouble(), "Popup height");
                            var sourceTrigger = expected.GetProperty("trigger").GetProperty("box"); var above = sourceBox.GetProperty("y").GetDouble() < sourceTrigger.GetProperty("y").GetDouble(); Near(above ? triggerBox.Top - surfaceBox.Bottom : surfaceBox.Top - triggerBox.Bottom, 4, "Source popup gap");
                            if (id == "collision") Near(surfaceBox.Left, sourceBox.GetProperty("x").GetDouble(), "Collision inset"); else Near(rtl ? surfaceBox.Left - triggerBox.Left : surfaceBox.Right - triggerBox.Right, 0, "Logical end alignment");
                            var options = expected.GetProperty("options").EnumerateArray().ToArray(); var checks = Checks(); Equal(checks.Length == options.Length, "Eligible top-level column count differs.");
                            foreach (var (check, option) in checks.Zip(options))
                            {
                                Equal(check.Tag is IBleeckerDataTableVisibilityColumn column && column.Id == option.GetProperty("id").GetString() && active.Columns.Any(original => ReferenceEquals(original, column)), "Original checkbox column differs.");
                                Equal(check.IsChecked == option.GetProperty("checked").GetBoolean(), "Controlled checkbox state differs.");
                                Equal(FrameworkElementAutomationPeer.CreatePeerForElement(check).GetName() == option.GetProperty("label").GetString(), "Accessible header/id label differs.");
                                Near(check.ActualWidth, 20, "Checkbox width"); Near(check.ActualHeight, 20, "Checkbox height");
                            }
                            var sourceViewport = expected.GetProperty("viewport"); Near(scroller.ActualHeight, sourceViewport.GetProperty("clientHeight").GetDouble(), "Viewport height"); Near(scroller.VerticalOffset, sourceViewport.GetProperty("scrollTop").GetDouble(), "Viewport offset");
                            geometry = new { popup = surfaceBox, trigger = triggerBox, viewport = Bounds(scroller, root), scroller.VerticalOffset, scroller.ExtentHeight, options = checks.Select(check => new { column = ((IBleeckerDataTableVisibilityColumn)check.Tag).Id, check.IsChecked, box = Bounds(check, root) }).ToArray() };
                        }
                        var file = $"data-table-column-toggle-{id}-{index++:00}-{phase}-{scheme}.png"; await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, file));
                        reports.Add(new { id, phase, file, errors, receipt, focus = Focus(focused), sourceFocus, nativeContainerFocus, nativeFocus = focused?.GetType().Name, callbacks = a.Requests.Count + b.Requests.Count, outsideClicks, visibleCells = probe.Text, geometry }); failures.AddRange(errors.Select(error => id + "/" + phase + ": " + error));
                        Require(errors.Count == 0, "Column picker differs from its actual browser/producer state; see the native report.");
                    }
                }
                finally { root.Children.Remove(host); a.Changed -= changed; b.Changed -= changed; }
                await ReadyAsync(() => !view.IsLoaded && a.Subscribers == 0 && b.Subscribers == 0);
            }
        }
        catch (Exception error) { failures.Add(error.ToString()); throw; }
        finally { app.ResizeClient(originalSize); await File.WriteAllTextAsync(Path.Combine(folder, $"data-table-column-toggle-{scheme}.json"), JsonSerializer.Serialize(new { passed = failures.Count == 0, nativeComponentAccepted = false, sourceFocusParityVerified = focusDifferences.Count == 0, focusDifferences, referenceSha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant(), reports, failures }, new JsonSerializerOptions { WriteIndented = true })); }
    }
    private async Task<object> PairAsync(NativeInputSession input, ushort key, bool shift)
    {
        var downSource = FocusManager.GetFocusedElement(XamlRoot); var downs = 0; var ups = 0; var shiftDowns = 0; var shiftUps = 0; var downOwned = true; var upOwned = true; var shiftOwned = true; var released = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var timer = System.Diagnostics.Stopwatch.StartNew(); var order = new List<string>(); double? downMilliseconds = null, upMilliseconds = null;
        var down = new KeyEventHandler((_, args) =>
        {
            if ((ushort)args.Key == key) { order.Add("keydown"); downMilliseconds = timer.Elapsed.TotalMilliseconds; downs++; downOwned &= ReferenceEquals(args.OriginalSource, downSource); }
            else if (args.Key == Windows.System.VirtualKey.Shift) { order.Add("shift-down"); shiftDowns++; shiftOwned &= ReferenceEquals(args.OriginalSource, downSource); }
        });
        var up = new KeyEventHandler((_, args) =>
        {
            if ((ushort)args.Key == key) { order.Add("keyup"); upMilliseconds = timer.Elapsed.TotalMilliseconds; ups++; upOwned &= ReferenceEquals(args.OriginalSource, FocusManager.GetFocusedElement(XamlRoot)); released.TrySetResult(); }
            else if (args.Key == Windows.System.VirtualKey.Shift) { order.Add("shift-up"); shiftUps++; shiftOwned &= ReferenceEquals(args.OriginalSource, FocusManager.GetFocusedElement(XamlRoot)); }
        });
        XamlRoot.Content.AddHandler(UIElement.KeyDownEvent, down, true); XamlRoot.Content.AddHandler(UIElement.KeyUpEvent, up, true);
        try
        {
            if (shift) input.ShiftKey(key); else input.Key(key); await released.Task.WaitAsync(TimeSpan.FromSeconds(2));
            if (shift) await ReadyAsync(() => shiftUps > 0);
            Require(downs == 1 && ups == 1 && downOwned && upOwned, "Native column key lost an exact actual focus owner.");
            Require(order.SequenceEqual(shift ? new[] { "shift-down", "keydown", "keyup", "shift-up" } : new[] { "keydown", "keyup" }), "Native column key order differs.");
            Require(shiftDowns == (shift ? 1 : 0) && shiftUps == (shift ? 1 : 0) && shiftOwned, "Native column Shift modifier lost its exact actual focus owner.");
        }
        finally { XamlRoot.Content.RemoveHandler(UIElement.KeyDownEvent, down); XamlRoot.Content.RemoveHandler(UIElement.KeyUpEvent, up); }
        return new { key, shift, downs, ups, downOwned, upOwned, shiftDowns, shiftUps, shiftOwned, order, downMilliseconds, upMilliseconds };
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
