using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

/// <summary>A wrapping consumer-owned path with lifetime inline expansion.</summary>
[ContentProperty(Name = nameof(Items))]
public sealed class BleeckerBreadcrumb : Control
{
    private Grid? host;
    private BreadcrumbPanel? panel;
    private bool expanded, observing;
    private DataTemplate? renderedLinkTemplate, renderedSeparatorTemplate;
    private readonly HashSet<BleeckerBreadcrumbItem> observed = [];
    private readonly List<(string Key, bool Current, FrameworkElement Element)> entries = [];
    private readonly Button expand = new() { Width = 24, Height = 24 };
    private readonly ResourceDictionary actionStyles = new() { Source = new Uri("ms-appx:///Orchard/Themes/BreadcrumbActions.xaml") };

    public BleeckerBreadcrumb()
    {
        DefaultStyleKey = typeof(BleeckerBreadcrumb);
        Items = new ObservableCollection<BleeckerBreadcrumbItem>();
        AutomationProperties.SetName(this, "Breadcrumb");
        expand.Click += (_, _) =>
        {
            var restoreFocus = expand.FocusState == FocusState.Keyboard;
            expanded = true; Render();
            if (restoreFocus) DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
                () => entries.Select(entry => entry.Element).OfType<HyperlinkButton>().Skip(1).FirstOrDefault()?.Focus(FocusState.Keyboard));
        };
        AutomationProperties.SetName(expand, "Show full path");
        BleeckerBreadcrumbMotion.Attach(expand);
        Loaded += (_, _) => DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
        {
            if (!IsLoaded) return;
            StartObserving(); Render();
        });
        Unloaded += (_, _) => StopObserving();
    }

    public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(nameof(Items), typeof(object), typeof(BleeckerBreadcrumb), new PropertyMetadata(null, (owner, args) =>
    {
        var control = (BleeckerBreadcrumb)owner;
        control.StopObserving(args.OldValue as ObservableCollection<BleeckerBreadcrumbItem>);
        if (args.NewValue is null && !control.IsLoaded && control.GetBindingExpression(ItemsProperty) is not null) { control.Render(); return; }
        if (args.NewValue is not ObservableCollection<BleeckerBreadcrumbItem>) throw new ArgumentException("Items requires a non-null ObservableCollection<BleeckerBreadcrumbItem>.");
        if (control.IsLoaded) control.StartObserving();
        control.Render();
    }));
    public ObservableCollection<BleeckerBreadcrumbItem> Items
    {
        get => GetValue(ItemsProperty) as ObservableCollection<BleeckerBreadcrumbItem>
            ?? throw new InvalidOperationException("Breadcrumb Items binding must resolve before the control is mounted.");
        set => SetValue(ItemsProperty, value ?? throw new ArgumentNullException(nameof(value)));
    }
    public static readonly DependencyProperty CollapsedAfterProperty = DependencyProperty.Register(nameof(CollapsedAfter), typeof(int), typeof(BleeckerBreadcrumb), new PropertyMetadata(0, OnPresentationChanged));
    public int CollapsedAfter { get => (int)GetValue(CollapsedAfterProperty); set => SetValue(CollapsedAfterProperty, value); }
    public static readonly DependencyProperty LinkTemplateProperty = DependencyProperty.Register(nameof(LinkTemplate), typeof(DataTemplate), typeof(BleeckerBreadcrumb), new PropertyMetadata(null, OnPresentationChanged));
    public DataTemplate? LinkTemplate { get => (DataTemplate?)GetValue(LinkTemplateProperty); set => SetValue(LinkTemplateProperty, value); }
    public static readonly DependencyProperty SeparatorTemplateProperty = DependencyProperty.Register(nameof(SeparatorTemplate), typeof(DataTemplate), typeof(BleeckerBreadcrumb), new PropertyMetadata(null, OnPresentationChanged));
    public DataTemplate? SeparatorTemplate { get => (DataTemplate?)GetValue(SeparatorTemplateProperty); set => SetValue(SeparatorTemplateProperty, value); }
    public event EventHandler<BleeckerBreadcrumbItem>? ItemInvoked;
    private static void OnPresentationChanged(DependencyObject owner, DependencyPropertyChangedEventArgs _) => ((BleeckerBreadcrumb)owner).Render();

    protected override void OnApplyTemplate()
    {
        if (panel is not null) { panel.Children.Clear(); host?.Children.Remove(panel); }
        panel = null; host = null;
        base.OnApplyTemplate();
        if (Template is null) return;
        host = GetTemplateChild("PART_Items") as Grid ?? throw new InvalidOperationException("Breadcrumb requires PART_Items.");
        panel = new BreadcrumbPanel(); host.Children.Add(panel);
        expand.Style = (Style)actionStyles[typeof(Button)];
        Render();
    }
    private void StartObserving()
    {
        if (!observing) { Items.CollectionChanged += OnCollectionChanged; observing = true; }
        ObserveItems();
    }
    private void StopObserving(ObservableCollection<BleeckerBreadcrumbItem>? previous = null)
    {
        if (observing) (previous ?? Items).CollectionChanged -= OnCollectionChanged;
        observing = false;
        foreach (var item in observed) item.Changed -= OnItemChanged;
        observed.Clear();
    }
    private void ObserveItems()
    {
        foreach (var item in observed) item.Changed -= OnItemChanged;
        observed.Clear();
        foreach (var item in Items)
        {
            if (item is null) throw new ArgumentException("Breadcrumb items must not be null.");
            if (observed.Add(item)) item.Changed += OnItemChanged;
        }
    }
    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs _) { ObserveItems(); Render(); }
    private void OnItemChanged(object? sender, EventArgs _) => Render();

    private void Render()
    {
        if (panel is null) return;
        if (GetValue(ItemsProperty) is not ObservableCollection<BleeckerBreadcrumbItem> items)
        {
            if (IsLoaded) _ = Items;
            panel.Children.Clear(); return;
        }
        var collapsed = CollapsedAfter > 0 && !expanded && items.Count > (long)CollapsedAfter + 1;
        BleeckerBreadcrumbItem?[] visible = collapsed ? [items[0], (BleeckerBreadcrumbItem?)null, items[^1]] : items.Cast<BleeckerBreadcrumbItem?>().ToArray();
        if (!ReferenceEquals(renderedLinkTemplate, LinkTemplate)) entries.Clear();
        var unused = entries.ToList();
        var next = new List<(string Key, bool Current, FrameworkElement Element)>();
        var children = new List<UIElement>();
        for (var index = 0; index < visible.Length; index++)
        {
            var item = visible[index];
            if (item is null) children.Add(expand);
            else
            {
                var current = index == visible.Length - 1;
                var key = item.Href + ":" + item.Label;
                var reuse = unused.FindIndex(entry => entry.Key == key && entry.Current == current);
                FrameworkElement element;
                if (reuse >= 0) { element = unused[reuse].Element; unused.RemoveAt(reuse); }
                else if (current)
                {
                    element = new TextBlock { FontFamily = (FontFamily)Application.Current.Resources["BleeckerPrimaryMediumFontFamily"],
                        FontWeight = Microsoft.UI.Text.FontWeights.Medium, FontSize = 14, LineHeight = 20,
                        LineStackingStrategy = LineStackingStrategy.BlockLineHeight, TextWrapping = TextWrapping.WrapWholeWords };
                    element.SetBinding(TextBlock.ForegroundProperty, new Binding { Source = this, Path = new PropertyPath(nameof(Foreground)) });
                    AutomationProperties.SetItemStatus(element, "Current page");
                }
                else if (LinkTemplate is not null) element = new ContentPresenter { ContentTemplate = LinkTemplate };
                else
                {
                    var link = new HyperlinkButton { Style = (Style)actionStyles[typeof(HyperlinkButton)] };
                    link.Click += (_, _) => ItemInvoked?.Invoke(this, (BleeckerBreadcrumbItem)link.Tag);
                    BleeckerBreadcrumbMotion.Attach(link);
                    element = link;
                }
                if (element is TextBlock text) text.Text = item.Label;
                else if (element is HyperlinkButton link) { link.Tag = item; link.Content = item.Label; link.Command = item.Command; link.CommandParameter = item.CommandParameter; AutomationProperties.SetName(link, item.Label); }
                else if (element is ContentPresenter presenter) presenter.Content = item;
                next.Add((key, current, element)); children.Add(element);
            }
            if (index < visible.Length - 1)
            {
                var previousIndex = index * 2 + 1;
                var reuse = ReferenceEquals(renderedSeparatorTemplate, SeparatorTemplate) && previousIndex < panel.Children.Count
                    ? panel.Children[previousIndex] as FrameworkElement : null;
                var separator = reuse ?? (SeparatorTemplate is null
                    ? (FrameworkElement)XamlReader.Load("<Viewbox xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Width='14' Height='14' IsHitTestVisible='False'><Canvas Width='24' Height='24'><Path Data='M9,18 L15,12 L9,6' Stroke='{ThemeResource BleeckerTextSecondaryBrush}' StrokeThickness='2' StrokeStartLineCap='Round' StrokeEndLineCap='Round' StrokeLineJoin='Round'/></Canvas></Viewbox>")
                    : new DecorativeSeparator { ContentTemplate = SeparatorTemplate });
                AutomationProperties.SetAccessibilityView(separator, AccessibilityView.Raw);
                children.Add(separator);
            }
        }
        // Retain unchanged keyed links and move them without remounting.
        for (var index = panel.Children.Count - 1; index >= 0; index--)
            if (!children.Contains(panel.Children[index])) panel.Children.RemoveAt(index);
        for (var index = 0; index < children.Count; index++)
        {
            var child = children[index];
            if (index < panel.Children.Count && ReferenceEquals(panel.Children[index], child)) continue;
            if (panel.Children.Contains(child)) panel.Children.Remove(child);
            panel.Children.Insert(index, child);
        }
        entries.Clear(); entries.AddRange(next);
        renderedLinkTemplate = LinkTemplate; renderedSeparatorTemplate = SeparatorTemplate;
        panel.InvalidateMeasure();
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new BreadcrumbPeer(this);
    private sealed class BreadcrumbPeer(FrameworkElement owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerBreadcrumb);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
    }
    private sealed class DecorativeSeparator : ContentControl
    {
        internal DecorativeSeparator() { IsTabStop = false; }
        protected override AutomationPeer OnCreateAutomationPeer() => new DecorativeSeparatorPeer(this);
    }
    private sealed class DecorativeSeparatorPeer(FrameworkElement owner) : FrameworkElementAutomationPeer(owner)
    {
        // Source aria-hidden covers the entire custom separator subtree.
        protected override bool IsControlElementCore() => false;
        protected override bool IsContentElementCore() => false;
        protected override List<AutomationPeer>? GetChildrenCore() => null;
    }
    private sealed class BreadcrumbPanel : Panel
    {
        private readonly List<(int Start, int Count, double Width, double Height)> rows = [];
        protected override Size MeasureOverride(Size available)
        {
            rows.Clear();
            var start = 0; var width = 0d; var height = 0d;
            for (var index = 0; index < Children.Count; index++)
            {
                var child = Children[index]; child.Measure(new Size(available.Width, double.PositiveInfinity));
                var proposed = width + (index > start ? 6 : 0) + child.DesiredSize.Width;
                if (index > start && proposed > available.Width) { rows.Add((start, index - start, width, height)); start = index; width = 0; height = 0; }
                width += (index > start ? 6 : 0) + child.DesiredSize.Width; height = Math.Max(height, child.DesiredSize.Height);
            }
            if (Children.Count > 0) rows.Add((start, Children.Count - start, width, height));
            return new Size(rows.Count == 0 ? 0 : rows.Max(row => row.Width), rows.Sum(row => row.Height) + Math.Max(0, rows.Count - 1) * 6);
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            var y = 0d;
            foreach (var row in rows)
            {
                var x = 0d;
                for (var index = row.Start; index < row.Start + row.Count; index++)
                {
                    var child = Children[index]; var size = child.DesiredSize;
                    child.Arrange(new Rect(x, y + (row.Height - size.Height) / 2, size.Width, size.Height)); x += size.Width + 6;
                }
                y += row.Height + 6;
            }
            return finalSize;
        }
    }
}
