using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Orchard;

/// <summary>An original consumer Select option with observable metadata.</summary>
public sealed class BleeckerSelectOption : INotifyPropertyChanged
{
    private string label = "", value = "";
    private bool disabled;
    public BleeckerSelectOption() { }
    public BleeckerSelectOption(string value, string label, bool isDisabled = false) { Value = value; Label = label; IsDisabled = isDisabled; }
    public string Value { get => value; set { ArgumentNullException.ThrowIfNull(value); if (this.value != value) { this.value = value; Notify(); } } }
    public string Label { get => label; set { ArgumentNullException.ThrowIfNull(value); if (label != value) { label = value; Notify(); } } }
    public bool IsDisabled { get => disabled; set { if (disabled != value) { disabled = value; Notify(); } } }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
