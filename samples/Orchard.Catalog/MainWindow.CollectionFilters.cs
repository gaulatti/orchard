using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Graphics;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunCollectionFiltersAsync(string folder)
    {
        using var stream = CatalogReferenceResources.Open(typeof(MainWindow).Assembly, "Orchard.Catalog.Reference.CollectionFilters.json") ?? throw new InvalidOperationException("Collection Filters reference missing.");
        using var bytes = new MemoryStream(); await stream.CopyToAsync(bytes); using var reference = JsonDocument.Parse(bytes.ToArray());
        var previousContent = Content; var previousSize = AppWindow.ClientSize;
        var canvas = (Canvas)Microsoft.UI.Xaml.Markup.XamlReader.Load("<Canvas xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{ThemeResource BleeckerBackgroundBrush}' UseLayoutRounding='False'/>");
        var anchor = new Button { Width = 2, Height = 2, Opacity = 0 }; Canvas.SetLeft(anchor, 4); Canvas.SetTop(anchor, 860); canvas.Children.Add(anchor);
        var frames = new List<object>(); var interactions = new List<object>(); var keyOrderInteractions = new List<object>(); var captures = new Dictionary<string, string>(); var removalInputs = new List<object>(); var inputTrace = new List<object>();
        static IEnumerable<T> Nodes<T>(DependencyObject root) where T : FrameworkElement
        { for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++) { var child = VisualTreeHelper.GetChild(root, index); if (child is T target) yield return target; foreach (var nested in Nodes<T>(child)) yield return nested; } }
        static object Paint(Brush? value)
        {
            var brush = value as SolidColorBrush ?? throw new InvalidOperationException("Collection Filters paint is not a solid brush.");
            return new { color = brush.Color.ToString(), opacity = brush.Opacity };
        }
        static object Palette(BleeckerCollectionFilters control)
        {
            var paints = new Dictionary<string, object>();
            foreach (var (name, key) in new[] { ("PART_Surface", "outer"), ("CollectionActive", "active"), ("CollectionBody", "body") })
            {
                var border = Nodes<Border>(control).Single(border => border.Name == name);
                paints[key + "Background"] = Paint(border.Background); paints[key + "Border"] = Paint(border.BorderBrush);
            }
            var count = Nodes<FrameworkElement>(control).Single(element => element.Name == "CollectionCountText");
            paints["countBackground"] = Paint(((Border)VisualTreeHelper.GetParent(count)).Background);
            paints["countForeground"] = Paint(Nodes<TextBlock>(count).Single(text => text.Name == "CollectionCountTextText").Foreground);
            if (Nodes<Button>(control).FirstOrDefault(button => button.Name == "CollectionChip") is { } chip) paints["chipBorder"] = Paint(chip.BorderBrush);
            return paints;
        }
        object Box(FrameworkElement element) { var rect = element.TransformToVisual(canvas).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight)); return new { x = rect.X, y = rect.Y, width = rect.Width, height = rect.Height }; }
        static Dictionary<string, BleeckerCollectionFilterValue> Values(JsonElement element) => element.ValueKind == JsonValueKind.Array
            ? element.EnumerateArray().ToDictionary(pair => pair[0].GetString()!, pair => pair[1].ValueKind is JsonValueKind.True or JsonValueKind.False ? new BleeckerCollectionFilterValue(pair[1].GetBoolean()) : new(pair[1].GetString()!))
            : element.EnumerateObject().ToDictionary(value => value.Name, value => value.Value.ValueKind is JsonValueKind.True or JsonValueKind.False ? new BleeckerCollectionFilterValue(value.Value.GetBoolean()) : new(value.Value.GetString()!));
        static object FilterValues(IReadOnlyDictionary<string, BleeckerCollectionFilterValue> values) => values.ToDictionary(value => value.Key, value => value.Value.Value);
        static object SortValue(BleeckerSortState sort) => new { field = sort.Field, order = sort.Order == BleeckerSortOrder.Ascending ? "asc" : "desc" };
        BleeckerCollectionFilters Create(JsonElement spec)
        {
            var sort = spec.GetProperty("currentSort");
            return new() { Width = spec.GetProperty("width").GetDouble(), DefaultExpanded = spec.TryGetProperty("expanded", out var expanded) && expanded.GetBoolean(), FlowDirection = spec.TryGetProperty("rtl", out var rtl) && rtl.GetBoolean() ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, CurrentFilters = Values(spec.TryGetProperty("filterEntries", out var entries) ? entries : spec.GetProperty("currentFilters")), CurrentSort = new(sort.GetProperty("field").GetString()!, sort.GetProperty("order").GetString() == "asc" ? BleeckerSortOrder.Ascending : BleeckerSortOrder.Descending),
                FilterOptions = spec.GetProperty("filterOptions").EnumerateArray().Select(option => new BleeckerCollectionFilterOption(option.GetProperty("field").GetString()!, option.GetProperty("label").GetString()!, Enum.Parse<BleeckerCollectionFilterType>(option.GetProperty("type").GetString()!, true), option.TryGetProperty("options", out var options) ? options.EnumerateArray().Select(item => new BleeckerSelectOption(item.GetProperty("value").GetString()!, item.GetProperty("label").GetString()!)).ToArray() : null)).ToArray(),
                SortOptions = spec.GetProperty("sortOptions").EnumerateArray().Select(option => new BleeckerCollectionSortOption(option.GetProperty("field").GetString()!, option.GetProperty("label").GetString()!)).ToArray() };
        }
        void Mount(BleeckerCollectionFilters control) { Canvas.SetLeft(control, 20); Canvas.SetTop(control, 20); canvas.Children.Add(control); }
        Button Button(BleeckerCollectionFilters control, string name) => Nodes<Button>(control).Single(button => button.Name == name);
        FrameworkElement Trigger(BleeckerSelect select) => Nodes<FrameworkElement>(select).Single(element => element.Name == "SelectTrigger");
        object FieldInput(Panel panel) => Box(panel.Children[1] is BleeckerSelect select ? Trigger(select) : Nodes<Button>(panel.Children[1]).Single());
        static bool Shown(FrameworkElement element)
        {
            for (DependencyObject? node = element; node is not null; node = VisualTreeHelper.GetParent(node)) if (node is FrameworkElement current && current.Visibility != Visibility.Visible) return false;
            return element.ActualWidth > 0 && element.ActualHeight > 0;
        }
        static void InvokeInactive(AutomationPeer peer)
        {
            try { ((Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider)peer.GetPattern(PatternInterface.Invoke))?.Invoke(); }
            catch (Exception error) when (error is ElementNotEnabledException or ElementNotAvailableException || error is System.Runtime.InteropServices.COMException && error.HResult is unchecked((int)0x80040200) or unchecked((int)0x80040201)) { }
        }
        try
        {
            Content = canvas; AppWindow.ResizeClient(new SizeInt32(1280, 900)); await AwaitPresentationAsync(canvas);
            Require(canvas.XamlRoot.RasterizationScale == 1, "Collection Filters requires unchanged 100% display scale.");
            using var input = new NativeInputSession(canvas.XamlRoot, folder, "collection-filters"); await input.AcquireAsync(canvas);
            foreach (var source in reference.RootElement.GetProperty("cases").EnumerateArray())
            {
                var spec = source.GetProperty("spec"); canvas.RequestedTheme = Enum.Parse<ElementTheme>(source.GetProperty("scheme").GetString()!); AppWindow.ResizeClient(new SizeInt32(spec.GetProperty("viewportWidth").GetInt32(), 900)); await AwaitPresentationAsync(canvas);
                var control = Create(spec); Mount(control); await AwaitPresentationAsync(control); await input.MoveAsync(anchor);
                var visible = source.GetProperty("measurement").GetProperty("visible").GetBoolean();
                var root = Nodes<Border>(control).Single(border => border.Name == "PART_Surface");
                var fields = !control.IsExpanded || !visible ? Array.Empty<object>() : Nodes<Panel>(control).Where(panel => panel.Name == "CollectionField").Select(panel => new { label = Nodes<TextBlock>(panel).First(text => text.Name == "CollectionFieldLabelText").Text, box = Box(panel), control = panel.Children.Count > 1 ? FieldInput(panel) : null }).Cast<object>().ToArray();
                var chips = Nodes<Button>(control).Where(button => button.Name == "CollectionChip").Select(button => new { name = AutomationProperties.GetName(button), label = Nodes<TextBlock>(button).Single(text => text.Name == "CollectionChipLabelText").Text, value = Nodes<TextBlock>(button).Single(text => text.Name == "CollectionChipValueText").Text, box = Box(button) }).ToArray();
                var controls = Nodes<FrameworkElement>(control).Where(element => (element is Button || element.Name == "SelectTrigger") && element.IsLoaded && Shown(element)).Select(element => new { name = FrameworkElementAutomationPeer.CreatePeerForElement(element).GetName(), box = Box(element), disabled = element is Control input && !input.IsEnabled }).ToArray();
                var header = Nodes<Border>(control).Single(border => border.Name == "CollectionHeader"); var body = Nodes<Border>(control).Single(border => border.Name == "CollectionBody");
                var file = source.GetProperty("file").GetString()!;
                if (visible) await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, file));
                else
                {
                    using var png = CatalogReferenceResources.Open(typeof(MainWindow).Assembly, "Orchard.Catalog.Reference.CollectionFilters." + file) ?? throw new InvalidOperationException("Empty Collection Filters reference missing.");
                    using var image = new MemoryStream(); await png.CopyToAsync(image); Require(Convert.ToHexStringLower(SHA256.HashData(image.ToArray())) == source.GetProperty("pngSha256").GetString(), "Empty source PNG changed.");
                    using var random = new Windows.Storage.Streams.InMemoryRandomAccessStream(); using (var writer = new Windows.Storage.Streams.DataWriter(random)) { writer.WriteBytes(image.ToArray()); await writer.StoreAsync(); writer.DetachStream(); } random.Seek(0);
                    var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(random); var pixels = (await decoder.GetPixelDataAsync(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, Windows.Graphics.Imaging.BitmapAlphaMode.Straight, new Windows.Graphics.Imaging.BitmapTransform(), Windows.Graphics.Imaging.ExifOrientationMode.IgnoreExifOrientation, Windows.Graphics.Imaging.ColorManagementMode.DoNotColorManage)).DetachPixelData();
                    await NativeScreenCapture.CaptureExactClientAsync(input.Window, Path.Combine(folder, file), pixels);
                }
                captures[file] = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(folder, file))));
                frames.Add(new { id = source.GetProperty("id").GetString(), scheme = source.GetProperty("scheme").GetString(), visible, viewportWidth = canvas.XamlRoot.Size.Width, host = Box(control), root = visible ? Box(root) : null, header = visible ? Box(header) : null, expanded = control.IsExpanded, body = control.IsExpanded && visible ? Box(body) : null, fields = control.IsExpanded && visible ? fields : [], chips = visible ? chips : [], controls = visible ? controls : [], paint = visible ? Palette(control) : null });
                canvas.Children.Remove(control); await AwaitPresentationAsync(canvas);
            }
            await File.WriteAllTextAsync(Path.Combine(folder, "collection-filters-frames.json"), JsonSerializer.Serialize(new { diagnosticOnly = true, scope = "collection-filters-frames-only-diagnostic", frames, captures, nativeComponentAccepted = false }, new JsonSerializerOptions { WriteIndented = true }));
            AppWindow.ResizeClient(new SizeInt32(1280, 900));
            var initial = reference.RootElement.GetProperty("cases").EnumerateArray().First(row => row.GetProperty("id").GetString() == "active-expanded").GetProperty("spec");
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark }) foreach (var accept in new[] { false, true })
            {
                canvas.RequestedTheme = theme; var control = Create(initial); var producer = new ContentControl { Tag = control.CurrentFilters };
                control.SetBinding(BleeckerCollectionFilters.CurrentFiltersProperty, new Binding { Source = producer, Path = new PropertyPath("Tag"), Mode = BindingMode.OneWay });
                var requests = new List<object>(); control.FiltersRequested += (_, request) => { requests.Add(new { kind = "filters", value = FilterValues(request.Filters) }); if (accept) producer.Tag = request.Filters; }; control.SortRequested += (_, request) => { requests.Add(new { kind = "sort", value = SortValue(request.Sort) }); if (accept) control.CurrentSort = request.Sort; };
                Mount(control); await AwaitPresentationAsync(control); var samples = new List<object>();
                async Task Sample(string action) { await AwaitPresentationAsync(control); samples.Add(new { action, currentFilters = FilterValues(control.CurrentFilters), currentSort = SortValue(control.CurrentSort), requests = requests.ToArray(), expanded = control.IsExpanded ? "true" : "false" }); }
                var falseChip = Nodes<Button>(control).Single(button => AutomationProperties.GetName(button) == "Remove Featured edit filter");
                // A native Button may remove itself during its release class
                // handler, before PointerReleased bubbles. Bind the real mouse
                // press to its request instead of requiring the retired node
                // to receive a later routed release notification.
                await input.MoveAsync(falseChip); var expected = input.ReadPointerPosition(); var trustedPress = false; var pressReceipt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var requestCount = requests.Count;
                var pressed = new Microsoft.UI.Xaml.Input.PointerEventHandler((_, args) =>
                {
                    var point = args.GetCurrentPoint(canvas); var client = NativeScreenCapture.ClientBounds(input.Window);
                    trustedPress |= !args.IsGenerated && args.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse && point.Properties.IsLeftButtonPressed && Math.Abs(client.X + point.Position.X * canvas.XamlRoot.RasterizationScale - expected.X) < .001 && Math.Abs(client.Y + point.Position.Y * canvas.XamlRoot.RasterizationScale - expected.Y) < .001;
                    if (trustedPress) pressReceipt.TrySetResult();
                });
                falseChip.AddHandler(UIElement.PointerPressedEvent, pressed, true); var down = false;
                try { await input.PressPointerAsync(); down = true; await pressReceipt.Task.WaitAsync(TimeSpan.FromSeconds(2)); Require(trustedPress, "Removing chip did not receive the trusted native mouse press."); input.PointerButton(false); down = false; await AwaitPresentationAsync(control); }
                finally { if (down) input.PointerButton(false); falseChip.RemoveHandler(UIElement.PointerPressedEvent, pressed); }
                Require(requests.Count == requestCount + 1, "The real removing click did not reach the consumer request."); removalInputs.Add(new { scheme = theme.ToString(), accept, trustedMousePress = trustedPress, physicalReleaseSent = true, consumerRequestObserved = true }); await Sample("remove-false");
                foreach (var (label, action, end) in new[] { ("Availability", "select", false), ("Featured edit", "boolean-false", true), ("Sort collection by", "sort-field", false) })
                {
                    var select = Nodes<BleeckerSelect>(control).Single(select => AutomationProperties.GetName(select) == label); var requested = new List<string>(); select.ValueRequested += (_, value) => requested.Add(value.Value);
                    object Focus() { var focused = Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(canvas.XamlRoot) as FrameworkElement; return new { type = focused?.GetType().Name, name = focused?.Name, value = (focused?.Tag as BleeckerSelectOption)?.Value }; }
                    object Scope()
                    {
                        var parent = VisualTreeHelper.GetParent(select); var chain = new List<object>(); for (DependencyObject? node = select; node is not null; node = VisualTreeHelper.GetParent(node)) if (node is FrameworkElement element) chain.Add(new { type = node.GetType().Name, name = element.Name, loaded = element.IsLoaded, visibility = element.Visibility.ToString() });
                        var canAct = parent?.GetType().GetMethod("CanAct", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(parent, null); return new { selected = select.Value, requested = requested.ToArray(), canAct, chain };
                    }
                    await input.ClickAsync(Trigger(select)); var focusWait = System.Diagnostics.Stopwatch.StartNew();
                    bool OptionFocused() => Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(canvas.XamlRoot) is FrameworkElement focused && focused.Tag is BleeckerSelectOption option && select.Options.Any(originalOption => ReferenceEquals(originalOption, option));
                    while (!OptionFocused() && focusWait.Elapsed < TimeSpan.FromSeconds(2)) await AwaitPresentationAsync(control);
                    Require(OptionFocused(), "Collection Select did not focus an original option before keyboard input."); var opened = Focus();
                    input.Key(end ? (ushort)0x23 : (ushort)0x24); await AwaitPresentationAsync(control); var edge = Focus(); if (!end) { input.Key(0x28); await AwaitPresentationAsync(control); } var chosen = Focus(); input.Key(0x0D); await Sample(action);
                    inputTrace.Add(new { scheme = theme.ToString(), accept, label, opened, edge, chosen, after = Focus(), scope = Scope() }); await File.WriteAllTextAsync(Path.Combine(folder, "collection-filters-input-trace.json"), JsonSerializer.Serialize(inputTrace, new JsonSerializerOptions { WriteIndented = true }));
                }
                await input.ClickAsync(Button(control, "CollectionDirection")); await Sample("sort-direction"); await input.ClickAsync(Button(control, "CollectionClear")); await Sample("clear");
                var toggle = Nodes<BleeckerButton>(control).Single(button => button.Name == "CollectionToggle"); await input.ClickAsync(Nodes<Button>(toggle).Single()); await Sample("collapse");
                control.DefaultExpanded = true; producer.Tag = new Dictionary<string, BleeckerCollectionFilterValue> { ["featured"] = false, ["missing"] = "Original" }; await Sample("programmatic-default-ignored");
                var bindingRetained = control.GetBindingExpression(BleeckerCollectionFilters.CurrentFiltersProperty) is not null; Require(bindingRetained && !control.IsExpanded, "Collection Filters assigned its producer or reapplied its default.");
                var before = requests.Count; var filterOptions = control.FilterOptions; var sortOptions = control.SortOptions;
                var stale = FrameworkElementAutomationPeer.CreatePeerForElement(Button(control, "CollectionClear")); var template = control.Template;
                control.Template = null; control.Template = template; await AwaitPresentationAsync(control);
                InvokeInactive(stale);
                Require(requests.Count == before && !control.IsExpanded && control.GetBindingExpression(BleeckerCollectionFilters.CurrentFiltersProperty) is not null, $"Template replacement retained a live obsolete action or lost state: requests={requests.Count}/{before}, expanded={control.IsExpanded}, binding={control.GetBindingExpression(BleeckerCollectionFilters.CurrentFiltersProperty) is not null}.");
                var hidden = FrameworkElementAutomationPeer.CreatePeerForElement(Button(control, "CollectionClear")); control.FilterOptions = Array.Empty<BleeckerCollectionFilterOption>(); control.SortOptions = Array.Empty<BleeckerCollectionSortOption>(); await AwaitPresentationAsync(control);
                InvokeInactive(hidden);
                Require(requests.Count == before && control.ActualHeight == 0, "Empty collection retained space or a live hidden action."); control.FilterOptions = filterOptions; control.SortOptions = sortOptions; await AwaitPresentationAsync(control); control.IsEnabled = false;
                InvokeInactive(FrameworkElementAutomationPeer.CreatePeerForElement(Button(control, "CollectionDirection")));
                Require(requests.Count == before, "Disabled collection emitted a request.");
                interactions.Add(new { scheme = theme.ToString(), accept, samples, bindingRetained, defaultUpdateIgnored = true, disabledRejected = true, templateReplacement = true, stalePeerRejected = true, emptySpaceRemoved = true, hiddenPeerRejected = true }); canvas.Children.Remove(control); await AwaitPresentationAsync(canvas);
            }
            var numericSpec = reference.RootElement.GetProperty("cases").EnumerateArray().First(row => row.GetProperty("id").GetString() == "numeric-keys").GetProperty("spec");
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark }) foreach (var accept in new[] { false, true })
            {
                canvas.RequestedTheme = theme; var control = Create(numericSpec); var original = control.CurrentFilters; var originalEntries = original.ToArray(); var requests = new List<IReadOnlyDictionary<string, BleeckerCollectionFilterValue>>(); var samples = new List<object>();
                control.FiltersRequested += (_, request) => { requests.Add(request.Filters); if (accept) control.CurrentFilters = request.Filters; };
                Mount(control); await AwaitPresentationAsync(control);
                async Task Sample(string action)
                {
                    await AwaitPresentationAsync(control); var chips = Nodes<Button>(control).Where(button => button.Name == "CollectionChip").ToArray();
                    samples.Add(new { action, keys = chips.Select(chip => (string)chip.Tag).ToArray(), chips = chips.Select(AutomationProperties.GetName).ToArray(), requestKeys = requests.Select(request => request.Keys.ToArray()).ToArray(), requests = requests.Select(request => new { kind = "filters", value = FilterValues(request) }).ToArray() });
                }
                await Sample("initial"); var removing = Nodes<Button>(control).Single(button => (string?)button.Tag == "2"); await input.MoveAsync(removing);
                var expected = input.ReadPointerPosition(); var trusted = false; var pressReceipt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var pressed = new Microsoft.UI.Xaml.Input.PointerEventHandler((_, args) =>
                {
                    var point = args.GetCurrentPoint(canvas); var client = NativeScreenCapture.ClientBounds(input.Window);
                    trusted |= !args.IsGenerated && args.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse && point.Properties.IsLeftButtonPressed && Math.Abs(client.X + point.Position.X * canvas.XamlRoot.RasterizationScale - expected.X) < .001 && Math.Abs(client.Y + point.Position.Y * canvas.XamlRoot.RasterizationScale - expected.Y) < .001;
                    if (trusted) pressReceipt.TrySetResult();
                });
                removing.AddHandler(UIElement.PointerPressedEvent, pressed, true); var down = false;
                try { await input.PressPointerAsync(); down = true; await pressReceipt.Task.WaitAsync(TimeSpan.FromSeconds(2)); input.PointerButton(false); down = false; await AwaitPresentationAsync(control); }
                finally { if (down) input.PointerButton(false); removing.RemoveHandler(UIElement.PointerPressedEvent, pressed); }
                Require(trusted && requests.Count == 1, "Numeric chip removal missed its trusted mouse input or request."); await Sample("remove-index");
                var select = Nodes<BleeckerSelect>(control).Single(select => AutomationProperties.GetName(select) == "Second"); await input.ClickAsync(Trigger(select)); var wait = System.Diagnostics.Stopwatch.StartNew();
                bool Focused() => Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(canvas.XamlRoot) is FrameworkElement focused && focused.Tag is BleeckerSelectOption option && select.Options.Any(originalOption => ReferenceEquals(originalOption, option));
                while (!Focused() && wait.Elapsed < TimeSpan.FromSeconds(2)) await AwaitPresentationAsync(control);
                Require(Focused(), "Numeric Boolean editor did not focus an original option."); input.Key(0x24); await AwaitPresentationAsync(control); input.Key(0x28); await AwaitPresentationAsync(control); input.Key(0x0D); await Sample("restore-index");
                Require(requests.Count == 2 && originalEntries.SequenceEqual(original) && (accept || ReferenceEquals(original, control.CurrentFilters)), "Numeric requests changed their original producer or were missing.");
                keyOrderInteractions.Add(new { scheme = theme.ToString(), accept, samples, trustedMouseInput = trusted, originalDictionaryUnchanged = true, declinedProducerRetained = accept || ReferenceEquals(original, control.CurrentFilters) }); canvas.Children.Remove(control); await AwaitPresentationAsync(canvas);
            }
            await File.WriteAllTextAsync(Path.Combine(folder, "collection-filters.json"), JsonSerializer.Serialize(new { passed = true, scope = "foreground-collection-filters-source-capture", referenceSha256 = Convert.ToHexStringLower(SHA256.HashData(bytes.ToArray())), librarySha256 = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(typeof(BleeckerCollectionFilters).Assembly.Location))), catalogSha256 = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(typeof(MainWindow).Assembly.Location))), frames, interactions, keyOrderInteractions, removalInputs, captures, rawClientFrames = true, sourcePixelsPainted = false, nativeComponentAccepted = false, sourceRasterParityVerified = false }, new JsonSerializerOptions { WriteIndented = true }));
            await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "collection-filters", environment = RuntimeEnvironment() }));
        }
        finally { Content = previousContent; AppWindow.ResizeClient(previousSize); await AwaitPresentationAsync(CatalogRoot); }
    }
}
