using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Orchard.Catalog;
public sealed partial class DetailLayoutsFixture : UserControl
{
    private int actions;
    public DetailLayoutsFixture() => InitializeComponent();
    private void RecordAction(object sender, RoutedEventArgs args) => Status.Text = $"Consumer action {++actions}";
}
