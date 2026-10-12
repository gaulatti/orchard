using Microsoft.UI.Xaml;
using System.Windows.Input;

namespace Orchard;

/// <summary>Consumer-owned content and action for one notification.</summary>
public sealed class BleeckerNotificationItem : DependencyObject
{
    internal event EventHandler? Changed;
    private static void OnChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args) =>
        ((BleeckerNotificationItem)owner).Changed?.Invoke(owner, EventArgs.Empty);

    public static readonly DependencyProperty IdProperty = DependencyProperty.Register(nameof(Id), typeof(string), typeof(BleeckerNotificationItem), new PropertyMetadata(string.Empty, OnChanged));
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(BleeckerNotificationItem), new PropertyMetadata(string.Empty, OnChanged));
    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(nameof(Description), typeof(string), typeof(BleeckerNotificationItem), new PropertyMetadata(string.Empty, OnChanged));
    public static readonly DependencyProperty TimestampProperty = DependencyProperty.Register(nameof(Timestamp), typeof(string), typeof(BleeckerNotificationItem), new PropertyMetadata(string.Empty, OnChanged));
    public static readonly DependencyProperty IsUnreadProperty = DependencyProperty.Register(nameof(IsUnread), typeof(bool), typeof(BleeckerNotificationItem), new PropertyMetadata(false, OnChanged));
    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(BleeckerNotificationItem), new PropertyMetadata(null, OnChanged));
    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(BleeckerNotificationItem), new PropertyMetadata(null, OnChanged));

    public string Id { get => (string)GetValue(IdProperty); set => SetValue(IdProperty, value); }
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Description { get => (string)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    public string Timestamp { get => (string)GetValue(TimestampProperty); set => SetValue(TimestampProperty, value); }
    public bool IsUnread { get => (bool)GetValue(IsUnreadProperty); set => SetValue(IsUnreadProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }
}
