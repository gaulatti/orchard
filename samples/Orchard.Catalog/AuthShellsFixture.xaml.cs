using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Orchard.Catalog;

public sealed partial class AuthShellsFixture : UserControl
{
    public AuthShellsFixture() => InitializeComponent();
    private void Change(object sender, RoutedEventArgs args)
    {
        if (Primary is null || Split is null || Start is null || Rtl is null || TextScale is null) return;
        Primary.Layout = Split.IsChecked == true ? BleeckerAuthShellLayout.Split : BleeckerAuthShellLayout.Centered;
        Primary.Side = Start.IsChecked == true ? BleeckerAuthShellSide.Start : BleeckerAuthShellSide.End;
        Primary.FlowDirection = Rtl.IsChecked == true ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        Primary.FontSize = TextScale.IsChecked == true ? 32 : 16;
    }
    private void LocalAction(object sender, RoutedEventArgs args) => Result.Text = "Local fictional action invoked; caller-owned form values retained";
}
