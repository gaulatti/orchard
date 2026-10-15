using System.Collections;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Orchard;

/// <summary>A consumer-owned pie record using the source's default name/value/color keys.</summary>
public sealed class BleeckerPieChartDatum : DependencyObject, IReadOnlyDictionary<string, object?>, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private static DependencyProperty Property(string name, Type type, object? initial) => DependencyProperty.Register(name, type, typeof(BleeckerPieChartDatum), new PropertyMetadata(initial,
        (owner, _) => ((BleeckerPieChartDatum)owner).PropertyChanged?.Invoke(owner, new PropertyChangedEventArgs(name))));
    public static readonly DependencyProperty NameProperty = Property(nameof(Name), typeof(string), null);
    public static readonly DependencyProperty ValueProperty = Property(nameof(Value), typeof(double), double.NaN);
    public static readonly DependencyProperty ColorProperty = Property(nameof(Color), typeof(Brush), null);
    public string Name { get => (string?)GetValue(NameProperty) ?? throw new InvalidOperationException("Pie datum Name must be supplied."); set { ArgumentNullException.ThrowIfNull(value); SetValue(NameProperty, value); } }
    public double Value { get { var value = (double)GetValue(ValueProperty); return double.IsFinite(value) ? value : throw new InvalidOperationException("Pie datum Value must be supplied and finite."); } set { if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); SetValue(ValueProperty, value); } }
    public Brush? Color { get => (Brush?)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }
    public object? this[string key] => TryGetValue(key, out var value) ? value : throw new KeyNotFoundException(key);
    public int Count => 3;
    public IEnumerable<string> Keys => ["name", "value", "color"];
    public IEnumerable<object?> Values => [Name, Value, Color];
    public bool ContainsKey(string key) => key is "name" or "value" or "color";
    public bool TryGetValue(string key, out object? value) { value = key switch { "name" => Name, "value" => Value, "color" => Color, _ => null }; return ContainsKey(key); }
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => Keys.Select(key => new KeyValuePair<string, object?>(key, this[key])).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
