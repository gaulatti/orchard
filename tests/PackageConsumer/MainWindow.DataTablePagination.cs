using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Orchard;

namespace PackageConsumer;

public sealed partial class MainWindow
{
    private readonly ConsumerPaginationSource paginationSource = new();
    private void InitializePagination()
    {
        PackagePagination.Table = paginationSource;
        paginationSource.Changed += (_, _) => PackagePaginationRows.Text = paginationSource.VisibleText;
        PackagePaginationRows.Text = paginationSource.VisibleText;
    }

    private async Task VerifyPaginationAsync()
    {
        paginationSource.SetPageIndex(0); PackagePagination.ApplyTemplate(); Root.UpdateLayout();
        var actions = BrandDescendants<Button>(PackagePagination).Where(button => button.Name.StartsWith("PART_", StringComparison.Ordinal)).ToDictionary(button => button.Name);
        void Invoke(string name) => ((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(actions[name]).GetPattern(PatternInterface.Invoke)).Invoke();
        Require(!actions["PART_First"].IsEnabled && actions["PART_Next"].IsEnabled, "Packaged pagination lost producer navigation flags.");
        Invoke("PART_Next"); await AwaitAsync(() => paginationSource.PageIndex == 1 && PackagePaginationRows.Text == "Story 11|Story 12|Story 13|Story 14|Story 15|Story 16|Story 17|Story 18|Story 19|Story 20", "Packaged pagination did not update original consumer rows.");
        Require(ReferenceEquals(paginationSource.VisibleRows.First(), paginationSource.Rows[10]), "Packaged paging replaced the original row.");
        var size = BrandDescendants<ComboBox>(PackagePagination).Single(); size.SelectedItem = size.Items.OfType<ComboBoxItem>().Single(item => (int)item.Tag == 20);
        await AwaitAsync(() => paginationSource.PageIndex == 0 && paginationSource.PageSize == 20 && PackagePaginationRows.Text == paginationSource.VisibleText, "Packaged size change did not preserve the top row's page.");
        Invoke("PART_Last"); await AwaitAsync(() => paginationSource.PageIndex == 1 && !actions["PART_Next"].IsEnabled, "Packaged last-page boundary differs.");
        Require(ReferenceEquals(paginationSource.VisibleRows.Last(), paginationSource.Rows[22]) && ReferenceEquals(PackagePagination.Table, paginationSource), "Packaged pagination lost consumer identity.");
        paginationSource.SelectedCount = 2; paginationSource.Publish(); Root.UpdateLayout();
        Require(BrandDescendants<TextBlock>(PackagePagination).Single(text => text.Name == "PART_Selection").Text == "2 of 23 row(s) selected.", "Packaged selection count did not reach the visible summary.");
        Require(FrameworkElementAutomationPeer.CreatePeerForElement(PackagePagination).GetAutomationControlType() == AutomationControlType.Group && FrameworkElementAutomationPeer.CreatePeerForElement(size).GetName() == "Rows per page", "Packaged pagination lost its native accessible boundary.");
        paginationSource.SelectedCount = 0; paginationSource.SetPageSize(10); paginationSource.SetPageIndex(0);
    }

    // Independent application rows and paging policy; no catalog or project reference.
    private sealed class ConsumerPaginationSource : IBleeckerDataTablePaginationSource
    {
        internal sealed record Row(string Title);
        internal readonly Row[] Rows = Enumerable.Range(1, 23).Select(index => new Row($"Story {index}")).ToArray();
        internal int SelectedCount;
        internal IEnumerable<Row> VisibleRows => Rows.Skip(PageIndex * PageSize).Take(PageSize);
        internal string VisibleText => string.Join('|', VisibleRows.Select(row => row.Title));
        public event EventHandler? Changed;
        public int PageIndex { get; private set; }
        public int PageSize { get; private set; } = 10;
        public int PageCount => (int)Math.Ceiling((double)Rows.Length / PageSize);
        public int FilteredRowCount => Rows.Length;
        public int FilteredSelectedRowCount => SelectedCount;
        public bool CanPreviousPage => PageIndex > 0;
        public bool CanNextPage => PageIndex < PageCount - 1;
        public void SetPageIndex(int value) { PageIndex = Math.Clamp(value, 0, PageCount - 1); Publish(); }
        public void SetPageSize(int value) { var top = PageIndex * PageSize; PageSize = Math.Max(1, value); PageIndex = top / PageSize; Publish(); }
        public void PreviousPage() => SetPageIndex(PageIndex - 1);
        public void NextPage() => SetPageIndex(PageIndex + 1);
        internal void Publish() => Changed?.Invoke(this, EventArgs.Empty);
    }
}
