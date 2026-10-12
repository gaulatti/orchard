using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class LabelsFixture : UserControl
{
    public LabelsFixture() => InitializeComponent();

    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        StartBringIntoView(new BringIntoViewOptions { AnimationDesired = false });
        await Task.Delay(150);
        UpdateLayout();
        var root = (Grid)VisualTreeHelper.GetChild(EmailLabel, 0);
        var text = (TextBlock)root.FindName("PART_Text");
        Require(text.FontFamily.Source.Contains("EncodeSans-Medium.ttf") && text.FontSize == 13 && text.FontWeight.Weight == 500 &&
            text.CharacterSpacing == 0 && text.LineHeight == 13 && EmailLabel.ActualHeight == 13, $"Label typography or line box differs: height={EmailLabel.ActualHeight}.");
        var color = ((SolidColorBrush)text.Foreground).Color;
        Require($"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}" == BleeckerTokens.Colors(scheme)["textPrimary"], "Label uses the wrong theme color.");
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(EmailLabel);
        Require(peer.GetName() == "Email Address" && peer.GetAutomationControlType() == AutomationControlType.Text && !EmailLabel.IsTabStop,
            "Label lost its name, text role or non-tabbable contract.");
        Require(ReferenceEquals(AutomationProperties.GetLabeledBy(EmailInput), EmailLabel), "Label did not associate with its input.");
        EmailLabel.Content = "Updated email";
        Require(peer.GetName() == "Updated email", "Label automation name did not follow consumer content.");
        EmailLabel.Content = "Email Address";
        var disabledRoot = (Grid)VisualTreeHelper.GetChild(DisabledLabel, 0);
        Require(Math.Abs(disabledRoot.Opacity - .6) < .000001 && LongLabel.ActualHeight > 13 && LongLabel.ActualWidth == 140,
            $"Disabled or wrapped label geometry differs: opacity={disabledRoot.Opacity}, width={LongLabel.ActualWidth}, height={LongLabel.ActualHeight}.");
        EmailLabel.Target = null;
        Require(!ReferenceEquals(AutomationProperties.GetLabeledBy(EmailInput), EmailLabel), "Detached label retained its input relationship.");
        EmailLabel.Target = EmailInput;
        AutomationProperties.SetName(EmailInput, "Consumer override");
        Require(FrameworkElementAutomationPeer.CreatePeerForElement(EmailInput).GetName() == "Consumer override", "Label replaced an explicit consumer accessibility name.");
        EmailInput.ClearValue(AutomationProperties.NameProperty);
        await VerifyContentAsync();
        await VerifyAssociationLifecycleAsync();
        await VerifyLiveAppearanceAsync(scheme);
        await MainWindow.CaptureElementAsync(this, System.IO.Path.Combine(folder, $"labels-{scheme}.png"));
    }

    private async Task VerifyLiveAppearanceAsync(BleeckerColorScheme scheme)
    {
        var host = (FrameworkElement)XamlRoot.Content;
        var original = host.RequestedTheme;
        var alternate = scheme == BleeckerColorScheme.Light ? BleeckerColorScheme.Dark : BleeckerColorScheme.Light;
        var width = EmailLabel.ActualWidth;
        var height = EmailLabel.ActualHeight;
        try
        {
            host.RequestedTheme = alternate == BleeckerColorScheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
            await AwaitAsync(() => HasPalette(alternate), "Labels retained the old palette after a live theme change.");
            Require(EmailLabel.ActualWidth == width && EmailLabel.ActualHeight == height && ReferenceEquals(AutomationProperties.GetLabeledBy(EmailInput), EmailLabel), "Theme change altered label geometry or association.");
        }
        finally { host.RequestedTheme = original; }
        await AwaitAsync(() => HasPalette(scheme), "Labels did not restore their original palette.");

        bool HasPalette(BleeckerColorScheme expected)
        {
            var colors = BleeckerTokens.Colors(expected);
            return Hex(EmailLabel.Foreground) == colors["textPrimary"] && Hex(ErrorLabel.Foreground) == colors[expected == BleeckerColorScheme.Light ? "terracotta" : "textPrimary"];
        }
        static string Hex(Brush brush)
        {
            var color = ((SolidColorBrush)brush).Color;
            return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    }

    private async Task VerifyContentAsync()
    {
        var template = (DataTemplate)Resources["LabelContentTemplate"];
        var root = (Grid)VisualTreeHelper.GetChild(TemplateLabel, 0);
        var plain = (TextBlock)root.FindName("PART_Text");
        var presenter = (ContentPresenter)root.FindName("PART_Content");
        await AwaitAsync(() => Descendants<TextBlock>(presenter).Any(text => text.Text == "Consumer template label"), "Label did not render consumer template content.");
        TemplateLabel.Content = "Updated template label";
        await AwaitAsync(() => Descendants<TextBlock>(presenter).Any(text => text.Text == "Updated template label"), "Label template did not receive a content update.");
        TemplateLabel.ContentTemplate = null;
        await AwaitAsync(() => plain.Visibility == Visibility.Visible && plain.Text == "Updated template label" && presenter.Visibility == Visibility.Collapsed, "Label did not restore its plain text presenter.");
        TemplateLabel.ContentTemplateSelector = new LabelTemplateSelector(template);
        await AwaitAsync(() => Descendants<TextBlock>(presenter).Any(text => text.Text == "Updated template label"), "Label ignored its consumer template selector.");
        TemplateLabel.ContentTemplateSelector = null;
        var richText = new TextBlock { Text = "Live rich content" };
        TemplateLabel.Content = richText;
        await AwaitAsync(() => ReferenceEquals(presenter.Content, richText) && richText.ActualHeight > 0 && plain.Visibility == Visibility.Collapsed, "Label did not render a consumer UIElement.");
        TemplateLabel.Content = "Consumer template label";
        TemplateLabel.ContentTemplate = template;
        await AwaitAsync(() => VisualTreeHelper.GetParent(richText) is null && Descendants<TextBlock>(presenter).Any(text => text.Text == "Consumer template label"), "Label retained obsolete rich content or failed to restore its template.");
    }

    private async Task VerifyAssociationLifecycleAsync()
    {
        var host = new StackPanel();
        var first = new TextBox();
        var second = new TextBox();
        var prior = new TextBlock { Text = "Prior label" };
        var consumer = new TextBlock { Text = "Consumer replacement" };
        var label = new BleeckerLabel { Content = "Temporary label", Target = first };
        AutomationProperties.SetLabeledBy(first, prior);
        host.Children.Add(prior);
        host.Children.Add(consumer);
        host.Children.Add(first);
        host.Children.Add(second);
        host.Children.Add(label);
        FixtureRoot.Children.Add(host);
        try
        {
            UpdateLayout();
            await AwaitAsync(() => label.IsLoaded && ReferenceEquals(AutomationProperties.GetLabeledBy(first), label), "Loaded label did not establish association.");
            label.Target = second;
            Require(ReferenceEquals(AutomationProperties.GetLabeledBy(first), prior) && ReferenceEquals(AutomationProperties.GetLabeledBy(second), label), "Changing label target did not restore and transfer association.");
            AutomationProperties.SetLabeledBy(second, consumer);
            label.Target = null;
            Require(ReferenceEquals(AutomationProperties.GetLabeledBy(second), consumer), "Detaching label overwrote a newer consumer association.");
            label.Target = first;
            host.Children.Remove(label);
            await AwaitAsync(() => !label.IsLoaded && ReferenceEquals(AutomationProperties.GetLabeledBy(first), prior), "Unloaded label retained association.");
            host.Children.Add(label);
            UpdateLayout();
            await AwaitAsync(() => label.IsLoaded && ReferenceEquals(AutomationProperties.GetLabeledBy(first), label), "Reloaded label did not restore association.");
            var inputPeer = FrameworkElementAutomationPeer.CreatePeerForElement(first);
            Require(inputPeer.GetLabeledBy()?.GetName() == "Temporary label", "Input automation peer did not expose its label relationship.");
        }
        finally { label.Target = null; FixtureRoot.Children.Remove(host); }
    }

    private sealed class LabelTemplateSelector(DataTemplate template) : DataTemplateSelector
    {
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) => template;
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match) yield return match;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }

    private async Task AwaitAsync(Func<bool> condition, string message)
    {
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        while (!condition() && elapsed.Elapsed < TimeSpan.FromSeconds(1)) { UpdateLayout(); await Task.Delay(16); }
        Require(condition(), message);
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
