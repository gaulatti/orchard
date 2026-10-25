using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace Orchard.Catalog;
public sealed partial class AdminShellsFixture : UserControl
{
    private int actions;
    private bool wide;
    public AdminShellsFixture() { InitializeComponent(); }
    private void OnFooterAction(object sender, RoutedEventArgs args) => Result.Text = $"Local footer action {++actions}";
    private void OnToggleWide(object sender, RoutedEventArgs args) { wide = !wide; BodyContent.Width = wide ? 1200 : double.NaN; }
    private void OnFarEdgeAction(object sender, RoutedEventArgs args) => Result.Text = $"Far edge action {++actions}";
}
