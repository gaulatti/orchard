using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Orchard;

namespace PackageConsumer;

public sealed partial class MainWindow
{
    private void VerifyPackageTabs()
    {
        var buttons = TabDescendants(PackageTabs).OfType<Button>().ToArray();
        Require(buttons.Length == 3 && buttons.All(button => button.FontFamily.Source.Contains("EncodeSans-Medium.ttf")), "Packaged tabs lost their template or font asset.");
        var ownerPeer = FrameworkElementAutomationPeer.CreatePeerForElement(PackageTabs);
        var selection = (ISelectionProvider)ownerPeer.GetPattern(PatternInterface.Selection);
        Require(ownerPeer.GetAutomationControlType() == AutomationControlType.Tab && selection.GetSelection().Length == 1, "Packaged tabs lost native selection semantics.");
        var activity = (ISelectionItemProvider)FrameworkElementAutomationPeer.CreatePeerForElement(buttons[1]).GetPattern(PatternInterface.SelectionItem);
        var accept = false; var requests = new List<string>();
        void Requested(object? sender, BleeckerTabRequestedEventArgs args) { requests.Add(args.Id); if (accept) PackageTabs.ActiveTab = args.Id; }
        PackageTabs.ActiveTabRequested += Requested;
        try
        {
            activity.Select();
            Require(PackageTabs.ActiveTab == "overview" && !activity.IsSelected && requests.Count == 1, "Packaged tabs accepted a declined consumer request.");
            accept = true; activity.Select();
            Require(PackageTabs.ActiveTab == "activity" && activity.IsSelected && requests.Count == 2, "Packaged tabs did not propagate an accepted request.");
            foreach (var variant in Enum.GetValues<BleeckerTabsVariant>()) foreach (var size in Enum.GetValues<BleeckerControlSize>())
            {
                PackageTabs.Variant = variant; PackageTabs.Size = size; Root.UpdateLayout();
                var widths = buttons.Select(button => button.ActualWidth).ToArray();
                var inset = variant == BleeckerTabsVariant.Underline ? 0 : variant == BleeckerTabsVariant.Segmented ? 8 : 10;
                Require(widths.Max() - widths.Min() <= 1d / 64 && Math.Abs(widths.Sum() + 8 + inset - PackageTabs.ActualWidth) < .001, "A packaged stretch variant did not fill its tracks.");
            }
        }
        finally { PackageTabs.ActiveTabRequested -= Requested; PackageTabs.ActiveTab = "overview"; PackageTabs.Variant = BleeckerTabsVariant.Underline; PackageTabs.Size = BleeckerControlSize.Medium; }
        var custom = TabDescendants(PackageTabLinks).OfType<Button>().First();
        var context = (BleeckerTabLinkContext)custom.DataContext;
        BleeckerTabItem? destination = null;
        void Navigation(object? sender, BleeckerTabNavigationEventArgs args) => destination = args.Item;
        PackageTabLinks.NavigationRequested += Navigation;
        try
        {
            context.Command.Execute(null);
            Require(destination?.Href == "../consumer/tab?keep=%2F#part" && !destination.External && PackageTabLinks.ActiveTab is null,
                "The packaged custom renderer changed navigation metadata or selection.");
        }
        finally { PackageTabLinks.NavigationRequested -= Navigation; }
    }

    private static IEnumerable<DependencyObject> TabDescendants(DependencyObject parent)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index); yield return child;
            foreach (var descendant in TabDescendants(child)) yield return descendant;
        }
    }
}
