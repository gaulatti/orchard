using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

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
        Require(FrameworkElementAutomationPeer.CreatePeerForElement(editor).GetName()=="Independent search"&&AutomationProperties.GetName(clear)=="Clear search","Packaged search lost accessible names.");
        await AcquireConsumerInputAsync();Require(clear.Focus(FocusState.Keyboard),"Packaged search clear refused focus.");var before=searchClears;
        await PaginationConsumerKeyAsync(0x20,clear,clear,()=>Require(searchClears==before,"Packaged search clear invoked before Space release."));
        Require(searchClears==before+1&&PackageSearch.Text=="Independent search","Packaged clear changed consumer-owned text.");
        PackageSearch.IsEditorEnabled=false;Root.UpdateLayout();Require(!editor.IsEnabled&&clear.IsEnabled,"Packaged search disabled sibling clear.");
        before=searchClears;await PaginationConsumerKeyAsync(0x20,clear,clear,()=>Require(searchClears==before,"Packaged disabled-editor clear invoked early."));Require(searchClears==before+1,"Packaged disabled editor blocked clear.");
        PackageSearch.IsEditorEnabled=true;Root.UpdateLayout();Require(editor.Focus(FocusState.Keyboard),"Packaged search editor refused focus.");
        await PaginationConsumerKeyAsync(0x1B,editor,editor,()=>{});Root.UpdateLayout();Require(PackageSearch.Text==""&&editor.Text==""&&clear.Visibility==Visibility.Collapsed,"Packaged Escape did not edit text/hide clear.");
        PackageSearch.Text="Independent search";Root.UpdateLayout();Require(editor.Text==PackageSearch.Text&&clear.Visibility==Visibility.Visible,"Packaged search did not update from consumer.");
    }
}
