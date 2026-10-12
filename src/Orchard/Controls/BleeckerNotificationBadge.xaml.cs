using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace Orchard;

/// <summary>A notification trigger, unread count and consumer-owned notification list.</summary>
[ContentProperty(Name = nameof(Items))]
public sealed partial class BleeckerNotificationBadge : UserControl
{
    private readonly HashSet<BleeckerNotificationItem> observed = [];
    public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(nameof(Items), typeof(object), typeof(BleeckerNotificationBadge),
        new PropertyMetadata(null, OnItemsReplaced));
    public ObservableCollection<BleeckerNotificationItem> Items
    {
        get => (ObservableCollection<BleeckerNotificationItem>)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value ?? throw new ArgumentNullException(nameof(value)));
    }

    public BleeckerNotificationBadge()
    {
        Items = [];
        InitializeComponent();
        Loaded += (_, _) => { Items.CollectionChanged += OnItemsChanged; ObserveItems(); UpdateState(); };
        Unloaded += (_, _) => { Items.CollectionChanged -= OnItemsChanged; StopObserving(); Popup.Hide(); };
        UpdateState();
    }

    public static readonly DependencyProperty CountProperty = DependencyProperty.Register(nameof(Count), typeof(int), typeof(BleeckerNotificationBadge),
        new PropertyMetadata(0, (owner, _) => ((BleeckerNotificationBadge)owner).UpdateState()));
    public static readonly DependencyProperty MarkAllReadCommandProperty = DependencyProperty.Register(nameof(MarkAllReadCommand), typeof(ICommand), typeof(BleeckerNotificationBadge),
        new PropertyMetadata(null, (owner, _) => ((BleeckerNotificationBadge)owner).UpdateState()));
    public int Count { get => (int)GetValue(CountProperty); set => SetValue(CountProperty, value); }
    public ICommand? MarkAllReadCommand { get => (ICommand?)GetValue(MarkAllReadCommandProperty); set => SetValue(MarkAllReadCommandProperty, value); }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs args) { ObserveItems(); UpdateState(); }
    private static void OnItemsReplaced(DependencyObject owner, DependencyPropertyChangedEventArgs args)
    {
        var badge = (BleeckerNotificationBadge)owner;
        if (args.OldValue is ObservableCollection<BleeckerNotificationItem> oldItems) oldItems.CollectionChanged -= badge.OnItemsChanged;
        if (args.NewValue is not ObservableCollection<BleeckerNotificationItem> newItems)
            throw new ArgumentException("Items must be a notification collection.");
        if (badge.IsLoaded) newItems.CollectionChanged += badge.OnItemsChanged;
        badge.ObserveItems();
        badge.UpdateState();
    }
    private void OnItemChanged(object? sender, EventArgs args) => UpdateState();
    private void StopObserving()
    {
        foreach (var item in observed) item.Changed -= OnItemChanged;
        observed.Clear();
    }
    private void ObserveItems()
    {
        StopObserving();
        if (!IsLoaded) return;
        foreach (var item in Items)
            if (observed.Add(item)) item.Changed += OnItemChanged;
    }
    private void UpdateState()
    {
        if (CountBadge is null) return;
        CountBadge.Visibility = Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        CountText.Text = Count > 99 ? "99+" : Count.ToString(CultureInfo.InvariantCulture);
        MarkRead.Visibility = (Count > 0 || Items.Any(item => item.IsUnread)) && MarkAllReadCommand is not null ? Visibility.Visible : Visibility.Collapsed;
        MarkRead.Command = MarkAllReadCommand;
        EmptyState.Visibility = Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
    private void OnNotificationClick(object sender, RoutedEventArgs args) => Popup.Hide();

    public static Visibility UnreadVisibility(bool value) => value ? Visibility.Visible : Visibility.Collapsed;
    public static Visibility ReadVisibility(bool value) => value ? Visibility.Collapsed : Visibility.Visible;
    public static Visibility TextVisibility(string? value) => string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;
    public static string ItemName(string? title, string? description, string? timestamp) =>
        string.Join(" ", new[] { title, description, timestamp }.Where(value => !string.IsNullOrEmpty(value)));
}
