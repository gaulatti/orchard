using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace Orchard.Catalog;
public sealed partial class SelectsFixture:UserControl
{
    private readonly ObservableCollection<BleeckerSelectOption> options=[new("a","Local library Alpha"),new("b","Local library Beta"),new("disabled","Unavailable local library",true)];
    public SelectsFixture(){InitializeComponent();foreach(var select in new[]{Header,Small,Medium,Large,Error,Disabled,Uncontrolled,Nested})select.Options=options;Unloaded+=(_,_)=>NestedConfirmation.IsOpen=false;}
    private void ValueRequested(object? sender,BleeckerSelectRequestedEventArgs args){((BleeckerSelect)sender!).Value=args.Value;Status.Text="Consumer accepted: "+args.Value;}
    private void RecordRequest(object? sender,BleeckerSelectRequestedEventArgs args)=>Status.Text="Request recorded; original Value prop remains empty: "+args.Value;
    private void OpenNestedConfirmation(object sender,RoutedEventArgs args)=>NestedConfirmation.IsOpen=true;
    private void CloseNestedConfirmation(object sender,RoutedEventArgs args)=>NestedConfirmation.IsOpen=false;
}
