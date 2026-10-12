using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace Orchard;

/// <summary>A branded native menu action with consumer-composed visual content.</summary>
/// <remarks>Set Text for the accessible action name. Content should contain presentation
/// elements, not nested interactive controls; invocation belongs to the menu item.</remarks>
[ContentProperty(Name = nameof(Content))]
public sealed class BleeckerMenuContentItem : MenuFlyoutItem
{
    public BleeckerMenuContentItem() => DefaultStyleKey = typeof(BleeckerMenuContentItem);

    public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
        nameof(Content), typeof(UIElement), typeof(BleeckerMenuContentItem), new PropertyMetadata(null));

    public UIElement? Content { get => (UIElement?)GetValue(ContentProperty); set => SetValue(ContentProperty, value); }
}
