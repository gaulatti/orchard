using System.Windows.Input;
using Microsoft.UI.Xaml;

namespace Orchard;

/// <summary>A consumer-owned path and its application command.</summary>
public sealed class BleeckerBreadcrumbItem : DependencyObject
{
    internal event EventHandler? Changed;
    private static void OnChanged(DependencyObject owner, DependencyPropertyChangedEventArgs _) =>
        ((BleeckerBreadcrumbItem)owner).Changed?.Invoke(owner, EventArgs.Empty);
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(BleeckerBreadcrumbItem), new PropertyMetadata("", OnChanged));
    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public static readonly DependencyProperty HrefProperty = DependencyProperty.Register(nameof(Href), typeof(string), typeof(BleeckerBreadcrumbItem), new PropertyMetadata("", OnChanged));
    public string Href { get => (string)GetValue(HrefProperty); set => SetValue(HrefProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public static readonly DependencyProperty ExternalProperty = DependencyProperty.Register(nameof(External), typeof(bool), typeof(BleeckerBreadcrumbItem), new PropertyMetadata(false, OnChanged));
    public bool External { get => (bool)GetValue(ExternalProperty); set => SetValue(ExternalProperty, value); }
    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(BleeckerBreadcrumbItem), new PropertyMetadata(null, OnChanged));
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(BleeckerBreadcrumbItem), new PropertyMetadata(null, OnChanged));
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }
}
