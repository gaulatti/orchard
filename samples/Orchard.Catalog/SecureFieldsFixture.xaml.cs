using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace Orchard.Catalog;

public sealed partial class SecureFieldsFixture : UserControl
{
    public SecureFieldsFixture() => InitializeComponent();
    private void Changed(object sender, RoutedEventArgs args)
    {
        if (Status is not null) Status.Text = $"Value length: {Sample.Password.Length}";
    }
    private void Clear(object sender, RoutedEventArgs args) => Sample.Password = string.Empty;
}
