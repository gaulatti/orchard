using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Orchard;

/// <summary>Consumer-owned Radar key, legend name, optional paint and fill.</summary>
public sealed class BleeckerRadarSeries : DependencyObject, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private static DependencyProperty Property(string name, Type type, object? initial = null) => DependencyProperty.Register(name, type, typeof(BleeckerRadarSeries),
        new PropertyMetadata(initial, (owner, _) => ((BleeckerRadarSeries)owner).PropertyChanged?.Invoke(owner, new(name))));
    public static readonly DependencyProperty KeyProperty = Property(nameof(Key), typeof(string));
    public static readonly DependencyProperty NameProperty = Property(nameof(Name), typeof(string));
    public static readonly DependencyProperty ColorProperty = Property(nameof(Color), typeof(Brush));
    public static readonly DependencyProperty FillProperty = Property(nameof(Fill), typeof(bool), false);
    public string Key { get => (string?)GetValue(KeyProperty) ?? throw new InvalidOperationException("Radar series Key must be supplied before consumption."); set { ArgumentNullException.ThrowIfNull(value); SetValue(KeyProperty, value); } }
    public string Name { get => (string?)GetValue(NameProperty) ?? throw new InvalidOperationException("Radar series Name must be supplied before consumption."); set { ArgumentNullException.ThrowIfNull(value); SetValue(NameProperty, value); } }
    public Brush? Color { get => (Brush?)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }
    public bool Fill { get => (bool)GetValue(FillProperty); set => SetValue(FillProperty, value); }
}
