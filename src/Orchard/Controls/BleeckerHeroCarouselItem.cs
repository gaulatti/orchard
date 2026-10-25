using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Orchard;

public enum BleeckerHeroCarouselItemKind { Post, Project }

/// <summary>Original caller data for one hero. ImageSource stays caller-owned.</summary>
[Microsoft.UI.Xaml.Data.Bindable]
public class BleeckerHeroCarouselItem : DependencyObject
{
    public BleeckerHeroCarouselItem() => Tags = new ObservableCollection<string>();
    public BleeckerHeroCarouselItem(string href, string title, string description) : this()
    { Href = href; Title = title; Description = description; }
    internal event EventHandler? Changed;
    private static DependencyProperty Property(string name, Type type, object? initial = null) => DependencyProperty.Register(name, type, typeof(BleeckerHeroCarouselItem), new PropertyMetadata(initial, (owner, _) => ((BleeckerHeroCarouselItem)owner).Changed?.Invoke(owner, EventArgs.Empty)));
    public static readonly DependencyProperty IdProperty = Property(nameof(Id), typeof(string));
    public static readonly DependencyProperty TitleProperty = Property(nameof(Title), typeof(string), "");
    public static readonly DependencyProperty DescriptionProperty = Property(nameof(Description), typeof(string), "");
    public static readonly DependencyProperty HrefProperty = Property(nameof(Href), typeof(string), "");
    public static readonly DependencyProperty ExternalProperty = Property(nameof(External), typeof(bool), false);
    public static readonly DependencyProperty CtaLabelProperty = Property(nameof(CtaLabel), typeof(string));
    public static readonly DependencyProperty KindProperty = Property(nameof(Kind), typeof(object));
    public static readonly DependencyProperty TagsProperty = Property(nameof(Tags), typeof(object));
    public static readonly DependencyProperty ImageSourceProperty = Property(nameof(ImageSource), typeof(ImageSource));
    public string? Id { get => (string?)GetValue(IdProperty); set => SetValue(IdProperty, value); }
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public string Description { get => (string)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public string Href { get => (string)GetValue(HrefProperty); set => SetValue(HrefProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public bool External { get => (bool)GetValue(ExternalProperty); set => SetValue(ExternalProperty, value); }
    public string? CtaLabel { get => (string?)GetValue(CtaLabelProperty); set => SetValue(CtaLabelProperty, value); }
    public BleeckerHeroCarouselItemKind? Kind
    {
        get => (BleeckerHeroCarouselItemKind?)GetValue(KindProperty);
        set { if (value is { } kind && !Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(value)); SetValue(KindProperty, value); }
    }
    public IEnumerable<string> Tags { get => (IEnumerable<string>)GetValue(TagsProperty); set => SetValue(TagsProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public ImageSource? ImageSource { get => (ImageSource?)GetValue(ImageSourceProperty); set => SetValue(ImageSourceProperty, value); }
}

/// <summary>XAML-creatable ordered original items.</summary>
public sealed class BleeckerHeroCarouselItems : ObservableCollection<BleeckerHeroCarouselItem>
{
    public BleeckerHeroCarouselItems() { }
    public BleeckerHeroCarouselItems(IEnumerable<BleeckerHeroCarouselItem> items) : base(items) { }
}
