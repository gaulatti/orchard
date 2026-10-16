using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Orchard;

/// <summary>A consumer-owned term, value and optional description in a DataList.</summary>
public sealed class BleeckerDataListItem : INotifyPropertyChanged
{
    private string id = "";
    private object? label, value, description;
    public BleeckerDataListItem() { }
    public BleeckerDataListItem(string id, object? label, object? value) { Id = id; Label = label; Value = value; }
    public string Id { get => id; set { ArgumentNullException.ThrowIfNull(value); Set(ref id, value); } }
    public object? Label { get => label; set => Set(ref label, value); }
    public object? Value { get => value; set => Set(ref this.value, value); }
    public object? Description { get => description; set => Set(ref description, value); }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
