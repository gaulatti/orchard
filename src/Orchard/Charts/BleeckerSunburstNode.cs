using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Orchard;

/// <summary>An original consumer-owned recursive Sunburst record.</summary>
public sealed class BleeckerSunburstNode : DependencyObject, IReadOnlyDictionary<string, object?>, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public BleeckerSunburstNode() => Children = new();
    private static DependencyProperty Property(string name, Type type) => DependencyProperty.Register(name, type, typeof(BleeckerSunburstNode), new PropertyMetadata(null,
        (owner, _) => ((BleeckerSunburstNode)owner).PropertyChanged?.Invoke(owner, new PropertyChangedEventArgs(name))));
    public static readonly DependencyProperty NameProperty = Property(nameof(Name), typeof(string));
    public static readonly DependencyProperty ValueProperty = Property(nameof(Value), typeof(object));
    public static readonly DependencyProperty FillProperty = Property(nameof(Fill), typeof(Brush));
    public static readonly DependencyProperty ChildrenProperty = Property(nameof(Children), typeof(object));
    public string Name { get => (string?)GetValue(NameProperty) ?? throw new InvalidOperationException("Sunburst node Name must be supplied."); set { ArgumentNullException.ThrowIfNull(value); SetValue(NameProperty, value); } }
    public double? Value { get => (double?)GetValue(ValueProperty); set { if (value is double number && !double.IsFinite(number)) throw new ArgumentOutOfRangeException(nameof(value)); SetValue(ValueProperty, value); } }
    public Brush? Fill { get => (Brush?)GetValue(FillProperty); set => SetValue(FillProperty, value); }
    public ObservableCollection<IReadOnlyDictionary<string, object?>> Children { get => (ObservableCollection<IReadOnlyDictionary<string, object?>>)GetValue(ChildrenProperty); set { ArgumentNullException.ThrowIfNull(value); SetValue(ChildrenProperty, value); } }
    public object? this[string key] => TryGetValue(key, out var value) ? value : throw new KeyNotFoundException(key);
    public int Count => 4;
    public IEnumerable<string> Keys => ["name", "value", "fill", "children"];
    public IEnumerable<object?> Values => [Name, Value, Fill, Children];
    public bool ContainsKey(string key) => key is "name" or "value" or "fill" or "children";
    public bool TryGetValue(string key, out object? value) { value = key switch { "name" => Name, "value" => Value, "fill" => Fill, "children" => Children, _ => null }; return ContainsKey(key); }
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => Keys.Select(key => new KeyValuePair<string, object?>(key, this[key])).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
