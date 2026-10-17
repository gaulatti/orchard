using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using System.Text.Json;

namespace Orchard.Catalog;

public sealed partial class MetricsFixture : UserControl
{
    private int detailsActions;
    public MetricsFixture() { InitializeComponent(); Details.Click += (_, _) => detailsActions++; }

    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        await MainWindow.AwaitPresentationAsync(this);
        using var stream = typeof(MetricsFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.Metric.json")!;
        using var reference = JsonDocument.Parse(stream);
        var receipts = new List<object>();
        foreach (var item in reference.RootElement.GetProperty("cases").EnumerateArray().Where(item => item.GetProperty("scheme").GetString() == scheme.ToString()))
        {
            var value = item.GetProperty("value");
            CaptureMetric.Language = item.GetProperty("locale").GetString()!;
            CaptureMetric.Value = value.ValueKind == JsonValueKind.Null ? null : value.ValueKind == JsonValueKind.Number ? value.GetDouble() : value.GetString() switch
            { "NaN" => double.NaN, "Infinity" => double.PositiveInfinity, "-Infinity" => double.NegativeInfinity, "-0" => -0d, _ => throw new InvalidDataException() };
            CaptureMetric.Format = item.TryGetProperty("format", out var f) ? Enum.Parse<BleeckerMetricFormat>(f.GetString()!, true) : BleeckerMetricFormat.Number;
            CaptureMetric.Currency = item.TryGetProperty("currency", out var c) ? c.GetString()! : "USD";
            CaptureMetric.Decimals = item.TryGetProperty("decimals", out var d) ? d.GetInt32() : null;
            CaptureMetric.Prefix = item.TryGetProperty("prefix", out var p) ? p.GetString() : null;
            CaptureMetric.Suffix = item.TryGetProperty("suffix", out var s) ? s.GetString() : null;
            CaptureMetric.UpdateLayout();
            var text = MetricText(CaptureMetric);
            var rendered = string.Concat(((Paragraph)text.Blocks[0]).Inlines.OfType<Run>().Select(run => run.Text));
            var expected = item.GetProperty("state").GetProperty("text").GetString();
            Require(rendered == expected, $"Metric rendered text differs: {item.GetProperty("id").GetString()}/{CaptureMetric.Language}.");
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(CaptureMetric);
            Require(peer.GetAutomationControlType() == AutomationControlType.Text && peer.GetName() == expected && !CaptureMetric.IsTabStop, "Metric accessibility value/role/focus differs.");
            Require(text.CharacterSpacing == -20 && Typography.GetNumeralAlignment(text) == FontNumeralAlignment.Tabular && text.FontFamily.Source.Contains("EncodeSans-Regular.ttf") && text.LineHeight == 24, "Metric source typography differs.");
            var color = ((SolidColorBrush)text.Foreground).Color;
            Require($"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}" == BleeckerTokens.Colors(scheme)["textPrimary"], "Metric theme foreground differs.");
            string? file = null;
            if (item.TryGetProperty("file", out var image))
            {
                file = image.GetString();
                CaptureHost.StartBringIntoView(new BringIntoViewOptions { AnimationDesired = false });
                await MainWindow.AwaitPresentationAsync(CaptureHost);
                await CaptureFrameAsync(Path.Combine(folder, file!));
            }
            receipts.Add(new { id = item.GetProperty("id").GetString(), locale = CaptureMetric.Language, rendered, expected, automationName = peer.GetName(), file });
        }
        // Same-instance updates, explicit naming and genuine template replacement.
        var specimenPeer = FrameworkElementAutomationPeer.CreatePeerForElement(Specimen);
        Specimen.Value = 2480;
        Require(specimenPeer.GetName() == "~ 2,480 reviews", "Metric did not update its retained automation peer.");
        AutomationProperties.SetName(Specimen, "Consumer override");
        Require(specimenPeer.GetName() == "Consumer override", "Metric replaced the consumer automation name.");
        Specimen.ClearValue(AutomationProperties.NameProperty); Specimen.Value = 1248;
        var template = Specimen.Template; Specimen.Template = null; Specimen.ApplyTemplate(); Specimen.Template = template; Specimen.ApplyTemplate();
        Require(specimenPeer.GetName() == "~ 1,248 reviews", "Metric template replacement lost state.");
        var prefix = Rich.Prefix; Rich.Prefix = "Local "; Rich.Prefix = prefix;
        Require(((TextBlock)prefix!).Text == "Reviewed: " && SlotHosts(Rich).Any(host => ReferenceEquals(host.Content, prefix)), "Metric replaced consumer content.");
        Require(!Rich.IsTabStop && Details.IsTabStop, "Metric altered nested action focusability.");
        var richTemplate = Rich.Template; Rich.Template = null; Rich.ApplyTemplate(); Rich.Template = richTemplate; Rich.ApplyTemplate();
        Require(SlotHosts(Rich).Any(host => ReferenceEquals(host.Content, prefix)), "Metric template replacement retained the old slot parent.");
        var model = new NumberBox { Value = 42 };
        Specimen.SetBinding(BleeckerMetric.ValueProperty, new Microsoft.UI.Xaml.Data.Binding { Source = model, Path = new PropertyPath(nameof(NumberBox.Value)), Mode = Microsoft.UI.Xaml.Data.BindingMode.OneWay });
        await MainWindow.AwaitPresentationAsync(Specimen);
        Require(Specimen.FormattedValue == "42", $"Metric did not consume the initial binding: value={Specimen.Value}, source={model.Value}.");
        model.Value = 84;
        await MainWindow.AwaitPresentationAsync(Specimen);
        Require(specimenPeer.GetName() == "~ 84 reviews", "Metric did not consume a live data binding.");
        var numeric = new Border { Tag = 42 };
        Specimen.SetBinding(BleeckerMetric.ValueProperty, new Microsoft.UI.Xaml.Data.Binding { Source = numeric, Path = new PropertyPath(nameof(Border.Tag)), Mode = Microsoft.UI.Xaml.Data.BindingMode.OneWay });
        await MainWindow.AwaitPresentationAsync(Specimen); Require(Specimen.FormattedValue == "42", "Metric did not convert an integer binding.");
        numeric.Tag = null; await MainWindow.AwaitPresentationAsync(Specimen); Require(Specimen.Value is null, "Metric lost a bound null numeric value.");
        numeric.Tag = 84d; await MainWindow.AwaitPresentationAsync(Specimen);
        var precision = new Border { Tag = 2 };
        Specimen.SetBinding(BleeckerMetric.DecimalsProperty, new Microsoft.UI.Xaml.Data.Binding { Source = precision, Path = new PropertyPath(nameof(Border.Tag)), Mode = Microsoft.UI.Xaml.Data.BindingMode.OneWay });
        await MainWindow.AwaitPresentationAsync(Specimen);
        Require(Specimen.FormattedValue == "84.00", "Metric did not consume bound decimal precision.");
        precision.Tag = null; await MainWindow.AwaitPresentationAsync(Specimen);
        Require(Specimen.Decimals is null && Specimen.FormattedValue == "84", "Metric did not restore source precision from a bound null.");
        Specimen.ClearValue(BleeckerMetric.DecimalsProperty);
        Specimen.ClearValue(BleeckerMetric.ValueProperty); Specimen.Value = 1248;
        Rich.IsEnabled = false; Require(!Details.IsEnabled, "Disabled metric left its nested action enabled."); Rich.IsEnabled = true;
        using (var input = new NativeInputSession(XamlRoot, folder, $"metric-slots-{scheme}"))
        {
            var focusEvents = new List<object>();
            void OnLosingFocus(UIElement sender, Microsoft.UI.Xaml.Input.LosingFocusEventArgs args) => focusEvents.Add(new { direction = args.Direction.ToString(), input = args.InputDevice.ToString(), state = args.FocusState.ToString(), oldName = (args.OldFocusedElement as FrameworkElement)?.Name, newName = (args.NewFocusedElement as FrameworkElement)?.Name, loaded = Details.IsLoaded });
            Details.LosingFocus += OnLosingFocus;
            await input.AcquireAsync(Details);
            Details.StartBringIntoView(new BringIntoViewOptions { AnimationDesired = false });
            await MainWindow.AwaitPresentationAsync(Details);
            Details.Focus(FocusState.Keyboard); await MainWindow.AwaitPresentationAsync(Details);
            await File.WriteAllTextAsync(Path.Combine(folder, $"metric-focus-precondition-{scheme}.json"), JsonSerializer.Serialize(new { stableFocus = ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), Details), focusEvents }));
            Require(ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), Details), "Metric did not retain focus after viewport presentation.");
            var before = detailsActions; await input.KeyAsync(0x20, Details);
            await MainWindow.AwaitPresentationAsync(Details);
            Require(detailsActions == before + 1, "Metric swallowed the nested action's real Space key.");
            var focusAfterSpace = ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), Details);
            Require(Details.Focus(FocusState.Keyboard) && ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), Details), "Metric focus-update precondition failed.");
            var lifecycle = new List<string>();
            RoutedEventHandler got = (_, _) => lifecycle.Add("got-focus"), lost = (_, _) => lifecycle.Add("lost-focus"), loaded = (_, _) => lifecycle.Add("loaded"), unloaded = (_, _) => lifecycle.Add("unloaded");
            Details.GotFocus += got; Details.LostFocus += lost; Details.Loaded += loaded; Details.Unloaded += unloaded;
            var originalInlines = ((Paragraph)MetricText(Rich).Blocks[0]).Inlines.ToArray();
            var originalHosts = SlotHosts(Rich).ToArray();
            Rich.Value = 13;
            var immediateFocus = (Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot) as FrameworkElement)?.Name;
            var immediateLoaded = Details.IsLoaded;
            await MainWindow.AwaitPresentationAsync(Details);
            var currentInlines = ((Paragraph)MetricText(Rich).Blocks[0]).Inlines.ToArray();
            var focusRetained = ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), Details);
            var prefixRetained = ReferenceEquals(originalInlines[0], currentInlines[0]); var suffixRetained = ReferenceEquals(originalInlines[2], currentInlines[2]);
            await File.WriteAllTextAsync(Path.Combine(folder, $"metric-focused-slots-{scheme}.json"), JsonSerializer.Serialize(new { focusAfterSpace, focusRetained, prefixRetained, suffixRetained, immediateFocus, immediateLoaded, loaded = Details.IsLoaded, focusEvents, lifecycle, focusedName = (Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot) as FrameworkElement)?.Name }));
            Require(prefixRetained && suffixRetained && focusRetained, $"Metric value update: prefixRetained={prefixRetained}, suffixRetained={suffixRetained}, focusRetained={focusRetained}.");
            Require(immediateLoaded && lifecycle.Count == 0 && SlotHosts(Rich).SequenceEqual(originalHosts), "Metric value updates unmounted a consumer or emitted focus/lifecycle events.");
            for (var value = 14; value <= 40; value++) Rich.Value = value;
            Rich.Decimals = 2; Rich.Format = BleeckerMetricFormat.Currency; Rich.Currency = "EUR"; Rich.Language = "de-DE";
            await MainWindow.AwaitPresentationAsync(Details);
            Require(ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), Details) && lifecycle.Count == 0, "Metric batched formatting updates disturbed consumer focus.");
            Rich.Prefix = "Before "; await MainWindow.AwaitPresentationAsync(Details);
            Rich.Prefix = "Updated "; await MainWindow.AwaitPresentationAsync(Details);
            Rich.Prefix = prefix; await MainWindow.AwaitPresentationAsync(Details);
            Require(ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), Details) && lifecycle.Count == 0, "Metric prefix updates disturbed the unchanged suffix control.");
            var slotGeometry = new List<object>();
            foreach (var direction in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
            foreach (var width in new[] { 140d, 320d })
            {
                Rich.FlowDirection = direction; Rich.Width = width; Rich.Prefix = "Consumer-owned words wrap before ";
                await MainWindow.AwaitPresentationAsync(Details);
                slotGeometry.Add(VerifySlotPlacement(Rich, direction, width));
                Require(ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), Details) && lifecycle.Count == 0, "Metric constrained/RTL layout disturbed the consumer control.");
            }
            Rich.ClearValue(WidthProperty); Rich.FlowDirection = FlowDirection.LeftToRight; Rich.Prefix = prefix;
            await MainWindow.AwaitPresentationAsync(Details);
            await File.WriteAllTextAsync(Path.Combine(folder, $"metric-rich-lifecycle-{scheme}.json"), JsonSerializer.Serialize(new { passed = true, updateEvents = lifecycle.ToArray(), focusEvents = focusEvents.ToArray(), batchedValues = 27, formattingUpdates = true, prefixUpdates = true, slotGeometry }, new JsonSerializerOptions { WriteIndented = true }));
            Details.GotFocus -= got; Details.LostFocus -= lost; Details.Loaded -= loaded; Details.Unloaded -= unloaded;
            Require(OutsideMetricFocus.Focus(FocusState.Keyboard), "Outside metric focus precondition failed.");
            Rich.Value = 41; await MainWindow.AwaitPresentationAsync(OutsideMetricFocus);
            Require(ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), OutsideMetricFocus), "Metric update stole deliberate outside focus.");
            Rich.Decimals = null; Rich.Format = BleeckerMetricFormat.Number; Rich.Language = "en-US";
            Rich.Value = 12;
            before = detailsActions; await input.ClickAsync(Details);
            await MainWindow.AwaitPresentationAsync(Details);
            Require(detailsActions == before + 1, "Metric swallowed the nested action's real pointer input.");
            Details.LosingFocus -= OnLosingFocus;
        }
        var panel = (StackPanel)Content; var position = panel.Children.IndexOf(Rich);
        panel.Children.RemoveAt(position); await MainWindow.AwaitPresentationAsync(this); Require(!Rich.IsLoaded, "Metric did not unload.");
        panel.Children.Insert(position, Rich); await MainWindow.AwaitPresentationAsync(Rich);
        Require(Rich.IsLoaded && ReferenceEquals(Rich.Prefix, prefix) && Rich.FormattedValue == "12", "Metric remount lost consumer content.");
        await File.WriteAllTextAsync(Path.Combine(folder, $"metric-{scheme}.json"), JsonSerializer.Serialize(new { passed = true, scheme = scheme.ToString(), cases = receipts, liveUpdates = true, templateReplacement = true, retainedRichContent = true, bindingUpdates = true, nativeSlotInput = true, focusedSlotRetained = true, stableConsumerLifecycle = true, remount = true }, new JsonSerializerOptions { WriteIndented = true }));
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    private static RichTextBlock MetricText(BleeckerMetric metric) => ((Grid)VisualTreeHelper.GetChild(metric, 0)).Children.OfType<RichTextBlock>().Single();
    private static IEnumerable<ContentPresenter> SlotHosts(BleeckerMetric metric) => ((Grid)VisualTreeHelper.GetChild(metric, 0)).Children.OfType<Canvas>().Single().Children.OfType<ContentPresenter>();

    private static object VerifySlotPlacement(BleeckerMetric metric, FlowDirection direction, double width)
    {
        var spacer = ((InlineUIContainer)((Paragraph)MetricText(metric).Blocks[0]).Inlines[2]).Child;
        var host = SlotHosts(metric).Single();
        var expected = spacer.TransformToVisual(metric).TransformBounds(new Windows.Foundation.Rect(0, 0, spacer.ActualSize.X, spacer.ActualSize.Y));
        var actual = host.TransformToVisual(metric).TransformBounds(new Windows.Foundation.Rect(0, 0, host.ActualWidth, host.ActualHeight));
        var error = new[] { Math.Abs(expected.X - actual.X), Math.Abs(expected.Y - actual.Y), Math.Abs(expected.Width - actual.Width), Math.Abs(expected.Height - actual.Height) }.Max();
        Require(error <= .05 && host.Opacity == 1 && host.IsHitTestVisible && host.IsLoaded && host.ActualWidth > 0 && host.ActualHeight > 0, $"Metric slot placement differs: {direction}/{width}, error={error}.");
        return new { direction = direction.ToString(), width, expected, actual, error };
    }

    private async Task CaptureFrameAsync(string path)
    {
        var bitmap = new Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap();
        await bitmap.RenderAsync(CaptureHost);
        Require(bitmap.PixelWidth == 320 && bitmap.PixelHeight == 120, "Metric capture changed size.");
        var buffer = await bitmap.GetPixelsAsync();
        var bytes = new byte[checked((int)buffer.Length)];
        using (var reader = Windows.Storage.Streams.DataReader.FromBuffer(buffer)) reader.ReadBytes(bytes);
        Require(bytes.Length == 320 * 120 * 4 && Enumerable.Range(0, 320 * 120).All(index => bytes[index * 4 + 3] == 255), "Metric capture is not an opaque complete frame.");
        // Square bracket glyphs can legitimately use fewer than nine grayscale
        // levels. Require actual foreground pixels against this exact frame's
        // background, instead of the catalog's multi-component color-count gate.
        Require(Enumerable.Range(1, 320 * 120 - 1).Any(index => bytes[index * 4] != bytes[0] || bytes[index * 4 + 1] != bytes[1] || bytes[index * 4 + 2] != bytes[2]), "Metric text did not paint.");
        await MainWindow.SavePngAsync(path, bytes, bitmap.PixelWidth, bitmap.PixelHeight);
    }

}
