using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace Orchard.Catalog;
public sealed partial class FeedColumnsFixture : UserControl
{
    private int actions;
    public FeedColumnsFixture() => InitializeComponent();
    private void RecordAction(object sender, RoutedEventArgs args) => Status.Text = $"Consumer action {++actions}";
    private void UpdateHeader(object sender, RoutedEventArgs args) { LocalFeed.Title = "Updated local coverage"; LocalFeed.Count = actions; }
}
