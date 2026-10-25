using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Orchard.Catalog;
public sealed partial class PanelsFixture : UserControl
{
    private int actions;
    public PanelsFixture() => InitializeComponent();
    private void RecordAction(object sender, RoutedEventArgs args) => Status.Text = $"Consumer action {++actions}";
}
