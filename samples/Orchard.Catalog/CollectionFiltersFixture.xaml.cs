using Microsoft.UI.Xaml.Controls;

namespace Orchard.Catalog;

public sealed partial class CollectionFiltersFixture : StackPanel
{
    public CollectionFiltersFixture()
    {
        InitializeComponent();
        var options = new BleeckerCollectionFilterOption[] { new("status", "Availability", BleeckerCollectionFilterType.Select, [new("available", "In stock"), new("preorder", "Pre-order")]), new("featured", "Featured edit", BleeckerCollectionFilterType.Boolean), new("publishedAfter", "Added after", BleeckerCollectionFilterType.Date) };
        var sorts = new BleeckerCollectionSortOption[] { new("createdAt", "Newest arrivals"), new("title", "Name"), new("price", "Price") };
        foreach (var control in new[] { Accepted, Declined, Constrained })
        {
            control.FilterOptions = options; control.SortOptions = sorts; control.CurrentSort = new("createdAt", BleeckerSortOrder.Descending);
            control.CurrentFilters = new Dictionary<string, BleeckerCollectionFilterValue> { ["featured"] = false, ["status"] = "available", ["publishedAfter"] = "2026-01-15" };
        }
        Numeric.FilterOptions = [new("2", "Second", BleeckerCollectionFilterType.Boolean)];
        Numeric.CurrentFilters = new Dictionary<string, BleeckerCollectionFilterValue> { ["tail"] = "Retained", ["10"] = true, ["2"] = false, ["01"] = "Original" };
    }
    private void AcceptFilters(object? sender, BleeckerCollectionFiltersRequestedEventArgs request) { Accepted.CurrentFilters = request.Filters; Status.Text = request.Filters.Count + " active filters"; }
    private void AcceptNumericFilters(object? sender, BleeckerCollectionFiltersRequestedEventArgs request) => Numeric.CurrentFilters = request.Filters;
    private void AcceptSort(object? sender, BleeckerCollectionSortRequestedEventArgs request) { Accepted.CurrentSort = request.Sort; Status.Text = "Sorted by " + request.Sort.Field + ", " + request.Sort.Order.ToString().ToLowerInvariant(); }
    private void DeclineFilters(object? sender, BleeckerCollectionFiltersRequestedEventArgs request) => Status.Text = "Filter request declined; original values retained";
    private void DeclineSort(object? sender, BleeckerCollectionSortRequestedEventArgs request) => Status.Text = "Sort request declined; original sort retained";
}
