using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;

namespace Orchard;

/// <summary>A caller-owned section title and sequence of original navigation items.</summary>
[ContentProperty(Name = nameof(Items))]
[Microsoft.UI.Xaml.Data.Bindable]
public sealed class BleeckerFooterSection : DependencyObject
{
    public BleeckerFooterSection() { Items = new ObservableCollection<BleeckerNavItem>(); }
    internal event EventHandler? Changed;
    private static void Change(DependencyObject owner, DependencyPropertyChangedEventArgs _) => ((BleeckerFooterSection)owner).Changed?.Invoke(owner, EventArgs.Empty);
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(BleeckerFooterSection), new PropertyMetadata("", Change));
    public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(nameof(Items), typeof(object), typeof(BleeckerFooterSection), new PropertyMetadata(null, Change));
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public IEnumerable<BleeckerNavItem> Items { get => GetValue(ItemsProperty) as IEnumerable<BleeckerNavItem> ?? throw new InvalidOperationException("Footer section items must resolve before mounting."); set => SetValue(ItemsProperty, value ?? throw new ArgumentNullException(nameof(value))); }
}
