using System.Windows.Input;
using Microsoft.UI.Xaml;

namespace Orchard;

/// <summary>Original item and prepared children for a caller's native link template.</summary>
[Microsoft.UI.Xaml.Data.Bindable]
public sealed class BleeckerHeroCarouselLinkContext : DependencyObject
{
    private readonly BleeckerHeroCarousel owner;
    private readonly Activation activation;
    internal BleeckerHeroCarouselLinkContext(BleeckerHeroCarousel owner) { this.owner = owner; activation = new(this); }
    internal bool Mounted { get; set; }
    internal FrameworkElement? Element { get; set; }
    private static DependencyProperty Property(string name, Type type, object? initial = null) => DependencyProperty.Register(name, type, typeof(BleeckerHeroCarouselLinkContext), new PropertyMetadata(initial));
    public static readonly DependencyProperty ItemProperty = Property(nameof(Item), typeof(BleeckerHeroCarouselItem));
    public static readonly DependencyProperty ContentProperty = Property(nameof(Content), typeof(object));
    public static readonly DependencyProperty StyleProperty = Property(nameof(Style), typeof(Style));
    public static readonly DependencyProperty AccessibleNameProperty = Property(nameof(AccessibleName), typeof(string), "");
    public static readonly DependencyProperty IsActiveProperty = Property(nameof(IsActive), typeof(bool), false);
    public BleeckerHeroCarouselItem Item { get => (BleeckerHeroCarouselItem)GetValue(ItemProperty); internal set => SetValue(ItemProperty, value); }
    public UIElement Content { get => (UIElement)GetValue(ContentProperty); internal set => SetValue(ContentProperty, value); }
    public Style? Style { get => (Style?)GetValue(StyleProperty); internal set => SetValue(StyleProperty, value); }
    public string AccessibleName { get => (string)GetValue(AccessibleNameProperty); internal set => SetValue(AccessibleNameProperty, value); }
    public bool IsActive { get => (bool)GetValue(IsActiveProperty); internal set => SetValue(IsActiveProperty, value); }
    public ICommand Command => activation;
    internal void Notify() => activation.Notify();
    private sealed class Activation(BleeckerHeroCarouselLinkContext context) : ICommand
    {
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => context.owner.CanInvoke(context, true);
        public void Execute(object? parameter) { if (context.owner.CanInvoke(context, false)) context.owner.Invoke(context); }
        internal void Notify() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
