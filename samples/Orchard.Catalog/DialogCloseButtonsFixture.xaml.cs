using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Orchard.Catalog;

public sealed partial class DialogCloseButtonsFixture : UserControl
{
    private int requests;
    public DialogCloseButtonsFixture() => InitializeComponent();
    private void CloseRequested(object sender, RoutedEventArgs args) => Status.Text = $"Close requested {++requests} times; this consumer keeps its preview open.";
}
