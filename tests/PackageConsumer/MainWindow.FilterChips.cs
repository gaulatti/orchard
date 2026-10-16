using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Orchard;

namespace PackageConsumer;

public sealed partial class MainWindow
{
    private int filterRemovals,filterClears;
    private readonly BleeckerFilter packageFilter=new("Status","Draft");
    private void InitializeFilterChips()
    {
        packageFilter.RemoveRequested+=(_,_)=>filterRemovals++;
        PackageFilters.ClearAllRequested+=(_,_)=>filterClears++;
        PackageFilters.Filters.Add(packageFilter);PackageFilters.Filters.Add(new("Desk","Culture"));
    }
    private async Task VerifyFilterChipsAsync()
    {
        PackageFilters.ApplyTemplate();Root.UpdateLayout();
        var action=BrandDescendants<Button>(PackageFilters).Single(button=>AutomationProperties.GetName(button)=="Remove Status filter");
        Require(FrameworkElementAutomationPeer.CreatePeerForElement(action).GetAutomationControlType()==AutomationControlType.Button&&action.IsEnabled,"Packaged filter lost its native action.");
        await AcquireConsumerInputAsync();Require(action.Focus(FocusState.Keyboard),"Packaged filter refused native focus.");var before=filterRemovals;
        await PaginationConsumerKeyAsync(0x20,action,action,()=>Require(filterRemovals==before,"Packaged filter invoked before Space release."));
        Require(filterRemovals==before+1&&PackageFilters.Filters.Count==2,"Packaged filter changed caller-owned data.");
        var clear=BrandDescendants<Button>(PackageFilters).Single(button=>button.Name=="ClearAllFilters");Require(clear.Focus(FocusState.Keyboard),"Packaged clear action refused focus.");before=filterClears;
        await PaginationConsumerKeyAsync(0x20,clear,clear,()=>Require(filterClears==before,"Packaged clear invoked before Space release."));
        Require(filterClears==before+1&&PackageFilters.Filters.Count==2,"Packaged clear changed caller-owned data.");
        packageFilter.Label="Changed";Root.UpdateLayout();Require(AutomationProperties.GetName(action)=="Remove Changed filter","Packaged filter lost live model updates.");
        packageFilter.Label="Status";
    }
}
