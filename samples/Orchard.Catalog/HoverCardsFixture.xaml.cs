using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Orchard.Catalog;

public sealed partial class HoverCardsFixture : StackPanel
{
    private int contributorActions;

    public HoverCardsFixture() => InitializeComponent();

    private void ContributorClicked(object sender, RoutedEventArgs args) => ActionStatus.Text = $"Contributor actions: {++contributorActions}";
}
