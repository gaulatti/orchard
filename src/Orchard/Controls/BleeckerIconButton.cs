using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Orchard;

public enum BleeckerIconButtonVariant { Default, Subtle, Ghost }
public enum BleeckerIconButtonSize { Small, Medium, Large }

/// <summary>A native icon-only button. Consumers must supply an accessible name.</summary>
public sealed class BleeckerIconButton : Button
{
    private ResourceDictionary? palette;

    public BleeckerIconButton()
    {
        ApplyVariant();
        ApplySize();
    }

    public static readonly DependencyProperty VariantProperty = DependencyProperty.Register(
        nameof(Variant), typeof(BleeckerIconButtonVariant), typeof(BleeckerIconButton),
        new PropertyMetadata(BleeckerIconButtonVariant.Default, (owner, _) => ((BleeckerIconButton)owner).ApplyVariant()));
    public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
        nameof(Size), typeof(BleeckerIconButtonSize), typeof(BleeckerIconButton),
        new PropertyMetadata(BleeckerIconButtonSize.Medium, (owner, _) => ((BleeckerIconButton)owner).ApplySize()));

    public BleeckerIconButtonVariant Variant { get => (BleeckerIconButtonVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
    public BleeckerIconButtonSize Size { get => (BleeckerIconButtonSize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    private void ApplyVariant()
    {
        if (!Enum.IsDefined(Variant)) throw new ArgumentOutOfRangeException(nameof(Variant));
        if (palette is not null) Resources.MergedDictionaries.Remove(palette);
        palette = new ResourceDictionary { Source = new Uri($"ms-appx:///Orchard/Themes/Buttons/Icon{Variant}.g.xaml") };
        Resources.MergedDictionaries.Add(palette);
        Style = (Style)palette[typeof(Button)];
    }

    private void ApplySize()
    {
        var dimension = Size switch
        {
            BleeckerIconButtonSize.Small => 32,
            BleeckerIconButtonSize.Medium => 40,
            BleeckerIconButtonSize.Large => 44,
            _ => throw new ArgumentOutOfRangeException(nameof(Size))
        };
        Width = MinWidth = dimension;
        Height = MinHeight = dimension;
        Padding = new Thickness(0);
    }
}
