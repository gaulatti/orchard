using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace PackageConsumer;

public sealed partial class MainWindow
{
    private int searchClears;
    private void ClearPackageSearch(object? sender,EventArgs args)=>searchClears++;
    private async Task VerifySearchInputAsync()
    {
        PackageSearch.Text="Independent search";PackageSearch.ApplyTemplate();Root.UpdateLayout();
        var clear=BrandDescendants<Button>(PackageSearch).Single();var editor=PackageSearch.Editor;
        Require(editor.Text==PackageSearch.Text&&editor.Padding==new Thickness(40,0,32,0),"Packaged search lost editor text/padding.");
        var chrome=BrandDescendants<Border>(editor).Single(border=>border.Name=="BorderElement");var dark=editor.ActualTheme==ElementTheme.Dark;
        await AwaitAsync(()=>chrome.BorderBrush is SolidColorBrush border&&Math.Abs(border.Color.A/255d*border.Opacity-(dark?.15:.4))<.000001,"Packaged input rounded its normal border alpha.");
        Require(FrameworkElementAutomationPeer.CreatePeerForElement(editor).GetName()=="Independent search"&&AutomationProperties.GetName(clear)=="Clear search","Packaged search lost accessible names.");
        await AcquireConsumerInputAsync();Require(clear.Focus(FocusState.Keyboard),"Packaged search clear refused focus.");var before=searchClears;
        await PaginationConsumerKeyAsync(0x20,clear,clear,()=>Require(searchClears==before,"Packaged search clear invoked before Space release."));
        Require(searchClears==before+1&&PackageSearch.Text=="Independent search","Packaged clear changed consumer-owned text.");
        PackageSearch.IsEditorEnabled=false;Root.UpdateLayout();Require(!editor.IsEnabled&&clear.IsEnabled,"Packaged search disabled sibling clear.");
        await AwaitAsync(()=>chrome.Background is SolidColorBrush background&&background.Color.A==255&&Math.Abs(background.Opacity-.65)<.000001&&background.Color.R==(dark?13:249)&&background.Color.G==(dark?24:246)&&background.Color.B==(dark?33:242),"Packaged input rounded its disabled background alpha or changed its color.");
        before=searchClears;await PaginationConsumerKeyAsync(0x20,clear,clear,()=>Require(searchClears==before,"Packaged disabled-editor clear invoked early."));Require(searchClears==before+1,"Packaged disabled editor blocked clear.");
        PackageSearch.IsEditorEnabled=true;Root.UpdateLayout();Require(editor.Focus(FocusState.Keyboard),"Packaged search editor refused focus.");
        await PaginationConsumerKeyAsync(0x1B,editor,editor,()=>{});Root.UpdateLayout();Require(PackageSearch.Text==""&&editor.Text==""&&clear.Visibility==Visibility.Collapsed,"Packaged Escape did not edit text/hide clear.");
        PackageSearch.Text="Independent search";Root.UpdateLayout();Require(editor.Text==PackageSearch.Text&&clear.Visibility==Visibility.Visible,"Packaged search did not update from consumer.");
    }
}
