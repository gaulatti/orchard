using System.Collections.Specialized;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

/// <summary>A controlled pair of ISO local dates with consumer-supplied presets.</summary>
public sealed class BleeckerDateRangePicker : Control
{
    private Grid? host;
    private RangePanel? layout;
    private BleeckerDatePicker? from, to;
    private BleeckerButton? clear;
    private Border? presetBorder;
    private StackPanel? presetHost;
    private INotifyCollectionChanged? observed;
    private readonly ResourceDictionary palette = BleeckerDateVisual.Palette();
    private readonly Func<DateTimeOffset> now;
    public BleeckerDateRangePicker() : this(() => DateTimeOffset.Now) { }
    internal BleeckerDateRangePicker(Func<DateTimeOffset> now)
    {
        this.now = now; DefaultStyleKey = typeof(BleeckerDateRangePicker); Value = new();
        Loaded += (_, _) => { Observe(); Refresh(); };
        Unloaded += (_, _) => { if (observed is not null) observed.CollectionChanged -= PresetsChanged; observed = null; };
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) Detach(); });
    }
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(object), typeof(BleeckerDateRangePicker), new PropertyMetadata(null, (owner, _) => ((BleeckerDateRangePicker)owner).Refresh()));
    public static readonly DependencyProperty PresetsProperty = DependencyProperty.Register(nameof(Presets), typeof(object), typeof(BleeckerDateRangePicker), new PropertyMetadata(null, (owner, _) => { var picker = (BleeckerDateRangePicker)owner; if (picker.IsLoaded) picker.Observe(); picker.RenderPresets(); }));
    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(BleeckerDateRangePicker), new PropertyMetadata("Date range", (owner, _) => ((BleeckerDateRangePicker)owner).Refresh()));
    public BleeckerDateRange Value { get => (BleeckerDateRange?)GetValue(ValueProperty) ?? throw new InvalidOperationException("Date range requires a non-null consumer value."); set { ArgumentNullException.ThrowIfNull(value); _ = BleeckerDates.Parse(value.From); _ = BleeckerDates.Parse(value.To); SetValue(ValueProperty, value); } }
    public IEnumerable<BleeckerDatePreset>? Presets { get => (IEnumerable<BleeckerDatePreset>?)GetValue(PresetsProperty); set => SetValue(PresetsProperty, value); }
    public string Placeholder { get => (string)GetValue(PlaceholderProperty); set { ArgumentNullException.ThrowIfNull(value); SetValue(PlaceholderProperty, value); } }
    public event EventHandler<BleeckerDateRangeRequestedEventArgs>? RangeRequested;
    internal BleeckerDatePicker FromPicker => from ?? throw new InvalidOperationException("Range picker is not templated.");
    internal BleeckerDatePicker ToPicker => to ?? throw new InvalidOperationException("Range picker is not templated.");
    internal BleeckerButton? ClearAction => clear;
    protected override void OnApplyTemplate()
    {
        Detach(); base.OnApplyTemplate(); host = GetTemplateChild("PART_Root") as Grid ?? throw new InvalidOperationException("Range picker requires PART_Root.");
        layout = new RangePanel(); host.Children.Add(layout);
        var card = (Border)XamlReader.Load("<Border xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' MinWidth='360' Padding='12' BorderThickness='1' CornerRadius='12' Background='{ThemeResource BleeckerDateSurfaceBrush}' BorderBrush='{ThemeResource BleeckerDateRangeBorderBrush}' UseLayoutRounding='False'/>");
        card.Resources.MergedDictionaries.Add(palette);
        var columns = new Grid { ColumnSpacing = 8, UseLayoutRounding = false }; columns.ColumnDefinitions.Add(new()); columns.ColumnDefinitions.Add(new() { Width = GridLength.Auto }); columns.ColumnDefinitions.Add(new()); card.Child = columns;
        from = new BleeckerDatePicker(now) { Placeholder = "Start date", RangeAppearance = true }; to = new BleeckerDatePicker(now) { Placeholder = "End date", RangeAppearance = true };
        AutomationProperties.SetName(from, "Start date"); AutomationProperties.SetName(to, "End date");
        from.DateRequested += FromRequested; to.DateRequested += ToRequested;
        columns.Children.Add(Field("FROM", from)); var second = Field("TO", to); Grid.SetColumn(second, 2); columns.Children.Add(second);
        var dash = (Border)XamlReader.Load("<Border xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Width='12' Height='1' Margin='0,0,0,20' VerticalAlignment='Bottom' Background='{ThemeResource BleeckerDateRangeDashBrush}'/>"); Grid.SetColumn(dash, 1); columns.Children.Add(dash); layout.Children.Add(card);
        clear = new BleeckerButton { Size = BleeckerButtonSize.Small, Variant = BleeckerButtonVariant.Ghost };
        var clearContent = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var clearText = new TextBlock { Text = "Clear", VerticalAlignment = VerticalAlignment.Center };
        clearContent.Children.Add(BleeckerDateVisual.Icon("M18 6L6 18 M6 6L18 18", 14, clearText, nameof(Foreground))); clearContent.Children.Add(clearText); clear.Content = clearContent;
        AutomationProperties.SetName(clear, "Clear date range"); clear.Click += ClearRequested; layout.Children.Add(clear);
        presetHost = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center, UseLayoutRounding = false };
        presetBorder = (Border)XamlReader.Load("<Border xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Height='40' Padding='4' BorderThickness='1' CornerRadius='7' Background='{ThemeResource BleeckerDateSurfaceBrush}' BorderBrush='{ThemeResource BleeckerDatePresetBorderBrush}' UseLayoutRounding='False'/>");
        presetBorder.Resources.MergedDictionaries.Add(BleeckerDateVisual.Palette()); presetBorder.Child = presetHost; layout.Children.Add(presetBorder);
        Observe(); RenderPresets(); Refresh();
    }
    private StackPanel Field(string label, BleeckerDatePicker picker)
    { var field = new StackPanel { Spacing = 6, UseLayoutRounding = false }; field.Children.Add(new TextBlock { Text = label, Style = (Style)palette["BleeckerDateRangeLabelStyle"] }); field.Children.Add(picker); return field; }
    private void Observe() { if (observed is not null) observed.CollectionChanged -= PresetsChanged; observed = Presets as INotifyCollectionChanged; if (observed is not null) observed.CollectionChanged += PresetsChanged; }
    private void PresetsChanged(object? sender, NotifyCollectionChangedEventArgs args) { if (ReferenceEquals(sender, observed)) RenderPresets(); }
    private void FromRequested(object? sender, BleeckerDateRequestedEventArgs args) { if (ReferenceEquals(sender, from)) Request(Value with { From = BleeckerDates.Serialize(args.Value) }); }
    private void ToRequested(object? sender, BleeckerDateRequestedEventArgs args) { if (ReferenceEquals(sender, to)) Request(Value with { To = BleeckerDates.Serialize(args.Value) }); }
    private void ClearRequested(object sender, RoutedEventArgs args) { if (ReferenceEquals(sender, clear) && clear?.Visibility == Visibility.Visible && (BleeckerDates.Parse(Value.From) is not null || BleeckerDates.Parse(Value.To) is not null)) Request(new()); }
    private void Request(BleeckerDateRange value) { if (IsLoaded && IsEnabled) RangeRequested?.Invoke(this, new(value)); }
    private void Refresh()
    {
        var value = Value; var start = BleeckerDates.Parse(value.From); var end = BleeckerDates.Parse(value.To);
        if (from is null || to is null || clear is null) return;
        from.Value = start; from.MaxDate = end; to.Value = end; to.MinDate = start;
        clear.Visibility = start is null && end is null ? Visibility.Collapsed : Visibility.Visible;
        layout?.InvalidateMeasure();
    }
    private void RenderPresets()
    {
        if (presetHost is null || presetBorder is null) return;
        var previous = presetHost.Children.OfType<BleeckerButton>().Where(button => button.Tag is BleeckerDatePreset).GroupBy(button => ((BleeckerDatePreset)button.Tag).Label).ToDictionary(group => group.Key, group => new Queue<BleeckerButton>(group));
        var ordered = new List<BleeckerButton>();
        foreach (var preset in Presets ?? [])
        {
            ArgumentNullException.ThrowIfNull(preset); ArgumentNullException.ThrowIfNull(preset.Label);
            BleeckerButton button;
            if (previous.TryGetValue(preset.Label, out var available) && available.Count > 0) button = available.Dequeue();
            else
            {
                button = new BleeckerButton { Size = BleeckerButtonSize.ExtraSmall, Variant = BleeckerButtonVariant.Ghost };
                button.Click += (_, _) => { if (IsLoaded && IsEnabled && button.IsLoaded && presetHost is { } active && active.Children.Contains(button) && button.Tag is BleeckerDatePreset current && (Presets?.Any(item => ReferenceEquals(item, current)) ?? false)) Request(BleeckerDates.Preset(now(), current.Days)); };
            }
            button.Content = preset.Label; button.Tag = preset; ordered.Add(button);
        }
        for (var i = presetHost.Children.Count - 1; i >= 0; i--) if (presetHost.Children[i] is not BleeckerButton candidate || !ordered.Contains(candidate)) presetHost.Children.RemoveAt(i);
        for (var i = 0; i < ordered.Count; i++) if (i >= presetHost.Children.Count || !ReferenceEquals(presetHost.Children[i], ordered[i])) { presetHost.Children.Remove(ordered[i]); presetHost.Children.Insert(i, ordered[i]); }
        presetBorder.Visibility = presetHost.Children.Count == 0 ? Visibility.Collapsed : Visibility.Visible; layout?.InvalidateMeasure();
    }
    private void Detach()
    {
        if (from is not null) from.DateRequested -= FromRequested; if (to is not null) to.DateRequested -= ToRequested;
        if (clear is not null) clear.Click -= ClearRequested;
        if (observed is not null) observed.CollectionChanged -= PresetsChanged; observed = null;
        host?.Children.Clear(); host = null; layout = null; from = to = null; clear = null; presetBorder = null; presetHost = null;
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new Peer(this);
    private sealed class Peer(BleeckerDateRangePicker owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
        protected override string GetClassNameCore() => nameof(BleeckerDateRangePicker);
        protected override string GetNameCore() { var name = base.GetNameCore(); return string.IsNullOrEmpty(name) ? owner.Placeholder : name; }
    }
    private sealed class RangePanel : Panel
    {
        private readonly List<(UIElement Child, Rect Bounds)> arranged = [];
        protected override Size MeasureOverride(Size availableSize)
        {
            arranged.Clear(); var visible = Children.OfType<FrameworkElement>().Where(x => x.Visibility == Visibility.Visible).ToArray();
            if (visible.Length == 0) return new();
            foreach (var child in visible) child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var width = double.IsFinite(availableSize.Width) ? availableSize.Width : visible.Sum(child => child.DesiredSize.Width) + 12 * (visible.Length - 1);
            var rows = new List<List<FrameworkElement>> { new() }; var occupied = 0d;
            foreach (var child in visible) { var minimum = ReferenceEquals(child, visible[0]) ? 360 : child.DesiredSize.Width; if (rows[^1].Count > 0 && occupied + 12 + minimum > width) { rows.Add([]); occupied = 0; } rows[^1].Add(child); occupied += (occupied == 0 ? 0 : 12) + minimum; }
            var y = 0d;
            foreach (var row in rows)
            {
                var cardInRow = ReferenceEquals(row[0], visible[0]); var cardWidth = cardInRow ? Math.Max(360, width - row.Skip(1).Sum(x => x.DesiredSize.Width + 12)) : 0;
                if (cardInRow) row[0].Measure(new Size(cardWidth, double.PositiveInfinity));
                var height = row.Max(x => x.DesiredSize.Height); var x = 0d;
                foreach (var child in row) { var itemWidth = ReferenceEquals(child, visible[0]) ? cardWidth : child.DesiredSize.Width; arranged.Add((child, new Rect(x, y + height - child.DesiredSize.Height, itemWidth, child.DesiredSize.Height))); x += itemWidth + 12; }
                y += height + 12;
            }
            return new Size(width, Math.Max(0, y - 12));
        }
        protected override Size ArrangeOverride(Size finalSize) { foreach (var (child, bounds) in arranged) child.Arrange(bounds); return finalSize; }
    }
}
