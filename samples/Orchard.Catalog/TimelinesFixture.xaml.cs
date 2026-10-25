using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class TimelinesFixture : UserControl
{
    private int actions;
    public TimelinesFixture()
    {
        InitializeComponent(); using var reference = ReadReference();
        CasePicker.ItemsSource = reference.RootElement.GetProperty("cases").EnumerateArray().Where(c => c.GetProperty("scheme").GetString() == "Light" && c.GetProperty("viewportWidth").GetInt32() == 1024).Select(c => c.GetProperty("id").GetString()!).ToArray();
        CasePicker.SelectedIndex = 0; ActualThemeChanged += (_, _) => UpdateBackground(); UpdateBackground();
    }
    private static JsonDocument ReadReference() => JsonDocument.Parse(CatalogReferenceResources.Open(typeof(TimelinesFixture).Assembly, "Orchard.Catalog.Reference.Timeline.json")!);
    private int Viewport => XamlRoot is { Size.Width: < 640 } ? 639 : XamlRoot?.Size.Width == 640 ? 640 : 1024;
    private void CaseChanged(object sender, SelectionChangedEventArgs args)
    {
        if (CasePicker.SelectedItem is not string id) return;
        using var reference = ReadReference(); Apply(reference.RootElement.GetProperty("cases").EnumerateArray().First(c => c.GetProperty("id").GetString() == id && c.GetProperty("viewportWidth").GetInt32() == Viewport));
    }
    private void UpdateBackground() => Frame.Background = new SolidColorBrush(ActualTheme == ElementTheme.Dark ? Windows.UI.Color.FromArgb(255, 13, 24, 33) : Microsoft.UI.Colors.White);
    private void Apply(JsonElement spec)
    {
        Timeline.Width = spec.GetProperty("width").GetDouble(); Frame.Width = spec.GetProperty("viewportWidth").GetInt32();
        Frame.Height = Math.Max(900, Math.Ceiling(spec.GetProperty("normal").GetProperty("box").GetProperty("height").GetDouble() + 40));
        // Browser screenshots clip overflow at the viewport, while retaining
        // overflow inside the Timeline itself (including its negative RTL x).
        Frame.Clip = new RectangleGeometry { Rect = new Rect(0, 0, Frame.Width, Frame.Height) };
        Timeline.FlowDirection = spec.GetProperty("direction").GetString() == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        Timeline.Items = new ObservableCollection<BleeckerTimelineItem>(spec.GetProperty("props").GetProperty("items").EnumerateArray().Select(item =>
            new BleeckerTimelineItem(item.GetProperty("id").GetString()!, item.GetProperty("title").GetString()!)
            {
                Description = item.TryGetProperty("description", out var description) ? description.GetString() : null,
                Timestamp = item.TryGetProperty("timestamp", out var timestamp) ? timestamp.GetString() : null,
                Status = item.TryGetProperty("status", out var status) ? Enum.Parse<BleeckerTimelineStatus>(status.GetString()!, true) : null,
                Icon = item.TryGetProperty("icon", out var icon) ? CreateIcon(icon) : null
            }));
        UpdateBackground();
    }
    private object? CreateIcon(JsonElement icon) => icon.ValueKind switch
    {
        JsonValueKind.Null => null, JsonValueKind.True => true, JsonValueKind.False => false, JsonValueKind.String => icon.GetString(),
        JsonValueKind.Number => icon.GetDouble(), JsonValueKind.Object => Action(icon.GetProperty("action").GetString()!),
        _ => throw new InvalidOperationException("Unknown Timeline fixture icon.")
    };
    private Button Action(string name)
    {
        var button = new Button { Width = 20, Height = 20, Padding = new Thickness(), BorderThickness = new Thickness(), MinWidth = 0, MinHeight = 0,
            Content = new TextBlock { Text = "E", FontFamily = (FontFamily)Application.Current.Resources["BleeckerPrimaryFontFamily"], FontSize = 16, LineHeight = 20,
                LineStackingStrategy = LineStackingStrategy.BlockLineHeight, UseLayoutRounding = false }, UseLayoutRounding = false };
        button.Style = (Style)Resources["InlineIconStyle"]; button.Template = (ControlTemplate)Resources["InlineIconTemplate"]; AutomationProperties.SetName(button, name);
        button.Click += (_, _) => actions++; return button;
    }
    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder, Grid catalogRoot, FrameworkElement foregroundTarget)
    {
        using var reference = ReadReference(); var viewport = Viewport; var cases = new List<object>(); var failures = new List<string>();
        using (var capture = new ViewportCapture(this, catalogRoot, folder, scheme))
        {
        await capture.MountAsync(foregroundTarget);
        foreach (var spec in reference.RootElement.GetProperty("cases").EnumerateArray().Where(c => c.GetProperty("scheme").GetString() == scheme.ToString() && c.GetProperty("viewportWidth").GetInt32() == viewport))
        {
            var id = spec.GetProperty("id").GetString()!;
            await File.AppendAllTextAsync(Path.Combine(folder, "timeline-progress.jsonl"), JsonSerializer.Serialize(new { id, scheme, viewport, phase = "apply" }) + "\n");
            Apply(spec); await SettleAsync();
            var source = spec.GetProperty("normal"); var errors = new List<string>(); var measurements = new List<object>();
            var rows = Descendants<Panel>(Timeline).Where(row => row.Name == "TimelineRow").ToArray();
            Near(Timeline.ActualWidth, source.GetProperty("box").GetProperty("width").GetDouble(), "timeline width"); Near(Timeline.ActualHeight, source.GetProperty("box").GetProperty("height").GetDouble(), "timeline height");
            Require(rows.Length == source.GetProperty("rows").GetArrayLength(), "Timeline row count differs.");
            for (var index = 0; index < rows.Length; index++)
            {
                var row = rows[index]; var expected = source.GetProperty("rows")[index];
                var icon = Part(row, "TimelineIcon"); var body = Part(row, "TimelineBody"); var header = Part(row, "TimelineHeader");
                CheckBox(row, expected.GetProperty("box"), "row"); CheckBox(icon, expected.GetProperty("icon").GetProperty("box"), "icon");
                CheckBox(body, expected.GetProperty("body"), "body"); CheckBox(header, expected.GetProperty("header"), "header");
                var title = Part(row, "TimelineTitle"); var timestamp = Part(row, "TimelineTimestamp"); var description = Part(row, "TimelineDescription");
                CheckText(title, expected.GetProperty("title"), "title"); CheckText(timestamp, expected.GetProperty("timestamp"), "timestamp"); CheckText(description, expected.GetProperty("description"), "description");
                var iconContent = Part(row, "TimelineOrdinal").Visibility == Visibility.Visible ? Part(row, "TimelineOrdinal") : Part(row, "TimelineIconText");
                var sourceItem = spec.GetProperty("props").GetProperty("items")[index];
                var consumerIcon = sourceItem.TryGetProperty("icon", out var sourceIcon) && sourceIcon.ValueKind == JsonValueKind.Object;
                if (!consumerIcon && expected.GetProperty("icon").GetProperty("firstBaseline").ValueKind != JsonValueKind.Null)
                {
                    Near(Typography(iconContent).baseline ?? throw new InvalidOperationException("Timeline lost its scalar icon."), expected.GetProperty("icon").GetProperty("firstBaseline").GetDouble(), "icon baseline");
                    if (expected.GetProperty("icon").GetProperty("child").ValueKind != JsonValueKind.Null) CheckBox(iconContent, expected.GetProperty("icon").GetProperty("child"), "ordinal");
                }
                var connector = Part(row, "TimelineConnector"); var expectedConnector = expected.GetProperty("connector");
                if (expectedConnector.ValueKind == JsonValueKind.Null) Require(connector.Visibility == Visibility.Collapsed, "Timeline retained the final connector.");
                else CheckBox(connector, expectedConnector.GetProperty("box"), "connector");
                var frame = (Border)Part(row, "TimelineIconFrame"); Require(frame.CornerRadius == new CornerRadius(9) && frame.BorderThickness == new Thickness(1), "Timeline icon chrome differs.");
                var peer = FrameworkElementAutomationPeer.CreatePeerForElement(row);
                Require(peer.GetAutomationControlType() == AutomationControlType.ListItem && AutomationProperties.GetPositionInSet(row) == index + 1 && AutomationProperties.GetSizeOfSet(row) == rows.Length, "Timeline row semantics differ.");
                Require(AutomationProperties.GetItemStatus(row).Equals(expected.GetProperty("status").GetString(), StringComparison.OrdinalIgnoreCase), "Timeline resolved status differs.");
                measurements.Add(new { row = Box(row), icon = Box(icon), body = Box(body), header = Box(header), title = Box(title), timestamp = Box(timestamp), description = Box(description),
                    titleTypography = Typography(title), timestampTypography = Typography(timestamp), descriptionTypography = Typography(description), iconTypography = Typography(consumerIcon ? icon : iconContent),
                    iconContent = Box(iconContent), status = AutomationProperties.GetItemStatus(row),
                    connector = connector.Visibility == Visibility.Visible ? (Rect?)Box(connector) : null, name = peer.GetName() });
            }
            var file = spec.GetProperty("file").GetString()!; await capture.CaptureAsync(file);
            failures.AddRange(errors.Select(error => id + ": " + error)); cases.Add(new { id, viewportWidth = viewport, rows = measurements, errors });
            await File.WriteAllTextAsync(Path.Combine(folder, $"timeline-source-{viewport}-{scheme}.json"), JsonSerializer.Serialize(new { cases, failures }, new JsonSerializerOptions { WriteIndented = true }));
            void Near(double actual, double target, string name) { if (Math.Abs(actual - target) > .05) errors.Add($"{name}: actual={actual:R}, source={target:R}"); }
            void CheckBox(FrameworkElement element, JsonElement target, string name)
            {
                var box = Box(element); Near(box.X, target.GetProperty("x").GetDouble(), name + " x"); Near(box.Y, target.GetProperty("y").GetDouble(), name + " y");
                Near(box.Width, target.GetProperty("width").GetDouble(), name + " width"); Near(box.Height, target.GetProperty("height").GetDouble(), name + " height");
            }
            void CheckText(FrameworkElement element, JsonElement target, string name)
            {
                if (target.ValueKind == JsonValueKind.Null) { Require(element.Visibility == Visibility.Collapsed, "Timeline retained absent " + name); return; }
                CheckBox(element, target.GetProperty("box"), name);
                Require(Descendants<TextBlock>(element).Single().TextReadingOrder == TextReadingOrder.UseFlowDirection, "Timeline lost explicit paragraph reading direction.");
                if (target.GetProperty("firstBaseline").ValueKind != JsonValueKind.Null)
                {
                    var actual = Typography(element); Near(actual.baseline ?? throw new InvalidOperationException("Timeline lost text."), target.GetProperty("firstBaseline").GetDouble(), name + " baseline");
                    Require(actual.text == System.Text.RegularExpressions.Regex.Replace(target.GetProperty("text").GetString()!, "[ \\t\\r\\n\\f]+", " ").Trim(' '), "Timeline changed scalar text.");
                }
            }
        }
        }
        await SettleAsync();
        Require(failures.Count == 0, "Timeline source geometry differs: " + string.Join("; ", failures.Take(16)));
        await VerifyLiveAsync(scheme, folder);
    }
    private async Task VerifyLiveAsync(BleeckerColorScheme scheme, string folder)
    {
        Timeline.FlowDirection = FlowDirection.LeftToRight; Timeline.Width = 320;
        var action = Action("Consumer edit"); var item = new BleeckerTimelineItem("a", "Created") { Icon = action, Timestamp = "09:15", Description = "Draft" };
        Timeline.Items = [item, new("b", "Review")]; await SettleAsync();
        var row = Descendants<Panel>(Timeline).First(p => p.Name == "TimelineRow");
        using var input = new NativeInputSession(XamlRoot, folder, "timeline-" + scheme); await input.AcquireAsync(CasePicker);
        var count = actions; await input.ClickAsync(action); action.Focus(FocusState.Keyboard); await input.KeyAsync(0x20, action); await input.KeyAsync(0x0D, action);
        Require(actions == count + 3 && ReferenceEquals(Descendants<Button>(Timeline).Single(), action), "Timeline lost its original consumer action.");
        Timeline.IsEnabled = false; await SettleAsync(); Require(!action.IsEnabled && !FrameworkElementAutomationPeer.CreatePeerForElement(action).IsEnabled(), "Disabled Timeline content stayed enabled.");
        Timeline.IsEnabled = true; item.Title = "Updated"; item.Status = BleeckerTimelineStatus.Error; Timeline.Items.Move(0, 1); await SettleAsync();
        Require(ReferenceEquals(Descendants<Panel>(Timeline).Last(p => p.Name == "TimelineRow"), row) && ReferenceEquals(Descendants<Button>(Timeline).Single(), action) && AutomationProperties.GetItemStatus(row) == "Error", "Timeline lost keyed metadata or icon updates.");
        item.Status = null; await SettleAsync(); Require(AutomationProperties.GetItemStatus(row) == "Pending", "Timeline default status did not follow its new ordinal.");
        Timeline.Items[1] = new("a", "Replacement") { Icon = action, Timestamp = "10:30" }; await SettleAsync();
        Require(ReferenceEquals(Descendants<Panel>(Timeline).Last(p => p.Name == "TimelineRow"), row), "Timeline replaced a retained key's row.");
        var template = Timeline.Template; Timeline.Template = null; await SettleAsync(); Timeline.Template = template; Timeline.ApplyTemplate(); await SettleAsync();
        Require(ReferenceEquals(Descendants<Button>(Timeline).Single(), action), "Timeline template replacement lost consumer content.");
        Frame.Children.Remove(Timeline); await MainWindow.AwaitPresentationAsync(Frame); Require(!Timeline.IsLoaded, "Timeline did not unload.");
        Timeline.Items[1].Title = "Changed while detached"; Frame.Children.Add(Timeline); await SettleAsync();
        Require(Timeline.IsLoaded && Descendants<TextBlock>(Timeline).Any(text => text.Text == "Changed while detached"), "Timeline did not hydrate detached updates.");
        Timeline.Items.RemoveAt(1); await SettleAsync(); Require(!Descendants<Button>(Timeline).Any(), "Timeline retained a removed consumer icon.");
        Timeline.Items = [new("zero", "Signed zero icon") { Icon = -0d }]; await SettleAsync();
        Require(Descendants<TextBlock>(Part(Timeline, "TimelineIconText")).Single().Text == "0", "Timeline changed the source's signed-zero icon rendering.");
        var shadowBrush = new SolidColorBrush(Microsoft.UI.Colors.Blue) { Opacity = .12 };
        var shadowTemplate = Timeline.Template; Timeline.Template = null; await SettleAsync();
        Timeline.Resources["BleeckerTimelineShadowBrush"] = shadowBrush;
        Timeline.Template = shadowTemplate; Timeline.ApplyTemplate(); await SettleAsync();
        var shadowHost = Part(Timeline, "TimelineShadowHost");
        var shadowContainer = (Microsoft.UI.Composition.ContainerVisual)Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.GetElementChildVisual(shadowHost);
        var shadow = (Microsoft.UI.Composition.DropShadow)((Microsoft.UI.Composition.SpriteVisual)shadowContainer.Children.First()).Shadow;
        Require(shadow.Color == shadowBrush.Color && Math.Abs(shadow.Opacity - .12) < .000001, "Timeline lost a scoped shadow resource override.");
        shadowBrush.Color = Microsoft.UI.Colors.Red; shadowBrush.Opacity = .2; await MainWindow.AwaitPresentationAsync(Timeline);
        Require(shadow.Color == shadowBrush.Color && Math.Abs(shadow.Opacity - .2) < .000001, "Timeline did not repaint a mutated shadow brush.");
        Timeline.Template = null; await SettleAsync(); Timeline.Resources.Remove("BleeckerTimelineShadowBrush");
        Timeline.Template = shadowTemplate; Timeline.ApplyTemplate(); await SettleAsync();
        var model = new BleeckerTimelineItem("model", "Original icon context");
        Timeline.IconTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='{Binding Title, Mode=OneWay}' /></DataTemplate>");
        Timeline.Items = [new("template", "Template row") { Icon = model }]; await SettleAsync();
        Require(Descendants<TextBlock>(Timeline).Any(text => text.Text == model.Title), "Timeline icon template lost its original data context.");
        model.Title = "Live icon context"; await SettleAsync(); Require(Descendants<TextBlock>(Timeline).Any(text => text.Text == model.Title), "Timeline icon template binding did not update.");
        Timeline.Items.Clear(); Timeline.IconTemplate = null; Timeline.Items.Add(new("responsive", "Title") { Timestamp = "10:30" }); await SettleAsync();
        await VerifyBreakpointAsync();
        await File.WriteAllTextAsync(Path.Combine(folder, $"timeline-input-{scheme}.json"), JsonSerializer.Serialize(new { passed = true, physicalActivations = 3, originalContent = true,
            inheritedDisabled = true, keyedReorder = true, keyedReplacement = true, ordinalDefaultStatus = true, templateReplacement = true, unloadRemount = true,
            removal = true, signedZero = true, liveShadowBrush = true, originalTemplateContext = true, liveTemplateBinding = true, liveBreakpoint = true }));
    }
    private async Task VerifyBreakpointAsync()
    {
        var window = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(XamlRoot.ContentIslandEnvironment.AppWindowId);
        var presenter = window.Presenter as Microsoft.UI.Windowing.OverlappedPresenter ?? throw new InvalidOperationException("Timeline requires an overlapped test window.");
        var size = window.ClientSize; var position = window.Position; var state = presenter.State;
        var row = Descendants<Panel>(Timeline).Single(p => p.Name == "TimelineRow");
        try
        {
            presenter.Restore(false);
            foreach (var width in new[] { 639, 640, 639 })
            {
                window.ResizeClient(new Windows.Graphics.SizeInt32(width, 900)); await SettleAsync();
                Require(XamlRoot.Size.Width == width && ReferenceEquals(Descendants<Panel>(Timeline).Single(p => p.Name == "TimelineRow"), row), "Timeline viewport change remounted its row.");
                var title = Box(Part(row, "TimelineTitle")); var timestamp = Box(Part(row, "TimelineTimestamp"));
                Require(width < 640 ? timestamp.Y >= title.Bottom + 4 : timestamp.Y < title.Bottom && timestamp.X > title.X, "Timeline missed the actual 640-DIP viewport boundary.");
            }
        }
        finally { if (state == Microsoft.UI.Windowing.OverlappedPresenterState.Maximized) presenter.Maximize(); else { window.ResizeClient(size); window.Move(position); } await SettleAsync(); }
        Require(window.ClientSize == size, "Timeline did not restore window size.");
    }
    private async Task CaptureEmptyAsync(string path)
    {
        var bitmap = new Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap(); await bitmap.RenderAsync(Frame); var buffer = await bitmap.GetPixelsAsync();
        var pixels = new byte[checked((int)buffer.Length)]; using (var reader = Windows.Storage.Streams.DataReader.FromBuffer(buffer)) reader.ReadBytes(pixels);
        await MainWindow.SavePngAsync(path, pixels, bitmap.PixelWidth, bitmap.PixelHeight);
    }
    private static FrameworkElement Part(DependencyObject root, string name) => Descendants<FrameworkElement>(root).Single(element => element.Name == name);
    private sealed record TextTypography(string text, double? baseline, double LineHeight);
    private TextTypography Typography(DependencyObject root)
    {
        var text = Descendants<TextBlock>(root).FirstOrDefault(text => text.Visibility == Visibility.Visible && text.Text.Length != 0);
        return text is null ? new("", null, 0) : new(text.Text, Box(text).Y + text.BaselineOffset, text.LineHeight);
    }
    private Rect Box(FrameworkElement element)
    {
        var bounds = element.TransformToVisual(Frame).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
        var root = Timeline.TransformToVisual(Frame).TransformBounds(new Rect(0, 0, Timeline.ActualWidth, Timeline.ActualHeight));
        return new Rect(bounds.X - root.X, bounds.Y - root.Y, bounds.Width, bounds.Height);
    }
    private async Task SettleAsync() { UpdateLayout(); await MainWindow.AwaitPresentationAsync(Timeline); UpdateLayout(); await MainWindow.AwaitPresentationAsync(Timeline); }
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        { var child = VisualTreeHelper.GetChild(root, i); if (child is T match) yield return match; foreach (var descendant in Descendants<T>(child)) yield return descendant; }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
