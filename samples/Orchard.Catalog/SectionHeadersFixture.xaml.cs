using System.Diagnostics;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class SectionHeadersFixture : UserControl
{
    public SectionHeadersFixture() => InitializeComponent();

    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        Require(XamlRoot.RasterizationScale == 1 && new Windows.UI.ViewManagement.UISettings().TextScaleFactor == 1,
            "Frozen Section Header comparison requires display/text scale 1; scaling acceptance remains open.");
        using var input = new NativeInputSession(XamlRoot, folder, $"section-header-{scheme}");
        await input.AcquireAsync(Specimen);
        using var reference = JsonDocument.Parse(CatalogReferenceResources.Open(typeof(SectionHeadersFixture).Assembly, "Orchard.Catalog.Reference.SectionHeaderLayout.json")!);
        var root = CatalogFixtureRoot.From(XamlRoot);
        var host = new StackPanel { Margin = new Thickness(20), Background = root.Background, VerticalAlignment = VerticalAlignment.Top };
        Grid.SetRowSpan(host, root.RowDefinitions.Count); Grid.SetColumnSpan(host, root.ColumnDefinitions.Count);
        root.Children.Add(host);
        var reports = new List<object>();
        try
        {
            foreach (var item in reference.RootElement.GetProperty("cases").EnumerateArray().Where(item => item.GetProperty("scheme").GetString() == scheme.ToString()))
            {
                var id = item.GetProperty("id").GetString()!; var props = item.GetProperty("props");
                var control = new BleeckerSectionHeader { Width = item.GetProperty("width").GetDouble(), HorizontalAlignment = HorizontalAlignment.Left,
                    Background = root.Background, Title = props.GetProperty("title").GetString(),
                    Eyebrow = props.TryGetProperty("eyebrow", out var e) ? e.GetString() : null,
                    Description = props.TryGetProperty("description", out var d) ? d.GetString() : null,
                    FlowDirection = item.TryGetProperty("rtl", out var rtl) && rtl.GetBoolean() ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
                host.Children.Clear(); host.Children.Add(control); await MountedAsync(control); control.ApplyTemplate(); UpdateLayout();
                var title = Part<TextBlock>(control, "PART_Title"); var description = Part<TextBlock>(control, "PART_Description"); var eyebrow = Part<BleeckerEyebrow>(control, "PART_Eyebrow");
                var paragraph = (FrameworkElement)VisualTreeHelper.GetParent(description);
                var metrics = new TextBlock { Text = "Mg", FontFamily = description.FontFamily, FontSize = description.FontSize, FontWeight = description.FontWeight,
                    IsHitTestVisible = false, Opacity = 0 };
                AutomationProperties.SetAccessibilityView(metrics, AccessibilityView.Raw);
                var probe = new Canvas { Height = 0, IsHitTestVisible = false }; probe.Children.Add(metrics); host.Children.Add(probe);
                await MountedAsync(metrics); UpdateLayout();
                var expected = item.GetProperty("layout");
                var actualHeight = control.ActualHeight; var expectedHeight = expected.GetProperty("header").GetProperty("height").GetDouble();
                reports.Add(new { id, width = control.ActualWidth, height = actualHeight, expectedHeight, titleHeight = title.ActualHeight,
                    descriptionWidth = description.ActualWidth, descriptionHeight = description.ActualHeight,
                    descriptionAlignment = description.HorizontalAlignment.ToString(), descriptionParent = VisualTreeHelper.GetParent(description).GetType().Name,
                    descriptionSlot = Microsoft.UI.Xaml.Controls.Primitives.LayoutInformation.GetLayoutSlot(description).Width,
                    descriptionY = description.TransformToVisual(control).TransformPoint(new Point()).Y,
                    paragraphY = paragraph.TransformToVisual(control).TransformPoint(new Point()).Y,
                    summaryBaselineY = description.TransformToVisual(control).TransformPoint(new Point()).Y + description.BaselineOffset,
                    titleBaseline = title.BaselineOffset, descriptionBaseline = description.BaselineOffset,
                    naturalSummaryHeight = metrics.ActualHeight, naturalSummaryBaseline = metrics.BaselineOffset,
                    eyebrowHeight = eyebrow.ActualHeight, titleY = title.TransformToVisual(control).TransformPoint(new Point()).Y });
                await File.WriteAllTextAsync(Path.Combine(folder, $"section-header-layout-{scheme}.json"), JsonSerializer.Serialize(reports, new JsonSerializerOptions { WriteIndented = true }));
                Require(Math.Abs(actualHeight - expectedHeight) < .001, $"Section Header {id} height differs: {actualHeight:R}/{expectedHeight:R}.");
                Require(title.FontSize == 28 && title.LineHeight == 35 && title.CharacterSpacing == -20 && title.FontFamily.Source.Contains("EncodeSans-SemiBold.ttf") &&
                    AutomationProperties.GetHeadingLevel(title) == AutomationHeadingLevel.Level2, "Section Header lost its title typography or heading level.");
                Require(description.FontSize == 16 && description.LineHeight == 28 && description.MaxWidth == 672 && description.FontFamily.Source.Contains("LibreFranklin-Regular.ttf"), "Section Header summary typography differs.");
                Require(!control.IsTabStop && !FrameworkElementAutomationPeer.CreatePeerForElement(control).IsKeyboardFocusable(), "Section Header became a keyboard stop.");
                Require(!PeerNames(FrameworkElementAutomationPeer.CreatePeerForElement(control)).Contains("Mg"), "Section Header exposed its private font probe.");
                if (!string.IsNullOrEmpty(control.Title))
                {
                    var peer = FrameworkElementAutomationPeer.CreatePeerForElement(title);
                    Require(peer.GetName() == control.Title && peer.GetHeadingLevel() == AutomationHeadingLevel.Level2, "Section Header title lost its real heading peer.");
                    var expectedY = expected.GetProperty("title").GetProperty("y").GetDouble() - expected.GetProperty("header").GetProperty("y").GetDouble();
                    Require(Math.Abs(title.TransformToVisual(control).TransformPoint(new Point()).Y - expectedY) < .001, "Section Header eyebrow/title gap differs.");
                }
                Require(Color(title.Foreground) == BleeckerTokens.Colors(scheme)["textPrimary"] && Color(description.Foreground) == BleeckerTokens.Colors(scheme)["textSecondary"], "Section Header palette differs.");
                if (!string.IsNullOrEmpty(control.Description))
                {
                    var expectedDescription = expected.GetProperty("description");
                    var expectedY = expectedDescription.GetProperty("y").GetDouble() - expected.GetProperty("header").GetProperty("y").GetDouble();
                    Require(Math.Abs(Microsoft.UI.Xaml.Controls.Primitives.LayoutInformation.GetLayoutSlot(description).Width - expectedDescription.GetProperty("width").GetDouble()) < .001 &&
                        Math.Abs(paragraph.TransformToVisual(control).TransformPoint(new Point()).Y - expectedY) < .001, "Section Header summary width/gap differs.");
                    var baseline = description.TransformToVisual(control).TransformPoint(new Point()).Y + description.BaselineOffset;
                    var expectedBaseline = expected.GetProperty("descriptionBaseline").GetDouble() - expected.GetProperty("header").GetProperty("y").GetDouble();
                    Require(Math.Abs(baseline - expectedBaseline) < .001, $"Section Header {id} summary baseline differs: {baseline:R}/{expectedBaseline:R}.");
                }
                if (actualHeight > 0) await MainWindow.CaptureElementAsync(control, Path.Combine(folder, $"section-header-{id}-{scheme}.png"));
                if (id == "full")
                {
                    var client = await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, $"section-header-composed-{scheme}.png"));
                    await File.WriteAllTextAsync(Path.Combine(folder, $"section-header-composed-{scheme}.json"), JsonSerializer.Serialize(new { client, bounds = input.ScreenBounds(control) }));
                    await VerifyInkRowsAsync(Path.Combine(folder, $"section-header-composed-{scheme}.png"), client, input.ScreenBounds(control), expected.GetProperty("descriptionInk"), scheme, folder);
                    await VerifyMutationAsync(control, host);
                }
            }
            host.Children.Clear();
            var before = new BleeckerButton { Content = "Before section", HorizontalAlignment = HorizontalAlignment.Left };
            var after = new BleeckerButton { Content = "After section", HorizontalAlignment = HorizontalAlignment.Left };
            host.Children.Add(before); host.Children.Add(new BleeckerSectionHeader { Title = "Keyboard section", Eyebrow = "Workspace", Description = "Passive section summary" }); host.Children.Add(after);
            UpdateLayout(); await MountedAsync(after);
            var beforeAction = Part<Button>(before, "PART_Action"); var afterAction = Part<Button>(after, "PART_Action");
            await input.AcquireAsync(before); Require(beforeAction.Focus(FocusState.Keyboard), "Could not focus preceding section action.");
            input.Key(0x09); await FocusedAsync(afterAction, "Section Header interrupted physical Tab traversal.");
            input.ShiftKey(0x09); await FocusedAsync(beforeAction, "Section Header interrupted reverse Tab traversal.");
            await File.WriteAllTextAsync(Path.Combine(folder, $"section-header-keyboard-{scheme}.json"), JsonSerializer.Serialize(new { forward = true, reverse = true, focused = Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot)?.GetType().Name }));
        }
        finally { root.Children.Remove(host); }
    }

    private static async Task VerifyInkRowsAsync(string path, Rect client, Rect bounds, JsonElement expected, BleeckerColorScheme scheme, string folder)
    {
        using var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
        using (var writer = new Windows.Storage.Streams.DataWriter(stream)) { writer.WriteBytes(await File.ReadAllBytesAsync(path)); await writer.StoreAsync(); writer.DetachStream(); }
        stream.Seek(0);
        var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream);
        var pixels = (await decoder.GetPixelDataAsync(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, Windows.Graphics.Imaging.BitmapAlphaMode.Straight,
            new Windows.Graphics.Imaging.BitmapTransform(), Windows.Graphics.Imaging.ExifOrientationMode.IgnoreExifOrientation, Windows.Graphics.Imaging.ColorManagementMode.DoNotColorManage)).DetachPixelData();
        var left = checked((int)(bounds.X - client.X)); var top = checked((int)(bounds.Y - client.Y));
        var backgroundOffset = checked((top * (int)decoder.PixelWidth + left) * 4);
        int? first = null, last = null;
        for (var y = expected.GetProperty("top").GetInt32(); y < expected.GetProperty("bottom").GetInt32(); y++)
            for (var x = expected.GetProperty("left").GetInt32(); x < expected.GetProperty("right").GetInt32(); x++)
            {
                var offset = checked(((top + y) * (int)decoder.PixelWidth + left + x) * 4);
                if (Enumerable.Range(0, 3).Any(channel => Math.Abs(pixels[offset + channel] - pixels[backgroundOffset + channel]) > expected.GetProperty("threshold").GetInt32()))
                { first = first is null ? y : Math.Min(first.Value, y); last = last is null ? y : Math.Max(last.Value, y); }
            }
        var passed = first == expected.GetProperty("first").GetInt32() && last == expected.GetProperty("last").GetInt32();
        await File.WriteAllTextAsync(Path.Combine(folder, $"section-header-ink-{scheme}.json"), JsonSerializer.Serialize(new { passed, first, last, expectedFirst = expected.GetProperty("first").GetInt32(), expectedLast = expected.GetProperty("last").GetInt32() }));
        Require(passed, "Composed Section Header summary glyph rows differ from the frozen browser.");
    }

    private async Task VerifyMutationAsync(BleeckerSectionHeader control, Panel host)
    {
        var source = new TextBlock { Text = "Consumer title" };
        control.SetBinding(BleeckerSectionHeader.TitleProperty, new Binding { Source = source, Path = new PropertyPath("Text") });
        source.Text = "Updated consumer title"; control.Description = ""; control.Eyebrow = ""; UpdateLayout();
        Require(Part<TextBlock>(control, "PART_Title").Text == source.Text && control.ActualHeight == 35 && control.GetBindingExpression(BleeckerSectionHeader.TitleProperty) is not null,
            "Section Header mutated a consumer binding or retained optional content gaps.");
        control.IsEnabled = false; UpdateLayout(); Require(control.Opacity == 1 && control.ActualHeight == 35, "Passive Section Header invented disabled opacity or layout."); control.IsEnabled = true;
        host.Children.Remove(control); host.Children.Add(control); await MountedAsync(control); source.Text = "Remounted consumer title"; UpdateLayout();
        Require(Part<TextBlock>(control, "PART_Title").Text == source.Text, "Section Header lost its binding on remount.");
        var template = control.Template;
        control.Template = (ControlTemplate)XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:orchard='using:Orchard' TargetType='orchard:BleeckerSectionHeader'><Border><TextBlock Text='{TemplateBinding Title}' /></Border></ControlTemplate>");
        control.ApplyTemplate(); UpdateLayout(); Require(((TextBlock)((Border)VisualTreeHelper.GetChild(control, 0)).Child).Text == source.Text, "Section Header rejected a consumer template.");
        control.Template = template; control.ApplyTemplate(); UpdateLayout(); source.Text = "Restored consumer title"; UpdateLayout();
        Require(Part<TextBlock>(control, "PART_Title").Text == source.Text && control.ActualHeight == 35, "Section Header failed to restore its template or binding.");
        control.Eyebrow = "Workspace"; control.Description = "Restored consumer summary"; UpdateLayout();
        var restored = Part<TextBlock>(control, "PART_Description");
        Require(control.ActualHeight == 105 && Math.Abs(restored.TransformToVisual(control).TransformPoint(new Point()).Y + restored.BaselineOffset - 96) < .001,
            "Section Header did not restore summary metrics after optional content and template replacement.");
    }
    private static IEnumerable<string> PeerNames(AutomationPeer peer)
    {
        yield return peer.GetName();
        foreach (var child in peer.GetChildren() ?? []) foreach (var name in PeerNames(child)) yield return name;
    }
    private static string Color(Brush brush) { var c = ((SolidColorBrush)brush).Color; return $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}"; }
    private static T Part<T>(DependencyObject owner, string name) where T : FrameworkElement
    {
        if (owner is T match && match.Name == name) return match;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(owner); i++) { try { return Part<T>(VisualTreeHelper.GetChild(owner, i), name); } catch (InvalidOperationException) { } }
        throw new InvalidOperationException($"Missing Section Header part {name}.");
    }
    private static async Task MountedAsync(FrameworkElement element)
    {
        var elapsed = Stopwatch.StartNew(); while (!element.IsLoaded && elapsed.Elapsed < TimeSpan.FromSeconds(2)) await Task.Delay(16);
        Require(element.IsLoaded, "Section Header specimen did not mount.");
        var frame = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Presented(object? sender, object args) => frame.TrySetResult();
        CompositionTarget.Rendering += Presented;
        try { await frame.Task.WaitAsync(TimeSpan.FromSeconds(2)); } finally { CompositionTarget.Rendering -= Presented; }
        element.UpdateLayout();
    }
    private async Task FocusedAsync(FrameworkElement target, string message)
    {
        var elapsed = Stopwatch.StartNew();
        while (!ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), target) && elapsed.Elapsed < TimeSpan.FromSeconds(2)) await Task.Delay(16);
        Require(ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), target), message);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
