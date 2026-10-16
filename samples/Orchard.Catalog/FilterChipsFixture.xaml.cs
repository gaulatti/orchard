using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class FilterChipsFixture : UserControl
{
    private int requested;
    public FilterChipsFixture()
    {
        InitializeComponent();Removable.RemoveRequested+=(_,_)=>Result.Text=$"Removal requested {++requested} times; this standalone chip stays mounted.";
        Filters.ClearAllRequested+=(_,_)=>DispatcherQueue.TryEnqueue(()=>{Filters.Filters.Clear();Result.Text="The consumer cleared its collection.";});Reset();
    }
    private void RestoreFilters(object sender,RoutedEventArgs args)=>Reset();
    private void Reset()
    {
        Filters.Filters.Clear();
        foreach(var(label,value,remove)in new[]{("Status","Draft",true),("Desk","Culture",true),("Language","Português",false)})
        {var item=new BleeckerFilter(label,value);if(remove)item.RemoveRequested+=(_,_)=>DispatcherQueue.TryEnqueue(()=>{Filters.Filters.Remove(item);Result.Text="The consumer removed "+item.Label+".";});Filters.Filters.Add(item);}
    }
    internal async Task VerifyAsync(string folder,BleeckerColorScheme scheme)
    {
        Reset();await MainWindow.AwaitPresentationAsync(this);
        using var input=new NativeInputSession(XamlRoot,folder,"filter-chip-catalog-"+scheme);await input.AcquireAsync(Removable);
        var action=Descendants<Button>(Removable).Single();var before=requested;
        if(!action.Focus(FocusState.Keyboard))throw new InvalidOperationException("Catalog filter remove refused focus.");
        await input.KeyAsync(0x20,action);
        if(requested!=before+1||!Removable.IsLoaded||AutomationProperties.GetName(action)!="Remove Status filter")throw new InvalidOperationException("Catalog filter callback or accessible name failed.");
        var original=Filters.Filters[0];var groupAction=Descendants<Button>(Filters).Single(button=>AutomationProperties.GetName(button)=="Remove Status filter");
        await input.ClickAsync(groupAction);await MainWindow.AwaitPresentationAsync(this);
        if(Filters.Filters.Contains(original)||Filters.Filters.Count!=2)throw new InvalidOperationException("Catalog consumer did not remove its filter.");
        var clear=Descendants<Button>(Filters).Single(button=>button.Name=="ClearAllFilters");await input.ClickAsync(clear);await MainWindow.AwaitPresentationAsync(this);
        if(Filters.Filters.Count!=0||Filters.ActualHeight!=0)throw new InvalidOperationException("Catalog consumer did not clear its group.");
        Reset();await MainWindow.AwaitPresentationAsync(this);
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject parent)where T:DependencyObject
    {for(var index=0;index<VisualTreeHelper.GetChildrenCount(parent);index++){var child=VisualTreeHelper.GetChild(parent,index);if(child is T match)yield return match;foreach(var nested in Descendants<T>(child))yield return nested;}}
}
