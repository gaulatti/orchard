using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Globalization.DateTimeFormatting;

namespace Orchard;

/// <summary>Caller-owned collection filter and sort requests over native controls.</summary>
public sealed partial class BleeckerCollectionFilters : Control
{
    private Grid? host;
    private readonly Microsoft.UI.Dispatching.DispatcherQueue uiQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread() ?? throw new InvalidOperationException("CollectionFilters requires a UI dispatcher.");
    private Border? surface;
    private Grid? frame;
    private readonly List<BleeckerCssShadow> shadows = [];
    private StackPanel? sections;
    private Border? header, chipsSurface, body;
    private FlowRow? headerGroups, leftGroup, sortGroup, chips;
    private FieldGrid? fields;
    private BleeckerButton? toggle;
    private Button? clear;
    private BleeckerSelect? sort;
    private BleeckerIconButton? direction;
    private Border? count;
    private BleeckerContentText? countText, sortLabel;
    private ContentControl? directionIcon;
    private bool capturedDefault, expanded, rendering;
    private XamlRoot? observedRoot;
    private readonly List<INotifyCollectionChanged> observedCollections = [];
    private readonly List<INotifyPropertyChanged> observedItems = [];
    private readonly List<FieldSlot> fieldSlots = [];
    private readonly Dictionary<string, Button> chipSlots = new(StringComparer.Ordinal);
    private readonly Dictionary<BleeckerCollectionSortOption, BleeckerSelectOption> sortSlots = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<string, TextBlock> paintSources = [];
    private readonly Dictionary<string, ResourceDictionary> actionStyles = [];
    private Style ActionStyle(string role) => (Style)(actionStyles.TryGetValue(role, out var resources) ? resources : actionStyles[role] = new() { Source = new Uri($"ms-appx:///Orchard/Themes/CollectionFilters/{role}.xaml") })[typeof(Button)];

    public BleeckerCollectionFilters()
    {
        DefaultStyleKey = typeof(BleeckerCollectionFilters); IsTabStop = false; UseLayoutRounding = false;
        FilterOptions = new ObservableCollection<BleeckerCollectionFilterOption>(); SortOptions = new ObservableCollection<BleeckerCollectionSortOption>();
        CurrentFilters = new ReadOnlyDictionary<string, BleeckerCollectionFilterValue>(new Dictionary<string, BleeckerCollectionFilterValue>());
        Loaded += (_, _) => { if (!capturedDefault) { expanded = DefaultExpanded; capturedDefault = true; } Observe(); Render(); };
        Unloaded += (_, _) => Disconnect();
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) Detach(); });
        foreach (var property in new[] { FlowDirectionProperty, LanguageProperty, IsEnabledProperty }) RegisterPropertyChangedCallback(property, (_, _) => Render());
    }
    private static DependencyProperty Property(string name, Type type, object? value) => DependencyProperty.Register(name, type, typeof(BleeckerCollectionFilters), new PropertyMetadata(value, (owner, _) => { var control = (BleeckerCollectionFilters)owner; if (control.IsLoaded) control.Observe(); control.Render(); }));
    public static readonly DependencyProperty FilterOptionsProperty = Property(nameof(FilterOptions), typeof(object), null);
    public static readonly DependencyProperty SortOptionsProperty = Property(nameof(SortOptions), typeof(object), null);
    public static readonly DependencyProperty CurrentFiltersProperty = Property(nameof(CurrentFilters), typeof(object), null);
    public static readonly DependencyProperty CurrentSortProperty = Property(nameof(CurrentSort), typeof(object), new BleeckerSortState("", BleeckerSortOrder.Ascending));
    public static readonly DependencyProperty DefaultExpandedProperty = Property(nameof(DefaultExpanded), typeof(bool), false);
    public IEnumerable<BleeckerCollectionFilterOption> FilterOptions { get => GetValue(FilterOptionsProperty) as IEnumerable<BleeckerCollectionFilterOption> ?? throw new InvalidOperationException("FilterOptions must resolve before mounting."); set => SetValue(FilterOptionsProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public IEnumerable<BleeckerCollectionSortOption> SortOptions { get => GetValue(SortOptionsProperty) as IEnumerable<BleeckerCollectionSortOption> ?? throw new InvalidOperationException("SortOptions must resolve before mounting."); set => SetValue(SortOptionsProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public IReadOnlyDictionary<string, BleeckerCollectionFilterValue> CurrentFilters { get => GetValue(CurrentFiltersProperty) as IReadOnlyDictionary<string, BleeckerCollectionFilterValue> ?? throw new InvalidOperationException("CurrentFilters must resolve before mounting."); set => SetValue(CurrentFiltersProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public BleeckerSortState CurrentSort { get => (BleeckerSortState)GetValue(CurrentSortProperty); set { ArgumentNullException.ThrowIfNull(value.Field); if (!Enum.IsDefined(value.Order)) throw new ArgumentOutOfRangeException(nameof(value)); SetValue(CurrentSortProperty, value); } }
    public bool DefaultExpanded { get => (bool)GetValue(DefaultExpandedProperty); set => SetValue(DefaultExpandedProperty, value); }
    public bool IsExpanded => capturedDefault ? expanded : DefaultExpanded;
    public event EventHandler<BleeckerCollectionFiltersRequestedEventArgs>? FiltersRequested;
    public event EventHandler<BleeckerCollectionSortRequestedEventArgs>? SortRequested;

    protected override void OnApplyTemplate()
    {
        Detach(); base.OnApplyTemplate(); if (Template is null) return;
        host = GetTemplateChild("PART_Layout") as Grid ?? throw new InvalidOperationException("CollectionFilters requires PART_Layout.");
        surface = GetTemplateChild("PART_Surface") as Border ?? throw new InvalidOperationException("CollectionFilters requires PART_Surface.");
        frame = GetTemplateChild("PART_Frame") as Grid ?? throw new InvalidOperationException("CollectionFilters requires PART_Frame.");
        foreach (var (id, recipe) in new[] { (1, new BleeckerCssShadow.Recipe(2, 1, 0, .025f, 26, 55, 77)), (2, new BleeckerCssShadow.Recipe(30, 10, -18, .18f, 26, 55, 77)) })
        {
            var shadow = new BleeckerCssShadow(this, surface, (Grid)GetTemplateChild("CollectionShadowHost" + id), (Microsoft.UI.Xaml.Shapes.Path)GetTemplateChild("CollectionShadowMask" + id), (Microsoft.UI.Xaml.Shapes.Path)GetTemplateChild("CollectionShadowExterior" + id)); shadow.Set(recipe); shadows.Add(shadow);
        }
        sections = new() { Spacing = 0, UseLayoutRounding = false, FlowDirection = FlowDirection.LeftToRight }; host.Children.Add(sections);
        headerGroups = new(this, 12, true); leftGroup = new(this, 8); sortGroup = new(this, 8);
        toggle = new() { Name = "CollectionToggle", Size = BleeckerButtonSize.Small };
        toggle.Click += (sender, _) => { if (sender is not FrameworkElement action || !CanRequest(action)) return; SetExpanded(!IsExpanded); };
        clear = new() { Name = "CollectionClear", Style = ActionStyle("Clear"), Content = Text("CollectionClearText", "Clear all", 12, 18, true), Margin = new(4, 0, 0, 0) };
        clear.Click += (sender, _) => { if (sender is not FrameworkElement action || !CanRequest(action)) return; FiltersRequested?.Invoke(this, new(new ReadOnlyDictionary<string, BleeckerCollectionFilterValue>(new Dictionary<string, BleeckerCollectionFilterValue>()))); Render(); };
        AutomationProperties.SetName(clear, "Clear all"); BindPaint(((BleeckerContentText)clear.Content).Text, TextBlock.ForegroundProperty, "BleeckerTextSecondaryBrush");
        leftGroup.Children.Add(toggle); leftGroup.Children.Add(clear);
        sortLabel = Text("CollectionSortLabel", "SORT", 11, 16.5, true); sortLabel.Text.CharacterSpacing = 80;
        sort = new() { Name = "CollectionSort", Width = 176 }; AutomationProperties.SetName(sort, "Sort collection by");
        sort.ValueRequested += (sender, request) => { if (sender is not FrameworkElement action || !CanRequest(action)) return; SortRequested?.Invoke(this, new(BleeckerCollectionFiltersModel.Field(CurrentSort, request.Value))); Render(); };
        directionIcon = Icon("m21 16-4 4-4-4 M17 20V4 m-14 4 4-4 4 4 M7 4v16", 16, "BleeckerTextPrimaryBrush");
        direction = new() { Name = "CollectionDirection", Variant = BleeckerIconButtonVariant.Ghost, Size = BleeckerIconButtonSize.Medium, Content = directionIcon };
        direction.Click += (sender, _) => { if (sender is not FrameworkElement action || !CanRequest(action)) return; SortRequested?.Invoke(this, new(BleeckerCollectionFiltersModel.Toggle(CurrentSort))); Render(); };
        sortGroup.Children.Add(sortLabel); sortGroup.Children.Add(sort); sortGroup.Children.Add(direction);
        headerGroups.Children.Add(leftGroup); headerGroups.Children.Add(sortGroup);
        header = new() { Name = "CollectionHeader", Child = headerGroups, MinHeight = 56, UseLayoutRounding = false }; sections.Children.Add(header);
        chips = new(this, 8); chipsSurface = Surface("CollectionActive", chips, "CollectionActiveBackground", "CollectionActiveBorder"); sections.Children.Add(chipsSurface);
        var introduction = new StackPanel { Spacing = 2, Margin = new(0, 0, 0, 16), UseLayoutRounding = false };
        introduction.Children.Add(Text("CollectionHeading", "Refine results", 14, 20, true)); introduction.Children.Add(Text("CollectionHint", "Selections apply immediately.", 12, 16, false, true));
        var bodyContent = new StackPanel { Spacing = 0, UseLayoutRounding = false }; bodyContent.Children.Add(introduction); fields = new(this); bodyContent.Children.Add(fields);
        body = Surface("CollectionBody", bodyContent, "CollectionBodyBackground", "CollectionBodyBorder"); sections.Children.Add(body);
        if (IsLoaded) Observe(); Render();
    }

    private bool CanRequest(FrameworkElement? action)
    {
        if (!IsLoaded || !IsEnabled || host is null || frame?.Visibility != Visibility.Visible || action?.IsLoaded != true) return false;
        for (DependencyObject? node = action; node is not null; node = VisualTreeHelper.GetParent(node))
        {
            if (node is FrameworkElement element && element.Visibility != Visibility.Visible) return false;
            if (ReferenceEquals(node, host)) return true;
        }
        return false;
    }
    private void RequestFilter(string field, BleeckerCollectionFilterValue? value, FrameworkElement action)
    {
        if (!CanRequest(action) || !FilterOptions.Any() && !SortOptions.Any()) return;
        FiltersRequested?.Invoke(this, new(BleeckerCollectionFiltersModel.Change(CurrentFilters, field, value))); Render();
    }
    private Border Surface(string name, UIElement child, string background, string border) => (Border)BuildSurface(name, child, background, border);
    private static FrameworkElement BuildSurface(string name, UIElement child, string background, string border)
    {
        var surface = (Border)XamlReader.Load($"<Border xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{{ThemeResource {background}}}' BackgroundSizing='OuterBorderEdge' BorderBrush='{{ThemeResource {border}}}' BorderThickness='0,1,0,0' UseLayoutRounding='False'/>"); surface.Name = name; surface.Child = child; return surface;
    }
    private static BleeckerContentText Text(string name, string value, double size, double line, bool medium = false, bool secondary = false)
    {
        var text = new BleeckerContentText(name, secondary ? "BleeckerSecondaryFontFamily" : "BleeckerPrimaryFontFamily", size, line, medium ? "BleeckerTextPrimaryBrush" : "BleeckerTextSecondaryBrush", medium) { NoWrap = true };
        if (medium) text.Text.FontFamily = new("ms-appx:///Orchard/Assets/Fonts/EncodeSans-Medium.ttf#Encode Sans"); text.Set(value, null); return text;
    }
    private static ContentControl Icon(string data, double size, string brush)
    {
        var control = (ContentControl)XamlReader.Load($"<ContentControl xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Foreground='{{ThemeResource {brush}}}' IsTabStop='False' AutomationProperties.AccessibilityView='Raw'/>");
        var path = (Microsoft.UI.Xaml.Shapes.Path)XamlReader.Load($"<Path xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Data='{data}' StrokeThickness='2' StrokeStartLineCap='Round' StrokeEndLineCap='Round' StrokeLineJoin='Round'/>");
        path.SetBinding(Microsoft.UI.Xaml.Shapes.Shape.StrokeProperty, new Binding { Source = control, Path = new PropertyPath("Foreground") });
        var canvas = new Canvas { Width = 24, Height = 24 }; canvas.Children.Add(path); control.Content = new Viewbox { Width = size, Height = size, Child = canvas }; control.Width = control.Height = size; return control;
    }
    private void BindPaint(FrameworkElement target, DependencyProperty property, string key)
    {
        if (!paintSources.TryGetValue(key, out var probe))
        {
            probe = (TextBlock)XamlReader.Load($"<TextBlock xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Foreground='{{ThemeResource {key}}}' Visibility='Collapsed' AutomationProperties.AccessibilityView='Raw'/>"); paintSources.Add(key, probe); host!.Children.Add(probe);
        }
        target.SetBinding(property, new Binding { Source = probe, Path = new PropertyPath("Foreground") });
    }
    private void Render()
    {
        if (rendering || sections is null || host is null || surface is null) return; rendering = true;
        try
        {
            var filterOptions = FilterOptions.ToArray(); var sortOptions = SortOptions.ToArray(); BleeckerCollectionFiltersModel.Validate(CurrentFilters);
            foreach (var option in filterOptions) if (option is null || option.Field is null || option.Label is null || !Enum.IsDefined(option.Type)) throw new ArgumentException("Filter options require original fields, labels and known types.");
            foreach (var option in sortOptions) if (option is null || option.Field is null || option.Label is null) throw new ArgumentException("Sort options require original fields and labels.");
            var visible = filterOptions.Length != 0 || sortOptions.Length != 0; frame!.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            foreach (var action in new Control[] { toggle!, clear!, sort!, direction! }) action.IsEnabled = visible && IsEnabled;
            if (!visible) { foreach (var slot in fieldSlots) slot.Deactivate(); fields!.Children.Clear(); return; }
            var width = XamlRoot?.Size.Width ?? 0; var padding = BleeckerCollectionFiltersModel.HorizontalPadding(width);
            header!.Padding = new(padding, 12, padding, 12); chipsSurface!.Padding = new(padding, 10, padding, 10); body!.Padding = new(padding, 20, padding, 20);
            toggle!.Variant = IsExpanded ? BleeckerButtonVariant.Subtle : BleeckerButtonVariant.Outline;
            var toggleContent = toggle.Content as StackPanel;
            if (toggleContent is null)
            {
                toggleContent = new() { Orientation = Orientation.Horizontal, Spacing = 8, UseLayoutRounding = false };
                toggleContent.Children.Add(Icon("M10 20a1 1 0 0 0 .553.895l2 1A1 1 0 0 0 14 21v-7a2 2 0 0 1 .517-1.341L21.74 4.67A1 1 0 0 0 21 3H3a1 1 0 0 0-.742 1.67l7.225 7.989A2 2 0 0 1 10 14z", 15, "BleeckerSeaBrush"));
                toggleContent.Children.Add(Text("CollectionToggleText", "Filters", 13, 19.5, true));
                countText = Text("CollectionCountText", "", 11, 16.5); countText.Text.FontFamily = (FontFamily)Application.Current.Resources["BleeckerPrimarySemiboldFontFamily"]; Typography.SetNumeralAlignment(countText.Text, FontNumeralAlignment.Tabular);
                count = (Border)XamlReader.Load("<Border xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{ThemeResource CollectionCountBackground}' Height='20' MinWidth='20' Padding='6,0' CornerRadius='5' Margin='2,0,0,0'/>"); count.Child = countText; toggleContent.Children.Add(count); toggle.Content = toggleContent;
            }
            var toggleBrush = IsExpanded ? "CollectionExpandedForeground" : "BleeckerSeaBrush";
            BindPaint((ContentControl)toggleContent.Children[0], ForegroundProperty, toggleBrush); BindPaint(((BleeckerContentText)toggleContent.Children[1]).Text, TextBlock.ForegroundProperty, toggleBrush);
            countText!.Set(CurrentFilters.Count.ToString(System.Globalization.CultureInfo.InvariantCulture), null); count!.Visibility = CurrentFilters.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            BindPaint(countText.Text, TextBlock.ForegroundProperty, "CollectionCountForeground");
            AutomationProperties.SetName(toggle, "Filters" + (CurrentFilters.Count > 0 ? " " + CurrentFilters.Count : "")); AutomationProperties.SetHelpText(toggle, IsExpanded ? "Expanded" : "Collapsed");
            clear!.Visibility = CurrentFilters.Count == 0 ? Visibility.Collapsed : Visibility.Visible; sortGroup!.Visibility = sortOptions.Length == 0 ? Visibility.Collapsed : Visibility.Visible; sortLabel!.Visibility = width >= 640 ? Visibility.Visible : Visibility.Collapsed;
            foreach (var original in sortOptions) if (!sortSlots.ContainsKey(original)) sortSlots.Add(original, new(original.Field, original.Label));
            foreach (var stale in sortSlots.Keys.Where(key => !sortOptions.Any(option => ReferenceEquals(key, option))).ToArray()) sortSlots.Remove(stale);
            var selectOptions = sortOptions.Select(option => sortSlots[option]).ToArray(); if (!sort!.Options.SequenceEqual(selectOptions, ReferenceEqualityComparer.Instance)) sort.Options = selectOptions;
            sort.Value = CurrentSort.Field; var descending = CurrentSort.Order == BleeckerSortOrder.Descending;
            AutomationProperties.SetName(direction!, descending ? "Sort ascending" : "Sort descending"); ToolTipService.SetToolTip(direction!, descending ? "Sort Ascending" : "Sort Descending"); directionIcon!.RenderTransformOrigin = new(.5, .5); directionIcon.RenderTransform = new RotateTransform { Angle = descending ? 180 : 0 };
            chipsSurface.Visibility = CurrentFilters.Count == 0 ? Visibility.Collapsed : Visibility.Visible; UpdateChips(filterOptions);
            body.Visibility = IsExpanded ? Visibility.Visible : Visibility.Collapsed;
            if (IsExpanded) UpdateFields(filterOptions); else { foreach (var slot in fieldSlots) slot.Deactivate(); fields!.Children.Clear(); }
            foreach (var element in new FrameworkElement[] { toggle, clear, sort, direction! }) element.FlowDirection = FlowDirection;
            chips!.FlowDirection = fields!.FlowDirection = FlowDirection.LeftToRight;
            headerGroups!.InvalidateMeasure(); chips!.InvalidateMeasure(); fields!.InvalidateMeasure(); InvalidateMeasure();
        }
        finally { rendering = false; }
    }
    private void UpdateChips(BleeckerCollectionFilterOption[] options)
    {
        foreach (var field in chipSlots.Keys.Where(field => !CurrentFilters.ContainsKey(field)).ToArray()) { chips!.Children.Remove(chipSlots[field]); chipSlots.Remove(field); }
        var formatter = new DateTimeFormatter("day month.abbreviated year.full", new[] { Language }); var index = 0;
        foreach (var (field, value) in BleeckerCollectionFiltersModel.Entries(CurrentFilters))
        {
            if (!chipSlots.TryGetValue(field, out var button))
            {
                button = new() { Name = "CollectionChip", Style = ActionStyle("Chip") }; var action = button;
                button.Click += (_, _) => { if (CurrentFilters.ContainsKey(field)) RequestFilter(field, null, action); };
                var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, UseLayoutRounding = false };
                row.Children.Add(Text("CollectionChipLabel", "", 12, 18, true)); row.Children.Add(Text("CollectionChipValue", "", 12, 18));
                var icon = Icon("M18 6 6 18 M6 6l12 12", 12, "BleeckerTextSecondaryBrush"); icon.Margin = new(2, 0, 0, 0); row.Children.Add(icon); button.Content = row; chipSlots.Add(field, button);
            }
            var option = options.FirstOrDefault(option => option.Field == field); var label = option?.Label ?? field; var rowContent = (StackPanel)button.Content;
            // Windows inserts LRM separators into its formatted fields. The
            // source Intl string is unmarked; keep those interop controls out
            // of both the chip value and its shaping input.
            ((BleeckerContentText)rowContent.Children[0]).Set(label, null); ((BleeckerContentText)rowContent.Children[1]).Set(BleeckerCollectionFiltersModel.Display(option, value, date => formatter.Format(date).Replace("\u200e", "", StringComparison.Ordinal)), null);
            AutomationProperties.SetName(button, "Remove " + label + " filter"); button.Tag = field;
            button.FlowDirection = FlowDirection;
            var current = chips!.Children.IndexOf(button); if (current != index) { if (current >= 0) chips.Children.RemoveAt(current); chips.Children.Insert(index, button); } index++;
        }
    }
    private void UpdateFields(BleeckerCollectionFilterOption[] options)
    {
        var unused = new List<FieldSlot>(fieldSlots); var next = new List<FieldSlot>();
        foreach (var option in options)
        {
            var slot = unused.FirstOrDefault(slot => ReferenceEquals(slot.Option, option));
            if (slot is null) slot = new(this, option); else unused.Remove(slot); next.Add(slot); slot.Update();
        }
        foreach (var stale in unused) fields!.Children.Remove(stale);
        for (var index = 0; index < next.Count; index++) { var current = fields!.Children.IndexOf(next[index]); if (current == index) continue; if (current >= 0) fields.Children.RemoveAt(current); fields.Children.Insert(index, next[index]); }
        fieldSlots.Clear(); fieldSlots.AddRange(next);
    }
    private void Observe()
    {
        Disconnect(); observedRoot = XamlRoot; if (observedRoot is not null) observedRoot.Changed += RootChanged;
        var sources = new List<object> { FilterOptions, SortOptions, CurrentFilters };
        foreach (var option in FilterOptions) if (option.Options is not null) { sources.Add(option.Options); sources.AddRange(option.Options); }
        foreach (var source in sources.Distinct(ReferenceEqualityComparer.Instance))
        {
            if (source is INotifyCollectionChanged collection) { collection.CollectionChanged += ProducerChanged; observedCollections.Add(collection); }
            if (source is INotifyPropertyChanged item) { item.PropertyChanged += ItemChanged; observedItems.Add(item); }
        }
    }
    private void ProducerChanged(object? sender, NotifyCollectionChangedEventArgs args) => DispatchProducer();
    private void ItemChanged(object? sender, PropertyChangedEventArgs args) => DispatchProducer();
    private void DispatchProducer() { if (uiQueue.HasThreadAccess) { if (IsLoaded) { Observe(); Render(); } } else if (!uiQueue.TryEnqueue(() => { if (IsLoaded) { Observe(); Render(); } })) throw new InvalidOperationException("CollectionFilters could not dispatch its producer update."); }
    private void RootChanged(XamlRoot sender, XamlRootChangedEventArgs args) => Render();
    private void SetExpanded(bool value)
    {
        var previous = IsExpanded; expanded = value; capturedDefault = true; Render();
        if (previous != value) FrameworkElementAutomationPeer.FromElement(this)?.RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty, previous ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed, value ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
    }
    private void Disconnect() { if (observedRoot is not null) observedRoot.Changed -= RootChanged; observedRoot = null; foreach (var source in observedCollections) source.CollectionChanged -= ProducerChanged; observedCollections.Clear(); foreach (var item in observedItems) item.PropertyChanged -= ItemChanged; observedItems.Clear(); }
    private void Detach() { Disconnect(); foreach (var shadow in shadows) shadow.Dispose(); shadows.Clear(); host?.Children.Clear(); host = null; surface = null; frame = null; sections = null; fieldSlots.Clear(); chipSlots.Clear(); sortSlots.Clear(); paintSources.Clear(); }
    protected override AutomationPeer OnCreateAutomationPeer() => new CollectionPeer(this);
    private sealed class CollectionPeer(BleeckerCollectionFilters owner) : FrameworkElementAutomationPeer(owner), IExpandCollapseProvider
    {
        protected override object? GetPatternCore(PatternInterface pattern) => pattern == PatternInterface.ExpandCollapse ? this : base.GetPatternCore(pattern);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
        protected override bool IsControlElementCore() => owner.frame?.Visibility == Visibility.Visible && base.IsControlElementCore();
        protected override bool IsContentElementCore() => owner.frame?.Visibility == Visibility.Visible && base.IsContentElementCore();
        public ExpandCollapseState ExpandCollapseState => owner.IsExpanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
        public void Expand() => Set(true);
        public void Collapse() => Set(false);
        private void Set(bool value) { if (!owner.IsEnabled) throw new ElementNotEnabledException(); if (!owner.CanRequest(owner.toggle)) return; owner.SetExpanded(value); }
    }
}
