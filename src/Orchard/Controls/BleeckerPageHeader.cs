using System.Collections;
using System.Collections.Specialized;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

/// <summary>A consumer-owned page heading, navigation metadata and action composition.</summary>
public sealed class BleeckerPageHeader : Control
{
    private TextBlock? title, description;
    private ContentPresenter? breadcrumbs;
    private ActionPanel? actions;
    private BackAction? back;
    private INotifyCollectionChanged? observedActions;
    private readonly BleeckerRichTextDefaults breadcrumbDefaults = new();
    private Grid? lead;
    private Grid? actionHost, backHost;
    private HeaderRow? row;
    private XamlRoot? observedRoot;

    public BleeckerPageHeader()
    {
        DefaultStyleKey = typeof(BleeckerPageHeader);
        Loaded += (_, _) => ObserveMounted();
        Unloaded += (_, _) =>
        {
            DisconnectRoot(); DisconnectActions(); LayoutUpdated -= OnLayoutUpdated; breadcrumbDefaults.Clear();
            // A same-turn removal/reinsertion can deliver Unloaded after Loaded.
            // Revalidate the current mount before restoring its subscriptions.
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => { if (IsLoaded) ObserveMounted(); });
        };
    }
    private void ObserveMounted()
    {
        ObserveRoot(); ObserveActions(); LayoutUpdated -= OnLayoutUpdated; LayoutUpdated += OnLayoutUpdated; UpdatePresentation();
    }

    private static DependencyProperty Property(string name, Type type, object? value) =>
        DependencyProperty.Register(name, type, typeof(BleeckerPageHeader), new PropertyMetadata(value, Changed));
    public static readonly DependencyProperty TitleProperty = Property(nameof(Title), typeof(string), "");
    public static readonly DependencyProperty DescriptionProperty = Property(nameof(Description), typeof(string), null);
    public static readonly DependencyProperty BreadcrumbsProperty = Property(nameof(Breadcrumbs), typeof(object), null);
    public static readonly DependencyProperty ActionsProperty = Property(nameof(Actions), typeof(object), null);
    public static readonly DependencyProperty BreadcrumbsTemplateProperty = Property(nameof(BreadcrumbsTemplate), typeof(DataTemplate), null);
    public static readonly DependencyProperty ActionsTemplateProperty = Property(nameof(ActionsTemplate), typeof(DataTemplate), null);
    public static readonly DependencyProperty BackHrefProperty = Property(nameof(BackHref), typeof(string), null);
    public static readonly DependencyProperty BackCommandProperty = Property(nameof(BackCommand), typeof(ICommand), null);
    public static readonly DependencyProperty BackCommandParameterProperty = Property(nameof(BackCommandParameter), typeof(object), null);
    public static readonly DependencyProperty BackLabelProperty = Property(nameof(BackLabel), typeof(string), "Back");

    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    public object? Breadcrumbs { get => GetValue(BreadcrumbsProperty); set => SetValue(BreadcrumbsProperty, value); }
    public object? Actions { get => GetValue(ActionsProperty); set => SetValue(ActionsProperty, value); }
    public DataTemplate? BreadcrumbsTemplate { get => (DataTemplate?)GetValue(BreadcrumbsTemplateProperty); set => SetValue(BreadcrumbsTemplateProperty, value); }
    public DataTemplate? ActionsTemplate { get => (DataTemplate?)GetValue(ActionsTemplateProperty); set => SetValue(ActionsTemplateProperty, value); }
    public string? BackHref { get => (string?)GetValue(BackHrefProperty); set => SetValue(BackHrefProperty, value); }
    public ICommand? BackCommand { get => (ICommand?)GetValue(BackCommandProperty); set => SetValue(BackCommandProperty, value); }
    public object? BackCommandParameter { get => GetValue(BackCommandParameterProperty); set => SetValue(BackCommandParameterProperty, value); }
    public string? BackLabel { get => (string?)GetValue(BackLabelProperty); set => SetValue(BackLabelProperty, value); }
    public event RoutedEventHandler? BackRequested;

    protected override void OnApplyTemplate()
    {
        if (back is not null) back.Click -= OnBack;
        actions?.Children.Clear();
        breadcrumbDefaults.Clear();
        if (breadcrumbs is not null) breadcrumbs.Loaded -= BreadcrumbsLoaded;
        base.OnApplyTemplate();
        if (Template is null) { title = description = null; breadcrumbs = null; actions = null; back = null; lead = actionHost = backHost = null; row = null; return; }
        title = GetTemplateChild("PART_Title") as TextBlock ?? throw new InvalidOperationException("Page Header requires PART_Title.");
        description = GetTemplateChild("PART_Description") as TextBlock ?? throw new InvalidOperationException("Page Header requires PART_Description.");
        breadcrumbs = GetTemplateChild("PART_Breadcrumbs") as ContentPresenter ?? throw new InvalidOperationException("Page Header requires PART_Breadcrumbs.");
        actionHost = GetTemplateChild("PART_Actions") as Grid ?? throw new InvalidOperationException("Page Header requires PART_Actions.");
        backHost = GetTemplateChild("PART_Back") as Grid ?? throw new InvalidOperationException("Page Header requires PART_Back.");
        var icon = GetTemplateChild("PART_BackIcon") as ContentControl ?? throw new InvalidOperationException("Page Header requires PART_BackIcon.");
        backHost.Children.Remove(icon);
        back = new BackAction(this) { Content = icon, Width = 32, Height = 32, MinWidth = 32, MinHeight = 32, Padding = new Thickness(0) };
        var palette = new ResourceDictionary { Source = new Uri("ms-appx:///Orchard/Themes/Buttons/IconGhost.g.xaml") };
        back.Resources.MergedDictionaries.Add(palette); back.Style = (Style)palette[typeof(Button)];
        back.SetBinding(Button.CommandProperty, new Binding { Source = this, Path = new PropertyPath(nameof(BackCommand)) });
        back.SetBinding(Button.CommandParameterProperty, new Binding { Source = this, Path = new PropertyPath(nameof(BackCommandParameter)) });
        back.Loaded += (_, _) =>
        {
            if (VisualTreeHelper.GetChildrenCount(back) > 0 && VisualTreeHelper.GetChild(back, 0) is ContentPresenter presenter)
                icon.SetBinding(ForegroundProperty, new Binding { Source = presenter, Path = new PropertyPath(nameof(Foreground)) });
        };
        backHost.Children.Add(back);
        actions = new ActionPanel(); actionHost.Children.Add(actions);
        lead = GetTemplateChild("PART_Lead") as Grid ?? throw new InvalidOperationException("Page Header requires PART_Lead.");
        var host = GetTemplateChild("PART_Row") as Grid ?? throw new InvalidOperationException("Page Header requires PART_Row.");
        host.Children.Remove(lead); host.Children.Remove(actionHost);
        row = new HeaderRow(); row.Children.Add(lead); row.Children.Add(actionHost); host.Children.Add(row);
        back.Click += OnBack;
        ObserveRoot(); RenderActions(); UpdatePresentation();
    }

    private static void Changed(DependencyObject owner, DependencyPropertyChangedEventArgs args)
    {
        var header = (BleeckerPageHeader)owner;
        if (args.Property == ActionsProperty) { header.DisconnectActions(); if (header.IsLoaded) header.ObserveActions(); }
        if (args.Property == ActionsProperty || args.Property == ActionsTemplateProperty) header.RenderActions();
        header.UpdatePresentation(); FrameworkElementAutomationPeer.FromElement(header)?.InvalidatePeer();
    }
    private void OnBack(object sender, RoutedEventArgs args) => BackRequested?.Invoke(this, args);
    private void ObserveRoot()
    {
        if (ReferenceEquals(observedRoot, XamlRoot)) return;
        DisconnectRoot(); observedRoot = XamlRoot;
        if (observedRoot is not null) observedRoot.Changed += RootChanged;
    }
    private void DisconnectRoot()
    {
        if (observedRoot is not null) observedRoot.Changed -= RootChanged;
        observedRoot = null;
    }
    private void RootChanged(XamlRoot sender, XamlRootChangedEventArgs args) => UpdatePresentation();
    private void UpdatePresentation()
    {
        if (back is null) return;
        var showBack = !string.IsNullOrEmpty(BackHref) || BackCommand is not null;
        back.Visibility = showBack ? Visibility.Visible : Visibility.Collapsed;
        AutomationProperties.SetName(back, BackLabel ?? "");
        FrameworkElementAutomationPeer.FromElement(back)?.InvalidatePeer();
        backHost!.Visibility = back.Visibility;
        lead!.ColumnSpacing = showBack ? 16 : 0;
        title!.Visibility = string.IsNullOrEmpty(Title) ? Visibility.Collapsed : Visibility.Visible;
        description!.Visibility = string.IsNullOrEmpty(Description) ? Visibility.Collapsed : Visibility.Visible;
        breadcrumbs!.Visibility = Breadcrumbs is null || Breadcrumbs is string { Length: 0 } ? Visibility.Collapsed : Visibility.Visible;
        actionHost!.Visibility = Actions is null || Actions is string { Length: 0 } ? Visibility.Collapsed : Visibility.Visible;
        breadcrumbs.Loaded -= BreadcrumbsLoaded; breadcrumbs.Loaded += BreadcrumbsLoaded;
        if (breadcrumbs.IsLoaded) breadcrumbDefaults.Apply(breadcrumbs);
        row!.Wide = XamlRoot is not null && XamlRoot.Size.Width >= 640;
        row.InvalidateMeasure();
    }
    private void BreadcrumbsLoaded(object sender, RoutedEventArgs args) => breadcrumbDefaults.Apply(breadcrumbs!);
    private void OnLayoutUpdated(object? sender, object args)
    {
        if (breadcrumbs is not null && breadcrumbs.IsLoaded) breadcrumbDefaults.Apply(breadcrumbs);
    }
    private void ObserveActions()
    {
        if (observedActions is not null) return;
        observedActions = Actions as INotifyCollectionChanged;
        if (observedActions is not null) observedActions.CollectionChanged += ActionsChanged;
        RenderActions();
    }
    private void DisconnectActions()
    {
        if (observedActions is not null) observedActions.CollectionChanged -= ActionsChanged;
        observedActions = null;
    }
    private void ActionsChanged(object? sender, NotifyCollectionChangedEventArgs args) { RenderActions(); UpdatePresentation(); }
    private void RenderActions()
    {
        if (actions is null) return;
        var previous = actions.Children.ToList();
        var next = new List<UIElement>();
        IEnumerable items = Actions is IEnumerable enumerable && Actions is not string ? enumerable : new object?[] { Actions };
        foreach (var item in items)
        {
            if (item is null || item is string { Length: 0 }) continue;
            var existing = previous.FirstOrDefault(child => ActionsTemplate is null && ReferenceEquals(child, item) ||
                child is ContentPresenter presenter && ReferenceEquals(presenter.Content, item) && ReferenceEquals(presenter.ContentTemplate, ActionsTemplate));
            var child = existing ?? (item is UIElement element && ActionsTemplate is null ? element : new ContentPresenter { Content = item, ContentTemplate = ActionsTemplate });
            if (next.Contains(child)) throw new ArgumentException("A Page Header action element cannot occupy two slots.");
            next.Add(child); previous.Remove(child);
        }
        foreach (var child in previous) actions.Children.Remove(child);
        for (var index = 0; index < next.Count; index++)
        {
            if (index < actions.Children.Count && ReferenceEquals(actions.Children[index], next[index])) continue;
            actions.Children.Remove(next[index]); actions.Children.Insert(index, next[index]);
        }
        actionHost!.Visibility = Actions is null || Actions is string { Length: 0 } ? Visibility.Collapsed : Visibility.Visible;
        row?.InvalidateMeasure();
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new HeaderPeer(this);
    private sealed class HeaderPeer(BleeckerPageHeader owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerPageHeader);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
        protected override bool IsKeyboardFocusableCore() => false;
        protected override string GetNameCore() => BleeckerAutomationMetadata.TryGetName(owner, this, out var name) ? name : string.IsNullOrEmpty(AutomationProperties.GetName(owner)) && AutomationProperties.GetLabeledBy(owner) is null ? owner.Title ?? "" : base.GetNameCore();
    }
    private sealed class BackAction(BleeckerPageHeader owner) : Button
    {
        protected override AutomationPeer OnCreateAutomationPeer() => new BackPeer(this, owner);
    }
    private sealed class BackPeer(BackAction action, BleeckerPageHeader owner) : ButtonAutomationPeer(action)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => string.IsNullOrEmpty(owner.BackHref) ? AutomationControlType.Button : AutomationControlType.Hyperlink;
    }

    private sealed class ActionPanel : Panel
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            double x = 0, y = 0, height = 0, width = 0;
            foreach (var child in Children.Where(child => child.Visibility == Visibility.Visible))
            {
                child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
                if (x > 0 && x + child.DesiredSize.Width > availableSize.Width) { y += height + 12; x = 0; height = 0; }
                width = Math.Max(width, x + child.DesiredSize.Width); height = Math.Max(height, child.DesiredSize.Height);
                x += child.DesiredSize.Width + 12;
            }
            return new Size(width, y + height);
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            var rows = new List<List<UIElement>>();
            var current = new List<UIElement>(); double x = 0;
            foreach (var child in Children.Where(child => child.Visibility == Visibility.Visible))
            {
                if (x > 0 && x + child.DesiredSize.Width > finalSize.Width) { rows.Add(current); current = []; x = 0; }
                current.Add(child); x += child.DesiredSize.Width + 12;
            }
            if (current.Count > 0) rows.Add(current);
            double y = 0;
            foreach (var row in rows)
            {
                var height = row.Max(child => child.DesiredSize.Height); x = 0;
                foreach (var child in row)
                {
                    child.Arrange(new Rect(x, y + (height - child.DesiredSize.Height) / 2, child.DesiredSize.Width, child.DesiredSize.Height));
                    x += child.DesiredSize.Width + 12;
                }
                y += height + 12;
            }
            return finalSize;
        }
    }

    private sealed class HeaderRow : Panel
    {
        internal bool Wide;
        private double leadWidth, actionsWidth;
        protected override Size MeasureOverride(Size availableSize)
        {
            var lead = Children[0]; var actions = Children[1];
            lead.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            actions.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            leadWidth = lead.DesiredSize.Width; actionsWidth = actions.DesiredSize.Width;
            var hasActions = actions.Visibility == Visibility.Visible;
            if (!Wide)
            {
                lead.Measure(new Size(availableSize.Width, double.PositiveInfinity));
                actions.Measure(new Size(availableSize.Width, double.PositiveInfinity));
                return new Size(Math.Max(lead.DesiredSize.Width, actions.DesiredSize.Width), lead.DesiredSize.Height + (hasActions ? 28 + actions.DesiredSize.Height : 0));
            }
            var gap = hasActions ? 28 : 0;
            var total = leadWidth + actionsWidth;
            if (double.IsFinite(availableSize.Width) && total + gap > availableSize.Width && total > 0)
            {
                var capacity = Math.Max(0, availableSize.Width - gap);
                leadWidth = capacity * leadWidth / total; actionsWidth = capacity - leadWidth;
            }
            lead.Measure(new Size(leadWidth, double.PositiveInfinity)); actions.Measure(new Size(actionsWidth, double.PositiveInfinity));
            return new Size(leadWidth + actionsWidth + gap, Math.Max(lead.DesiredSize.Height, actions.DesiredSize.Height));
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            var lead = Children[0]; var actions = Children[1];
            if (Wide)
            {
                lead.Arrange(new Rect(0, 0, leadWidth, lead.DesiredSize.Height));
                actions.Arrange(new Rect(Math.Max(0, finalSize.Width - actionsWidth), 0, actionsWidth, actions.DesiredSize.Height));
            }
            else
            {
                lead.Arrange(new Rect(0, 0, finalSize.Width, lead.DesiredSize.Height));
                actions.Arrange(new Rect(0, lead.DesiredSize.Height + 28, finalSize.Width, actions.DesiredSize.Height));
            }
            return finalSize;
        }
    }
}
