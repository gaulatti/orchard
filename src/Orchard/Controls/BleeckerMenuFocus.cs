using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Runtime.CompilerServices;

namespace Orchard;

/// <summary>Connects native menu focus to the branded template's focus states.</summary>
public static class BleeckerMenuFocus
{
    private sealed record Subscription(long Token);
    private static readonly ConditionalWeakTable<Control, Subscription> Subscriptions = new();
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(BleeckerMenuFocus), new PropertyMetadata(false, OnEnabledChanged));

    public static bool GetIsEnabled(DependencyObject target) => (bool)target.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject target, bool value) => target.SetValue(IsEnabledProperty, value);

    private static void OnEnabledChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        if (target is not Control control) throw new ArgumentException("Menu focus requires a Control.", nameof(target));
        if ((bool)args.NewValue)
        {
            Subscriptions.Add(control, new Subscription(control.RegisterPropertyChangedCallback(Control.FocusStateProperty, OnFocusStateChanged)));
            control.Loaded += OnFocusChanged;
            Update(control);
        }
        else
        {
            if (Subscriptions.TryGetValue(control, out var subscription))
            {
                control.UnregisterPropertyChangedCallback(Control.FocusStateProperty, subscription.Token);
                Subscriptions.Remove(control);
            }
            control.Loaded -= OnFocusChanged;
            VisualStateManager.GoToState(control, "Unfocused", false);
        }
    }

    private static void OnFocusChanged(object sender, RoutedEventArgs args) => Update((Control)sender);
    private static void OnFocusStateChanged(DependencyObject sender, DependencyProperty property) => Update((Control)sender);
    private static void Update(Control control) => VisualStateManager.GoToState(control,
        control.FocusState == FocusState.Keyboard ? "Focused" : "Unfocused", false);
}
