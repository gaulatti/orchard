using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Orchard;
using Windows.Foundation;
using Windows.Graphics;

namespace PackageConsumer;

public partial class App
{
    private async void RunHiddenCollectionFiltersAsync(string referenceDirectory)
    {
        var arguments = Environment.GetCommandLineArgs(); var output = arguments[Array.IndexOf(arguments, "--verify-output") + 1];
        nint handle = 0; var frames = new List<object>(); var checks = new List<object>(); var keyOrderChecks = new List<object>(); var failures = new List<string>();
        var completed = false; var guards = 0; var activationEvents = 0; var focusEvents = 0; string? failure = null, isolationFailure = null, referenceHash = null;
        void Guard() { guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); if (isolationFailure is not null || window!.Visible || window.AppWindow.IsVisible || IsWindowVisible(handle) || foreground == Environment.ProcessId || focusEvents != 0) throw new InvalidOperationException(isolationFailure ?? "Hidden Collection Filters became visible, focused or owned foreground input."); }
        static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        void Check(string id, Action action) { Guard(); try { action(); checks.Add(new { id, passed = true }); } catch (Exception error) { failures.Add(id + ": " + error.Message); checks.Add(new { id, passed = false, error = error.Message }); } Guard(); }
        static void Invoke(AutomationPeer peer, bool inactive = false)
        {
            try { ((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke))?.Invoke(); }
            catch (Exception error) when (inactive && (error is ElementNotEnabledException or ElementNotAvailableException || error is COMException && error.HResult is unchecked((int)0x80040200) or unchecked((int)0x80040201))) { }
        }
        static Dictionary<string, BleeckerCollectionFilterValue> Values(JsonElement values) => values.EnumerateObject().ToDictionary(value => value.Name, value => value.Value.ValueKind is JsonValueKind.True or JsonValueKind.False ? new BleeckerCollectionFilterValue(value.Value.GetBoolean()) : new(value.Value.GetString()!));
        static BleeckerCollectionFilters Create(JsonElement spec)
        {
            var sort = spec.GetProperty("currentSort");
            return new() { Width = spec.GetProperty("width").GetDouble(), DefaultExpanded = spec.TryGetProperty("expanded", out var expanded) && expanded.GetBoolean(), FlowDirection = spec.TryGetProperty("rtl", out var rtl) && rtl.GetBoolean() ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, CurrentFilters = spec.TryGetProperty("filterEntries", out var entries) ? entries.EnumerateArray().ToDictionary(pair => pair[0].GetString()!, pair => pair[1].ValueKind is JsonValueKind.True or JsonValueKind.False ? new BleeckerCollectionFilterValue(pair[1].GetBoolean()) : new(pair[1].GetString()!)) : Values(spec.GetProperty("currentFilters")), CurrentSort = new(sort.GetProperty("field").GetString()!, sort.GetProperty("order").GetString() == "asc" ? BleeckerSortOrder.Ascending : BleeckerSortOrder.Descending),
                FilterOptions = spec.GetProperty("filterOptions").EnumerateArray().Select(option => new BleeckerCollectionFilterOption(option.GetProperty("field").GetString()!, option.GetProperty("label").GetString()!, Enum.Parse<BleeckerCollectionFilterType>(option.GetProperty("type").GetString()!, true), option.TryGetProperty("options", out var options) ? options.EnumerateArray().Select(item => new BleeckerSelectOption(item.GetProperty("value").GetString()!, item.GetProperty("label").GetString()!)).ToArray() : null)).ToArray(),
                SortOptions = spec.GetProperty("sortOptions").EnumerateArray().Select(option => new BleeckerCollectionSortOption(option.GetProperty("field").GetString()!, option.GetProperty("label").GetString()!)).ToArray() };
        }
        try
        {
            window = new Window(); handle = WinRT.Interop.WindowNative.GetWindowHandle(window); Guard(); window.Activated += (_, args) => { if (args.WindowActivationState != WindowActivationState.Deactivated) { activationEvents++; isolationFailure = "Hidden collection window activated."; } }; window.VisibilityChanged += (_, args) => { if (args.Visible) isolationFailure = "Hidden collection window became visible."; };
            var width = 1280; var root = new Canvas { Width = width, Height = 900, UseLayoutRounding = false }; root.GotFocus += (_, _) => focusEvents++; window.Content = root; window.AppWindow.ResizeClient(new SizeInt32(width, 900));
            async Task Layout() { for (var index = 0; index < 3; index++) { root.Measure(new Size(width, 900)); root.Arrange(new Rect(0, 0, width, 900)); root.UpdateLayout(); Guard(); await Task.Delay(10); } }
            object Box(FrameworkElement element) { var rect = element.TransformToVisual(root).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight)); return new { x = rect.X, y = rect.Y, width = rect.Width, height = rect.Height }; }
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
                    var border = HiddenDesc<Border>(control).Single(border => border.Name == name);
                    paints[key + "Background"] = Paint(border.Background); paints[key + "Border"] = Paint(border.BorderBrush);
                }
                var count = HiddenDesc<FrameworkElement>(control).Single(element => element.Name == "CollectionCountText");
                paints["countBackground"] = Paint(((Border)VisualTreeHelper.GetParent(count)).Background);
                paints["countForeground"] = Paint(HiddenDesc<TextBlock>(count).Single(text => text.Name == "CollectionCountTextText").Foreground);
                if (HiddenDesc<Button>(control).FirstOrDefault(button => button.Name == "CollectionChip") is { } chip) paints["chipBorder"] = Paint(chip.BorderBrush);
                return paints;
            }
            void Mount(BleeckerCollectionFilters control) { Canvas.SetLeft(control, 20); Canvas.SetTop(control, 20); root.Children.Add(control); }
            Button Action(BleeckerCollectionFilters control, string name) => HiddenDesc<Button>(control).Single(button => button.Name == name);
            AutomationPeer Peer(BleeckerCollectionFilters control, string name) => FrameworkElementAutomationPeer.CreatePeerForElement(Action(control, name));
            object Input(Panel panel) => Box(panel.Children[1] is BleeckerSelect select ? HiddenDesc<FrameworkElement>(select).Single(element => element.Name == "SelectTrigger") : HiddenDesc<Button>(panel.Children[1]).Single());
            await Layout(); Assert(root.XamlRoot is not null && root.XamlRoot.RasterizationScale == 1, "Hidden collection requires the actual 100% root.");
            var bytes = await File.ReadAllBytesAsync(Path.Combine(referenceDirectory, "CollectionFilters.json")); referenceHash = Convert.ToHexStringLower(SHA256.HashData(bytes)); using var reference = JsonDocument.Parse(bytes);
            Assert(reference.RootElement.GetProperty("sourceSha256").GetString() == "9b220e110dde6293e081ff555c6b69c9a0e34fee352e5b46888646b418774d69", "Unexpected Collection Filters source.");
            foreach (var source in reference.RootElement.GetProperty("cases").EnumerateArray())
            {
                var spec = source.GetProperty("spec"); width = spec.GetProperty("viewportWidth").GetInt32(); root.Width = width; window.AppWindow.ResizeClient(new SizeInt32(width, 900)); root.RequestedTheme = Enum.Parse<ElementTheme>(source.GetProperty("scheme").GetString()!); await Layout();
                var control = Create(spec); Mount(control); await Layout(); var visible = source.GetProperty("measurement").GetProperty("visible").GetBoolean();
                var surface = HiddenDesc<Border>(control).Single(border => border.Name == "PART_Surface"); var header = HiddenDesc<Border>(control).Single(border => border.Name == "CollectionHeader"); var body = HiddenDesc<Border>(control).Single(border => border.Name == "CollectionBody");
                var fields = !visible || !control.IsExpanded ? Array.Empty<object>() : HiddenDesc<Panel>(control).Where(panel => panel.Name == "CollectionField").Select(panel => new { label = HiddenDesc<TextBlock>(panel).First(text => text.Name == "CollectionFieldLabelText").Text, box = Box(panel), control = panel.Children.Count > 1 ? Input(panel) : null }).Cast<object>().ToArray();
                var chips = HiddenDesc<Button>(control).Where(button => button.Name == "CollectionChip").Select(button => new { name = AutomationProperties.GetName(button), label = HiddenDesc<TextBlock>(button).Single(text => text.Name == "CollectionChipLabelText").Text, value = HiddenDesc<TextBlock>(button).Single(text => text.Name == "CollectionChipValueText").Text, box = Box(button) }).ToArray();
                frames.Add(new { id = source.GetProperty("id").GetString(), scheme = source.GetProperty("scheme").GetString(), visible, viewportWidth = root.XamlRoot!.Size.Width, host = Box(control), root = visible ? Box(surface) : null, header = visible ? Box(header) : null, expanded = control.IsExpanded, body = visible && control.IsExpanded ? Box(body) : null, fields, chips = visible ? chips : [], paint = visible ? Palette(control) : null }); root.Children.Remove(control); await Layout();
            }
            width = 1280; root.Width = width; window.AppWindow.ResizeClient(new SizeInt32(width, 900));
            var initial = reference.RootElement.GetProperty("cases").EnumerateArray().First(row => row.GetProperty("id").GetString() == "active-expanded").GetProperty("spec");
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                root.RequestedTheme = theme; var control = Create(initial); var originalSort = control.CurrentSort; var filters = control.CurrentFilters; var options = control.FilterOptions; var sorts = control.SortOptions; var producer = new TextBlock { Tag = filters };
                control.SetBinding(BleeckerCollectionFilters.CurrentFiltersProperty, new Binding { Source = producer, Path = new PropertyPath("Tag"), Mode = BindingMode.OneWay }); var filterRequests = new List<IReadOnlyDictionary<string, BleeckerCollectionFilterValue>>(); var sortRequests = new List<BleeckerSortState>(); control.FiltersRequested += (_, request) => filterRequests.Add(request.Filters); control.SortRequested += (_, request) => sortRequests.Add(request.Sort);
                Mount(control); await Layout(); var chip = HiddenDesc<Button>(control).Single(button => AutomationProperties.GetName(button) == "Remove Featured edit filter"); var chipPeer = FrameworkElementAutomationPeer.CreatePeerForElement(chip); Invoke(chipPeer); Invoke(chipPeer); await Layout();
                Check(theme + "-declined-binding", () => Assert(ReferenceEquals(filters, control.CurrentFilters) && control.GetBindingExpression(BleeckerCollectionFilters.CurrentFiltersProperty) is not null && filterRequests.Count == 2 && filterRequests.All(request => !request.ContainsKey("featured") && request["status"].Value is "preorder") && !ReferenceEquals(filterRequests[0], filterRequests[1]), "Package collection assigned its producer or reused a requested dictionary."));
                Invoke(Peer(control, "CollectionDirection")); Invoke(Peer(control, "CollectionDirection")); await Layout();
                Check(theme + "-sort-owner", () => Assert(ReferenceEquals(originalSort, control.CurrentSort) && sortRequests.Count == 2 && sortRequests.All(sort => sort.Field == originalSort.Field && sort.Order == BleeckerSortOrder.Ascending), "Package collection assigned its caller's sort."));
                producer.Tag = new Dictionary<string, BleeckerCollectionFilterValue> { ["featured"] = false, ["missing"] = "Original" }; await Layout();
                Check(theme + "-false-unknown", () => Assert(control.CurrentFilters["featured"].Value is false && HiddenDesc<TextBlock>(control).Any(text => text.Name == "CollectionChipValueText" && text.Text == "No") && ReferenceEquals(control.FilterOptions, options) && filterRequests.Count == 2 && sortRequests.Count == 2, "Package collection lost original false/unknown values or emitted on a producer update."));
                var expand = (IExpandCollapseProvider)FrameworkElementAutomationPeer.CreatePeerForElement(control).GetPattern(PatternInterface.ExpandCollapse); expand.Collapse(); control.DefaultExpanded = true; await Layout();
                Check(theme + "-default-once", () => Assert(!control.IsExpanded && expand.ExpandCollapseState == ExpandCollapseState.Collapsed && filterRequests.Count == 2, "Package collection reapplied its default."));
                var stale = Peer(control, "CollectionClear"); var template = control.Template; control.Template = null; control.Template = template; await Layout(); Invoke(stale, true);
                Check(theme + "-template", () => Assert(!control.IsExpanded && control.GetBindingExpression(BleeckerCollectionFilters.CurrentFiltersProperty) is not null && filterRequests.Count == 2, "Package collection kept a live stale action or lost template state."));
                var hidden = Peer(control, "CollectionClear"); control.FilterOptions = Array.Empty<BleeckerCollectionFilterOption>(); control.SortOptions = Array.Empty<BleeckerCollectionSortOption>(); await Layout(); Invoke(hidden, true);
                Check(theme + "-empty", () => Assert(control.ActualHeight == 0 && !FrameworkElementAutomationPeer.CreatePeerForElement(control).IsControlElement() && filterRequests.Count == 2, "Empty package collection retained space, metadata or an actionable peer.")); control.FilterOptions = options; control.SortOptions = sorts; await Layout();
                var detached = Peer(control, "CollectionClear"); root.Children.Remove(control); await Layout(); Invoke(detached, true);
                Check(theme + "-detach", () => Assert(filterRequests.Count == 2 && sortRequests.Count == 2, "Detached package collection emitted a request."));
            }
            var numericSource = reference.RootElement.GetProperty("cases").EnumerateArray().First(row => row.GetProperty("id").GetString() == "numeric-keys").GetProperty("spec");
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                root.RequestedTheme = theme; var control = Create(numericSource); var original = control.CurrentFilters; var snapshot = original.ToArray(); var requests = new List<IReadOnlyDictionary<string, BleeckerCollectionFilterValue>>(); control.FiltersRequested += (_, request) => requests.Add(request.Filters);
                Mount(control); await Layout(); var keys = HiddenDesc<Button>(control).Where(button => button.Name == "CollectionChip").Select(button => (string)button.Tag).ToArray();
                var removing = HiddenDesc<Button>(control).Single(button => (string?)button.Tag == "2"); Invoke(FrameworkElementAutomationPeer.CreatePeerForElement(removing)); Invoke(FrameworkElementAutomationPeer.CreatePeerForElement(removing)); await Layout();
                Check(theme + "-numeric-order", () => Assert(requests.Count == 2 && !ReferenceEquals(requests[0], requests[1]) && ReferenceEquals(control.CurrentFilters, original) && snapshot.SequenceEqual(original), "Numeric package requests assigned or changed the original dictionary."));
                keyOrderChecks.Add(new { scheme = theme.ToString(), keys, callerKeys = original.Keys.ToArray(), requestKeys = requests.Select(request => request.Keys.ToArray()).ToArray(), originalDictionaryUnchanged = snapshot.SequenceEqual(original) });
                root.Children.Remove(control); await Layout();
            }
            Assert(frames.Count == 50 && checks.Count == 16, "Incomplete hidden collection matrix."); completed = failures.Count == 0;
        }
        catch (Exception error) { failure = error.ToString(); }
        finally
        {
            var visible = window?.Visible ?? false; var nativeVisible = handle != 0 && IsWindowVisible(handle); GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var isolated = !visible && !nativeVisible && activationEvents == 0 && focusEvents == 0 && isolationFailure is null && foreground != Environment.ProcessId;
            var librarySha256 = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(typeof(BleeckerCollectionFilters).Assembly.Location)));
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = completed && isolated, scope = "hidden-collection-filters-contracts", hiddenWindow = true, activateCalled = false, showCalled = false, focusCalled = false, inputSent = false, visible, nativeVisible, activationEvents, focusEvents, guardChecks = guards, isolated, processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(), osArchitecture = RuntimeInformation.OSArchitecture.ToString(), referenceSha256 = referenceHash, librarySha256, frames, checks, keyOrderChecks, failures, failure, isolationFailure, visibleRuntimeVerified = false, sourceRasterParityVerified = false, nativeComponentAccepted = false }, new JsonSerializerOptions { WriteIndented = true })); window?.Close(); Environment.Exit(completed && isolated ? 0 : 1);
        }
    }
}
