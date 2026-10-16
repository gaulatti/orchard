using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class DataListsFixture : UserControl
{
    private int actions;
    public DataListsFixture()
    {
        InitializeComponent();
        using var reference = ReadReference();
        CasePicker.ItemsSource = reference.RootElement.GetProperty("cases").EnumerateArray().Where(c => c.GetProperty("scheme").GetString() == "Light" && c.GetProperty("viewportWidth").GetInt32() == 1024).Select(c => c.GetProperty("id").GetString()!).ToArray();
        CasePicker.SelectedIndex = 0;
        ActualThemeChanged += (_, _) => UpdateBackground(); UpdateBackground();
    }
    private static JsonDocument ReadReference() => JsonDocument.Parse(typeof(DataListsFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.DataList.json")!);
    private int Viewport => XamlRoot is { Size.Width: < 640 } ? 639 : XamlRoot?.Size.Width == 640 ? 640 : 1024;
    private void CaseChanged(object sender, SelectionChangedEventArgs args)
    {
        if (CasePicker.SelectedItem is not string id) return;
        using var reference = ReadReference();
        Apply(reference.RootElement.GetProperty("cases").EnumerateArray().First(c => c.GetProperty("id").GetString() == id && c.GetProperty("viewportWidth").GetInt32() == Viewport));
    }
    private void UpdateBackground() => Frame.Background = new SolidColorBrush(ActualTheme == ElementTheme.Dark ? Windows.UI.Color.FromArgb(255, 13, 24, 33) : Microsoft.UI.Colors.White);
    private void Apply(JsonElement spec)
    {
        List.Width = spec.GetProperty("width").GetDouble(); Frame.Width = spec.GetProperty("viewportWidth").GetInt32();
        Frame.Height = Math.Max(900, Math.Ceiling(spec.GetProperty("normal").GetProperty("box").GetProperty("height").GetDouble() + 40));
        List.FlowDirection = spec.GetProperty("direction").GetString() == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        List.IsStriped = spec.GetProperty("props").GetProperty("striped").GetBoolean();
        List.Items = new ObservableCollection<BleeckerDataListItem>(spec.GetProperty("props").GetProperty("items").EnumerateArray().Select(item =>
            new BleeckerDataListItem(item.GetProperty("id").GetString()!, CreateContent(item.GetProperty("label")), CreateContent(item.GetProperty("value")))
            { Description = item.TryGetProperty("description", out var description) ? CreateContent(description) : null }));
        UpdateBackground();
    }
    private object? CreateContent(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null => null, JsonValueKind.True => true, JsonValueKind.False => false,
        JsonValueKind.String => value.GetString(), JsonValueKind.Number => value.GetDouble(),
        JsonValueKind.Object => Action(value.GetProperty("action").GetString()!),
        _ => throw new InvalidOperationException("Unexpected DataList fixture content.")
    };
    private Button Action(string label)
    {
        // The source's unstyled button inherits Encode Sans Medium 14/20 from
        // dd. Give this consumer button the same typography and zero box chrome.
        var text = new TextBlock { Text = label, LineHeight = 20, LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
            FontFamily = (FontFamily)Application.Current.Resources["BleeckerPrimaryMediumFontFamily"], FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.Medium, UseLayoutRounding = false };
        var button = new Button { Content = text, Padding = new Thickness(), BorderThickness = new Thickness(), MinWidth = 0, MinHeight = 0, UseLayoutRounding = false };
        button.Template = (ControlTemplate)Resources["InlineActionTemplate"];
        button.SetBinding(Control.ForegroundProperty, new Microsoft.UI.Xaml.Data.Binding { Source = List, Path = new PropertyPath(nameof(List.Foreground)) });
        button.Click += (_, _) => actions++; return button;
    }
    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        using var reference = ReadReference();
        using var typography = JsonDocument.Parse(typeof(DataListsFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.DataListTypography.json")!);
        var cases = new List<object>(); var failures = new List<string>();
        var viewport = Viewport;
        foreach (var spec in reference.RootElement.GetProperty("cases").EnumerateArray().Where(c => c.GetProperty("scheme").GetString() == scheme.ToString() && c.GetProperty("viewportWidth").GetInt32() == viewport))
        {
            await File.AppendAllTextAsync(Path.Combine(folder, "data-list-progress.jsonl"), JsonSerializer.Serialize(new { phase = "apply", id = spec.GetProperty("id").GetString(), scheme, viewport }) + "\n");
            Apply(spec); await SettleAsync();
            await File.AppendAllTextAsync(Path.Combine(folder, "data-list-progress.jsonl"), JsonSerializer.Serialize(new { phase = "presented", id = spec.GetProperty("id").GetString(), scheme, viewport }) + "\n");
            var id = spec.GetProperty("id").GetString()!; var expected = spec.GetProperty("normal"); var errors = new List<string>();
            var expectedText = typography.RootElement.GetProperty("reports").EnumerateArray().Single(test => test.GetProperty("id").GetString() == id &&
                test.GetProperty("viewportWidth").GetInt32() == viewport && test.GetProperty("scheme").GetString() == scheme.ToString());
            var rows = Descendants<Panel>(List).Where(row => row.Name == "DataListRow").ToArray();
            Near(List.ActualWidth, expected.GetProperty("box").GetProperty("width").GetDouble(), "list width");
            Near(List.ActualHeight, expected.GetProperty("box").GetProperty("height").GetDouble(), "list height");
            Require(rows.Length == expected.GetProperty("rows").GetArrayLength(), "DataList row count differs.");
            var measurements = new List<object>();
            for (var i = 0; i < rows.Length; i++)
            {
                var row = rows[i]; var source = expected.GetProperty("rows")[i];
                var label = Descendants<Panel>(row).Single(slot => slot.Name == "DataListLabel");
                var value = Descendants<Panel>(row).Single(slot => slot.Name == "DataListValue");
                var description = Descendants<Panel>(row).Single(slot => slot.Name == "DataListDescription");
                CheckBox(row, source.GetProperty("box"), "row"); CheckBox(label, source.GetProperty("label").GetProperty("box"), "label");
                var valueBox = Box(value); var sourceValue = source.GetProperty("value").GetProperty("box");
                Near(valueBox.X, sourceValue.GetProperty("x").GetDouble(), "value x"); Near(valueBox.Y, sourceValue.GetProperty("y").GetDouble(), "value y"); Near(valueBox.Width, sourceValue.GetProperty("width").GetDouble(), "value width");
                Near(value.ActualHeight + (source.GetProperty("description").ValueKind != JsonValueKind.Null ? 4 + description.ActualHeight : 0), sourceValue.GetProperty("height").GetDouble(), "value total height");
                if (source.GetProperty("description").ValueKind != JsonValueKind.Null) CheckBox(description, source.GetProperty("description").GetProperty("box"), "description");
                CheckTypography(label, "label"); CheckTypography(value, "value"); CheckTypography(description, "description");
                var peer = FrameworkElementAutomationPeer.CreatePeerForElement(row);
                Require(peer.GetAutomationControlType() == AutomationControlType.ListItem && AutomationProperties.GetPositionInSet(row) == i + 1 && AutomationProperties.GetSizeOfSet(row) == rows.Length, "DataList lost row semantics.");
                measurements.Add(new { row = Box(row), label = Box(label), value = valueBox, description = Box(description), name = peer.GetName(),
                    labelTypography = Typography(label), valueTypography = Typography(value), descriptionTypography = Typography(description) });
                void CheckTypography(Panel slot, string key)
                {
                    var target = expectedText.GetProperty("rows")[i].GetProperty(key);
                    if (target.ValueKind == JsonValueKind.Null || target.GetProperty("firstBaseline").ValueKind == JsonValueKind.Null || target.GetProperty("rich").GetBoolean()) return;
                    var actual = Typography(slot);
                    var baseline = actual.baseline ?? throw new InvalidOperationException("DataList lost a scalar text baseline.");
                    Near(baseline, target.GetProperty("firstBaseline").GetDouble(), key + " baseline");
                    Near(actual.LineHeight, double.Parse(target.GetProperty("lineHeight").GetString()![..^2], System.Globalization.CultureInfo.InvariantCulture), key + " line height");
                }
            }
            var file = spec.GetProperty("file").GetString()!;
            if (rows.Length != 0) await MainWindow.CaptureElementAsync(Frame, Path.Combine(folder, file));
            else
            {
                var bitmap = new Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap(); await bitmap.RenderAsync(Frame);
                var buffer = await bitmap.GetPixelsAsync(); var pixels = new byte[checked((int)buffer.Length)];
                using (var reader = Windows.Storage.Streams.DataReader.FromBuffer(buffer)) reader.ReadBytes(pixels);
                await MainWindow.SavePngAsync(Path.Combine(folder, file), pixels, bitmap.PixelWidth, bitmap.PixelHeight);
            }
            cases.Add(new { id, viewportWidth = viewport, rows = measurements, errors }); failures.AddRange(errors.Select(error => id + ": " + error));
            await File.WriteAllTextAsync(Path.Combine(folder, $"data-list-source-{viewport}-{scheme}.json"), JsonSerializer.Serialize(new { cases, failures }, new JsonSerializerOptions { WriteIndented = true }));
            void Near(double actual, double target, string what) { if (Math.Abs(actual - target) > .05) errors.Add($"{what}: actual={actual:R}, source={target:R}"); }
            void CheckBox(FrameworkElement element, JsonElement target, string what)
            {
                var box = Box(element); Near(box.X, target.GetProperty("x").GetDouble(), what + " x"); Near(box.Y, target.GetProperty("y").GetDouble(), what + " y");
                Near(box.Width, target.GetProperty("width").GetDouble(), what + " width"); Near(box.Height, target.GetProperty("height").GetDouble(), what + " height");
            }
        }
        Require(failures.Count == 0, "DataList source geometry differs: " + string.Join("; ", failures.Take(12)));
        await VerifyLiveAsync(scheme, folder);
    }
    private async Task VerifyLiveAsync(BleeckerColorScheme scheme, string folder)
    {
        var button = Action("Consumer edit"); var first = new BleeckerDataListItem("a", "Title", button) { Description = "Draft" };
        List.Items = [first, new("b", "Count", 0)]; List.IsStriped = true; List.Width = 320; List.FlowDirection = FlowDirection.LeftToRight;
        await SettleAsync();
        using var input = new NativeInputSession(XamlRoot, folder, "data-list-" + scheme); await input.AcquireAsync(CasePicker);
        var count = actions; await input.ClickAsync(button); button.Focus(FocusState.Keyboard); await input.KeyAsync(0x20, button); await input.KeyAsync(0x0D, button);
        Require(actions == count + 3 && ReferenceEquals(Descendants<Button>(List).Single(), button), "DataList did not retain the consumer action through native input.");
        List.IsEnabled = false; await SettleAsync();
        Require(!button.IsEnabled && !FrameworkElementAutomationPeer.CreatePeerForElement(button).IsEnabled(), "Disabled DataList content remained enabled.");
        List.IsEnabled = true; await SettleAsync();
        Require(button.IsEnabled, "DataList did not restore its consumer action's inherited enabled state.");
        var row = Descendants<Panel>(List).First(p => p.Name == "DataListRow");
        first.Label = "Updated title"; first.Description = "Updated summary"; List.Items.Move(0, 1); await SettleAsync();
        Require(ReferenceEquals(Descendants<Panel>(List).Last(p => p.Name == "DataListRow"), row) && ReferenceEquals(Descendants<Button>(List).Single(), button), "DataList lost keyed content after live updates/reordering.");
        var template = List.Template; List.Template = null; await SettleAsync(); List.Template = template; List.ApplyTemplate(); await SettleAsync();
        Require(ReferenceEquals(Descendants<Button>(List).Single(), button), "DataList template replacement lost consumer content.");
        Frame.Children.Remove(List); await MainWindow.AwaitPresentationAsync(Frame); Require(!List.IsLoaded, "DataList did not unload.");
        first.Description = "Changed while detached"; Frame.Children.Add(List); await SettleAsync();
        Require(List.IsLoaded && ReferenceEquals(Descendants<Button>(List).Single(), button), "DataList did not remount consumer content.");
        List.Items.Remove(first); await SettleAsync(); Require(!Descendants<Button>(List).Any(), "DataList retained a removed consumer action.");
        List.Items.Clear();
        var dataTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='{Binding Value, Mode=OneWay}' /></DataTemplate>");
        List.LabelTemplate = List.ValueTemplate = List.DescriptionTemplate = dataTemplate;
        var labelModel = new BleeckerDataListItem("label", null, "Template term");
        var valueModel = new BleeckerDataListItem("value", null, "Template value");
        var descriptionModel = new BleeckerDataListItem("description", null, "Template summary");
        List.Items.Add(new("template", labelModel, valueModel) { Description = descriptionModel }); await SettleAsync();
        foreach (var expected in new[] { "Template term", "Template value", "Template summary" })
            Require(Descendants<TextBlock>(List).Any(text => text.Text == expected), "DataList template lost its original consumer data context.");
        valueModel.Value = "Live template value"; await SettleAsync();
        Require(Descendants<TextBlock>(List).Any(text => text.Text == "Live template value"), "DataList template binding did not update.");
        List.Items.Clear(); List.LabelTemplate = List.ValueTemplate = List.DescriptionTemplate = null;
        List.Items.Add(new("responsive", "Term", "Value")); await SettleAsync();
        await VerifyBreakpointAsync();
        await File.WriteAllTextAsync(Path.Combine(folder, $"data-list-input-{scheme}.json"), JsonSerializer.Serialize(new { passed = true, physicalActivations = 3, originalContent = true, liveUpdates = true, keyedReorder = true, templateReplacement = true, unloadRemount = true, removal = true, inheritedDisabled = true, contentTemplates = 3, liveTemplateBinding = true, liveBreakpoint = true }));
    }
    private async Task VerifyBreakpointAsync()
    {
        var window = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(XamlRoot.ContentIslandEnvironment.AppWindowId);
        var presenter = window.Presenter as Microsoft.UI.Windowing.OverlappedPresenter ?? throw new InvalidOperationException("DataList viewport verification requires an overlapped window.");
        var originalSize = window.ClientSize; var originalPosition = window.Position; var originalState = presenter.State;
        var row = Descendants<Panel>(List).Single(p => p.Name == "DataListRow");
        try
        {
            presenter.Restore(false);
            foreach (var width in new[] { 639, 640, 639 })
            {
                window.ResizeClient(new Windows.Graphics.SizeInt32(width, 1000)); await SettleAsync();
                Require(XamlRoot.Size.Width == width && ReferenceEquals(Descendants<Panel>(List).Single(p => p.Name == "DataListRow"), row), "DataList viewport change remounted its keyed row.");
                var label = Box(Descendants<Panel>(row).Single(p => p.Name == "DataListLabel"));
                var value = Box(Descendants<Panel>(row).Single(p => p.Name == "DataListValue"));
                Require(width < 640 ? value.Y > label.Y && value.X == label.X : value.Y == label.Y && value.X > label.X,
                    "DataList did not update layout at the real 640-DIP viewport boundary.");
            }
        }
        finally
        {
            if (originalState == Microsoft.UI.Windowing.OverlappedPresenterState.Maximized) presenter.Maximize();
            else { window.ResizeClient(originalSize); window.Move(originalPosition); }
            await SettleAsync();
        }
        Require(window.ClientSize == originalSize, "DataList viewport test did not restore the original client size.");
    }
    private Rect Box(FrameworkElement element)
    {
        var bounds = element.TransformToVisual(Frame).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
        var root = List.TransformToVisual(Frame).TransformBounds(new Rect(0, 0, List.ActualWidth, List.ActualHeight));
        return new Rect(bounds.X - root.X, bounds.Y - root.Y, bounds.Width, bounds.Height);
    }
    private sealed record TextTypography(string text, double? baseline, double LineHeight);
    private TextTypography Typography(Panel slot)
    {
        var text = Descendants<TextBlock>(slot).FirstOrDefault(text => text.Visibility == Visibility.Visible && text.Text.Length != 0);
        return text is null ? new("", null, 0) : new(text.Text, Box(text).Y + text.BaselineOffset, text.LineHeight);
    }
    private async Task SettleAsync() { UpdateLayout(); await MainWindow.AwaitPresentationAsync(List); UpdateLayout(); await MainWindow.AwaitPresentationAsync(List); }
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        { var child = VisualTreeHelper.GetChild(root, i); if (child is T match) yield return match; foreach (var descendant in Descendants<T>(child)) yield return descendant; }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
