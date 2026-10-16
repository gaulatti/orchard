using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using ShapePath = Microsoft.UI.Xaml.Shapes.Path;
using Windows.Foundation;
using PageSizeChooser = Orchard.BleeckerSelectSurface;
using PageSizeOption = Orchard.BleeckerSelectItem;

namespace Orchard;

/// <summary>A responsive view over the consumer table's original pagination and selection.</summary>
public sealed partial class BleeckerDataTablePagination : Control
{
    private Grid? host;
    private PaginationPanel? layout;
    private TextBlock? summary, page;
    private PageSizeChooser? size;
    private Button? first, previous, next, last;
    private IBleeckerDataTablePaginationSource? observedTable;
    private EventHandler? tableChanged;
    private INotifyCollectionChanged? observedOptions;
    private XamlRoot? observedRoot;
    private bool updating;
    private int[] renderedOptions = [];

    public BleeckerDataTablePagination()
    {
        DefaultStyleKey = typeof(BleeckerDataTablePagination);
        PageSizeOptions = new ObservableCollection<int>([10, 20, 30, 50, 100]);
        Loaded += (_, _) => { Observe(); Refresh(); };
        Unloaded += (_, _) => Disconnect();
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) DetachTemplate(); });
        RegisterPropertyChangedCallback(FontFamilyProperty, (_, _) => Refresh());
        RegisterPropertyChangedCallback(FontSizeProperty, (_, _) => Refresh());
    }

    public static readonly DependencyProperty TableProperty = DependencyProperty.Register(nameof(Table), typeof(object), typeof(BleeckerDataTablePagination), new PropertyMetadata(null, Changed));
    public IBleeckerDataTablePaginationSource? Table { get => GetValue(TableProperty) as IBleeckerDataTablePaginationSource; set => SetValue(TableProperty, value); }
    public static readonly DependencyProperty PageSizeOptionsProperty = DependencyProperty.Register(nameof(PageSizeOptions), typeof(object), typeof(BleeckerDataTablePagination), new PropertyMetadata(null, Changed));
    public IEnumerable<int> PageSizeOptions
    {
        get => GetValue(PageSizeOptionsProperty) as IEnumerable<int> ?? throw new ArgumentException("PageSizeOptions must be an integer sequence.");
        set { ArgumentNullException.ThrowIfNull(value); SetValue(PageSizeOptionsProperty, value); }
    }
    private static void Changed(DependencyObject owner, DependencyPropertyChangedEventArgs args)
    { var control = (BleeckerDataTablePagination)owner; if (control.IsLoaded) control.Observe(); control.Refresh(); }

    protected override void OnApplyTemplate()
    {
        DetachTemplate(); base.OnApplyTemplate();
        host = GetTemplateChild("PART_Layout") as Grid ?? throw new InvalidOperationException("Pagination requires PART_Layout.");
        summary = Text("PART_Selection", false); page = Text("PART_Page", true);
        var rows = Text("PART_RowsLabel", false); rows.Text = "Rows per page";
        var resources = new ResourceDictionary { Source = new Uri("ms-appx:///Orchard/Themes/Orchard.xaml") };
        size = new PageSizeChooser(() => updating, () => Table, PageSizeDisplay, item => ReferenceEquals(size?.SelectedItem, item), "Rows per page options", "OrchardPageSizeChooser") { Name = "PART_PageSize", Width = 80, MinWidth = 0, MinHeight = 40, Height = 40, FontSize = 14, Padding = new Thickness(14, 0, 14, 0), UseLayoutRounding = false };
        size.SetBinding(FontFamilyProperty, new Binding { Source = this, Path = new PropertyPath(nameof(FontFamily)) });
        AutomationProperties.SetLabeledBy(size, rows); AutomationProperties.SetName(size, "Rows per page"); size.SelectionChanged += PageSizeSelectionChanged;
        first = Navigation("PART_First", "Go to first page", "M11 17L6 12L11 7 M18 17L13 12L18 7", FirstClicked);
        previous = Navigation("PART_Previous", "Go to previous page", "M15 18L9 12L15 6", PreviousClicked);
        next = Navigation("PART_Next", "Go to next page", "M9 18L15 12L9 6", NextClicked);
        last = Navigation("PART_Last", "Go to last page", "M6 17L11 12L6 7 M13 17L18 12L13 7", LastClicked);
        var sizes = new FlexRow { Gap = 8 }; sizes.Children.Add(rows); sizes.Children.Add(size);
        var navigation = new FlexRow { Gap = 4 }; foreach (var button in new[] { first, previous, next, last }) navigation.Children.Add(button);
        var right = new FlexRow { Gap = 16 }; right.Children.Add(sizes); right.Children.Add(page); right.Children.Add(navigation);
        layout = new PaginationPanel(summary, right); host.Children.Add(layout);
        if (IsLoaded) Observe(); Refresh();
    }

    private TextBlock Text(string name, bool medium)
    {
        var text = (TextBlock)XamlReader.Load("<TextBlock xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Foreground='{ThemeResource BleeckerTextSecondaryBrush}'/>");
        text.Name = name; text.FontSize = 14; text.LineHeight = 20; text.LineStackingStrategy = LineStackingStrategy.BlockLineHeight; text.TextWrapping = TextWrapping.Wrap; text.UseLayoutRounding = false; text.CharacterSpacing = 0;
        text.SetBinding(TextBlock.FontFamilyProperty, new Binding { Source = this, Path = new PropertyPath(nameof(FontFamily)) });
        text.FontWeight = medium ? Microsoft.UI.Text.FontWeights.Medium : Microsoft.UI.Text.FontWeights.Normal;
        if (medium) text.FontFamily = (FontFamily)Application.Current.Resources["BleeckerPrimaryMediumFontFamily"];
        if (medium) text.SetBinding(TextBlock.ForegroundProperty, new Binding { Source = this, Path = new PropertyPath(nameof(Foreground)) });
        return text;
    }
    private static Button Navigation(string name, string label, string figures, RoutedEventHandler clicked)
    {
        var palette = new ResourceDictionary { Source = new Uri("ms-appx:///Orchard/Themes/Buttons/Secondary.g.xaml") };
        var button = new Button { Name = name, Style = (Style)palette[typeof(Button)], Width = 32, Height = 32, MinWidth = 32, MinHeight = 32, Padding = new Thickness(0), UseLayoutRounding = false };
        button.Resources.MergedDictionaries.Add(palette); AutomationProperties.SetName(button, label);
        var canvas = new Canvas { Width = 24, Height = 24, IsHitTestVisible = false };
        var path = (ShapePath)XamlReader.Load("<Path xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Data='" + figures + "' StrokeThickness='2' StrokeStartLineCap='Round' StrokeEndLineCap='Round' StrokeLineJoin='Round'/>");
        path.SetBinding(ShapePath.StrokeProperty, new Binding { Source = button, Path = new PropertyPath(nameof(Foreground)) }); canvas.Children.Add(path);
        var icon = new Viewbox { Width = 16, Height = 16, Child = canvas, IsHitTestVisible = false }; AutomationProperties.SetAccessibilityView(icon, AccessibilityView.Raw); button.Content = icon;
        button.Click += clicked; return button;
    }
    private void FirstClicked(object sender, RoutedEventArgs args) => Execute(table => table.SetPageIndex(0));
    private void PreviousClicked(object sender, RoutedEventArgs args) => Execute(table => table.PreviousPage());
    private void NextClicked(object sender, RoutedEventArgs args) => Execute(table => table.NextPage());
    private void LastClicked(object sender, RoutedEventArgs args) => Execute(table => table.SetPageIndex(table.PageCount - 1));
    private void Execute(Action<IBleeckerDataTablePaginationSource> action)
    { if (!IsLoaded || !IsEnabled) return; action(Table ?? throw new InvalidOperationException("Pagination requires its consumer Table.")); Refresh(); }
    private void PageSizeSelectionChanged(object? sender, EventArgs args)
    { if (!updating && size?.SelectedItem is PageSizeOption { Tag: int value }) Execute(table => table.SetPageSize(value)); }
    private string PageSizeDisplay() => Table is { } table ? size?.Items.OfType<PageSizeOption>().FirstOrDefault(item => (int)item.Tag == table.PageSize)?.Content?.ToString() ?? "" : "";

    private void Observe()
    {
        Disconnect(); observedTable = Table;
        if (observedTable is null) throw new InvalidOperationException("A mounted pagination control requires its consumer Table.");
        var source = observedTable; tableChanged = (_, _) => Dispatch(() => { if (ReferenceEquals(Table, source) && ReferenceEquals(observedTable, source)) Refresh(); }); observedTable.Changed += tableChanged;
        observedOptions = PageSizeOptions as INotifyCollectionChanged; if (observedOptions is not null) observedOptions.CollectionChanged += OptionsChanged;
        observedRoot = XamlRoot; if (observedRoot is not null) observedRoot.Changed += RootChanged;
    }
    private void Dispatch(Action action) { if (!IsLoaded) return; if (DispatcherQueue.HasThreadAccess) action(); else DispatcherQueue.TryEnqueue(() => { if (IsLoaded) action(); }); }
    private void OptionsChanged(object? sender, NotifyCollectionChangedEventArgs args) => Dispatch(() => { if (ReferenceEquals(sender, PageSizeOptions)) Refresh(); });
    private void RootChanged(XamlRoot sender, XamlRootChangedEventArgs args) => Refresh();
    private void Disconnect()
    {
        if (observedTable is not null && tableChanged is not null) observedTable.Changed -= tableChanged; observedTable = null; tableChanged = null;
        if (observedOptions is not null) observedOptions.CollectionChanged -= OptionsChanged; observedOptions = null;
        if (observedRoot is not null) observedRoot.Changed -= RootChanged; observedRoot = null;
    }
    private void Refresh()
    {
        if (layout is null || size is null || page is null || summary is null || first is null || last is null || previous is null || next is null) return;
        var table = Table; if (table is null) return;
        summary.Text = table.FilteredSelectedRowCount > 0 ? $"{table.FilteredSelectedRowCount.ToString(CultureInfo.InvariantCulture)} of {table.FilteredRowCount.ToString(CultureInfo.InvariantCulture)} row(s) selected." : "";
        summary.Visibility = summary.Text.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        page.Text = $"Page {((long)table.PageIndex + 1).ToString(CultureInfo.InvariantCulture)} of {table.PageCount.ToString(CultureInfo.InvariantCulture)}";
        updating = true;
        var restoreViewportFocus = ((PageSizeChooser)size).PreserveViewportFocus();
        try
        {
            var options = PageSizeOptions.ToArray();
            if (!renderedOptions.SequenceEqual(options) || size.Items.Count != options.Length)
            {
                var available = size.Items.OfType<PageSizeOption>().GroupBy(item => (int)item.Tag).ToDictionary(group => group.Key, group => new Queue<PageSizeOption>(group));
                var ordered = options.Select(value => available.TryGetValue(value, out var queue) && queue.Count > 0 ? queue.Dequeue() : new PageSizeOption((PageSizeChooser)size) { Content = value.ToString(CultureInfo.InvariantCulture), Tag = value, FontFamily = FontFamily, FontSize = 14, MinHeight = 36 }).ToArray();
                for (var index = size.Items.Count - 1; index >= 0; index--) if (!ordered.Contains(size.Items[index])) size.Items.RemoveAt(index);
                for (var index = 0; index < ordered.Length; index++)
                {
                    var position = size.Items.IndexOf(ordered[index]);
                    if (position == index) continue;
                    if (position >= 0) size.Items.RemoveAt(position);
                    size.Items.Insert(index, ordered[index]);
                }
                renderedOptions = options;
            }
            size.SelectedItem = size.Items.OfType<PageSizeOption>().FirstOrDefault(item => (int)item.Tag == table.PageSize);
        }
        finally { updating = false; }
        restoreViewportFocus?.Invoke();
        first.IsEnabled = previous.IsEnabled = table.CanPreviousPage; next.IsEnabled = last.IsEnabled = table.CanNextPage;
        var wide = XamlRoot is not null && XamlRoot.Size.Width >= 640; first.Visibility = last.Visibility = wide ? Visibility.Visible : Visibility.Collapsed; layout.Wide = wide; layout.InvalidateMeasure();
        ((PageSizeChooser)size).RefreshOpenValue();
    }
    private void DetachTemplate()
    {
        if (size is not null) size.SelectionChanged -= PageSizeSelectionChanged;
        if (first is not null) first.Click -= FirstClicked; if (previous is not null) previous.Click -= PreviousClicked; if (next is not null) next.Click -= NextClicked; if (last is not null) last.Click -= LastClicked;
        host?.Children.Clear(); host = null; layout = null; summary = page = null; size = null; first = previous = next = last = null; renderedOptions = [];
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new PaginationPeer(this);
    private sealed class PaginationPeer(BleeckerDataTablePagination owner) : FrameworkElementAutomationPeer(owner)
    { protected override string GetClassNameCore() => nameof(BleeckerDataTablePagination); protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group; }
}
