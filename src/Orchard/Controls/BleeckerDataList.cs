using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI.ViewManagement;

namespace Orchard;

/// <summary>A responsive description list retaining consumer-owned row content.</summary>
public sealed partial class BleeckerDataList : Control
{
    private Panel? rows;
    private readonly List<Entry> entries = [];
    private ObservableCollection<BleeckerDataListItem>? observedItems;
    private readonly HashSet<BleeckerDataListItem> observedRows = [];
    private XamlRoot? observedRoot;
    private bool observedCompact;
    private readonly Dictionary<string, TextBlock> palette = [];
    private readonly List<(TextBlock Probe, long Token)> paletteObservers = [];
    private readonly UISettings settings = new();
    private double textScale = 1;
    internal bool Compact => XamlRoot is { } root && root.Size.Width < 640;

    public BleeckerDataList()
    {
        DefaultStyleKey = typeof(BleeckerDataList);
        Items = [];
        Loaded += (_, _) => { Observe(); Render(); };
        Unloaded += (_, _) => StopObserving();
        ActualThemeChanged += (_, _) => Render();
        LayoutUpdated += (_, _) =>
        {
            if (!IsLoaded) return;
            if (textScale != settings.TextScaleFactor) { textScale = settings.TextScaleFactor; Render(); }
            foreach (var entry in entries) entry.AlignText();
        };
        foreach (var property in new[] { FlowDirectionProperty, FontFamilyProperty, FontSizeProperty, CharacterSpacingProperty, IsTextScaleFactorEnabledProperty })
            RegisterPropertyChangedCallback(property, (_, _) => Render());
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) DetachTemplate(); });
    }
    private static DependencyProperty Property(string name, Type type, object? initial = null) =>
        DependencyProperty.Register(name, type, typeof(BleeckerDataList), new PropertyMetadata(initial, (owner, _) =>
        {
            var list = (BleeckerDataList)owner; if (list.IsLoaded) list.Observe(); list.Render();
        }));
    public static readonly DependencyProperty ItemsProperty = Property(nameof(Items), typeof(object));
    public static readonly DependencyProperty IsStripedProperty = Property(nameof(IsStriped), typeof(bool), false);
    public static readonly DependencyProperty LabelTemplateProperty = Property(nameof(LabelTemplate), typeof(DataTemplate));
    public static readonly DependencyProperty ValueTemplateProperty = Property(nameof(ValueTemplate), typeof(DataTemplate));
    public static readonly DependencyProperty DescriptionTemplateProperty = Property(nameof(DescriptionTemplate), typeof(DataTemplate));
    public ObservableCollection<BleeckerDataListItem> Items { get => GetValue(ItemsProperty) as ObservableCollection<BleeckerDataListItem> ?? throw new InvalidOperationException("DataList Items must be an observable item collection."); set { ArgumentNullException.ThrowIfNull(value); SetValue(ItemsProperty, value); } }
    public bool IsStriped { get => (bool)GetValue(IsStripedProperty); set => SetValue(IsStripedProperty, value); }
    public DataTemplate? LabelTemplate { get => (DataTemplate?)GetValue(LabelTemplateProperty); set => SetValue(LabelTemplateProperty, value); }
    public DataTemplate? ValueTemplate { get => (DataTemplate?)GetValue(ValueTemplateProperty); set => SetValue(ValueTemplateProperty, value); }
    public DataTemplate? DescriptionTemplate { get => (DataTemplate?)GetValue(DescriptionTemplateProperty); set => SetValue(DescriptionTemplateProperty, value); }

    protected override void OnApplyTemplate()
    {
        DetachTemplate(); base.OnApplyTemplate();
        if (Template is null) return;
        rows = GetTemplateChild("PART_Rows") as Panel ?? throw new InvalidOperationException("DataList requires PART_Rows.");
        foreach (var key in new[] { "Primary", "Secondary", "Stripe", "Divider" })
            palette[key] = GetTemplateChild("PART_" + key) as TextBlock ?? throw new InvalidOperationException("DataList requires palette " + key + ".");
        foreach (var probe in palette.Values)
            paletteObservers.Add((probe, probe.RegisterPropertyChangedCallback(TextBlock.ForegroundProperty, (_, _) => Render())));
        Render();
    }
    private void DetachTemplate()
    {
        foreach (var (probe, token) in paletteObservers) probe.UnregisterPropertyChangedCallback(TextBlock.ForegroundProperty, token);
        paletteObservers.Clear();
        foreach (var entry in entries) entry.ReleaseContent();
        rows?.Children.Clear(); entries.Clear(); palette.Clear(); rows = null;
    }
    private void Observe()
    {
        StopObserving();
        observedItems = Items; observedItems.CollectionChanged += CollectionChanged;
        foreach (var item in Items)
        {
            if (item is null) throw new InvalidOperationException("DataList items cannot be null.");
            if (observedRows.Add(item)) item.PropertyChanged += ItemChanged;
        }
        observedRoot = XamlRoot;
        observedCompact = Compact;
        if (observedRoot is not null) observedRoot.Changed += RootChanged;
    }
    private void StopObserving()
    {
        if (observedItems is not null) observedItems.CollectionChanged -= CollectionChanged;
        foreach (var item in observedRows) item.PropertyChanged -= ItemChanged;
        observedRows.Clear(); observedItems = null;
        if (observedRoot is not null) observedRoot.Changed -= RootChanged;
        observedRoot = null;
    }
    private void CollectionChanged(object? sender, NotifyCollectionChangedEventArgs args) { Observe(); Render(); }
    private void ItemChanged(object? sender, PropertyChangedEventArgs args) => Render();
    private void RootChanged(XamlRoot sender, XamlRootChangedEventArgs args)
    {
        if (observedCompact == Compact) return;
        observedCompact = Compact;
        foreach (var entry in entries) entry.InvalidateMeasure(); rows?.InvalidateMeasure();
    }
    private Brush Paint(string name) => palette[name].Foreground;
    private FontFamily Font(string name) => (FontFamily)Application.Current.Resources[name];
    private void Render()
    {
        if (rows is null) return;
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in Items)
            if (item is null || !keys.Add(item.Id)) throw new InvalidOperationException("DataList requires non-null items with unique IDs.");
        var previous = entries.ToDictionary(entry => entry.Key, StringComparer.Ordinal);
        var next = new List<Entry>();
        foreach (var item in Items)
        {
            if (!previous.Remove(item.Id, out var entry)) entry = new Entry(this, item);
            next.Add(entry);
        }
        foreach (var entry in previous.Values) { entry.ReleaseContent(); rows.Children.Remove(entry); }
        entries.Clear(); entries.AddRange(next);
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (i >= rows.Children.Count || !ReferenceEquals(rows.Children[i], entry)) { rows.Children.Remove(entry); rows.Children.Insert(i, entry); }
            entry.Update(Items[i], i, entries.Count);
        }
        rows.InvalidateMeasure();
        FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new ListPeer(this);
    private sealed class ListPeer(BleeckerDataList owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerDataList);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.List;
    }
}
