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
            var text = (RichTextBlock)VisualTreeHelper.GetChild(CaptureMetric, 0);
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
        Require(((TextBlock)prefix!).Text == "Reviewed: " && ReferenceEquals(((InlineUIContainer)((Paragraph)((RichTextBlock)VisualTreeHelper.GetChild(Rich, 0)).Blocks[0]).Inlines[0]).Child, prefix), "Metric reparented or replaced consumer content.");
        Require(!Rich.IsTabStop && Details.IsTabStop, "Metric altered nested action focusability.");
        var richTemplate = Rich.Template; Rich.Template = null; Rich.ApplyTemplate(); Rich.Template = richTemplate; Rich.ApplyTemplate();
        Require(ReferenceEquals(((InlineUIContainer)((Paragraph)((RichTextBlock)VisualTreeHelper.GetChild(Rich, 0)).Blocks[0]).Inlines[0]).Child, prefix), "Metric template replacement retained the old inline parent.");
        var model = new NumberBox { Value = 42 };
        Specimen.SetBinding(BleeckerMetric.ValueProperty, new Microsoft.UI.Xaml.Data.Binding { Source = model, Path = new PropertyPath(nameof(NumberBox.Value)), Mode = Microsoft.UI.Xaml.Data.BindingMode.OneWay });
        await MainWindow.AwaitPresentationAsync(Specimen);
        Require(Specimen.FormattedValue == "42", $"Metric did not consume the initial binding: value={Specimen.Value}, source={model.Value}.");
        model.Value = 84;
        await MainWindow.AwaitPresentationAsync(Specimen);
        Require(specimenPeer.GetName() == "~ 84 reviews", "Metric did not consume a live data binding.");
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
            await input.AcquireAsync(Details); Details.Focus(FocusState.Keyboard);
            var before = detailsActions; await input.KeyAsync(0x20, Details);
            await MainWindow.AwaitPresentationAsync(Details);
            Require(detailsActions == before + 1, "Metric swallowed the nested action's real Space key.");
            before = detailsActions; await input.ClickAsync(Details);
            await MainWindow.AwaitPresentationAsync(Details);
            Require(detailsActions == before + 1, "Metric swallowed the nested action's real pointer input.");
        }
        var panel = (StackPanel)Content; var position = panel.Children.IndexOf(Rich);
        panel.Children.RemoveAt(position); await MainWindow.AwaitPresentationAsync(this); Require(!Rich.IsLoaded, "Metric did not unload.");
        panel.Children.Insert(position, Rich); await MainWindow.AwaitPresentationAsync(Rich);
        Require(Rich.IsLoaded && ReferenceEquals(Rich.Prefix, prefix) && Rich.FormattedValue == "12", "Metric remount lost consumer content.");
        await File.WriteAllTextAsync(Path.Combine(folder, $"metric-{scheme}.json"), JsonSerializer.Serialize(new { passed = true, scheme = scheme.ToString(), cases = receipts, liveUpdates = true, templateReplacement = true, retainedRichContent = true, bindingUpdates = true, nativeSlotInput = true, remount = true }, new JsonSerializerOptions { WriteIndented = true }));
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

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
