using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;

namespace Orchard;

/// <summary>A direct original link or a group in a Navigation Menu.</summary>
public interface IBleeckerNavigationMenuEntry { }

/// <summary>A XAML-creatable collection of original navigation entries.</summary>
public sealed class BleeckerNavigationMenuEntries : ObservableCollection<IBleeckerNavigationMenuEntry> { }

/// <summary>A XAML-creatable collection of original grouped links.</summary>
public sealed class BleeckerNavigationMenuLinks : ObservableCollection<BleeckerNavigationMenuLink> { }

/// <summary>An original navigation destination with optional grouped-link description.</summary>
[Microsoft.UI.Xaml.Data.Bindable]
public class BleeckerNavigationMenuLink : BleeckerNavItem, IBleeckerNavigationMenuEntry
{
    public BleeckerNavigationMenuLink() { }
    public BleeckerNavigationMenuLink(string href, string label, string? description = null, bool external = false) : base(href, label, external) => Description = description;
    internal event EventHandler? DescriptionChanged;
    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(nameof(Description), typeof(string), typeof(BleeckerNavigationMenuLink), new PropertyMetadata(null, (owner, _) => ((BleeckerNavigationMenuLink)owner).DescriptionChanged?.Invoke(owner, EventArgs.Empty)));
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
}

/// <summary>An original caller group. Href is retained as data and is not a navigation action.</summary>
[ContentProperty(Name = nameof(Items))]
[Microsoft.UI.Xaml.Data.Bindable]
public class BleeckerNavigationMenuGroup : DependencyObject, IBleeckerNavigationMenuEntry
{
    public BleeckerNavigationMenuGroup() => Items = new BleeckerNavigationMenuLinks();
    public BleeckerNavigationMenuGroup(string trigger, IEnumerable<BleeckerNavigationMenuLink> items, string? href = null) : this()
    { Trigger = trigger; Items = items; Href = href; }
    internal event EventHandler? Changed;
    private static void Change(DependencyObject owner, DependencyPropertyChangedEventArgs _) => ((BleeckerNavigationMenuGroup)owner).Changed?.Invoke(owner, EventArgs.Empty);
    public static readonly DependencyProperty TriggerProperty = DependencyProperty.Register(nameof(Trigger), typeof(string), typeof(BleeckerNavigationMenuGroup), new PropertyMetadata("", Change));
    public static readonly DependencyProperty HrefProperty = DependencyProperty.Register(nameof(Href), typeof(string), typeof(BleeckerNavigationMenuGroup), new PropertyMetadata(null, Change));
    public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(nameof(Items), typeof(object), typeof(BleeckerNavigationMenuGroup), new PropertyMetadata(null, Change));
    public string Trigger { get => (string)GetValue(TriggerProperty); set => SetValue(TriggerProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public string? Href { get => (string?)GetValue(HrefProperty); set => SetValue(HrefProperty, value); }
    public IEnumerable<BleeckerNavigationMenuLink> Items { get => (IEnumerable<BleeckerNavigationMenuLink>)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value ?? throw new ArgumentNullException(nameof(value))); }
}
