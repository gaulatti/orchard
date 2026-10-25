using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace Orchard.Catalog;
public sealed partial class SimpleDataTablesFixture : StackPanel
{
    private sealed record Item(string Id, string Name, int Count, Button Action);
    public SimpleDataTablesFixture()
    {
        InitializeComponent();
        var items = new[] { Row("cedar", "Cedar bulletin", 12), Row("birch", "Birch update", 3), Row("ash", "Ash brief", 25) };
        Data.GetRowKey = (row, _) => ((Item)row).Id;
        Data.Columns.Add(new("Name", "Fictional item"));
        Data.Columns.Add(new("Count", "Count", alignment: BleeckerTableCellAlignment.Right));
        Data.Columns.Add(new("Action", "Original action", isSortable: false, cellContentSelector: (row, _) => ((Item)row).Action));
        Data.CellStyleSelector = column => column.Key switch { "Count" => (Style)Resources["FictionalCountCellStyle"], "Name" => (Style)Resources["FictionalNameCellStyle"], _ => null };
        Data.HeaderStyleSelector = column => column.Key == "Count" ? (Style)Resources["FictionalCountHeaderStyle"] : null;
        var foot = new BleeckerTableRow(); var label = new BleeckerTableCell { ColumnSpan = 2 }; var total = new BleeckerTableCell { ColumnSpan = 2, HorizontalContentAlignment = HorizontalAlignment.Right }; foot.Cells.Add(label); foot.Cells.Add(total);
        Data.FooterRowsSelector = sorted => { label.Content = "Fictional sorted order: " + string.Join(", ", sorted.Cast<Item>().Select(row => row.Id)); total.Content = sorted.Cast<Item>().Sum(row => row.Count).ToString(); return [foot]; };
        Data.ItemsSource = items;
    }
    private Item Row(string id, string name, int count)
    {
        var action = new Button { Content = "Inspect " + id };
        action.Click += (_, _) => Status.Text = "Original action: " + id;
        return new(id, name, count, action);
    }
    private void StandaloneSort(object? sender, BleeckerTableSort sort) { StandaloneHeader.CurrentSort = sort; Status.Text = "Standalone sort: " + sort.Order; }
    private void Sorted(object? sender, BleeckerTableSort sort) => Status.Text = "Sort request: " + sort.Field + " " + sort.Order;
    private void Selected(object? sender, IReadOnlySet<string> keys) => Status.Text = "Selected: " + string.Join(", ", keys);
}
