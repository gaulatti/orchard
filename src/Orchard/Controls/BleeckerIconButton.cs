using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace Orchard;

public enum BleeckerIconButtonVariant { Default, Subtle, Ghost }
public enum BleeckerIconButtonSize { Small, Medium, Large }

/// <summary>A native icon-only button. Consumers must supply an accessible name.</summary>
public class BleeckerIconButton : Button
{
    private ResourceDictionary? palette;
    private Control? foregroundContent;
    private Binding? foregroundBinding;
    private Control? observedContent;
    private long styleCallback;

    public BleeckerIconButton()
    {
        ApplyVariant();
        ApplySize();
        RegisterPropertyChangedCallback(ContentProperty, (_, _) => BindContentForeground());
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        BindContentForeground();
    }

    private void BindContentForeground()
    {
        if (observedContent is not null) observedContent.UnregisterPropertyChangedCallback(StyleProperty, styleCallback);
        observedContent = Content as Control;
        if (observedContent is not null)
            styleCallback = observedContent.RegisterPropertyChangedCallback(StyleProperty, (_, _) => BindContentForeground());
        if (foregroundContent is not null && foregroundContent.GetBindingExpression(ForegroundProperty)?.ParentBinding == foregroundBinding)
            foregroundContent.ClearValue(ForegroundProperty);
        foregroundContent = null;
        foregroundBinding = null;
        if (Content is not Control content || content.ReadLocalValue(ForegroundProperty) != DependencyProperty.UnsetValue) return;
        for (var style = content.Style; style is not null; style = style.BasedOn)
            if (style.Setters.OfType<Setter>().Any(setter => setter.Property == ForegroundProperty)) return;
        if (Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(this) == 0 ||
            Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(this, 0) is not ContentPresenter presenter) return;
        foregroundContent = content;
        foregroundBinding = new Binding { Source = presenter, Path = new PropertyPath("Foreground") };
        content.SetBinding(ForegroundProperty, foregroundBinding);
    }

    public static readonly DependencyProperty VariantProperty = DependencyProperty.Register(
        nameof(Variant), typeof(BleeckerIconButtonVariant), typeof(BleeckerIconButton),
        new PropertyMetadata(BleeckerIconButtonVariant.Default, (owner, _) => ((BleeckerIconButton)owner).ApplyVariant()));
    public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
        nameof(Size), typeof(BleeckerIconButtonSize), typeof(BleeckerIconButton),
        new PropertyMetadata(BleeckerIconButtonSize.Medium, (owner, _) => ((BleeckerIconButton)owner).ApplySize()));

    public BleeckerIconButtonVariant Variant { get => (BleeckerIconButtonVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
    public BleeckerIconButtonSize Size { get => (BleeckerIconButtonSize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>The current palette recipe, for specialized icon-button defaults.</summary>
    protected Style VariantStyle => (Style)(palette ?? throw new InvalidOperationException("Icon Button palette is not initialized."))[typeof(Button)];

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
