using Microsoft.UI.Xaml.Controls;
namespace Orchard.Catalog;
public sealed partial class SlidersFixture:UserControl
{
    public SlidersFixture()=>InitializeComponent();
    private void AcceptValue(object sender,BleeckerSliderValueRequestedEventArgs args){((BleeckerSlider)sender).Value=args.Value;Status.Text=$"Consumer accepted {args.Value}.";}
}
