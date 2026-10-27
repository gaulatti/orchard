using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Runtime.InteropServices;
using System.Text.Json;
using Windows.Foundation;
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
        // Exercise the original consumer controls in the visible window. The
        // overview's long StackPanel has no scrolling ancestor to bring them in.
        var parent = (Panel)VisualTreeHelper.GetParent(PackagePagination);
        Require(ReferenceEquals(VisualTreeHelper.GetParent(PackagePaginationRows), parent), "Packaged pagination and row projection lost their shared consumer parent.");
        await AwaitAsync(() => PackagePagination.IsLoaded && paginationSource.Subscribers == 2, "Original pagination did not finish observing its consumer source.");
        var position = parent.Children.IndexOf(PackagePagination); var rowsPosition = parent.Children.IndexOf(PackagePaginationRows);
        var host = new StackPanel { Width = 800, Spacing = 16, Margin = new Thickness(32), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Background = Root.Background };
        parent.Children.Remove(PackagePaginationRows); parent.Children.Remove(PackagePagination);
        await AwaitAsync(() => !PackagePagination.IsLoaded && !PackagePaginationRows.IsLoaded && paginationSource.Subscribers == 1, "Packaged pagination did not unmount and disconnect its original consumer controls.");
        host.Children.Add(PackagePagination); host.Children.Add(PackagePaginationRows); Root.Children.Add(host);
        try
        {
            await AwaitAsync(() => PackagePagination.IsLoaded && PackagePaginationRows.IsLoaded && paginationSource.Subscribers == 2, "Packaged pagination did not mount and observe its visible original controls.");
            await VerifyVisiblePaginationAsync();
        }
        finally
        {
            Root.Children.Remove(host);
            await AwaitAsync(() => !PackagePagination.IsLoaded && paginationSource.Subscribers == 1, "Visible pagination did not disconnect before restoration.");
            host.Children.Clear(); parent.Children.Insert(position, PackagePagination); parent.Children.Insert(rowsPosition, PackagePaginationRows);
            await AwaitAsync(() => PackagePagination.IsLoaded && PackagePaginationRows.IsLoaded && paginationSource.Subscribers == 2, "Original pagination did not reconnect after restoration.");
        }
    }

    private async Task VerifyVisiblePaginationAsync()
    {
        paginationSource.SetPageIndex(0); PackagePagination.ApplyTemplate(); Root.UpdateLayout();
        var actions = BrandDescendants<Button>(PackagePagination).Where(button => button.Name.StartsWith("PART_", StringComparison.Ordinal)).ToDictionary(button => button.Name);
        void Invoke(string name) => ((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(actions[name]).GetPattern(PatternInterface.Invoke)).Invoke();
        Require(!actions["PART_First"].IsEnabled && actions["PART_Next"].IsEnabled, "Packaged pagination lost producer navigation flags.");
        Invoke("PART_Next"); await AwaitAsync(() => paginationSource.PageIndex == 1 && PackagePaginationRows.Text == "Story 11|Story 12|Story 13|Story 14|Story 15|Story 16|Story 17|Story 18|Story 19|Story 20", "Packaged pagination did not update original consumer rows.");
        Require(ReferenceEquals(paginationSource.VisibleRows.First(), paginationSource.Rows[10]), "Packaged paging replaced the original row.");
        var size = BrandDescendants<ItemsControl>(PackagePagination).Single(item => item.Name == "PART_PageSize"); ((ISelectionItemProvider)FrameworkElementAutomationPeer.CreatePeerForElement(size.Items.OfType<Button>().Single(item => (int)item.Tag == 20)).GetPattern(PatternInterface.SelectionItem)).Select();
        await AwaitAsync(() => paginationSource.PageIndex == 0 && paginationSource.PageSize == 20 && PackagePaginationRows.Text == paginationSource.VisibleText, "Packaged size change did not preserve the top row's page.");
        Invoke("PART_Last"); await AwaitAsync(() => paginationSource.PageIndex == 1 && !actions["PART_Next"].IsEnabled, "Packaged last-page boundary differs.");
        Require(ReferenceEquals(paginationSource.VisibleRows.Last(), paginationSource.Rows[22]) && ReferenceEquals(PackagePagination.Table, paginationSource), "Packaged pagination lost consumer identity.");
        paginationSource.SelectedCount = 2; paginationSource.Publish(); Root.UpdateLayout();
        Require(BrandDescendants<TextBlock>(PackagePagination).Single(text => text.Name == "PART_Selection").Text == "2 of 23 row(s) selected.", "Packaged selection count did not reach the visible summary.");
        var sizePeer = FrameworkElementAutomationPeer.CreatePeerForElement(size);
        Require(FrameworkElementAutomationPeer.CreatePeerForElement(PackagePagination).GetAutomationControlType() == AutomationControlType.Group && sizePeer.GetName() == "Rows per page" && sizePeer.GetAutomationControlType() == AutomationControlType.ComboBox, "Packaged pagination lost its native accessible boundary.");
        paginationSource.SelectedCount = 0; paginationSource.SetPageSize(10); paginationSource.SetPageIndex(0);
        var presentation = await AwaitPaginationPresentationAsync(size);
        await AcquireConsumerInputAsync(); Require(size.Focus(FocusState.Keyboard), "Packaged page-size selector refused native focus.");
        var input = new List<object>(); var sizeRequests = paginationSource.SizeRequests;
        bool SizeIsOpen() => ((IExpandCollapseProvider)FrameworkElementAutomationPeer.CreatePeerForElement(size).GetPattern(PatternInterface.ExpandCollapse)).ExpandCollapseState == Microsoft.UI.Xaml.Automation.ExpandCollapseState.Expanded;
        var measuredGap = double.NaN; var measuredWidth = double.NaN;
        void VerifyOpenPopup()
        {
            var value = BrandDescendants<ContentPresenter>(size).Single(item => item.Name == "ContentPresenter");
            var text = value.Content is TextBlock block ? block.Text : value.Content?.ToString();
            var border = HostedSelectBorder(size);
            VerifyPackagedSelectShadow(border);
            var trigger = size.TransformToVisual(Root).TransformBounds(new Rect(0, 0, size.ActualWidth, size.ActualHeight)); var bounds = border.TransformToVisual(Root).TransformBounds(new Rect(0, 0, border.ActualWidth, border.ActualHeight));
            measuredGap = bounds.Top - trigger.Bottom; measuredWidth = bounds.Width;
            var scale = Root.XamlRoot.RasterizationScale;
            Require(double.IsFinite(scale) && scale > 0, "Packaged popup has no usable physical-pixel scale.");
            // The native Popup origin can snap to a physical pixel even when
            // the trigger's XAML layout starts at a fractional effective pixel.
            var horizontalError = Math.Abs(bounds.Left - trigger.Left);
            var horizontalTolerance = .5 / scale + 1d / 32;
            Require(text == "10" && value.Opacity == 1 && Math.Abs(measuredGap - 4) <= 1d / 32 && measuredWidth == 80 && horizontalError <= horizontalTolerance, $"Packaged popup did not preserve its controlled value, source gap, width or pixel-aligned placement: text={text}, opacity={value.Opacity:R}, gap={measuredGap:R}, width={measuredWidth:R}, horizontalError={horizontalError:R}, horizontalTolerance={horizontalTolerance:R}, scale={scale:R}, trigger={trigger}, popup={bounds}, root={Root.ActualWidth:R}x{Root.ActualHeight:R}.");
            Require(trigger.Left >= 0 && trigger.Top >= 0 && trigger.Right <= Root.ActualWidth && trigger.Bottom <= Root.ActualHeight && bounds.Left >= 0 && bounds.Top >= 0 && bounds.Right <= Root.ActualWidth && bounds.Bottom <= Root.ActualHeight, "Packaged chooser or popup is outside the painted consumer window.");
            var viewport = BrandDescendants<ContentControl>(border).Single(item => item.Name == "PART_Listbox"); var viewportPeer = FrameworkElementAutomationPeer.CreatePeerForElement(viewport);
            Require(viewportPeer.GetAutomationControlType() == AutomationControlType.List && viewportPeer.GetPattern(PatternInterface.Scroll) is IScrollProvider && size.Items.OfType<Button>().All(item => FrameworkElementAutomationPeer.CreatePeerForElement(item).GetAutomationControlType() == AutomationControlType.ListItem), "Packaged chooser lost its native list, scroll or option roles.");
            var accessibleValue = (IValueProvider)sizePeer.GetPattern(PatternInterface.Value); Require(accessibleValue.IsReadOnly && accessibleValue.Value == "10", "Packaged chooser lost its accessible controlled value.");
        }
        var ten = size.Items.OfType<Button>().Single(item => (int)item.Tag == 10); var twenty = size.Items.OfType<Button>().Single(item => (int)item.Tag == 20);
        input.Add(await PaginationConsumerKeyAsync(0x28, size, ten, () => { Require(SizeIsOpen() && paginationSource.SizeRequests == sizeRequests, "Packaged ArrowDown mutated the table instead of opening its options."); VerifyOpenPopup(); }));
        input.Add(await PaginationConsumerKeyAsync(0x09, ten, ten, () => Require(SizeIsOpen() && paginationSource.SizeRequests == sizeRequests, "Packaged popup Tab escaped or changed paging.")));
        input.Add(await PaginationConsumerKeyAsync(0x28, ten, twenty, () => Require(paginationSource.PageSize == 10 && paginationSource.SizeRequests == sizeRequests, "Packaged highlight committed before selection.")));
        input.Add(await PaginationConsumerKeyAsync(0x0D, twenty, size, () => Require(!SizeIsOpen() && paginationSource.PageSize == 20 && paginationSource.PageIndex == 0 && paginationSource.SizeRequests == sizeRequests + 1 && PackagePaginationRows.Text == paginationSource.VisibleText && ReferenceEquals(paginationSource.VisibleRows.First(), paginationSource.Rows[0]), "Packaged Enter did not commit exactly once through original consumer rows.")));
        var args = Environment.GetCommandLineArgs(); var report = args[Array.IndexOf(args, "--verify-output") + 1];
        await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(report)!, $"consumer-pagination-keyboard-{Root.RequestedTheme}.json"), JsonSerializer.Serialize(new { passed = true, input, originalRows = true, oneSizeCommit = true, measuredGap, measuredWidth, controlledValue = "10", visibleInWindow = true, nativeAutomationRoles = true, presentation, sourceSubscribers = paginationSource.Subscribers }));
        paginationSource.SetPageSize(10); paginationSource.SetPageIndex(0);
    }

    private async Task<object> AwaitPaginationPresentationAsync(FrameworkElement chooser)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var elapsed = System.Diagnostics.Stopwatch.StartNew(); var stable = 0; var frames = 0; Rect? previous = null; Rect? previousRows = null;
        bool Visible(Rect box) => box.Width > 0 && box.Height > 0 && box.Left >= 0 && box.Top >= 0 && box.Right <= Root.ActualWidth && box.Bottom <= Root.ActualHeight;
        void Rendering(object? sender, object args)
        {
            frames++;
            var bounds = chooser.TransformToVisual(Root).TransformBounds(new Rect(0, 0, chooser.ActualWidth, chooser.ActualHeight));
            var rows = PackagePaginationRows.TransformToVisual(Root).TransformBounds(new Rect(0, 0, PackagePaginationRows.ActualWidth, PackagePaginationRows.ActualHeight));
            var ready = chooser.IsLoaded && PackagePaginationRows.IsLoaded && paginationSource.Subscribers == 2 && Visible(bounds) && Visible(rows);
            stable = ready && previous == bounds && previousRows == rows ? stable + 1 : 0; previous = bounds; previousRows = rows;
            if (stable >= 2) completion.TrySetResult();
        }
        CompositionTarget.Rendering += Rendering;
        try { await completion.Task.WaitAsync(TimeSpan.FromSeconds(2)); }
        finally { CompositionTarget.Rendering -= Rendering; }
        return new { stable, frames, milliseconds = elapsed.Elapsed.TotalMilliseconds, chooser = previous, rows = previousRows };
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
                try { await AwaitAsync(() => downs == 1 && ReferenceEquals(FocusManager.GetFocusedElement(Root.XamlRoot), to), "Packaged pagination did not reach its explicit held-key focus target."); }
                catch (Exception error)
                {
                    var focused = FocusManager.GetFocusedElement(Root.XamlRoot);
                    var focus = focused is FrameworkElement element ? $"{element.GetType().Name}/{element.Name}/tag:{element.Tag}/loaded:{element.IsLoaded}" : focused?.GetType().Name ?? "null";
                    throw new InvalidOperationException($"Packaged pagination held-key gate failed: key={key}, downs={downs}, fromLoaded={from.IsLoaded}, toLoaded={to.IsLoaded}, expectedTag={to.Tag}, focused={focus}, ownForeground={GetForegroundWindow() == WinRT.Interop.WindowNative.GetWindowHandle(this)}, popups={VisualTreeHelper.GetOpenPopupsForXamlRoot(Root.XamlRoot).Count}.",error);
                }
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
        private EventHandler? changed;
        internal int Subscribers { get; private set; }
        public event EventHandler? Changed { add { changed += value; Subscribers++; } remove { changed -= value; Subscribers--; } }
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
        internal void Publish() => changed?.Invoke(this, EventArgs.Empty);
    }
}
