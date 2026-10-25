using System.Collections.Specialized;
using System.Globalization;
using System.Windows.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;

namespace Orchard;

/// <summary>A caller-owned media grid with controlled search and an original-item preview.</summary>
[ContentProperty(Name = nameof(Items))]
public sealed partial class BleeckerMediaLibrary : Control
{
    private readonly Dictionary<string, Tile> tiles = new(StringComparer.Ordinal);
    private readonly HashSet<BleeckerMediaLibraryItem> observedItems = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<ICommand> commands = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<string, TextBlock> palette = [];
    private readonly List<(DependencyObject Owner, DependencyProperty Property, long Token)> observers = [];
    private IReadOnlyList<BleeckerMediaLibraryItem> items = [];
    private INotifyCollectionChanged? collection;
    private Grid? templateRoot;
    private MediaPanel? panel;
    private BleeckerModal? modal;
    private Preview? preview;
    private XamlRoot? observedRoot;
    private BleeckerModalHost? inputHost;
    private IDisposable? popupObservation;
    private BleeckerMediaLibraryItem? selected;
    private EventHandler<string>? deleteRequested;
    private bool rendering, pending, updatingSearch, themeQueued;
    private int searchRevision;
    private Func<string, string>? createdAtFormatter;
    internal double RemScale => FontSize / 16;
    internal bool HasCallerFontFamily { get { if (ReadLocalValue(FontFamilyProperty) != DependencyProperty.UnsetValue) return true; for (var style = Style; style is not null; style = style.BasedOn) if (style.Setters.OfType<Setter>().Any(s => s.Property == FontFamilyProperty)) return true; return false; } }

    public BleeckerMediaLibrary()
    {
        DefaultStyleKey = typeof(BleeckerMediaLibrary); IsTabStop = false; UseLayoutRounding = false;
        Items = new BleeckerMediaLibraryItems();
        Loaded += LoadedComponent; Unloaded += UnloadedComponent;
        ActualThemeChanged += (_, _) => { if (themeQueued) return; themeQueued = true; if (!DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => { themeQueued = false; Refresh(); })) throw new InvalidOperationException("Media Library theme update was rejected."); };
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) Detach(); });
        foreach (var property in new[] { FontSizeProperty, FontFamilyProperty, ForegroundProperty, FlowDirectionProperty, LanguageProperty, IsTextScaleFactorEnabledProperty, IsEnabledProperty }) RegisterPropertyChangedCallback(property, (_, _) => Refresh());
    }
    private static DependencyProperty Property(string name, Type type, object? value = null) => DependencyProperty.Register(name, type, typeof(BleeckerMediaLibrary), new PropertyMetadata(value, (owner, _) => ((BleeckerMediaLibrary)owner).Refresh()));
    public static readonly DependencyProperty ItemsProperty = Property(nameof(Items), typeof(object));
    public static readonly DependencyProperty TitleProperty = Property(nameof(Title), typeof(string), "");
    public static readonly DependencyProperty SearchTermProperty = Property(nameof(SearchTerm), typeof(string), "");
    public static readonly DependencyProperty SearchPlaceholderProperty = Property(nameof(SearchPlaceholder), typeof(string), "Search media...");
    public static readonly DependencyProperty EmptyMessageProperty = Property(nameof(EmptyMessage), typeof(string), "No media found.");
    public static readonly DependencyProperty ActionsProperty = Property(nameof(Actions), typeof(object));
    public static readonly DependencyProperty ActionsTemplateProperty = Property(nameof(ActionsTemplate), typeof(DataTemplate));
    public static readonly DependencyProperty FilterProperty = Property(nameof(Filter), typeof(object));
    public static readonly DependencyProperty FilterTemplateProperty = Property(nameof(FilterTemplate), typeof(DataTemplate));
    public static readonly DependencyProperty SearchCommandProperty = Property(nameof(SearchCommand), typeof(ICommand));
    public static readonly DependencyProperty CopyCommandProperty = Property(nameof(CopyCommand), typeof(ICommand));
    public static readonly DependencyProperty DeleteCommandProperty = Property(nameof(DeleteCommand), typeof(ICommand));
    public static readonly DependencyProperty UrlCommandProperty = Property(nameof(UrlCommand), typeof(ICommand));
    public static readonly DependencyProperty PreviewSurfaceStyleProperty = Property(nameof(PreviewSurfaceStyle), typeof(Style));
    public static readonly DependencyProperty TileStyleProperty = Property(nameof(TileStyle), typeof(Style));
    public IEnumerable<BleeckerMediaLibraryItem> Items { get => (IEnumerable<BleeckerMediaLibraryItem>)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public string SearchTerm { get => (string)GetValue(SearchTermProperty); set => SetValue(SearchTermProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public string SearchPlaceholder { get => (string)GetValue(SearchPlaceholderProperty); set => SetValue(SearchPlaceholderProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public string EmptyMessage { get => (string)GetValue(EmptyMessageProperty); set => SetValue(EmptyMessageProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public object? Actions { get => GetValue(ActionsProperty); set => SetValue(ActionsProperty, value); }
    public DataTemplate? ActionsTemplate { get => (DataTemplate?)GetValue(ActionsTemplateProperty); set => SetValue(ActionsTemplateProperty, value); }
    public object? Filter { get => GetValue(FilterProperty); set => SetValue(FilterProperty, value); }
    public DataTemplate? FilterTemplate { get => (DataTemplate?)GetValue(FilterTemplateProperty); set => SetValue(FilterTemplateProperty, value); }
    public ICommand? SearchCommand { get => (ICommand?)GetValue(SearchCommandProperty); set => SetValue(SearchCommandProperty, value); }
    public ICommand? CopyCommand { get => (ICommand?)GetValue(CopyCommandProperty); set => SetValue(CopyCommandProperty, value); }
    public ICommand? DeleteCommand { get => (ICommand?)GetValue(DeleteCommandProperty); set => SetValue(DeleteCommandProperty, value); }
    /// <summary>Optional original-string URL routing. Without a command the native hyperlink uses its supplied absolute URI.</summary>
    public ICommand? UrlCommand { get => (ICommand?)GetValue(UrlCommandProperty); set => SetValue(UrlCommandProperty, value); }
    public Style? PreviewSurfaceStyle { get => (Style?)GetValue(PreviewSurfaceStyleProperty); set => SetValue(PreviewSurfaceStyleProperty, value); }
    public Style? TileStyle { get => (Style?)GetValue(TileStyleProperty); set => SetValue(TileStyleProperty, value); }
    /// <summary>Original selected object, retained even when its list entry is removed or replaced.</summary>
    public BleeckerMediaLibraryItem? SelectedItem => selected;
    public bool IsPreviewOpen => selected is not null;
    /// <summary>Optional native clipboard adapter; absent an adapter the Windows clipboard receives the original string.</summary>
    public Action<string>? ClipboardWriter { get; set; }
    /// <summary>Optional caller date formatter; the default uses local culture/time zone and displays Invalid Date for unparseable values.</summary>
    public Func<string, string>? CreatedAtFormatter { get => createdAtFormatter; set { createdAtFormatter = value; Refresh(); } }
    public event EventHandler<string>? SearchTermChangeRequested;
    public event EventHandler<string>? CopyRequested;
    public event EventHandler<string>? DeleteRequested { add { deleteRequested += value; Refresh(); } remove { deleteRequested -= value; Refresh(); } }
    public event EventHandler<BleeckerMediaLibraryItem>? ImageFailed;
    public event EventHandler? PreviewChanged;
    internal bool HasDelete => deleteRequested is not null || DeleteCommand is not null;
    internal SolidColorBrush Brush(string name) => palette.TryGetValue(name, out var part) && part.Foreground is SolidColorBrush brush ? brush : throw new InvalidOperationException("Media Library requires its semantic palette: " + name);
    internal bool EnabledWithin
    {
        get { if (!IsLoaded || panel is null) return false; for (DependencyObject? node = this; node is not null; node = VisualTreeHelper.GetParent(node)) if (node is FrameworkElement e && (e.Visibility != Visibility.Visible || e is Control c && !c.IsEnabled)) return false; return true; }
    }
    internal bool InputAllowed => EnabledWithin && BleeckerPopupScope.AllowsInput(XamlRoot, this) && inputHost?.AllowsFocusTarget(this) != false;
    internal bool PreviewInputAllowed => EnabledWithin && selected is not null && modal?.IsOpen == true && preview is not null && BleeckerPopupScope.AllowsInput(XamlRoot, preview) && inputHost?.AllowsFocusTarget(preview) == true;
    internal void RequestSearch(string value)
    {
        if (updatingSearch || value == SearchTerm) return;
        var revision = ++searchRevision;
        try { if (InputAllowed && SearchCommand?.CanExecute(value) != false) { SearchCommand?.Execute(value); SearchTermChangeRequested?.Invoke(this, value); } }
        finally { UpdateSearch(); if (!DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => { if (IsLoaded && revision == searchRevision) UpdateSearch(); })) throw new InvalidOperationException("Media Library controlled search restore was rejected."); }
    }
    private void UpdateSearch()
    {
        if (panel is null) return; updatingSearch = true;
        try { panel.Search.Text = SearchTerm; panel.Search.Editor.Text = SearchTerm; panel.Search.PlaceholderText = SearchPlaceholder; }
        finally { updatingSearch = false; }
    }
    public void OpenPreview(BleeckerMediaLibraryItem item)
    {
        ArgumentNullException.ThrowIfNull(item); if (!InputAllowed) return;
        if (!items.Any(i => ReferenceEquals(i, item))) throw new ArgumentException("OpenPreview requires an original item currently in this library.", nameof(item));
        selected = item; Refresh(); PreviewChanged?.Invoke(this, EventArgs.Empty);
    }
    public void ClosePreview()
    {
        if (!PreviewInputAllowed) return; selected = null; Refresh(); PreviewChanged?.Invoke(this, EventArgs.Empty);
    }
    private void RequestDelete(Tile tile)
    {
        if (!InputAllowed || !tile.Open.IsLoaded || !tiles.TryGetValue(tile.Item.Id, out var current) || !ReferenceEquals(current, tile) || !HasDelete || DeleteCommand?.CanExecute(tile.Item.Id) == false) return;
        var id = tile.Item.Id; var command = DeleteCommand; var callback = deleteRequested; command?.Execute(id); callback?.Invoke(this, id);
    }
    internal void RequestCopy(string value)
    {
        if (!PreviewInputAllowed || CopyCommand?.CanExecute(value) == false) return;
        var callback = CopyRequested; var command = CopyCommand;
        if (command is not null || callback is not null) { command?.Execute(value); callback?.Invoke(this, value); return; }
        if (ClipboardWriter is { } writer) { writer(value); return; }
        var package = new DataPackage(); package.SetText(value); Clipboard.SetContent(package);
    }
    internal string FormatDate(string value) => CreatedAtFormatter is { } format ? format(value) : DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var date) ? date.ToLocalTime().ToString("d", CultureInfo.CurrentCulture) : "Invalid Date";
    internal void Failed(BleeckerMediaLibraryItem item) => ImageFailed?.Invoke(this, item);
    protected override void OnApplyTemplate()
    {
        Detach(); base.OnApplyTemplate(); if (Template is null) return;
        templateRoot = GetTemplateChild("PART_Root") as Grid ?? throw new InvalidOperationException("Media Library requires PART_Root.");
        foreach (var key in new[] { "Primary", "Secondary", "Desert", "TileBackground", "TileBorder", "TileHoverBorder", "Overlay", "OverlayText", "Delete", "Placeholder", "EmptyBackground", "EmptyBorder", "PreviewBackground", "PreviewBorder", "Link", "Focus" }) palette[key] = GetTemplateChild("PART_" + key) as TextBlock ?? throw new InvalidOperationException("Media Library requires PART_" + key);
        panel = new(this); modal = new() { Name = "MediaPreview" }; preview = new(this); modal.Content = preview; modal.CloseRequested += (_, _) => ClosePreview(); templateRoot.Children.Add(panel); templateRoot.Children.Add(modal); Refresh();
    }
    private void LoadedComponent(object sender, RoutedEventArgs args)
    {
        observedRoot = XamlRoot; if (observedRoot is not null) { observedRoot.Changed += RootChanged; popupObservation = BleeckerPopupScope.Observe(observedRoot, UpdateInput); }
        LayoutUpdated += LayoutComponent; ConnectModalHost(); ObserveAncestors(); Refresh();
    }
    private void UnloadedComponent(object sender, RoutedEventArgs args)
    {
        LayoutUpdated -= LayoutComponent; searchRevision++; selected = null; if (modal is not null) modal.IsOpen = false;
        if (inputHost is not null) inputHost.ScopeChanged -= UpdateInput; inputHost = null;
        if (observedRoot is not null) observedRoot.Changed -= RootChanged; observedRoot = null; popupObservation?.Dispose(); popupObservation = null; Disconnect(); DisconnectAncestors();
        foreach (var tile in tiles.Values) tile.SetOver(false); preview?.Update();
    }
    private void RootChanged(XamlRoot sender, XamlRootChangedEventArgs args) => Refresh();
    private void LayoutComponent(object? sender, object args) => ConnectModalHost();
    private void ConnectModalHost() { var next = BleeckerModalHost.Find(this); if (ReferenceEquals(next, inputHost)) return; if (inputHost is not null) inputHost.ScopeChanged -= UpdateInput; inputHost = next; if (inputHost is not null) inputHost.ScopeChanged += UpdateInput; UpdateInput(); }
    private void Detach()
    {
        foreach (var tile in tiles.Values) tile.Dispose(); tiles.Clear(); panel?.Dispose(); panel = null; preview?.Dispose(); preview = null;
        if (modal is not null) { modal.IsOpen = false; modal.Content = null; } modal = null; templateRoot?.Children.Clear(); templateRoot = null; palette.Clear();
    }
    public void Refresh()
    {
        if (!DispatcherQueue.HasThreadAccess) throw new InvalidOperationException("Media Library data must change on its owning UI dispatcher.");
        if (rendering) { pending = true; return; }
        if (GetValue(ItemsProperty) is not IEnumerable<BleeckerMediaLibraryItem> values) { if (!IsLoaded && GetBindingExpression(ItemsProperty) is not null) return; throw new InvalidOperationException("Media Library requires original items before mounting."); }
        if (!double.IsFinite(FontSize) || FontSize <= 0) throw new ArgumentOutOfRangeException(nameof(FontSize));
        rendering = true;
        try
        {
            items = values.ToArray(); var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in items) { ArgumentNullException.ThrowIfNull(item); if (item.Id is null || item.Filename is null) throw new ArgumentException("Original media IDs and filenames must be non-null."); if (!keys.Add(item.Id)) throw new ArgumentException("Original sibling media IDs must be unique."); }
            Observe(); if (panel is null || modal is null || preview is null) return;
            foreach (var key in tiles.Keys.Where(k => !keys.Contains(k)).ToArray()) { tiles[key].Dispose(); tiles.Remove(key); }
            for (var index = 0; index < items.Count; index++) { var item = items[index]; if (!tiles.TryGetValue(item.Id, out var tile)) { tile = new(this); tiles.Add(item.Id, tile); panel.Children.Add(tile.Root); } tile.Update(item, index); }
            UpdateSearch(); panel.Update(); preview.Update(); modal.Title = string.IsNullOrEmpty(selected?.Filename) ? "Media Preview" : selected.Filename; modal.IsEnabled = EnabledWithin; modal.FlowDirection = FlowDirection; modal.Language = Language; modal.IsTextScaleFactorEnabled = IsTextScaleFactorEnabled; modal.SurfaceStyle = PreviewSurfaceStyle ?? panel.DefaultPreviewStyle; modal.IsOpen = IsLoaded && selected is not null; UpdateInput();
        }
        finally { rendering = false; }
        if (pending) { pending = false; Refresh(); }
    }
    private void UpdateInput() { panel?.UpdateInput(); foreach (var tile in tiles.Values) tile.UpdateInput(); preview?.UpdateInput(); }
    private void Observe()
    {
        Disconnect(); if (!IsLoaded) return; collection = Items as INotifyCollectionChanged; if (collection is not null) collection.CollectionChanged += CollectionChanged;
        foreach (var item in selected is null ? items : items.Append(selected)) if (observedItems.Add(item)) item.Changed += ItemChanged;
        foreach (var command in new[] { SearchCommand, CopyCommand, DeleteCommand, UrlCommand }) if (command is not null && commands.Add(command)) command.CanExecuteChanged += CommandChanged;
    }
    private void Disconnect() { if (collection is not null) collection.CollectionChanged -= CollectionChanged; collection = null; foreach (var item in observedItems) item.Changed -= ItemChanged; observedItems.Clear(); foreach (var command in commands) command.CanExecuteChanged -= CommandChanged; commands.Clear(); }
    private void CollectionChanged(object? sender, NotifyCollectionChangedEventArgs args) => Refresh();
    private void ItemChanged(object? sender, EventArgs args) => Refresh();
    private void CommandChanged(object? sender, EventArgs args) { if (!DispatcherQueue.TryEnqueue(UpdateInput)) throw new InvalidOperationException("Media Library command update was rejected."); }
    private void ObserveAncestors()
    {
        DisconnectAncestors(); for (DependencyObject? node = this; node is not null; node = VisualTreeHelper.GetParent(node)) if (node is FrameworkElement e) { observers.Add((e, VisibilityProperty, e.RegisterPropertyChangedCallback(VisibilityProperty, (_, _) => UpdateInput()))); if (e is Control) observers.Add((e, IsEnabledProperty, e.RegisterPropertyChangedCallback(IsEnabledProperty, (_, _) => { if (modal is not null) modal.IsEnabled = EnabledWithin; UpdateInput(); }))); }
    }
    private void DisconnectAncestors() { foreach (var o in observers) o.Owner.UnregisterPropertyChangedCallback(o.Property, o.Token); observers.Clear(); }
    protected override AutomationPeer OnCreateAutomationPeer() => new FrameworkElementAutomationPeer(this);
}
