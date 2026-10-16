using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Runtime.InteropServices;
using System.Text.Json;
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
        await AcquireConsumerInputAsync(); PackagePagination.StartBringIntoView(); Require(size.Focus(FocusState.Keyboard), "Packaged page-size selector refused native focus.");
        var input = new List<object>(); var sizeRequests = paginationSource.SizeRequests;
        var ten = size.Items.OfType<ComboBoxItem>().Single(item => (int)item.Tag == 10); var twenty = size.Items.OfType<ComboBoxItem>().Single(item => (int)item.Tag == 20);
        input.Add(await PaginationConsumerKeyAsync(0x28, size, ten, () => Require(size.IsDropDownOpen && paginationSource.SizeRequests == sizeRequests, "Packaged ArrowDown mutated the table instead of opening its options.")));
        input.Add(await PaginationConsumerKeyAsync(0x09, ten, ten, () => Require(size.IsDropDownOpen && paginationSource.SizeRequests == sizeRequests, "Packaged popup Tab escaped or changed paging.")));
        input.Add(await PaginationConsumerKeyAsync(0x28, ten, twenty, () => Require(paginationSource.PageSize == 10 && paginationSource.SizeRequests == sizeRequests, "Packaged highlight committed before selection.")));
        input.Add(await PaginationConsumerKeyAsync(0x0D, twenty, size, () => Require(!size.IsDropDownOpen && paginationSource.PageSize == 20 && paginationSource.PageIndex == 0 && paginationSource.SizeRequests == sizeRequests + 1 && PackagePaginationRows.Text == paginationSource.VisibleText && ReferenceEquals(paginationSource.VisibleRows.First(), paginationSource.Rows[0]), "Packaged Enter did not commit exactly once through original consumer rows.")));
        var args = Environment.GetCommandLineArgs(); var report = args[Array.IndexOf(args, "--verify-output") + 1];
        await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(report)!, $"consumer-pagination-keyboard-{Root.RequestedTheme}.json"), JsonSerializer.Serialize(new { passed = true, input, originalRows = true, oneSizeCommit = true }));
        paginationSource.SetPageSize(10); paginationSource.SetPageIndex(0);
    }

    private async Task<object> PaginationConsumerKeyAsync(ushort key, Control from, Control to, Action verifyHeld)
    {
        Require(ReferenceEquals(FocusManager.GetFocusedElement(Root.XamlRoot), from), "Packaged pagination key lost its initial owner.");
        var downs = 0; var ups = 0; var downOwned = true; var upOwned = true;
        var down = new KeyEventHandler((_, args) => { if ((ushort)args.Key == key) { downs++; downOwned &= ReferenceEquals(args.OriginalSource, from); } });
        var up = new KeyEventHandler((_, args) => { if ((ushort)args.Key == key) { ups++; upOwned &= ReferenceEquals(args.OriginalSource, to); } });
        void Send(bool released) { var events = new[] { new ConsumerInput { Type = 1, Data = new ConsumerInputData { Keyboard = new ConsumerKeyboard { VirtualKey = key, Flags = released ? 2u : 0u } } } }; Require(SendInput(1, events, Marshal.SizeOf<ConsumerInput>()) == 1, "Packaged pagination key did not enter native input."); }
        from.AddHandler(UIElement.KeyDownEvent, down, true); to.AddHandler(UIElement.KeyUpEvent, up, true);
        try
        {
            Send(false);
            try
            {
                await AwaitAsync(() => downs == 1 && ReferenceEquals(FocusManager.GetFocusedElement(Root.XamlRoot), to), "Packaged pagination did not reach its explicit held-key focus target.");
                var presented = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var frames = 0;
                void Rendering(object? sender, object args) { if (++frames == 2) presented.TrySetResult(); }
                CompositionTarget.Rendering += Rendering;
                try { await presented.Task.WaitAsync(TimeSpan.FromSeconds(2)); } finally { CompositionTarget.Rendering -= Rendering; }
                verifyHeld();
            }
            finally { Send(true); }
            await AwaitAsync(() => ups == 1, "Packaged pagination lost its explicit key release owner.");
            Require(downs == 1 && ups == 1 && downOwned && upOwned && ReferenceEquals(FocusManager.GetFocusedElement(Root.XamlRoot), to), "Packaged pagination key lost exact input ownership.");
        }
        finally { from.RemoveHandler(UIElement.KeyDownEvent, down); to.RemoveHandler(UIElement.KeyUpEvent, up); }
        return new { key, downs, ups, downOwned, upOwned };
    }

    // Independent application rows and paging policy; no catalog or project reference.
    private sealed class ConsumerPaginationSource : IBleeckerDataTablePaginationSource
    {
        internal sealed record Row(string Title);
        internal readonly Row[] Rows = Enumerable.Range(1, 23).Select(index => new Row($"Story {index}")).ToArray();
        internal int SelectedCount;
        internal int SizeRequests;
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
        public void SetPageSize(int value) { SizeRequests++; var top = PageIndex * PageSize; PageSize = Math.Max(1, value); PageIndex = top / PageSize; Publish(); }
        public void PreviousPage() => SetPageIndex(PageIndex - 1);
        public void NextPage() => SetPageIndex(PageIndex + 1);
        internal void Publish() => Changed?.Invoke(this, EventArgs.Empty);
    }
}
