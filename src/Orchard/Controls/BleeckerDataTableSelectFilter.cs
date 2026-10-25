using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace Orchard;

/// <summary>A controlled Select over the consumer's original filter column.</summary>
public sealed class BleeckerDataTableSelectFilter : Control
{
    private Grid? host;
    private readonly Microsoft.UI.Dispatching.DispatcherQueue uiQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread() ?? throw new InvalidOperationException("Select filter requires a UI dispatcher.");
    private BleeckerSelectSurface? select;
    private readonly BleeckerSelectOption empty = new();
    private readonly List<BleeckerSelectOption> observedItems = [];
    private IBleeckerDataTableFilterSource? observedTable;
    private EventHandler? tableChanged;
    private INotifyCollectionChanged? observedOptions;
    private XamlRoot? observedRoot;
    private bool updating;
    // Preserve the last applied key when the original observable option mutates.
    private readonly Dictionary<BleeckerSelectItem, string> optionKeys = [];
    private IBleeckerDataTableFilterColumn? Column => Table?.GetColumn(ColumnId ?? throw new InvalidOperationException("A column filter requires its original ColumnId."));
    private object? Value => BleeckerSelectFilterValue.Internal(Column?.GetFilterValue());
    private IEnumerable<BleeckerSelectOption> Choices => new[] { empty }.Concat(Options);

    public BleeckerDataTableSelectFilter()
    {
        DefaultStyleKey = typeof(BleeckerDataTableSelectFilter);
        Loaded += (_, _) => { Observe(); Refresh(); };
        Unloaded += (_, _) => { if (select is not null) select.IsDropDownOpen = false; Disconnect(); };
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) DetachTemplate(); });
        RegisterPropertyChangedCallback(IsEnabledProperty, (_, _) => Refresh());
        RegisterPropertyChangedCallback(FontFamilyProperty, (_, _) => Refresh());
        ActualThemeChanged += (_, _) => Refresh();
        AutomationProperties.SetAccessibilityView(this, AccessibilityView.Raw);
    }
    public static readonly DependencyProperty TableProperty = DependencyProperty.Register(nameof(Table), typeof(object), typeof(BleeckerDataTableSelectFilter), new PropertyMetadata(null, Changed));
    public IBleeckerDataTableFilterSource? Table { get => GetValue(TableProperty) as IBleeckerDataTableFilterSource; set => SetValue(TableProperty, value); }
    public static readonly DependencyProperty ColumnIdProperty = DependencyProperty.Register(nameof(ColumnId), typeof(string), typeof(BleeckerDataTableSelectFilter), new PropertyMetadata(null, Changed));
    public string? ColumnId { get => (string?)GetValue(ColumnIdProperty); set => SetValue(ColumnIdProperty, value); }
    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(BleeckerDataTableSelectFilter), new PropertyMetadata("All", Changed));
    public string Placeholder { get => (string)GetValue(PlaceholderProperty); set { ArgumentNullException.ThrowIfNull(value); SetValue(PlaceholderProperty, value); } }
    public static readonly DependencyProperty OptionsProperty = DependencyProperty.Register(nameof(Options), typeof(object), typeof(BleeckerDataTableSelectFilter), new PropertyMetadata(null, Changed));
    public IEnumerable<BleeckerSelectOption> Options
    {
        get => GetValue(OptionsProperty) as IEnumerable<BleeckerSelectOption> ?? throw new InvalidOperationException("A column filter requires its original Select Options.");
        set { ArgumentNullException.ThrowIfNull(value); SetValue(OptionsProperty, value); }
    }
    private static void Changed(DependencyObject owner, DependencyPropertyChangedEventArgs args)
    { var control = (BleeckerDataTableSelectFilter)owner; if (control.IsLoaded) control.Observe(); control.Refresh(); }

    protected override void OnApplyTemplate()
    {
        DetachTemplate(); base.OnApplyTemplate();
        host = GetTemplateChild("PART_Layout") as Grid ?? throw new InvalidOperationException("A column filter requires PART_Layout.");
        select = new BleeckerSelectSurface(() => updating, () => Column, Display, item => item.Tag is BleeckerSelectOption choice && BleeckerSelectFilterValue.Matches(Value, choice), "Filter options", "OrchardDataTableSelect")
        { Name = "PART_Select", MinWidth = 0, Height = 40, MinHeight = 40, FontSize = 14, Padding = new Thickness(14, 0, 14, 0), UseLayoutRounding = false, FlowDirection = FlowDirection.LeftToRight };
        select.SetBinding(FontFamilyProperty, new Binding { Source = this, Path = new PropertyPath(nameof(FontFamily)) });
        select.SelectionChanged += SelectionChanged; select.SizeChanged += SelectSizeChanged; host.Children.Add(select);
        if (IsLoaded) Observe(); Refresh();
    }
    private string Display() => BleeckerSelectFilterValue.Display(Value, Placeholder, Choices);
    private void SelectionChanged(object? sender, EventArgs args)
    {
        if (updating || !IsLoaded || !IsEnabled || select?.SelectedItem is not { IsEnabled: true, Tag: BleeckerSelectOption option } item) return;
        if (!select.Items.Contains(item) || !Choices.Any(original => ReferenceEquals(original, option))) return;
        var column = Column;
        if (column is null) return;
        var value = BleeckerSelectFilterValue.Key(option);
        if (Value is not string current || current != value) column.SetFilterValue(value == BleeckerSelectFilterValue.EmptyOption ? null : option.Value);
        Refresh();
    }
    private void Observe()
    {
        Disconnect(); observedTable = Table ?? throw new InvalidOperationException("A mounted column filter requires its original Table.");
        _ = ColumnId ?? throw new InvalidOperationException("A mounted column filter requires its original ColumnId.");
        var table = observedTable;
        tableChanged = (_, _) => Dispatch(() => { if (ReferenceEquals(table, Table) && ReferenceEquals(table, observedTable)) Refresh(); }); table.Changed += tableChanged;
        observedOptions = Options as INotifyCollectionChanged; if (observedOptions is not null) observedOptions.CollectionChanged += OptionsChanged;
        foreach (var item in Options.Distinct()) { if (item is null) throw new InvalidOperationException("Select options must be original non-null consumer objects."); item.PropertyChanged += ItemChanged; observedItems.Add(item); }
        observedRoot = XamlRoot; if (observedRoot is not null) observedRoot.Changed += RootChanged;
    }
    private void Dispatch(Action work) { if (uiQueue.HasThreadAccess) { if (IsLoaded) work(); } else if (!uiQueue.TryEnqueue(() => { if (IsLoaded) work(); })) throw new InvalidOperationException("The column filter could not apply its original source notification."); }
    private void OptionsChanged(object? sender, NotifyCollectionChangedEventArgs args) => Dispatch(() => { if (ReferenceEquals(sender, GetValue(OptionsProperty))) { Observe(); Refresh(); } });
    private void ItemChanged(object? sender, PropertyChangedEventArgs args) => Dispatch(() => { if (observedItems.Any(item => ReferenceEquals(item, sender))) Refresh(); });
    private void RootChanged(XamlRoot sender, XamlRootChangedEventArgs args) => Refresh();
    private void SelectSizeChanged(object sender, SizeChangedEventArgs args) => select?.QueueOpenPosition();
    private void Disconnect()
    {
        if (observedTable is not null && tableChanged is not null) observedTable.Changed -= tableChanged; observedTable = null; tableChanged = null;
        if (observedOptions is not null) observedOptions.CollectionChanged -= OptionsChanged; observedOptions = null;
        foreach (var item in observedItems) item.PropertyChanged -= ItemChanged; observedItems.Clear();
        if (observedRoot is not null) observedRoot.Changed -= RootChanged; observedRoot = null;
    }
    private void Refresh()
    {
        if (host is null || select is null || Table is null || ColumnId is null || GetValue(OptionsProperty) is null) return;
        var column = Column;
        if (column is null) { select.IsDropDownOpen = false; select.Visibility = Visibility.Collapsed; host.Width = double.NaN; return; }
        select.Visibility = Visibility.Visible; select.IsEnabled = IsEnabled; empty.Label = Placeholder;
        AutomationProperties.SetName(select, Placeholder); var wide = XamlRoot?.Size.Width >= 640; host.Width = wide ? 176 : double.NaN; host.HorizontalAlignment = wide ? HorizontalAlignment.Left : HorizontalAlignment.Stretch;
        updating = true; var restore = select.PreserveOptionsFocus(item => optionKeys[item]);
        try
        {
            var available = select.Items.OfType<BleeckerSelectItem>().GroupBy(item => optionKeys[item]).ToDictionary(group => group.Key, group => new Queue<BleeckerSelectItem>(group));
            var nextKeys = new Dictionary<BleeckerSelectItem, string>();
            var ordered = Choices.Select(option =>
            {
                var item = available.TryGetValue(option.Value, out var queue) && queue.Count > 0 ? queue.Dequeue() : new BleeckerSelectItem(select) { FontSize = 14, MinHeight = 36 };
                item.Tag = option; item.Content = option.Label; item.IsEnabled = IsEnabled && !option.IsDisabled; nextKeys.Add(item, option.Value); return item;
            }).ToArray();
            for (var index = select.Items.Count - 1; index >= 0; index--) if (!ordered.Contains(select.Items[index])) select.Items.RemoveAt(index);
            for (var index = 0; index < ordered.Length; index++) { var current = select.Items.IndexOf(ordered[index]); if (current == index) continue; if (current >= 0) select.Items.RemoveAt(current); select.Items.Insert(index, ordered[index]); }
            select.SelectedItem = ordered.LastOrDefault(item => BleeckerSelectFilterValue.Matches(Value, (BleeckerSelectOption)item.Tag));
            optionKeys.Clear(); foreach (var item in nextKeys) optionKeys.Add(item.Key, item.Value);
        }
        finally { updating = false; }
        restore?.Invoke(); select.RefreshOpenValue();
    }
    private void DetachTemplate()
    {
        if (select is not null) { select.SelectionChanged -= SelectionChanged; select.SizeChanged -= SelectSizeChanged; select.IsDropDownOpen = false; }
        host?.Children.Clear(); host = null; select = null; optionKeys.Clear();
    }
}
