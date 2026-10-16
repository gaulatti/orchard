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

public sealed partial class DataTableFiltersFixture : UserControl
{
    private readonly SelectFilterTable preview = new();
    public DataTableFiltersFixture()
    { InitializeComponent(); Preview.Table = preview; Preview.Options = [(BleeckerSelectOption)Resources["DraftOption"], (BleeckerSelectOption)Resources["PublishedOption"]]; preview.Changed += (_, _) => Rows.Text = preview.VisibleText; Rows.Text = preview.VisibleText; }
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    { for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++) { var child = VisualTreeHelper.GetChild(root, index); if (child is T item) yield return item; foreach (var nested in Descendants<T>(child)) yield return nested; } }
    private static ItemsControl Select(BleeckerDataTableSelectFilter view) => Descendants<ItemsControl>(view).Single(item => item.Name == "PART_Select");
    private static IExpandCollapseProvider Provider(ItemsControl select) => (IExpandCollapseProvider)FrameworkElementAutomationPeer.CreatePeerForElement(select).GetPattern(PatternInterface.ExpandCollapse);
    private static Rect Bounds(FrameworkElement element, UIElement root) => element.TransformToVisual(root).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
    private async Task ReadyAsync(Func<bool> condition)
    { var timer = System.Diagnostics.Stopwatch.StartNew(); while (!condition() && timer.ElapsedMilliseconds < 2000) { UpdateLayout(); await Task.Delay(16); } Require(condition(), "Filter readiness exceeded its original two-second bound."); }
    private static IEnumerable<KeyValuePair<string, object?>> Filters(JsonElement value) => value.EnumerateArray().Select(item => new KeyValuePair<string, object?>(item.GetProperty("id").GetString()!, Decode(item.GetProperty("value"))));
    private static object? Decode(JsonElement value) => value.ValueKind switch { JsonValueKind.Null => null, JsonValueKind.String => value.GetString(), JsonValueKind.True => true, JsonValueKind.False => false, JsonValueKind.Number => value.GetDouble(), JsonValueKind.Array => value.EnumerateArray().Select(Decode).ToArray(), _ => throw new InvalidOperationException("Unknown frozen filter descriptor.") };
    internal async Task VerifySourceAsync(BleeckerColorScheme scheme, string folder, bool desktopFocusContract = false, bool liveOptions = false)
    {
        var referenceName = liveOptions ? "DataTableFiltersLive" : "DataTableFilters";
        var reportName = liveOptions ? "data-table-filters-live" : "data-table-filters";
        var inputName = liveOptions ? "filters-live" : "filters";
        using var stream = typeof(DataTableFiltersFixture).Assembly.GetManifestResourceStream($"Orchard.Catalog.Reference.{referenceName}.json")!; using var buffer = new MemoryStream(); await stream.CopyToAsync(buffer); var bytes = buffer.ToArray(); using var reference = JsonDocument.Parse(bytes);
        var root = (Grid)XamlRoot.Content; var app = AppWindow.GetFromWindowId(XamlRoot.ContentIslandEnvironment.AppWindowId); var originalSize = app.ClientSize; var reports = new List<object>(); var failures = new List<string>(); var focusDifferences = new List<object>();
        try
        {
            foreach (var sample in reference.RootElement.GetProperty("cases").EnumerateArray().Where(item => item.GetProperty("scheme").GetString() == scheme.ToString()))
            {
                var id = sample.GetProperty("id").GetString()!; var props = sample.GetProperty("props");
                var a = new SelectFilterTable(initial: props.TryGetProperty("filters", out var initial) ? Filters(initial) : null); var b = new SelectFilterTable(true, props.TryGetProperty("secondFilters", out var second) ? Filters(second) : null); var active = a; var originalRowsA = a.Rows.ToArray(); var originalRowsB = b.Rows.ToArray();
                var options = props.TryGetProperty("options", out var choices) ? choices.EnumerateArray().Select(item => new BleeckerSelectOption(item.GetProperty("value").GetString()!, item.GetProperty("label").GetString()!, item.TryGetProperty("disabled", out var disabled) && disabled.GetBoolean())).ToArray() : new[] { new BleeckerSelectOption("Draft", "Draft"), new BleeckerSelectOption("Published", "Published") };
                var width = sample.GetProperty("viewportWidth").GetInt32(); var hostWidth = sample.GetProperty("hostWidth").GetDouble(); var top = sample.GetProperty("top").GetDouble(); var rtl = sample.GetProperty("direction").GetString() == "rtl";
                var view = new BleeckerDataTableSelectFilter { Table = active, ColumnId = props.TryGetProperty("columnId", out var column) ? column.GetString()! : "status", Options = options, FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
                if (props.TryGetProperty("placeholder", out var placeholder)) view.Placeholder = placeholder.GetString()!;
                var layout = new Grid { Width = hostWidth, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(rtl ? width - 16 - hostWidth : 16, top, 0, 0) }; layout.Children.Add(view);
                var outsideClicks = 0; var outside = new Button { Name = "Outside", Content = "Outside", HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(16), TabIndex = 0 }; outside.Click += (_, _) => outsideClicks++;
                var probe = new TextBlock { Text = active.VisibleText, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(16, top + 72, 16, 0), VerticalAlignment = VerticalAlignment.Top };
                EventHandler changed = (sender, _) => { if (ReferenceEquals(sender, active)) probe.Text = active.VisibleText; }; a.Changed += changed; b.Changed += changed;
                var host = new Grid { Background = root.Background, TabFocusNavigation = KeyboardNavigationMode.Cycle }; Grid.SetRowSpan(host, root.RowDefinitions.Count); Grid.SetColumnSpan(host, root.ColumnDefinitions.Count); host.Children.Add(layout); host.Children.Add(outside); host.Children.Add(probe); root.Children.Add(host);
                using var input = new NativeInputSession(XamlRoot, folder, $"{inputName}-{id}-{scheme}");
                try
                {
                    await ReadyAsync(() => view.IsLoaded && a.Subscribers == 2); view.ApplyTemplate(); await MainWindow.AwaitPresentationAsync(view); await input.AcquireAsync(outside); outsideClicks = 0;
                    app.ResizeClient(new SizeInt32(width, 745)); await ReadyAsync(() => XamlRoot.Size.Width == width && XamlRoot.Size.Height == 745); await MainWindow.AwaitPresentationAsync(view);
                    var select = Select(view); select.TabIndex = 1;
                    Popup Popup() => VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Single(item => item.Child is Border { Name: "PopupBorder" });
                    string Focus(object? node) => node switch { Button option when select.Items.Contains(option) => "option:" + select.Items.IndexOf(option), _ when ReferenceEquals(node, select) => "trigger", Control { Name: "PART_Listbox" } => "listbox", _ when ReferenceEquals(node, outside) => "outside", null => "BODY", _ => node.GetType().Name };
                    var index = 0; var removedFocusedColumn = false; BleeckerOverlaySurface? lastShadowSurface = null;
                    foreach (var point in sample.GetProperty("snapshots").EnumerateArray())
                    {
                        var phase = point.GetProperty("phase").GetString()!; var expected = point.GetProperty("state"); var errors = new List<string>(); object? receipt = null;
                        var previousFocus = FocusManager.GetFocusedElement(XamlRoot);
                        if (sample.TryGetProperty("updates", out var updates) && updates.TryGetProperty(phase, out var update))
                        {
                            if (update.TryGetProperty("options", out var nextOptions))
                            {
                                options = nextOptions.EnumerateArray().Select(item => new BleeckerSelectOption(item.GetProperty("value").GetString()!, item.GetProperty("label").GetString()!, item.TryGetProperty("disabled", out var disabled) && disabled.GetBoolean())).ToArray();
                                view.Options = options;
                            }
                            if (update.TryGetProperty("placeholder", out var nextPlaceholder)) view.Placeholder = nextPlaceholder.GetString()!;
                        }
                        else switch (phase)
                        {
                            case "closed": break;
                            case "open":
                                try { await input.ClickAsync(select); }
                                catch (Exception error)
                                {
                                    await File.WriteAllTextAsync(Path.Combine(folder, $"filters-{id}-{scheme}-open-failure.json"), JsonSerializer.Serialize(new { phase, index, error = error.ToString(), open = Provider(select).ExpandCollapseState.ToString(), focus = Focus(FocusManager.GetFocusedElement(XamlRoot)), rawFocus = FocusManager.GetFocusedElement(XamlRoot)?.GetType().Name, pointer = input.ReadPointerPosition(), popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Select(popup => new { child = popup.Child.GetType().Name, name = (popup.Child as FrameworkElement)?.Name }) }));
                                    await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, $"filters-{id}-{scheme}-open-failure.png")); throw;
                                }
                                break;
                            case "draft": case "published": case "all": case "same-all": var label = phase == "draft" ? "Draft" : phase == "published" ? "Published" : view.Placeholder; await input.ClickAsync(select.Items.OfType<Button>().Single(item => item.Content?.ToString() == label)); break;
                            case "focus": Require(outside.Focus(FocusState.Keyboard), "Native entry lost its original preceding focus owner."); receipt = await PairAsync(input, 9, false); break;
                            case "down": receipt = await PairAsync(input, 0x28, false); break;
                            case "home": receipt = await PairAsync(input, 0x24, false); break;
                            case "end": receipt = await PairAsync(input, 0x23, false); break;
                            case "enter": receipt = await PairAsync(input, 0x0D, false); break;
                            case "escape": receipt = await PairAsync(input, 0x1B, false); break;
                            case "tab": case "reverse-tab": receipt = await PairAsync(input, 9, phase == "reverse-tab"); break;
                            case "type-p": case "type-d": receipt = await PairAsync(input, phase == "type-p" ? (ushort)0x50 : (ushort)0x44, false); break;
                            case "outside":
                                var modal = Popup(); var modalEvents = new List<object>();
                                var observe = new PointerEventHandler((_, args) => { var point = args.GetCurrentPoint(root); modalEvents.Add(new { source = args.OriginalSource.GetType().Name, args.IsGenerated, point.Position, point.Properties.IsLeftButtonPressed }); });
                                modal.AddHandler(UIElement.PointerMovedEvent, observe, true); modal.AddHandler(UIElement.PointerPressedEvent, observe, true); modal.AddHandler(UIElement.PointerReleasedEvent, observe, true);
                                try { receipt = await input.DismissModalPointerSourceOutsideAsync(select, modal, outside); }
                                finally { modal.RemoveHandler(UIElement.PointerMovedEvent, observe); modal.RemoveHandler(UIElement.PointerPressedEvent, observe); modal.RemoveHandler(UIElement.PointerReleasedEvent, observe); await File.WriteAllTextAsync(Path.Combine(folder, $"filters-{id}-{scheme}-modal-route.json"), JsonSerializer.Serialize(new { modalEvents, open = modal.IsOpen, outsideClicks })); }
                                break;
                            case "external-draft": case "external-published": active.SetExternalFilters([new("status", phase == "external-draft" ? "Draft" : "Published")]); break;
                            case "replace": active = b; view.Table = b; probe.Text = b.VisibleText; break;
                            case "old-notification": a.SetExternalFilters([new("status", "Published")]); break;
                            case "restore-column": view.ColumnId = "status"; break;
                            case "remove-column": removedFocusedColumn = ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), select); view.ColumnId = "missing"; break;
                            case "title-column": view.ColumnId = "title"; break;
                            default: throw new InvalidOperationException("Unknown frozen filter action.");
                        }
                        var open = expected.GetProperty("popupOpen").GetBoolean(); await ReadyAsync(() => select.Visibility == Visibility.Collapsed ? !open : select.IsLoaded && (Provider(select).ExpandCollapseState == ExpandCollapseState.Expanded) == open); await MainWindow.AwaitPresentationAsync(view);
                        if (open) { await ReadyAsync(() => Popup().Child is FrameworkElement { IsLoaded: true }); await MainWindow.AwaitPresentationAsync((FrameworkElement)Popup().Child); }
                        void Equal(bool value, string message) { if (!value) errors.Add(message); }
                        void Near(double actual, double wanted, string message) => Equal(double.IsFinite(actual) && Math.Abs(actual - wanted) <= 1d / 32, $"{message}: {actual:R}/{wanted:R}");
                        var focused = FocusManager.GetFocusedElement(XamlRoot); var sourceFocus = expected.GetProperty("focused").GetString();
                        if (phase != "closed" && Focus(focused) != sourceFocus) focusDifferences.Add(new { id, phase, sourceFocus, nativeFocus = Focus(focused), rawNativeFocus = focused?.GetType().Name });
                        var nativeFocusPolicy = desktopFocusContract && removedFocusedColumn && id == "missing" && phase is "remove-column" or "restore-column" && sourceFocus == "BODY";
                        var expectedFocus = nativeFocusPolicy ? "outside" : sourceFocus;
                        if (phase != "closed") Equal(Focus(focused) == expectedFocus, "Focused role differs: " + Focus(focused) + "/" + expectedFocus);
                        Equal(ReferenceEquals(view.Table, active) && probe.Text == active.VisibleText && active.VisibleText == expected.GetProperty("visibleRows").GetString(), "Original table/visible rows differ.");
                        Equal(a.Requests.Count + b.Requests.Count == expected.GetProperty("events").GetArrayLength() && outsideClicks == expected.GetProperty("outsideClicks").GetInt32(), "Original callback or modal outside-action count differs.");
                        Equal(a.Rows.Zip(originalRowsA).All(pair => ReferenceEquals(pair.First, pair.Second)) && b.Rows.Zip(originalRowsB).All(pair => ReferenceEquals(pair.First, pair.Second)), "Original row objects differ."); Equal(input.OwnsForeground, "Native filter lost its foreground boundary.");
                        var sourceTrigger = expected.GetProperty("trigger"); object? geometry = null; bool? shadowClosed = null;
                        if (!open && lastShadowSurface is not null)
                        {
                            shadowClosed = ShadowIsCleared(lastShadowSurface);
                            Equal(shadowClosed.Value, "Closed Select retained its loaded surface or Composition shadow.");
                        }
                        if (sourceTrigger.ValueKind == JsonValueKind.Null) Equal(select.Visibility == Visibility.Collapsed, "Missing original column still exposes a selector.");
                        else
                        {
                            var triggerBox = Bounds(select, root); var sourceBox = sourceTrigger.GetProperty("box"); Near(triggerBox.Width, sourceBox.GetProperty("width").GetDouble(), "Trigger width"); Near(triggerBox.Height, 40, "Trigger height"); Near(triggerBox.X, sourceBox.GetProperty("x").GetDouble(), "Trigger x"); Near(triggerBox.Y, sourceBox.GetProperty("y").GetDouble(), "Trigger y");
                            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(select); Equal(peer.GetName() == sourceTrigger.GetProperty("name").GetString() && ((IValueProvider)peer.GetPattern(PatternInterface.Value)).Value == sourceTrigger.GetProperty("text").GetString(), "Accessible name or actual controlled value differs.");
                            if (open)
                            {
                                var popupBox = Bounds((FrameworkElement)Popup().Child, root); var sourcePopup = expected.GetProperty("popup").GetProperty("box"); Near(popupBox.Width, sourcePopup.GetProperty("width").GetDouble(), "Popup width"); Near(popupBox.Height, sourcePopup.GetProperty("height").GetDouble(), "Popup height"); Near(popupBox.X, sourcePopup.GetProperty("x").GetDouble(), "Popup x"); Near(popupBox.Y, sourcePopup.GetProperty("y").GetDouble(), "Popup y");
                                var items = select.Items.OfType<Button>().ToArray(); var original = expected.GetProperty("options").EnumerateArray().ToArray(); Equal(items.Length == original.Length, "Original options count differs.");
                                foreach (var (item, sourceItem, optionIndex) in items.Zip(original).Select((pair, optionIndex) => (pair.First, pair.Second, optionIndex)))
                                {
                                    Equal(item.Content?.ToString() == sourceItem.GetProperty("label").GetString() && !item.IsEnabled == (sourceItem.GetProperty("disabled").GetString() == "true"), "Original label/disabled state differs.");
                                    Equal(item.Tag is BleeckerSelectOption && (optionIndex == 0 || ReferenceEquals(options[optionIndex - 1], item.Tag)), "A consumer option was replaced.");
                                    var mark = Descendants<Viewbox>(item).Single(node => node.Name == "Checkmark"); Equal((mark.Visibility == Visibility.Visible) == (sourceItem.GetProperty("state").GetString() == "checked"), "Controlled checked paint differs.");
                                }
                                var scroll = Descendants<ScrollViewer>(Popup().Child).Single(); Near(scroll.VerticalOffset, expected.GetProperty("viewport").GetProperty("scrollTop").GetDouble(), "Source scroll offset"); Near(scroll.ActualHeight, expected.GetProperty("viewport").GetProperty("clientHeight").GetDouble(), "Source viewport height");
                                lastShadowSurface = Descendants<BleeckerOverlaySurface>(Popup().Child).Single(item => item.Name == "PopupSurface");
                                geometry = new { trigger = triggerBox, popup = popupBox, viewport = Bounds(scroll, root), scroll.VerticalOffset, shadow = DescribeShadow((FrameworkElement)Popup().Child), options = items.Select(item => new { text = item.Content?.ToString(), item.IsEnabled, box = Bounds(item, root) }).ToArray() };
                            }
                            else geometry = new { trigger = triggerBox };
                        }
                        var file = $"{reportName}-{id}-{index++:00}-{phase}-{scheme}.png"; await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, file)); reports.Add(new { id, phase, file, errors, receipt, focus = Focus(focused), sourceFocus, expectedFocus, nativeFocusPolicy, focusRetained = ReferenceEquals(previousFocus, focused), priorFocusLoaded = previousFocus is FrameworkElement { IsLoaded: true }, rawFocus = focused?.GetType().Name, callbacks = a.Requests.Count + b.Requests.Count, outsideClicks, rows = probe.Text, shadowClosed, geometry }); failures.AddRange(errors.Select(error => id + "/" + phase + ": " + error));
                    }
                }
                catch (Exception error) { failures.Add(id + ": " + error); }
                finally { root.Children.Remove(host); a.Changed -= changed; b.Changed -= changed; }
                await ReadyAsync(() => !view.IsLoaded && a.Subscribers == 0 && b.Subscribers == 0);
            }
            Require(failures.Count == 0, "One or more original filter cases failed; all independent case evidence is retained.");
        }
        catch (Exception error) { failures.Add(error.ToString()); throw; }
        finally { app.ResizeClient(originalSize); await File.WriteAllTextAsync(Path.Combine(folder, $"{reportName}-{scheme}.json"), JsonSerializer.Serialize(new { passed = failures.Count == 0, desktopFocusContract, liveOptions, sourceFocusParityVerified = focusDifferences.Count == 0, focusDifferences, nativeComponentAccepted = false, referenceSha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant(), reports, failures }, new JsonSerializerOptions { WriteIndented = true })); }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
