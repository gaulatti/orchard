using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Orchard.Catalog;
public sealed partial class PageFramesFixture : UserControl
{
    private int actions;
    public PageFramesFixture() => InitializeComponent();
    private void RecordAction(object sender, RoutedEventArgs args) => Status.Text = $"Consumer action {++actions}: {Title.Text}";
}
