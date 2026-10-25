using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Orchard;

/// <summary>The original item, prepared content, source style and guarded activation for a custom link.</summary>
[Microsoft.UI.Xaml.Data.Bindable]
public sealed class BleeckerNavLinkContext : DependencyObject
{
    private readonly Func<BleeckerNavLinkContext, bool> canInvoke;
    private readonly Action<BleeckerNavLinkContext> invoke;
    private readonly Activation activation;
    internal BleeckerNavLinkContext(BleeckerNavMenu owner) : this(owner.CanInvoke, owner.Invoke) { }
    internal BleeckerNavLinkContext(Func<BleeckerNavLinkContext, bool> canInvoke, Action<BleeckerNavLinkContext> invoke) { this.canInvoke = canInvoke; this.invoke = invoke; activation = new(this); }
    internal bool Mounted { get; set; }
    internal FrameworkElement? Element { get; set; }
    public static readonly DependencyProperty ItemProperty = DependencyProperty.Register(nameof(Item), typeof(BleeckerNavItem), typeof(BleeckerNavLinkContext), new PropertyMetadata(null));
    public BleeckerNavItem Item { get => (BleeckerNavItem)GetValue(ItemProperty); internal set => SetValue(ItemProperty, value); }
    public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(nameof(Content), typeof(object), typeof(BleeckerNavLinkContext), new PropertyMetadata(null));
    public UIElement Content { get => (UIElement)GetValue(ContentProperty); internal set => SetValue(ContentProperty, value); }
    public static readonly DependencyProperty StyleProperty = DependencyProperty.Register(nameof(Style), typeof(Style), typeof(BleeckerNavLinkContext), new PropertyMetadata(null));
    public Style Style { get => (Style)GetValue(StyleProperty); internal set => SetValue(StyleProperty, value); }
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(BleeckerNavLinkContext), new PropertyMetadata(""));
    public string Label { get => (string)GetValue(LabelProperty); internal set => SetValue(LabelProperty, value); }
    public static readonly DependencyProperty HrefProperty = DependencyProperty.Register(nameof(Href), typeof(string), typeof(BleeckerNavLinkContext), new PropertyMetadata(""));
    public string Href { get => (string)GetValue(HrefProperty); internal set => SetValue(HrefProperty, value); }
    public static readonly DependencyProperty ExternalProperty = DependencyProperty.Register(nameof(External), typeof(bool), typeof(BleeckerNavLinkContext), new PropertyMetadata(false));
    public bool External { get => (bool)GetValue(ExternalProperty); internal set => SetValue(ExternalProperty, value); }
    public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register(nameof(Foreground), typeof(Brush), typeof(BleeckerNavLinkContext), new PropertyMetadata(null));
    public Brush Foreground { get => (Brush)GetValue(ForegroundProperty); internal set => SetValue(ForegroundProperty, value); }
    public ICommand Command => activation;
    internal void Notify() => activation.Notify();
    private sealed class Activation(BleeckerNavLinkContext context) : ICommand
    {
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => context.canInvoke(context);
        public void Execute(object? parameter) { if (CanExecute(parameter)) context.invoke(context); }
        internal void Notify() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
