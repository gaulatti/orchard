using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Orchard;

/// <summary>The original node and indexed-link collections owned by a consumer.</summary>
public sealed class BleeckerSankeyChartData : DependencyObject, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public BleeckerSankeyChartData() { Nodes = new(); Links = new(); }
    private static DependencyProperty Property(string name) => DependencyProperty.Register(name, typeof(object), typeof(BleeckerSankeyChartData), new PropertyMetadata(null,
        (owner, _) => ((BleeckerSankeyChartData)owner).PropertyChanged?.Invoke(owner, new PropertyChangedEventArgs(name))));
    public static readonly DependencyProperty NodesProperty = Property(nameof(Nodes));
    public static readonly DependencyProperty LinksProperty = Property(nameof(Links));
    public ObservableCollection<IReadOnlyDictionary<string, object?>> Nodes { get => (ObservableCollection<IReadOnlyDictionary<string, object?>>)GetValue(NodesProperty); set { ArgumentNullException.ThrowIfNull(value); SetValue(NodesProperty, value); } }
    public ObservableCollection<IReadOnlyDictionary<string, object?>> Links { get => (ObservableCollection<IReadOnlyDictionary<string, object?>>)GetValue(LinksProperty); set { ArgumentNullException.ThrowIfNull(value); SetValue(LinksProperty, value); } }
}

/// <summary>A named Sankey node with an optional original native brush.</summary>
public sealed class BleeckerSankeyNode : DependencyObject, IReadOnlyDictionary<string, object?>, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private static DependencyProperty Property(string name, Type type) => DependencyProperty.Register(name, type, typeof(BleeckerSankeyNode), new PropertyMetadata(null,
        (owner, _) => ((BleeckerSankeyNode)owner).PropertyChanged?.Invoke(owner, new PropertyChangedEventArgs(name))));
    public static readonly DependencyProperty NameProperty = Property(nameof(Name), typeof(string));
    public static readonly DependencyProperty FillProperty = Property(nameof(Fill), typeof(Brush));
    public string Name { get => (string?)GetValue(NameProperty) ?? throw new InvalidOperationException("Sankey node Name must be supplied."); set { ArgumentNullException.ThrowIfNull(value); SetValue(NameProperty, value); } }
    public Brush? Fill { get => (Brush?)GetValue(FillProperty); set => SetValue(FillProperty, value); }
    public object? this[string key] => TryGetValue(key, out var value) ? value : throw new KeyNotFoundException(key);
    public int Count => 2;
    public IEnumerable<string> Keys => ["name", "fill"];
    public IEnumerable<object?> Values => [Name, Fill];
    public bool ContainsKey(string key) => key is "name" or "fill";
    public bool TryGetValue(string key, out object? value) { value = key switch { "name" => Name, "fill" => Fill, _ => null }; return ContainsKey(key); }
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => Keys.Select(key => new KeyValuePair<string, object?>(key, this[key])).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>A consumer-owned indexed flow; indexes refer to the original Nodes order.</summary>
public sealed class BleeckerSankeyLink : DependencyObject, IReadOnlyDictionary<string, object?>, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private static DependencyProperty Property(string name, Type type, object initial) => DependencyProperty.Register(name, type, typeof(BleeckerSankeyLink), new PropertyMetadata(initial,
        (owner, _) => ((BleeckerSankeyLink)owner).PropertyChanged?.Invoke(owner, new PropertyChangedEventArgs(name))));
    public static readonly DependencyProperty SourceProperty = Property(nameof(Source), typeof(int), -1);
    public static readonly DependencyProperty TargetProperty = Property(nameof(Target), typeof(int), -1);
    public static readonly DependencyProperty ValueProperty = Property(nameof(Value), typeof(double), double.NaN);
    public int Source { get => (int)GetValue(SourceProperty); set => SetValue(SourceProperty, value); }
    public int Target { get => (int)GetValue(TargetProperty); set => SetValue(TargetProperty, value); }
    public double Value { get { var value = (double)GetValue(ValueProperty); return double.IsFinite(value) ? value : throw new InvalidOperationException("Sankey flow Value must be supplied and finite."); } set { if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); SetValue(ValueProperty, value); } }
    public object? this[string key] => TryGetValue(key, out var value) ? value : throw new KeyNotFoundException(key);
    public int Count => 3;
    public IEnumerable<string> Keys => ["source", "target", "value"];
    public IEnumerable<object?> Values => [Source, Target, Value];
    public bool ContainsKey(string key) => key is "source" or "target" or "value";
    public bool TryGetValue(string key, out object? value) { value = key switch { "source" => Source, "target" => Target, "value" => Value, _ => null }; return ContainsKey(key); }
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => Keys.Select(key => new KeyValuePair<string, object?>(key, this[key])).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
