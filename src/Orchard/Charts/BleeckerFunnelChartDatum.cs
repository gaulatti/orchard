using System.Collections;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Orchard;

/// <summary>A consumer-owned funnel record using the source's default name/value/fill keys.</summary>
public sealed class BleeckerFunnelChartDatum : DependencyObject, IReadOnlyDictionary<string, object?>, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private static DependencyProperty Property(string name, Type type, object? initial) => DependencyProperty.Register(name, type, typeof(BleeckerFunnelChartDatum), new PropertyMetadata(initial,
        (owner, _) => ((BleeckerFunnelChartDatum)owner).PropertyChanged?.Invoke(owner, new PropertyChangedEventArgs(name))));
    public static readonly DependencyProperty NameProperty = Property(nameof(Name), typeof(string), null);
    public static readonly DependencyProperty ValueProperty = Property(nameof(Value), typeof(double), double.NaN);
    public static readonly DependencyProperty FillProperty = Property(nameof(Fill), typeof(Brush), null);
    public string Name { get => (string?)GetValue(NameProperty) ?? throw new InvalidOperationException("Funnel datum Name must be supplied."); set { ArgumentNullException.ThrowIfNull(value); SetValue(NameProperty, value); } }
    public double Value { get { var value = (double)GetValue(ValueProperty); return double.IsFinite(value) ? value : throw new InvalidOperationException("Funnel datum Value must be supplied and finite."); } set { if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); SetValue(ValueProperty, value); } }
    public Brush? Fill { get => (Brush?)GetValue(FillProperty); set => SetValue(FillProperty, value); }
    public object? this[string key] => TryGetValue(key, out var value) ? value : throw new KeyNotFoundException(key);
    public int Count => 3;
    public IEnumerable<string> Keys => ["name", "value", "fill"];
    public IEnumerable<object?> Values => [Name, Value, Fill];
    public bool ContainsKey(string key) => key is "name" or "value" or "fill";
    public bool TryGetValue(string key, out object? value) { value = key switch { "name" => Name, "value" => Value, "fill" => Fill, _ => null }; return ContainsKey(key); }
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => Keys.Select(key => new KeyValuePair<string, object?>(key, this[key])).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
