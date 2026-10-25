using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Orchard.Catalog;

public sealed partial class FieldsFixture : UserControl
{
    public FieldsFixture() => InitializeComponent();
    private void ResetRequested(object sender, RoutedEventArgs args) { Code.Text = ""; Status.Text = "The consumer accepted Reset."; }
}
