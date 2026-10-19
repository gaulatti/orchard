using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Media;

namespace Orchard;

/// <summary>A consumer-owned label, numeric value and optional segment brush.</summary>
public sealed class BleeckerProgressStackItem : INotifyPropertyChanged
{
    private string label = "";
    private double value;
    private Brush? color;
    public string Label { get => label; set { ArgumentNullException.ThrowIfNull(value); Set(ref label, value); } }
    public double Value
    {
        get => value;
        set
        {
            if (BitConverter.DoubleToInt64Bits(this.value) == BitConverter.DoubleToInt64Bits(value)) return;
            this.value = value; PropertyChanged?.Invoke(this, new(nameof(Value)));
        }
    }
    public Brush? Color { get => color; set => Set(ref color, value); }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value; PropertyChanged?.Invoke(this, new(name));
    }
}
