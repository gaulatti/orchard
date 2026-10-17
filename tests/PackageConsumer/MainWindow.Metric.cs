using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

namespace PackageConsumer;

public sealed partial class MainWindow
{
    private async Task VerifyMetricAsync()
    {
        PackageMetric.Language = "en-US"; PackageMetric.Value = 1234567;
        PackageMetric.ApplyTemplate(); Root.UpdateLayout();
        var text = (RichTextBlock)VisualTreeHelper.GetChild(PackageMetric, 0);
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(PackageMetric);
        Require(PackageMetric.FormattedValue == "1.2M" && peer.GetName() == "1.2M reviews" && peer.GetAutomationControlType() == AutomationControlType.Text && !PackageMetric.IsTabStop, "Packaged metric lost compact format, source slots or text accessibility.");
        Require(text.CharacterSpacing == -20 && Typography.GetNumeralAlignment(text) == FontNumeralAlignment.Tabular && text.FontFamily.Source.Contains("EncodeSans-Regular.ttf"), "Packaged metric lost bundled type or tabular digits.");
        PackageMetric.Format = Orchard.BleeckerMetricFormat.Currency; PackageMetric.Currency = "JPY"; PackageMetric.Value = 1234.56;
        Require(PackageMetric.FormattedValue == "¥1,235", "Packaged metric lost currency minor units.");
        PackageMetric.Language = "de-DE"; PackageMetric.Currency = "EUR";
        Require(PackageMetric.FormattedValue == "1.234,56 €", "Packaged metric lost consumer language updates.");
        PackageMetric.Value = null;
        Require(peer.GetName() == " reviews", "Packaged metric invented a missing value.");
        PackageMetric.Format = Orchard.BleeckerMetricFormat.Number; PackageMetric.Language = "en-US";
        var source = new NumberBox { Value = 42 };
        PackageMetric.SetBinding(Orchard.BleeckerMetric.ValueProperty, new Microsoft.UI.Xaml.Data.Binding { Source = source, Path = new PropertyPath(nameof(NumberBox.Value)), Mode = Microsoft.UI.Xaml.Data.BindingMode.OneWay });
        await AwaitAsync(() => PackageMetric.FormattedValue == "42", "Packaged metric lost its nullable data-binding boundary.");
        source.Value = 84;
        await AwaitAsync(() => peer.GetName() == "84 reviews", "Packaged metric lost a live bound value.");
        PackageMetric.ClearValue(Orchard.BleeckerMetric.ValueProperty);
        PackageMetric.Language = "en-US"; PackageMetric.Format = Orchard.BleeckerMetricFormat.Compact; PackageMetric.Value = 1234567;
    }
}
