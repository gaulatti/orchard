using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Text.Json;
using Windows.UI.ViewManagement;

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
        var bindingElapsed = System.Diagnostics.Stopwatch.StartNew();
        while (SelectionValueInput.Text != "grid" && bindingElapsed.Elapsed < TimeSpan.FromSeconds(1)) await Task.Delay(16);
        Require(SingleGroup.SelectedValue == "grid" && SelectionValueInput.Text == "grid" && GridItem.IsChecked == true && ListItem.IsChecked == false,
            $"Single selection failed to reach the consumer and clear the previous item: value={SingleGroup.SelectedValue}; text={SelectionValueInput.Text}; grid={GridItem.IsChecked}; list={ListItem.IsChecked}.");
        ((IToggleProvider)gridPeer.GetPattern(PatternInterface.Toggle)).Toggle();
        await MainWindow.AwaitPresentationAsync(this);
        Require(string.IsNullOrEmpty(SingleGroup.SelectedValue) && selection.GetSelection().Length == 0, "Activating the selected single item did not clear selection.");
        SelectionValueInput.Text = "list";
        await MainWindow.AwaitPresentationAsync(this);
        Require(SingleGroup.GetBindingExpression(BleeckerToggleGroup.SelectedValueProperty) is not null, "Selecting an item replaced the consumer binding.");
        Require(((ISelectionItemProvider)listPeer.GetPattern(PatternInterface.SelectionItem)).IsSelected,
            "Consumer selection did not reach the item automation state.");
        SingleGroup.Items.Remove(ListItem);
        SingleGroup.Items.Insert(1, ListItem);
        SingleGroup.UpdateLayout();
        await MainWindow.AwaitPresentationAsync(this);
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
        var selectionEvents = 0;
        EventHandler selectionListener = (_, _) => selectionEvents++;
        MultipleGroup.SelectionChanged += selectionListener;
        try { culture.Select(); }
        finally { MultipleGroup.SelectionChanged -= selectionListener; }
        Require(MultipleGroup.SelectedValues.SequenceEqual(new[] { "culture" }) && LocalItem.IsChecked == false,
            "SelectionItem.Select did not replace the multi-selection.");
        Require(selectionEvents == 1, $"SelectionItem.Select raised {selectionEvents} notifications for one action.");
        MultipleGroup.IsEnabled = false;
        Require(!CultureItem.IsEnabled, "Disabled group did not disable its children.");
        MultipleGroup.IsEnabled = true;
        MultipleGroup.SelectedValues.Clear();
        MultipleGroup.SelectedValues.Add("local");
    }

    internal async Task VerifyPhysicalAsync(ElementTheme theme, string folder)
    {
        using var input = new NativeInputSession(XamlRoot, folder, $"toggle-group-{theme}");
        await input.AcquireAsync(GridItem);
        SelectionValueInput.Text = "list";
        MultipleGroup.SelectedValues.Clear();
        MultipleGroup.SelectedValues.Add("local");
        await MainWindow.AwaitPresentationAsync(this);
        var light = theme == ElementTheme.Light;
        var surface = (SolidColorBrush)SingleGroup.Background;
        var border = (SolidColorBrush)SingleGroup.BorderBrush;
        Require(Hex(surface) == (light ? "#FFF9F6F2" : "#FFFFFFFF") &&
            Math.Abs(surface.Opacity - (light ? .65 : .05)) < .001 &&
            Hex(border) == (light ? "#FFE6D5B8" : "#FFFFFFFF") &&
            Math.Abs(border.Opacity - (light ? .2 : .07)) < .001,
            "Toggle Group did not render its themed surface and border.");
        await input.ClickAsync(GridItem);
        await MainWindow.AwaitPresentationAsync(this);
        Require(SingleGroup.SelectedValue == "grid" && SelectionValueInput.Text == "grid" && GridItem.IsChecked == true,
            "Pointer click did not select Grid in the caller binding.");
        await input.ClickAsync(GridItem);
        await MainWindow.AwaitPresentationAsync(this);
        Require(string.IsNullOrEmpty(SingleGroup.SelectedValue) && SelectionValueInput.Text == "" && GridItem.IsChecked == false,
            "Pointer click did not clear the selected item.");
        Require(ListItem.Focus(FocusState.Keyboard), "List item refused keyboard focus.");
        input.Key(0x27);
        await MainWindow.AwaitPresentationAsync(this);
        Require(ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), GridItem) &&
            string.IsNullOrEmpty(SingleGroup.SelectedValue),
            "Right arrow did not move focus to Grid without selecting it.");
        await input.KeyAsync(0x20, GridItem);
        await MainWindow.AwaitPresentationAsync(this);
        Require(SingleGroup.SelectedValue == "grid" && GridItem.IsChecked == true,
            "Space did not select the focused Grid item.");
        await input.ClickAsync(CultureItem);
        Require(MultipleGroup.SelectedValues.SequenceEqual(new[] { "local", "culture" }),
            "Pointer click did not add Culture to the caller's multiple selection.");
        await input.ClickAsync(CultureItem);
        Require(MultipleGroup.SelectedValues.SequenceEqual(new[] { "local" }),
            "Pointer click did not remove Culture from the caller's multiple selection.");
        Require(CultureItem.Focus(FocusState.Keyboard), "Culture item refused keyboard focus.");
        await input.KeyAsync(0x20, CultureItem);
        Require(MultipleGroup.SelectedValues.SequenceEqual(new[] { "local", "culture" }) && SelectionResult.Text.Contains("culture", StringComparison.Ordinal),
            "Space did not add Culture to the caller's multiple selection.");
        var reducedMotion = !new UISettings().AnimationsEnabled;
        if (reducedMotion)
        {
            var presenter = (ContentPresenter)VisualTreeHelper.GetChild(CultureItem, 0);
            var color = ((SolidColorBrush)presenter.Background).Color;
            await Task.Delay(50);
            Require(((SolidColorBrush)presenter.Background).Color == color,
                "Selected Toggle Group item continued a background transition under reduced motion.");
        }
        await File.WriteAllTextAsync(Path.Combine(folder, $"toggle-group-runtime-{theme}.json"),
            JsonSerializer.Serialize(new { passed = true, theme = theme.ToString(), pointerActions = 4,
                keyboardActions = 2, arrowFocus = true, callerSelection = true, reducedMotionVerified = reducedMotion }));
    }

    private static string Hex(SolidColorBrush brush) =>
        $"#{brush.Color.A:X2}{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2}";

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
