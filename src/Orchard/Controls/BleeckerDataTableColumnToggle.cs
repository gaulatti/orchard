using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.UI.ViewManagement;

namespace Orchard;

/// <summary>A nonmodal column picker over the consumer's original table and columns.</summary>
public sealed partial class BleeckerDataTableColumnToggle : Control
{
    private Grid? host;
    private readonly Microsoft.UI.Dispatching.DispatcherQueue uiQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread() ?? throw new InvalidOperationException("Column toggle requires a UI dispatcher.");
    private TriggerButton? trigger;
    private TextBlock? caption;
    private Popup? popup;
    private PickerDialog? dialog;
    private BleeckerOverlaySurface? surface;
    private StackPanel? columnHost;
    private ScrollViewer? scroll;
    private readonly List<ColumnRow> rows = [];
    private readonly ResourceDictionary palette = new() { Source = new Uri("ms-appx:///Orchard/Themes/DataTableColumnTogglePalette.xaml") };
    private readonly AccessibilitySettings accessibility = new();
    private readonly UISettings settings = new();
    private IBleeckerDataTableColumnVisibilitySource? observedTable;
    private EventHandler? tableChanged;
    private XamlRoot? observedRoot;
    private bool updating, openingFromKeyboard, initialFocusPending, restoreFocus;

    public BleeckerDataTableColumnToggle()
    {
        DefaultStyleKey = typeof(BleeckerDataTableColumnToggle);
        Loaded += (_, _) => { Observe(); settings.ColorValuesChanged += ColorsChanged; Refresh(); };
        Unloaded += (_, _) => { settings.ColorValuesChanged -= ColorsChanged; Close(false); Disconnect(); };
        ActualThemeChanged += (_, _) => { RenderRows(); UpdatePosition(); };
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) DetachTemplate(); });
        RegisterPropertyChangedCallback(IsEnabledProperty, (_, _) => { if (!IsEnabled) Close(false); Refresh(); });
        RegisterPropertyChangedCallback(FontFamilyProperty, (_, _) => RenderRows());
        RegisterPropertyChangedCallback(ForegroundProperty, (_, _) => RenderRows());
        RegisterPropertyChangedCallback(FlowDirectionProperty, (_, _) => { Refresh(); RenderRows(); });
        InitializePopupInput();
    }
    public static readonly DependencyProperty TableProperty = DependencyProperty.Register(nameof(Table), typeof(object), typeof(BleeckerDataTableColumnToggle), new PropertyMetadata(null, (owner, _) =>
    { var view = (BleeckerDataTableColumnToggle)owner; if (view.IsLoaded) view.Observe(); view.Refresh(); }));
    public IBleeckerDataTableColumnVisibilitySource? Table { get => GetValue(TableProperty) as IBleeckerDataTableColumnVisibilitySource; set => SetValue(TableProperty, value); }
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(BleeckerDataTableColumnToggle), new PropertyMetadata("Columns", (owner, _) => ((BleeckerDataTableColumnToggle)owner).Refresh()));
    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    protected override void OnApplyTemplate()
    {
        DetachTemplate(); base.OnApplyTemplate();
        host = GetTemplateChild("PART_Root") as Grid ?? throw new InvalidOperationException("Column toggle requires PART_Root.");
        var buttonPalette = new ResourceDictionary { Source = new Uri("ms-appx:///Orchard/Themes/Buttons/Secondary.g.xaml") };
        trigger = new TriggerButton(this) { Name = "PART_Trigger", Height = 36, MinHeight = 36, MinWidth = 0, Padding = new Thickness(14, 0, 14, 0), FontSize = 13, UseLayoutRounding = false, Style = (Style)buttonPalette[typeof(Button)] };
        trigger.Resources.MergedDictionaries.Add(buttonPalette);
        trigger.SetBinding(FontFamilyProperty, new Binding { Source = this, Path = new PropertyPath(nameof(FontFamily)) });
        var content = new Grid { ColumnSpacing = 8, UseLayoutRounding = false }; content.ColumnDefinitions.Add(new() { Width = GridLength.Auto }); content.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        var icon = (Viewbox)XamlReader.Load("""
            <Viewbox xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" Width="14" Height="14" IsHitTestVisible="False"><Canvas Width="24" Height="24" FlowDirection="LeftToRight"><Path Data="M14 17H5 M19 7H10" StrokeThickness="2" StrokeStartLineCap="Round" StrokeEndLineCap="Round"/><Ellipse Width="6" Height="6" Canvas.Left="14" Canvas.Top="14" StrokeThickness="2"/><Ellipse Width="6" Height="6" Canvas.Left="4" Canvas.Top="4" StrokeThickness="2"/></Canvas></Viewbox>
            """);
        foreach (var shape in ((Canvas)icon.Child).Children.OfType<Microsoft.UI.Xaml.Shapes.Shape>()) BleeckerButtonGlyphForeground.Attach(shape);
        AutomationProperties.SetAccessibilityView(icon, AccessibilityView.Raw); content.Children.Add(icon);
        caption = new TextBlock { FontSize = 13, LineHeight = 19.5, LineStackingStrategy = LineStackingStrategy.BlockLineHeight, VerticalAlignment = VerticalAlignment.Center, UseLayoutRounding = false };
        caption.SetBinding(TextBlock.FontFamilyProperty, new Binding { Source = trigger, Path = new PropertyPath(nameof(FontFamily)) }); caption.SetBinding(TextBlock.FontWeightProperty, new Binding { Source = trigger, Path = new PropertyPath(nameof(FontWeight)) });
        Grid.SetColumn(caption, 1); content.Children.Add(caption); trigger.Content = content; trigger.Click += TriggerClicked;
        surface = (BleeckerOverlaySurface)XamlReader.Load("""
            <local:BleeckerOverlaySurface xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:local="using:Orchard" Name="PART_Surface" Width="208" MinWidth="224" Padding="6" UseLayoutRounding="False" Background="{ThemeResource BleeckerMenuBackgroundBrush}" BorderBrush="{ThemeResource BleeckerMenuBorderBrush}" BorderThickness="1" CornerRadius="{StaticResource BleeckerUiCornerRadius}"/>
            """);
        var stack = new StackPanel { UseLayoutRounding = false };
        var heading = (TextBlock)XamlReader.Load("<TextBlock xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Text='VISIBLE COLUMNS' Foreground='{ThemeResource BleeckerTextSecondaryBrush}' FontSize='10' CharacterSpacing='90' LineHeight='15' LineStackingStrategy='BlockLineHeight' UseLayoutRounding='False'/>");
        heading.FontFamily = (FontFamily)Application.Current.Resources["BleeckerPrimarySemiboldFontFamily"]; AutomationProperties.SetName(heading, "Visible columns");
        stack.Children.Add(new Border { Padding = new Thickness(8), Child = heading });
        columnHost = new StackPanel { Name = "PART_Columns", UseLayoutRounding = false };
        scroll = new ScrollViewer { Name = "PART_Viewport", MaxHeight = 240, Content = columnHost, IsTabStop = false, HorizontalScrollMode = ScrollMode.Disabled, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, VerticalScrollMode = ScrollMode.Enabled, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, BringIntoViewOnFocusChange = false };
        AutomationProperties.SetAccessibilityView(scroll, AccessibilityView.Raw); stack.Children.Add(scroll); surface.Content = stack;
        dialog = new PickerDialog { Name = "PART_Dialog", IsTabStop = true, UseSystemFocusVisuals = false, HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch, Content = surface };
        popup = new Popup { Name = "PART_Popup", IsLightDismissEnabled = false, ShouldConstrainToRootBounds = true, Child = dialog };
        popup.Opened += PopupOpened; popup.Closed += PopupClosed; surface.SizeChanged += SurfaceChanged;
        host.Children.Add(trigger); host.Children.Add(popup);
        if (IsLoaded) Observe(); Refresh();
    }
    private void TriggerClicked(object sender, RoutedEventArgs args) { if (popup?.IsOpen == true) Close(true); else Open(); }
    private void Observe()
    {
        Disconnect(); observedTable = Table;
        if (observedTable is null) throw new InvalidOperationException("A mounted column toggle requires its original consumer Table.");
        var source = observedTable; tableChanged = (_, _) => Dispatch(() => { if (ReferenceEquals(source, Table) && ReferenceEquals(source, observedTable)) Refresh(); }); source.Changed += tableChanged;
        observedRoot = XamlRoot; if (observedRoot is not null) observedRoot.Changed += RootChanged;
    }
    private void Disconnect()
    {
        if (observedTable is not null && tableChanged is not null) observedTable.Changed -= tableChanged; observedTable = null; tableChanged = null;
        if (observedRoot is not null) observedRoot.Changed -= RootChanged; observedRoot = null;
    }
    private void Dispatch(Action work) { if (uiQueue.HasThreadAccess) { if (IsLoaded) work(); } else if (!uiQueue.TryEnqueue(() => { if (IsLoaded) work(); })) throw new InvalidOperationException("Column toggle could not dispatch its original source notification."); }
    private void RootChanged(XamlRoot sender, XamlRootChangedEventArgs args) => UpdatePosition();
    private void ColorsChanged(UISettings sender, object args) => uiQueue.TryEnqueue(() => { if (IsLoaded) RenderRows(); });
    private void Refresh()
    {
        if (trigger is null || caption is null || columnHost is null) return;
        caption.Text = Label ?? ""; AutomationProperties.SetName(trigger, Label ?? ""); trigger.IsEnabled = IsEnabled; trigger.FlowDirection = FlowDirection;
        if (Table is not { } table) return;
        var columns = table.GetAllColumns() ?? throw new InvalidOperationException("The consumer must return its original column list.");
        var eligible = columns.Where(column => column.HasAccessor && column.CanHide).ToArray();
        updating = true;
        try
        {
            var available = rows.GroupBy(row => row.Column.Id).ToDictionary(group => group.Key, group => new Queue<ColumnRow>(group));
            var ordered = eligible.Select(column => { var row = available.TryGetValue(column.Id, out var queue) && queue.Count > 0 ? queue.Dequeue() : new ColumnRow(this, column); row.Column = column; row.Update(); return row; }).ToArray();
            var removedRows = rows.Where(row => !ordered.Contains(row)).ToArray();
            if (popup?.IsOpen == true && removedRows.Any(row => IsInside(FocusManager.GetFocusedElement(XamlRoot), row)))
            {
                // Removing the focused DOM input leaves body focus in the
                // source. Windows requires a live focus target; retain the
                // existing dialog container without selecting a column.
                if (!dialog!.Focus(FocusState.Programmatic)) throw new InvalidOperationException("Column removal could not focus its existing native dialog container.");
            }
            foreach (var removed in removedRows) { columnHost.Children.Remove(removed); removed.Detach(); }
            for (var index = 0; index < ordered.Length; index++) { var old = columnHost.Children.IndexOf(ordered[index]); if (old == index) continue; if (old >= 0) columnHost.Children.RemoveAt(old); columnHost.Children.Insert(index, ordered[index]); }
            rows.Clear(); rows.AddRange(ordered);
        }
        finally { updating = false; }
        UpdatePosition();
    }
    private void Change(ColumnRow row, bool visible)
    {
        if (updating || !IsLoaded || !IsEnabled || !row.Checkbox.IsLoaded || !rows.Contains(row)) return;
        var table = Table ?? throw new InvalidOperationException("Column selection requires its original consumer Table.");
        if (!table.GetAllColumns().Any(column => ReferenceEquals(column, row.Column) && column.HasAccessor && column.CanHide)) return;
        row.Column.ToggleVisibility(visible); Refresh();
    }
    private Brush Palette(string key)
    {
        var theme = accessibility.HighContrast ? "HighContrast" : ActualTheme == ElementTheme.Light ? "Light" : "Default";
        return (Brush)((ResourceDictionary)palette.ThemeDictionaries[theme])[key];
    }
    private void RenderRows() { foreach (var row in rows) row.Render(); if (dialog is not null) { dialog.RequestedTheme = ActualTheme; dialog.FlowDirection = FlowDirection; } }
    private void DetachTemplate()
    {
        Close(false);
        if (trigger is not null) trigger.Click -= TriggerClicked;
        if (popup is not null) { popup.Opened -= PopupOpened; popup.Closed -= PopupClosed; popup.Child = null; }
        if (surface is not null) surface.SizeChanged -= SurfaceChanged;
        foreach (var row in rows) row.Detach(); rows.Clear(); host?.Children.Clear(); host = null; trigger = null; caption = null; popup = null; surface = null; dialog = null; scroll = null; columnHost = null;
    }
}
