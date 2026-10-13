using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Orchard;

/// <summary>Consumer-owned image and text for a position in an AvatarGroup.</summary>
public sealed class BleeckerAvatarData : DependencyObject
{
    internal event EventHandler? Changed;
    private static void OnChanged(DependencyObject owner, DependencyPropertyChangedEventArgs _) => ((BleeckerAvatarData)owner).Changed?.Invoke(owner, EventArgs.Empty);
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(nameof(Source), typeof(ImageSource), typeof(BleeckerAvatarData), new PropertyMetadata(null, OnChanged));
    public ImageSource? Source { get => (ImageSource?)GetValue(SourceProperty); set => SetValue(SourceProperty, value); }
    public static readonly DependencyProperty FallbackProperty = DependencyProperty.Register(nameof(Fallback), typeof(string), typeof(BleeckerAvatarData), new PropertyMetadata(null, OnChanged));
    public string? Fallback { get => (string?)GetValue(FallbackProperty); set => SetValue(FallbackProperty, value); }
    public static readonly DependencyProperty AltProperty = DependencyProperty.Register(nameof(Alt), typeof(string), typeof(BleeckerAvatarData), new PropertyMetadata(null, OnChanged));
    public string? Alt { get => (string?)GetValue(AltProperty); set => SetValue(AltProperty, value); }
}
