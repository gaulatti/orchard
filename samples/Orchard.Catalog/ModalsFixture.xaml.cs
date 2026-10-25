using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace Orchard.Catalog;

public sealed partial class ModalsFixture:StackPanel
{
    private int requests;
    private bool expanded;
    public ModalsFixture(){InitializeComponent();Unloaded+=(_,_)=>{Nested.IsOpen=Default.IsOpen=Wide.IsOpen=false;};}
    private void OpenDefault(object sender,RoutedEventArgs args)=>Default.IsOpen=true;
    private void OpenWide(object sender,RoutedEventArgs args)=>Wide.IsOpen=true;
    private void OpenNested(object sender,RoutedEventArgs args)=>Nested.IsOpen=true;
    private void ToggleDetails(object sender,RoutedEventArgs args)
    {
        expanded=!expanded;DynamicText.Text=expanded?string.Join('\n',Enumerable.Repeat("Fictional local component detail",60)):"Fictional short detail";
        DetailButton.Content=expanded?"Shrink fictional content":"Expand fictional content";
    }
    private void NestedClosed(object sender,RoutedEventArgs args)=>Nested.IsOpen=false;
    private void Closed(object sender,RoutedEventArgs args){((BleeckerModal)sender).IsOpen=false;Status.Text=$"Close requests: {++requests}";}
}
