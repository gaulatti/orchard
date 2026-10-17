using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class SearchInputsFixture : UserControl
{
    public SearchInputsFixture()=>InitializeComponent();
    private void ClearSearch(object? sender,EventArgs args){Search.Text="";Result.Text="The consumer cleared its search.";}
    private void ClearDisabled(object? sender,EventArgs args){DisabledEditor.Text="";Result.Text="The consumer cleared the disabled editor.";}
    private void KeepSearch(object? sender,EventArgs args)=>Result.Text="The read-only example keeps its consumer text.";
    internal async Task VerifyAsync(string folder,BleeckerColorScheme scheme)
    {
        Search.Text="Culture";DisabledEditor.Text="Disabled editor; clear stays available";await MainWindow.AwaitPresentationAsync(this);
        using var input=new NativeInputSession(XamlRoot,folder,"search-input-catalog-"+scheme);await input.AcquireAsync(Search.Editor);
        var clear=Children<Button>(Search).Single();await input.ClickAsync(clear);await MainWindow.AwaitPresentationAsync(this);
        if(Search.Text!=""||Search.Editor.Text!="")throw new InvalidOperationException("Catalog clear did not update its consumer text.");
        clear=Children<Button>(DisabledEditor).Single();await input.ClickAsync(clear);await MainWindow.AwaitPresentationAsync(this);
        if(DisabledEditor.Text!=""||DisabledEditor.Editor.IsEnabled)throw new InvalidOperationException("Disabled editor prevented its sibling clear callback.");
        Search.Text="Culture";DisabledEditor.Text="Disabled editor; clear stays available";await MainWindow.AwaitPresentationAsync(this);
    }
    private static IEnumerable<T> Children<T>(DependencyObject parent)where T:DependencyObject
    {for(var i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++){var child=VisualTreeHelper.GetChild(parent,i);if(child is T item)yield return item;foreach(var nested in Children<T>(child))yield return nested;}}
}
