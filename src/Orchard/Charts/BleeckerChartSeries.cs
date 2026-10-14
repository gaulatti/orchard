using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System.ComponentModel;

namespace Orchard;

/// <summary>Consumer-owned data key, display name and optional native series paint.</summary>
/// <remarks>Key and Name must be supplied before consumption; empty strings are retained.
/// Null Color selects the chart's live scoped palette. An explicit brush, including
/// transparent paint, takes precedence and remains owned by the consumer.</remarks>
public sealed class BleeckerChartSeries : DependencyObject,INotifyPropertyChanged
{
    internal event EventHandler? Changed;
    public event PropertyChangedEventHandler? PropertyChanged;
    private static void OnChanged(DependencyObject owner,DependencyPropertyChangedEventArgs args)
    {
        var series=(BleeckerChartSeries)owner;
        var name=args.Property==KeyProperty?nameof(Key):args.Property==NameProperty?nameof(Name):
            args.Property==ColorProperty?nameof(Color):throw new InvalidOperationException("Unknown chart series property.");
        series.PropertyChanged?.Invoke(series,new PropertyChangedEventArgs(name));
        series.Changed?.Invoke(series,EventArgs.Empty);
    }

    public static readonly DependencyProperty KeyProperty = DependencyProperty.Register(nameof(Key),typeof(string),typeof(BleeckerChartSeries),new PropertyMetadata(null,OnChanged));
    public static readonly DependencyProperty NameProperty = DependencyProperty.Register(nameof(Name),typeof(string),typeof(BleeckerChartSeries),new PropertyMetadata(null,OnChanged));
    public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(nameof(Color),typeof(Brush),typeof(BleeckerChartSeries),new PropertyMetadata(null,OnChanged));

    public string Key
    {
        get => (string?)GetValue(KeyProperty) ?? throw new InvalidOperationException("Chart series Key must be supplied before consumption.");
        set { ArgumentNullException.ThrowIfNull(value);SetValue(KeyProperty,value); }
    }
    public string Name
    {
        get => (string?)GetValue(NameProperty) ?? throw new InvalidOperationException("Chart series Name must be supplied before consumption.");
        set { ArgumentNullException.ThrowIfNull(value);SetValue(NameProperty,value); }
    }
    public Brush? Color { get => (Brush?)GetValue(ColorProperty);set => SetValue(ColorProperty,value); }
}
