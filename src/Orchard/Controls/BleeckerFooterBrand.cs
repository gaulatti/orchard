using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Orchard;

/// <summary>Caller-owned brand metadata for a Footer's large brand lockup.</summary>
[Microsoft.UI.Xaml.Data.Bindable]
public sealed class BleeckerFooterBrand : DependencyObject
{
    internal event EventHandler? Changed;
    private static DependencyProperty Property(string name, Type type, object? value) => DependencyProperty.Register(name, type, typeof(BleeckerFooterBrand), new PropertyMetadata(value, (owner, _) => ((BleeckerFooterBrand)owner).Changed?.Invoke(owner, EventArgs.Empty)));
    public static readonly DependencyProperty HrefProperty = Property(nameof(Href), typeof(string), "");
    public static readonly DependencyProperty BrandNameProperty = Property(nameof(BrandName), typeof(string), null);
    public static readonly DependencyProperty LogoAltProperty = Property(nameof(LogoAlt), typeof(string), "gaulatti");
    public static readonly DependencyProperty LogoSourceProperty = Property(nameof(LogoSource), typeof(ImageSource), null);
    public static readonly DependencyProperty DescriptionProperty = Property(nameof(Description), typeof(string), null);
    public string Href { get => (string)GetValue(HrefProperty); set => SetValue(HrefProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public string? BrandName { get => (string?)GetValue(BrandNameProperty); set => SetValue(BrandNameProperty, value); }
    public string LogoAlt { get => (string)GetValue(LogoAltProperty); set => SetValue(LogoAltProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public ImageSource? LogoSource { get => (ImageSource?)GetValue(LogoSourceProperty); set => SetValue(LogoSourceProperty, value); }
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
}
