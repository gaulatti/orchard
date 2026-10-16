using System.Diagnostics;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class DataTablePaginationsFixture : UserControl
{
    private readonly PaginationTable table = new();
    public DataTablePaginationsFixture()
    {
        InitializeComponent(); Preview.Table = table; table.Changed += (_, _) => Rows.Text = table.VisibleText; Rows.Text = table.VisibleText;
    }
    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        await ReadyAsync(() => Preview.IsLoaded); Preview.ApplyTemplate(); await MainWindow.AwaitPresentationAsync(Preview);
        var buttons = Descendants<Button>(Preview).ToDictionary(item => item.Name);
        static void Invoke(Button button) => ((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(button).GetPattern(PatternInterface.Invoke)).Invoke();
        table.SetPagination(0, 10); await MainWindow.AwaitPresentationAsync(Preview);
        Require(!buttons["PART_First"].IsEnabled && !buttons["PART_Previous"].IsEnabled && buttons["PART_Next"].IsEnabled && buttons["PART_Last"].IsEnabled, "Initial page enablement differs.");
        Invoke(buttons["PART_Next"]); await ReadyAsync(() => table.PageIndex == 1 && Rows.Text == table.VisibleText); Require(table.VisibleRows[0] == table.Rows[10], "Navigation replaced the original consumer rows.");
        Invoke(buttons["PART_Last"]); await ReadyAsync(() => table.PageIndex == 4); Require(table.VisibleRows.Count == 7 && !buttons["PART_Last"].IsEnabled, "Last-page boundary differs.");
        table.SetPagination(2, 10); await MainWindow.AwaitPresentationAsync(Preview);
        var size = Descendants<ComboBox>(Preview).Single(); size.SelectedItem = size.Items.OfType<ComboBoxItem>().Single(item => (int)item.Tag == 20);
        await ReadyAsync(() => table.PageIndex == 1 && table.PageSize == 20); Require(table.VisibleRows[0] == table.Rows[20] && table.Requests[^1] == (nameof(PaginationTable.SetPageSize), 20), "Page size did not preserve the original top row.");
        table.SetSelection(["row-001", "row-002", "row-047"]); table.SetFilter(true); Require(table.FilteredSelectedRowCount == 2 && table.Selection.Count == 3 && table.FilteredRowCount == 24, "Filtering lost consumer selection.");
        table.SetSort(true); Require(ReferenceEquals(table.VisibleRows[0], table.Rows[46]), "Sorting replaced the original rows.");
        table.SetFilter(false); table.SetSort(false); table.SetPagination(0, 10); table.SetSelection([]); await MainWindow.AwaitPresentationAsync(Preview);
        Require(FrameworkElementAutomationPeer.CreatePeerForElement(Preview).GetAutomationControlType() == AutomationControlType.Group, "Pagination lost its group role.");
        await VerifyLifecycleAsync(scheme, folder);
        await MainWindow.CaptureElementAsync(Preview, Path.Combine(folder, $"data-table-pagination-{scheme}.png"));
        await File.WriteAllTextAsync(Path.Combine(folder, $"data-table-pagination-{scheme}.json"), JsonSerializer.Serialize(new { passed = true, table.PageCount, table.PageIndex, table.PageSize, originalRows = table.VisibleRows.All(row => table.Rows.Any(original => ReferenceEquals(original, row))), requests = table.Requests.ToArray() }));
    }
    private async Task ReadyAsync(Func<bool> condition)
    { var clock = Stopwatch.StartNew(); var readiness = new NativeReadiness(DispatcherQueue); while (!condition() && clock.Elapsed < TimeSpan.FromSeconds(2)) { UpdateLayout(); await readiness.WaitAsync(); } Require(condition(), "Pagination did not reach its consumer state within two seconds."); }
    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject { for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++) { var child = VisualTreeHelper.GetChild(parent, index); if (child is T match) yield return match; foreach (var descendant in Descendants<T>(child)) yield return descendant; } }
    private static void Require(bool condition, string error) { if (!condition) throw new InvalidOperationException(error); }
}
