using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Orchard;

/// <summary>Original link, prepared content/style and guarded host activation for a custom renderer.</summary>
[Microsoft.UI.Xaml.Data.Bindable]
public sealed class BleeckerNavigationLinkContext : DependencyObject
{
    private readonly BleeckerNavigationMenu owner;
    private readonly Activation activation;
    private readonly CloseActivation close;
    internal BleeckerNavigationLinkContext(BleeckerNavigationMenu owner) { this.owner = owner; activation = new(this); close = new(this); }
    internal bool Mounted { get; set; }
    internal FrameworkElement? Element { get; set; }
    private static DependencyProperty Property(string name, Type type, object? value = null) => DependencyProperty.Register(name, type, typeof(BleeckerNavigationLinkContext), new PropertyMetadata(value));
    public static readonly DependencyProperty ItemProperty = Property(nameof(Item), typeof(BleeckerNavigationMenuLink));
    public static readonly DependencyProperty ContentProperty = Property(nameof(Content), typeof(object));
    public static readonly DependencyProperty StyleProperty = Property(nameof(Style), typeof(Style));
    public static readonly DependencyProperty ForegroundProperty = Property(nameof(Foreground), typeof(Brush));
    public static readonly DependencyProperty IsGroupedProperty = Property(nameof(IsGrouped), typeof(bool), false);
    public static readonly DependencyProperty CloseCommandProperty = Property(nameof(CloseCommand), typeof(ICommand));
    public BleeckerNavigationMenuLink Item { get => (BleeckerNavigationMenuLink)GetValue(ItemProperty); internal set => SetValue(ItemProperty, value); }
    public UIElement Content { get => (UIElement)GetValue(ContentProperty); internal set => SetValue(ContentProperty, value); }
    public Style Style { get => (Style)GetValue(StyleProperty); internal set => SetValue(StyleProperty, value); }
    public Brush Foreground { get => (Brush)GetValue(ForegroundProperty); internal set => SetValue(ForegroundProperty, value); }
    public bool IsGrouped { get => (bool)GetValue(IsGroupedProperty); internal set { SetValue(IsGroupedProperty, value); SetValue(CloseCommandProperty, value ? close : null); } }
    /// <summary>The source's optional onClick callback: grouped links close; direct links receive null.</summary>
    public ICommand? CloseCommand => (ICommand?)GetValue(CloseCommandProperty);
    /// <summary>Default activation: request grouped dismissal, then invoke the caller's host routing.</summary>
    public ICommand Command => activation;
    internal void Notify() { activation.Notify(); close.Notify(); }
    private sealed class Activation(BleeckerNavigationLinkContext context) : ICommand
    {
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => context.owner.CanInvoke(context, ignoreCommandCoercion: true);
        public void Execute(object? parameter) { if (CanExecute(parameter)) context.owner.Invoke(context); }
        internal void Notify() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
    private sealed class CloseActivation(BleeckerNavigationLinkContext context) : ICommand
    {
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => context.IsGrouped && context.owner.CanInvoke(context);
        public void Execute(object? parameter) { if (CanExecute(parameter)) context.owner.Close(); }
        internal void Notify() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
