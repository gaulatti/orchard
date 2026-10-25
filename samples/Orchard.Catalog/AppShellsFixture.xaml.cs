using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace Orchard.Catalog;
public sealed partial class AppShellsFixture:UserControl
{
    public AppShellsFixture(){InitializeComponent();Suite.Options=[new("monitor","Local monitoring"),new("editorial","Local editorial")];}
    private void SelectSuite(object? sender,BleeckerSelectRequestedEventArgs args){Suite.Value=args.Value;Selection.Text=args.Value=="monitor"?"Fictional monitoring suite":"Fictional editorial suite";}
    private void ClearNote(object sender,RoutedEventArgs args)=>Note.Text="";
}
