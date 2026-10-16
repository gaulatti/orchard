using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI.ViewManagement;

namespace Orchard;

/// <summary>A responsive, non-selecting history of consumer-owned records and icons.</summary>
public sealed partial class BleeckerTimeline : Control
{
    private Panel? rows;
    private readonly List<Entry> entries = [];
    private ObservableCollection<BleeckerTimelineItem>? observedItems;
    private readonly HashSet<BleeckerTimelineItem> observedRows = [];
    private XamlRoot? observedRoot;
    private bool observedCompact;
    private readonly Dictionary<string, TextBlock> palette = [];
    private readonly List<(TextBlock Probe, long Token)> paletteObservers = [];
    private readonly UISettings settings = new();
    private readonly AccessibilitySettings accessibility = new();
    private SolidColorBrush? observedShadow;
    private long shadowColorToken, shadowOpacityToken;
    private double textScale = 1;
    internal bool Compact => XamlRoot is { Size.Width: < 640 };

    public BleeckerTimeline()
    {
        DefaultStyleKey = typeof(BleeckerTimeline); Items = [];
        Loaded += (_, _) => { Observe(); ObserveShadowBrush(); Render(); };
        Unloaded += (_, _) => { if (IsLoaded) return; StopObserving(); StopShadowBrush(); foreach (var entry in entries) entry.ClearShadow(); };
        ActualThemeChanged += (_, _) => Render();
        LayoutUpdated += (_, _) =>
        {
            if (!IsLoaded) return;
            if (textScale != settings.TextScaleFactor) { textScale = settings.TextScaleFactor; Render(); }
            foreach (var entry in entries) entry.Present();
        };
        foreach (var property in new[] { FlowDirectionProperty, FontFamilyProperty, FontSizeProperty, CharacterSpacingProperty, IsTextScaleFactorEnabledProperty })
            RegisterPropertyChangedCallback(property, (_, _) => Render());
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) DetachTemplate(); });
    }
    public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(nameof(Items), typeof(object), typeof(BleeckerTimeline), new PropertyMetadata(null, Changed));
    public static readonly DependencyProperty IconTemplateProperty = DependencyProperty.Register(nameof(IconTemplate), typeof(DataTemplate), typeof(BleeckerTimeline), new PropertyMetadata(null, Changed));
    public ObservableCollection<BleeckerTimelineItem> Items { get => GetValue(ItemsProperty) as ObservableCollection<BleeckerTimelineItem> ?? throw new InvalidOperationException("Timeline requires an observable item collection."); set { ArgumentNullException.ThrowIfNull(value); SetValue(ItemsProperty, value); } }
    public DataTemplate? IconTemplate { get => (DataTemplate?)GetValue(IconTemplateProperty); set => SetValue(IconTemplateProperty, value); }
    private static void Changed(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var owner = (BleeckerTimeline)sender; if (owner.IsLoaded) owner.Observe(); owner.Render();
    }
    protected override void OnApplyTemplate()
    {
        DetachTemplate(); base.OnApplyTemplate(); if (Template is null) return;
        rows = GetTemplateChild("PART_Rows") as Panel ?? throw new InvalidOperationException("Timeline requires PART_Rows.");
        foreach (var key in new[] { "Primary", "Secondary", "Completed", "Active", "Pending", "Error", "OnStatus", "Line", "Border", "Shadow" })
            palette[key] = GetTemplateChild("PART_" + key) as TextBlock ?? throw new InvalidOperationException("Timeline requires palette " + key + ".");
        foreach (var probe in palette.Values)
            paletteObservers.Add((probe, probe.RegisterPropertyChangedCallback(TextBlock.ForegroundProperty, (_, _) => { ObserveShadowBrush(); Render(); })));
        ObserveShadowBrush();
        Render();
    }
    private void DetachTemplate()
    {
        foreach (var (probe, token) in paletteObservers) probe.UnregisterPropertyChangedCallback(TextBlock.ForegroundProperty, token);
        paletteObservers.Clear(); StopShadowBrush(); foreach (var entry in entries) entry.Release();
        rows?.Children.Clear(); entries.Clear(); palette.Clear(); rows = null;
    }
    private void Observe()
    {
        StopObserving(); observedItems = Items; observedItems.CollectionChanged += CollectionChanged;
        foreach (var item in Items)
        {
            if (item is null) throw new InvalidOperationException("Timeline items cannot be null.");
            if (observedRows.Add(item)) item.PropertyChanged += ItemChanged;
        }
        observedRoot = XamlRoot; observedCompact = Compact;
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
        observedCompact = Compact; foreach (var entry in entries) entry.InvalidateMeasure(); rows?.InvalidateMeasure();
    }
    private Brush Paint(string name) => palette[name].Foreground;
    private void ObserveShadowBrush()
    {
        StopShadowBrush(); if (!IsLoaded || !palette.TryGetValue("Shadow", out var probe)) return;
        observedShadow = probe.Foreground as SolidColorBrush ?? throw new InvalidOperationException("Timeline shadow requires a solid brush.");
        shadowColorToken = observedShadow.RegisterPropertyChangedCallback(SolidColorBrush.ColorProperty, (_, _) => { foreach (var entry in entries) entry.Present(); });
        shadowOpacityToken = observedShadow.RegisterPropertyChangedCallback(Brush.OpacityProperty, (_, _) => { foreach (var entry in entries) entry.Present(); });
    }
    private void StopShadowBrush()
    {
        if (observedShadow is null) return;
        observedShadow.UnregisterPropertyChangedCallback(SolidColorBrush.ColorProperty, shadowColorToken);
        observedShadow.UnregisterPropertyChangedCallback(Brush.OpacityProperty, shadowOpacityToken); observedShadow = null;
    }
    private FontFamily Font(string key) => FontFamily.Source == ((FontFamily)Application.Current.Resources["BleeckerPrimaryMediumFontFamily"]).Source ? (FontFamily)Application.Current.Resources[key] : FontFamily;
    private void Render()
    {
        if (rows is null) return;
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in Items) if (item is null || !keys.Add(item.Id)) throw new InvalidOperationException("Timeline requires non-null items with unique IDs.");
        var previous = entries.ToDictionary(entry => entry.Key, StringComparer.Ordinal); var next = new List<Entry>();
        foreach (var item in Items) { if (!previous.Remove(item.Id, out var entry)) entry = new Entry(this, item); next.Add(entry); }
        foreach (var entry in previous.Values) { entry.Release(); rows.Children.Remove(entry); }
        entries.Clear(); entries.AddRange(next);
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (i >= rows.Children.Count || !ReferenceEquals(rows.Children[i], entry)) { rows.Children.Remove(entry); rows.Children.Insert(i, entry); }
            entry.Update(Items[i], i, entries.Count);
        }
        rows.InvalidateMeasure(); FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new ListPeer(this);
    private sealed class ListPeer(BleeckerTimeline owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerTimeline);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.List;
    }
}
