using Microsoft.UI.Xaml.Controls;

namespace Orchard.Catalog;

public sealed partial class PaginationsFixture : UserControl
{
    public PaginationsFixture() => InitializeComponent();
    private void AcceptPage(object sender, BleeckerPageRequestedEventArgs args)
    {
        Accepted.CurrentPage = args.Page; Accepted.HasPrevPage = args.Page > 1; Accepted.HasNextPage = args.Page < Accepted.TotalPages;
        Status.Text = $"Consumer accepted page {args.Page} of {Accepted.TotalPages}";
    }
    private void DeclinePage(object sender, BleeckerPageRequestedEventArgs args) => Status.Text = $"Consumer declined page {args.Page}";
}
