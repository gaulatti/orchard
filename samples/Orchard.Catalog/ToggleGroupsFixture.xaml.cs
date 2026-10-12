using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace Orchard.Catalog;

public sealed partial class ToggleGroupsFixture : UserControl
{
    public ToggleGroupsFixture()
    {
        InitializeComponent();
        SingleGroup.SelectionChanged += (_, _) => ShowSelection();
        MultipleGroup.SelectionChanged += (_, _) => ShowSelection();
        MultipleGroup.SelectedValues.Add("local");
        ShowSelection();
    }

    private void ShowSelection() => SelectionResult.Text = $"View: {(string.IsNullOrEmpty(SingleGroup.SelectedValue) ? "none" : SingleGroup.SelectedValue)}; topics: {string.Join(", ", MultipleGroup.SelectedValues)}";

    internal async Task VerifyAsync()
    {
        SingleGroup.UpdateLayout();
        Require(ListItem.CornerRadius == new CornerRadius(8), "Grouped toggles must use the reference's 8px radius.");
        var inheritedStyle = GridItem.Style;
        var consumerStyle = new Style(typeof(BleeckerToggleGroupItem)) { BasedOn = inheritedStyle };
        consumerStyle.Setters.Add(new Setter(Control.CornerRadiusProperty, new CornerRadius(12)));
        GridItem.Style = consumerStyle;
        SingleGroup.Variant = BleeckerToggleVariant.Outline;
        Require(ReferenceEquals(GridItem.Style, consumerStyle) && GridItem.CornerRadius == new CornerRadius(12),
            "Group presentation replaced a consumer-supplied item style.");
        GridItem.Style = inheritedStyle;
        SingleGroup.Variant = BleeckerToggleVariant.Default;
        var groupPeer = FrameworkElementAutomationPeer.CreatePeerForElement(SingleGroup);
        var selection = (ISelectionProvider)groupPeer.GetPattern(PatternInterface.Selection);
        var listPeer = FrameworkElementAutomationPeer.CreatePeerForElement(ListItem);
        var gridPeer = FrameworkElementAutomationPeer.CreatePeerForElement(GridItem);
        var gridSelection = (ISelectionItemProvider)gridPeer.GetPattern(PatternInterface.SelectionItem);
        Require(!selection.CanSelectMultiple && !selection.IsSelectionRequired, "Single group exposed incorrect selection cardinality.");
        gridSelection.Select();
        Require(SingleGroup.SelectedValue == "grid" && SelectionValueInput.Text == "grid" && GridItem.IsChecked == true && ListItem.IsChecked == false,
            "Single selection failed to clear the previous item.");
        ((IToggleProvider)gridPeer.GetPattern(PatternInterface.Toggle)).Toggle();
        await Task.Delay(80);
        Require(string.IsNullOrEmpty(SingleGroup.SelectedValue) && selection.GetSelection().Length == 0, "Activating the selected single item did not clear selection.");
        SelectionValueInput.Text = "list";
        await Task.Delay(80);
        Require(SingleGroup.GetBindingExpression(BleeckerToggleGroup.SelectedValueProperty) is not null, "Selecting an item replaced the consumer binding.");
        Require(((ISelectionItemProvider)listPeer.GetPattern(PatternInterface.SelectionItem)).IsSelected,
            "Consumer selection did not reach the item automation state.");
        SingleGroup.Items.Remove(ListItem);
        SingleGroup.Items.Insert(1, ListItem);
        SingleGroup.UpdateLayout();
        await Task.Delay(80);
        Require(SingleGroup.SelectedValue == "list" && ListItem.IsChecked == true, "Selection did not survive item reorder.");
        SingleGroup.Items.Remove(ListItem);
        SingleGroup.Items.Insert(0, ListItem);
        SingleGroup.UpdateLayout();

        var multiplePeer = FrameworkElementAutomationPeer.CreatePeerForElement(MultipleGroup);
        var multi = (ISelectionProvider)multiplePeer.GetPattern(PatternInterface.Selection);
        Require(multi.CanSelectMultiple, "Multiple group did not expose multiple selection.");
        MultipleGroup.SelectedValues.Clear();
        MultipleGroup.SelectedValues.Add("local");
        var culture = (ISelectionItemProvider)FrameworkElementAutomationPeer.CreatePeerForElement(CultureItem).GetPattern(PatternInterface.SelectionItem);
        culture.AddToSelection();
        Require(MultipleGroup.SelectedValues.SequenceEqual(new[] { "local", "culture" }) && multi.GetSelection().Length == 2,
            "Multiple selection replaced an existing selected value.");
        culture.RemoveFromSelection();
        Require(LocalItem.IsChecked == true && CultureItem.IsChecked == false, "Removing selection changed the wrong item.");
        MultipleGroup.SelectedValues.Add("culture");
        Require(CultureItem.IsChecked == true && SelectionResult.Text.Contains("culture", StringComparison.Ordinal), "External collection changes did not update the UI.");
        culture.Select();
        Require(MultipleGroup.SelectedValues.SequenceEqual(new[] { "culture" }) && LocalItem.IsChecked == false,
            "SelectionItem.Select did not replace the multi-selection.");
        MultipleGroup.IsEnabled = false;
        Require(!CultureItem.IsEnabled, "Disabled group did not disable its children.");
        MultipleGroup.IsEnabled = true;
        MultipleGroup.SelectedValues.Clear();
        MultipleGroup.SelectedValues.Add("local");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
