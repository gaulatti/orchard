using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace Orchard.Catalog;
public sealed partial class TablesFixture : StackPanel
{
    public TablesFixture() => InitializeComponent();
    private void Changed(object sender, RoutedEventArgs args) { OriginalRow.IsSelected = !OriginalRow.IsSelected; Status.Text = "Selected: Inspect fictional item"; }
}
