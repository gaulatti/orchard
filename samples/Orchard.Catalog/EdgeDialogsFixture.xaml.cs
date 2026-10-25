using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace Orchard.Catalog;

public sealed partial class EdgeDialogsFixture:StackPanel
{
    private int requests;private bool drawerExpanded,sheetExpanded;
    public EdgeDialogsFixture(){InitializeComponent();SheetSelect.Options=[new("a","Fictional Alpha"),new("b","Fictional Beta")];Unloaded+=(_,_)=>{Nested.IsOpen=Drawer.IsOpen=Sheet.IsOpen=LeftSheet.IsOpen=TopSheet.IsOpen=BottomSheet.IsOpen=OwnScroll.IsOpen=false;};}
    private void OpenDrawer(object sender,RoutedEventArgs args)=>Drawer.IsOpen=true;
    private void OpenNested(object sender,RoutedEventArgs args)=>Nested.IsOpen=true;
    private void OpenSheet(object sender,RoutedEventArgs args){var side=Enum.Parse<BleeckerSheetSide>((string)((FrameworkElement)sender).Tag);(side switch{BleeckerSheetSide.Left=>LeftSheet,BleeckerSheetSide.Top=>TopSheet,BleeckerSheetSide.Bottom=>BottomSheet,_=>Sheet}).IsOpen=true;}
    private void OpenOwnScroll(object sender,RoutedEventArgs args)=>OwnScroll.IsOpen=true;
    private void ResizeDrawer(object sender,RoutedEventArgs args){drawerExpanded=!drawerExpanded;DrawerDetail.Text=drawerExpanded?string.Join('\n',Enumerable.Repeat("Fictional drawer component detail",60)):"Short fictional detail";}
    private void ResizeSheet(object sender,RoutedEventArgs args){sheetExpanded=!sheetExpanded;SheetDetail.Text=sheetExpanded?string.Join('\n',Enumerable.Repeat("Fictional sheet component detail",60)):"Short fictional detail";}
    private void Closed(object sender,RoutedEventArgs args){((BleeckerEdgeDialog)sender).IsOpen=false;Status.Text=$"Close requests: {++requests}";}
    private void ChoiceRequested(object? sender,BleeckerSelectRequestedEventArgs args){SheetSelect.Value=args.Value;Status.Text="Caller accepted fictional choice: "+args.Value;}
}
