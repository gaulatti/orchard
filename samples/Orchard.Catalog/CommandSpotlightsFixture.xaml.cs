using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Orchard.Catalog;

public sealed partial class CommandSpotlightsFixture : UserControl
{
    private readonly BleeckerCommandSpotlightAction[] originals;
    private int styleIndex;
    private int hotkeyIndex;
    private readonly BleeckerCommandSpotlightAction[] collectionOriginals;
    private bool largeCollection;
    public CommandSpotlightsFixture()
    {
        InitializeComponent();
        originals = [
            new BleeckerCommandSpotlightAction("alpha", "Original alpha command") { Description = "Original navigation description", Group = "Navigation", Keywords = ["coast", "quick route"], Shortcut = ["Ctrl", "1"], Icon = "□", Href = "/ignored-original-link", OnSelectAsync = () => { Result.Text = "Own callback: original alpha command"; return Task.CompletedTask; } },
            new BleeckerCommandSpotlightAction("beta", "Original beta command") { Description = "Original tools description", Group = "Tools", Keywords = ["portrait"], OnSelectAsync = () => { Result.Text = "Own callback: original beta command"; return Task.CompletedTask; } },
            new BleeckerCommandSpotlightAction("gamma", "Original gamma command") { Description = "Original second navigation description", Group = " Navigation ", Shortcut = ["G"], Icon = "○" },
            new BleeckerCommandSpotlightAction("disabled", "Original disabled command") { Description = "Original unavailable description", Group = "Tools", Disabled = true },
            new BleeckerCommandSpotlightAction("ungrouped", "Original ungrouped command") { Keywords = ["miscellaneous"] },
            new BleeckerCommandSpotlightAction("unicode", "Straße 東京 é العربية") { Description = "Original Unicode action", Group = "  ", Keywords = ["İstanbul", "ΣΟΣ"], Shortcut = ["Alt", "U"] }
        ];
        foreach (var action in originals) ((BleeckerCommandSpotlightActions)Primary.Actions).Add(action);
        collectionOriginals = Enumerable.Range(0,1000).Select(i=>new BleeckerCommandSpotlightAction("collection-"+i,$"Fictional command {i:D4}"){Group="Group "+i%10}).ToArray();
        Collection.Actions = collectionOriginals.Take(32).ToArray();
        Collection.ActionSelected += (_,action)=>Result.Text="Original collection action: "+action.Id;
        foreach (var control in new[] { Primary, Controlled, Scaled, Customized, Unicode })
        {
            if (!ReferenceEquals(control, Primary)) control.Actions = originals;
            control.ActionSelected += (_, action) => Result.Text = "Outer callback, original ID: " + action.Id;
            control.NavigationRequested += (_, request) => { request.Handled = true; Result.Text = "Fictional navigation requested: " + request.Href; };
            control.ActionFailed += (_, failure) => Result.Text = "Fictional action failed: " + failure.Action.Id;
        }
        Controlled.Open = false;
        Controlled.OpenRequested += (_, open) => { Result.Text = "Controlled open request: " + open; if (Reject.IsChecked != true) Controlled.Open = open; };
    }
    private void Rename(object sender, RoutedEventArgs args) => originals[0].Title = "Updated original alpha command";
    private void CycleCollection(object sender, RoutedEventArgs args)
    {
        largeCollection = !largeCollection;
        Collection.Actions = largeCollection ? collectionOriginals : collectionOriginals.Take(32).ToArray();
        CollectionCycle.Content = largeCollection ? "Restore 32 fictional commands" : "Use 1,000 fictional commands";
    }
    private void CycleHotkey(object sender, RoutedEventArgs args)
    {
        hotkeyIndex = (hotkeyIndex + 1) % 3;
        Unicode.Hotkey = new[] { "ß", "ArrowUp", "/" }[hotkeyIndex];
        HotkeyCycle.Content = new[] { "Use ArrowUp hotkey", "Use / hotkey", "Restore ß hotkey" }[hotkeyIndex];
    }
    private void CycleStyles(object sender, RoutedEventArgs args)
    {
        styleIndex = (styleIndex + 1) % 3;
        Customized.SurfaceStyle = styleIndex == 0 ? null : (Style)Resources[styleIndex == 1 ? "CompactSpotlightSurface" : "WideSpotlightSurface"];
        Customized.TriggerStyle = styleIndex == 0 ? null : (Style)Resources["RoomySpotlightTrigger"];
        StyleCycle.Content = styleIndex == 0 ? "Apply compact caller styles" : styleIndex == 1 ? "Apply wide caller styles" : "Restore default styles";
    }
    private void Reverse(object sender, RoutedEventArgs args) => Primary.Actions = Primary.Actions.Reverse().ToArray();
    private void Direction(object sender, RoutedEventArgs args) => Primary.FlowDirection = ((CheckBox)sender).IsChecked == true ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
}
