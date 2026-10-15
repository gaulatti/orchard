using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Orchard;

/// <summary>Consumer-owned Scatter identity, axis keys, paint and optional data.</summary>
public sealed class BleeckerScatterSeries : DependencyObject, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private static DependencyProperty Property(string name, Type type) => DependencyProperty.Register(name, type, typeof(BleeckerScatterSeries), new PropertyMetadata(null,
        (owner, _) => ((BleeckerScatterSeries)owner).PropertyChanged?.Invoke(owner, new PropertyChangedEventArgs(name))));
    public static readonly DependencyProperty KeyProperty = Property(nameof(Key), typeof(string));
    public static readonly DependencyProperty NameProperty = Property(nameof(Name), typeof(string));
    public static readonly DependencyProperty XKeyProperty = Property(nameof(XKey), typeof(string));
    public static readonly DependencyProperty YKeyProperty = Property(nameof(YKey), typeof(string));
    public static readonly DependencyProperty ZKeyProperty = Property(nameof(ZKey), typeof(string));
    public static readonly DependencyProperty ColorProperty = Property(nameof(Color), typeof(Brush));
    public static readonly DependencyProperty DataProperty = Property(nameof(Data), typeof(object));
    private string Required(DependencyProperty property) => (string?)GetValue(property) ?? throw new InvalidOperationException($"Scatter series {property} must be supplied before consumption.");
    public string Key { get => Required(KeyProperty); set { ArgumentNullException.ThrowIfNull(value); SetValue(KeyProperty, value); } }
    public string Name { get => Required(NameProperty); set { ArgumentNullException.ThrowIfNull(value); SetValue(NameProperty, value); } }
    public string XKey { get => Required(XKeyProperty); set { ArgumentNullException.ThrowIfNull(value); SetValue(XKeyProperty, value); } }
    public string YKey { get => Required(YKeyProperty); set { ArgumentNullException.ThrowIfNull(value); SetValue(YKeyProperty, value); } }
    public string? ZKey { get => (string?)GetValue(ZKeyProperty); set => SetValue(ZKeyProperty, value); }
    public Brush? Color { get => (Brush?)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }
    public ObservableCollection<IReadOnlyDictionary<string, object?>>? Data { get => (ObservableCollection<IReadOnlyDictionary<string, object?>>?)GetValue(DataProperty); set => SetValue(DataProperty, value); }
}
