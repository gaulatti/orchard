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
        var text = ((Grid)VisualTreeHelper.GetChild(PackageMetric, 0)).Children.OfType<RichTextBlock>().Single();
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
        var consumerButton = new Button { Content = "Consumer details" };
        PackageMetric.Suffix = consumerButton; Root.UpdateLayout();
        PackageMetric.StartBringIntoView(new BringIntoViewOptions { AnimationDesired = false });
        await AwaitAsync(() => consumerButton.IsLoaded, "Packaged metric did not mount the consumer control.");
        Require(consumerButton.Focus(FocusState.Keyboard), "Packaged metric consumer focus precondition failed.");
        var events = new List<string>();
        RoutedEventHandler got = (_, _) => events.Add("got"), lost = (_, _) => events.Add("lost"), unloaded = (_, _) => events.Add("unloaded");
        consumerButton.GotFocus += got; consumerButton.LostFocus += lost; consumerButton.Unloaded += unloaded;
        PackageMetric.Value = 2480; Root.UpdateLayout();
        await AwaitAsync(() => PackageMetric.FormattedValue == "2.5K", "Packaged rich metric did not update its numeric value.");
        Require(events.Count == 0 && consumerButton.IsLoaded && ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(Root.XamlRoot), consumerButton), "Packaged metric disturbed consumer focus/lifecycle during an update.");
        consumerButton.GotFocus -= got; consumerButton.LostFocus -= lost; consumerButton.Unloaded -= unloaded;
        PackageMetric.Suffix = " reviews"; PackageMetric.Value = 1234567;
    }
}
