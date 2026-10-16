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

namespace Orchard;

/// <summary>A responsive view over the consumer table's original pagination and selection.</summary>
public sealed partial class BleeckerDataTablePagination : Control
{
    private Grid? host;
    private PaginationPanel? layout;
    private TextBlock? summary, page;
    private ComboBox? size;
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
        size = new PageSizeComboBox(this) { Name = "PART_PageSize", Style = (Style)resources["BleeckerComboBoxStyle"], Width = 80, MinWidth = 0, MinHeight = 40, Height = 40, PlaceholderText = "", FontSize = 14, Padding = new Thickness(14, 0, 14, 0), UseLayoutRounding = false };
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
    private void PageSizeSelectionChanged(object sender, SelectionChangedEventArgs args)
    { if (!updating && size?.SelectedItem is ComboBoxItem { Tag: int value }) Execute(table => table.SetPageSize(value)); }

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
        try
        {
            var options = PageSizeOptions.ToArray();
            if (!renderedOptions.SequenceEqual(options) || size.Items.Count != options.Length)
            {
                var available = size.Items.OfType<ComboBoxItem>().GroupBy(item => (int)item.Tag).ToDictionary(group => group.Key, group => new Queue<ComboBoxItem>(group));
                var ordered = options.Select(value => available.TryGetValue(value, out var queue) && queue.Count > 0 ? queue.Dequeue() : new PageSizeComboBoxItem((PageSizeComboBox)size) { Content = value.ToString(CultureInfo.InvariantCulture), Tag = value, FontFamily = FontFamily, FontSize = 14, MinHeight = 36 }).ToArray();
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
            size.SelectedItem = size.Items.OfType<ComboBoxItem>().FirstOrDefault(item => (int)item.Tag == table.PageSize);
        }
        finally { updating = false; }
        first.IsEnabled = previous.IsEnabled = table.CanPreviousPage; next.IsEnabled = last.IsEnabled = table.CanNextPage;
        var wide = XamlRoot is not null && XamlRoot.Size.Width >= 640; first.Visibility = last.Visibility = wide ? Visibility.Visible : Visibility.Collapsed; layout.Wide = wide; layout.InvalidateMeasure();
        ((PageSizeComboBox)size).RefreshOpenValue();
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
    private sealed class PageSizeComboBox : ComboBox
    {
        private readonly BleeckerDataTablePagination owner;
        private Popup? positionedPopup;
        private long horizontalToken, verticalToken;
        private ContentPresenter? displayedPresenter;
        private TextBlock? controlledValue;
        private long contentToken;
        private bool positionQueued;
        internal PageSizeComboBox(BleeckerDataTablePagination owner)
        {
            this.owner = owner;
            MaxDropDownHeight = 290;
            DropDownOpened += (_, _) => QueueOpenPosition();
        }
        internal void RefreshOpenValue()
        {
            if (!IsDropDownOpen) return;
            var table = owner.Table ?? throw new InvalidOperationException("The opened chooser requires its consumer Table.");
            var value = (ContentPresenter)GetTemplateChild("ContentPresenter");
            var label = controlledValue ?? throw new InvalidOperationException("The opened chooser requires its controlled value label.");
            label.Text = Items.OfType<ComboBoxItem>().FirstOrDefault(item => (int)item.Tag == table.PageSize)?.Content?.ToString() ?? "";
            if (!ReferenceEquals(value.Content, label)) value.Content = label;
            QueueOpenPosition();
        }
        internal void QueueOpenPosition()
        {
            if (!IsDropDownOpen || positionQueued) return;
            positionQueued = true;
            if (!DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    if (!IsDropDownOpen || positionedPopup is null) return;
                    var border = (Border)GetTemplateChild("PopupBorder"); positionedPopup.Child.UpdateLayout();
                    var target = TransformToVisual(XamlRoot.Content).TransformBounds(new Rect(0, 0, ActualWidth, ActualHeight));
                    var painted = border.TransformToVisual(XamlRoot.Content).TransformBounds(new Rect(0, 0, border.ActualWidth, border.ActualHeight));
                    var dx = target.Left - painted.Left; var dy = target.Bottom + 4 - painted.Top;
                    if (Math.Abs(dx) > 1d / 64) positionedPopup.HorizontalOffset += dx;
                    if (Math.Abs(dy) > 1d / 64) positionedPopup.VerticalOffset += dy;
                    RefreshOpenValue();
                    if (FocusManager.GetFocusedElement(XamlRoot) is ComboBoxItem focused && Items.Contains(focused))
                    {
                        var scroll = (ScrollViewer)GetTemplateChild("ScrollViewer");
                        var item = focused.TransformToVisual(scroll).TransformBounds(new Rect(0, 0, focused.ActualWidth, focused.ActualHeight));
                        if (item.Top < 0) scroll.ChangeView(null, Math.Max(0, scroll.VerticalOffset + item.Top), null, true);
                        else if (item.Bottom > scroll.ActualHeight) scroll.ChangeView(null, scroll.VerticalOffset + item.Bottom - scroll.ActualHeight, null, true);
                    }
                }
                finally { positionQueued = false; }
            })) { positionQueued = false; throw new InvalidOperationException("The opened chooser could not schedule its native layout."); }
        }
        protected override void OnKeyDown(KeyRoutedEventArgs args)
        {
            if (IsDropDownOpen && args.Key == Windows.System.VirtualKey.Tab) { args.Handled = true; return; }
            if (IsEnabled && !IsDropDownOpen && args.Key is Windows.System.VirtualKey.Down or Windows.System.VirtualKey.Up or Windows.System.VirtualKey.Enter or Windows.System.VirtualKey.Space)
            {
                IsDropDownOpen = true;
                args.Handled = true;
                return;
            }
            base.OnKeyDown(args);
        }
        protected override void OnApplyTemplate()
        {
            if (positionedPopup is not null)
            {
                positionedPopup.UnregisterPropertyChangedCallback(Popup.HorizontalOffsetProperty, horizontalToken);
                positionedPopup.UnregisterPropertyChangedCallback(Popup.VerticalOffsetProperty, verticalToken);
            }
            if (displayedPresenter is not null) displayedPresenter.UnregisterPropertyChangedCallback(ContentPresenter.ContentProperty, contentToken);
            base.OnApplyTemplate();
            var popup = GetTemplateChild("Popup") as Popup ?? throw new InvalidOperationException("The native page-size ComboBox template requires Popup.");
            popup.ShouldConstrainToRootBounds = true;
            popup.PlacementTarget = this;
            popup.DesiredPlacement = PopupPlacementMode.BottomEdgeAlignedLeft;
            popup.VerticalOffset = 4;
            var templateRoot = VisualTreeHelper.GetChild(this, 0) as FrameworkElement ?? throw new InvalidOperationException("The page-size template requires its layout root.");
            var dropDown = VisualStateManager.GetVisualStateGroups(templateRoot).Single(group => group.Name == "DropDownStates");
            var border = GetTemplateChild("PopupBorder") as Border ?? throw new InvalidOperationException("The page-size template requires PopupBorder.");
            // The native split animation hides the trigger and aligns its selected
            // row over it. The source keeps the trigger visible beneath its popper.
            foreach (var state in dropDown.States)
            {
                var storyboard = state.Storyboard ?? throw new InvalidOperationException("The page-size state requires its completion storyboard.");
                storyboard.Stop(); storyboard.Children.Clear();
                var opacity = new DoubleAnimation { To = state.Name == "Opened" ? 1 : 0, Duration = new Duration(TimeSpan.Zero) };
                Storyboard.SetTarget(opacity, border); Storyboard.SetTargetProperty(opacity, "Opacity"); storyboard.Children.Add(opacity);
            }
            var scroll = GetTemplateChild("ScrollViewer") as ScrollViewer ?? throw new InvalidOperationException("The page-size template requires ScrollViewer.");
            border.Width = Width; border.Margin = new Thickness(0);
            scroll.MinWidth = 0; scroll.MaxHeight = 288;
            positionedPopup = popup;
            horizontalToken = popup.RegisterPropertyChangedCallback(Popup.HorizontalOffsetProperty, (_, _) => QueueOpenPosition());
            verticalToken = popup.RegisterPropertyChangedCallback(Popup.VerticalOffsetProperty, (_, _) => QueueOpenPosition());
            displayedPresenter = (ContentPresenter)GetTemplateChild("ContentPresenter");
            controlledValue = new TextBlock();
            controlledValue.SetBinding(TextBlock.FontFamilyProperty, new Binding { Source = this, Path = new PropertyPath(nameof(FontFamily)) });
            controlledValue.SetBinding(TextBlock.FontSizeProperty, new Binding { Source = this, Path = new PropertyPath(nameof(FontSize)) });
            AutomationProperties.SetAccessibilityView(controlledValue, AccessibilityView.Raw);
            contentToken = displayedPresenter.RegisterPropertyChangedCallback(ContentPresenter.ContentProperty, (_, _) => QueueOpenPosition());
        }
    }
    private sealed class PageSizeComboBoxItem(PageSizeComboBox owner) : ComboBoxItem
    {
        protected override void OnGotFocus(RoutedEventArgs args)
        {
            base.OnGotFocus(args);
            if (owner.IsDropDownOpen) { StartBringIntoView(new BringIntoViewOptions { AnimationDesired = false }); owner.QueueOpenPosition(); }
        }
        protected override void OnKeyDown(KeyRoutedEventArgs args)
        {
            if (owner.IsDropDownOpen && args.Key == Windows.System.VirtualKey.Tab) { args.Handled = true; return; }
            base.OnKeyDown(args);
        }
    }
}
