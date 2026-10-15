using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.UI.Xaml;

namespace Orchard;

/// <summary>A consumer-owned destination or expandable group. Href is opaque.</summary>
[Microsoft.UI.Xaml.Data.Bindable]
public sealed class BleeckerSidebarItem : DependencyObject
{
    public BleeckerSidebarItem() => Items = new ObservableCollection<BleeckerSidebarItem>();
    internal event EventHandler? Changed;
    private static void Change(DependencyObject owner, DependencyPropertyChangedEventArgs _) => ((BleeckerSidebarItem)owner).Changed?.Invoke(owner, EventArgs.Empty);
    public static readonly DependencyProperty IdProperty = DependencyProperty.Register(nameof(Id), typeof(string), typeof(BleeckerSidebarItem), new PropertyMetadata("", Change));
    public string Id { get => (string)GetValue(IdProperty); set => SetValue(IdProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(BleeckerSidebarItem), new PropertyMetadata("", Change));
    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public static readonly DependencyProperty HrefProperty = DependencyProperty.Register(nameof(Href), typeof(string), typeof(BleeckerSidebarItem), new PropertyMetadata(null, Change));
    public string? Href { get => (string?)GetValue(HrefProperty); set => SetValue(HrefProperty, value); }
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon), typeof(object), typeof(BleeckerSidebarItem), new PropertyMetadata(null, Change));
    public object? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public static readonly DependencyProperty BadgeProperty = DependencyProperty.Register(nameof(Badge), typeof(object), typeof(BleeckerSidebarItem), new PropertyMetadata(null, Change));
    public object? Badge { get => GetValue(BadgeProperty); set => SetValue(BadgeProperty, value); }
    public static readonly DependencyProperty IconTemplateProperty = DependencyProperty.Register(nameof(IconTemplate), typeof(DataTemplate), typeof(BleeckerSidebarItem), new PropertyMetadata(null, Change));
    public DataTemplate? IconTemplate { get => (DataTemplate?)GetValue(IconTemplateProperty); set => SetValue(IconTemplateProperty, value); }
    public static readonly DependencyProperty BadgeTemplateProperty = DependencyProperty.Register(nameof(BadgeTemplate), typeof(DataTemplate), typeof(BleeckerSidebarItem), new PropertyMetadata(null, Change));
    public DataTemplate? BadgeTemplate { get => (DataTemplate?)GetValue(BadgeTemplateProperty); set => SetValue(BadgeTemplateProperty, value); }
    public static readonly DependencyProperty ActiveProperty = DependencyProperty.Register(nameof(Active), typeof(bool), typeof(BleeckerSidebarItem), new PropertyMetadata(false, Change));
    public bool Active { get => (bool)GetValue(ActiveProperty); set => SetValue(ActiveProperty, value); }
    /// <summary>Source CSS state: blocks link pointer hit testing, retains keyboard activation, and does not disable group triggers.</summary>
    public static readonly DependencyProperty DisabledProperty = DependencyProperty.Register(nameof(Disabled), typeof(bool), typeof(BleeckerSidebarItem), new PropertyMetadata(false, Change));
    public bool Disabled { get => (bool)GetValue(DisabledProperty); set => SetValue(DisabledProperty, value); }
    public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(nameof(Items), typeof(object), typeof(BleeckerSidebarItem), new PropertyMetadata(null, Change));
    public ObservableCollection<BleeckerSidebarItem> Items { get => GetValue(ItemsProperty) as ObservableCollection<BleeckerSidebarItem> ?? throw new InvalidOperationException("Sidebar item Items must resolve before mounting."); set => SetValue(ItemsProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(BleeckerSidebarItem), new PropertyMetadata(null, Change));
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(BleeckerSidebarItem), new PropertyMetadata(null, Change));
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }
}
