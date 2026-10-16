using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Orchard;

/// <summary>Consumer-owned FilterGroup data and optional removal action.</summary>
public sealed class BleeckerFilter : INotifyPropertyChanged
{
    private string label = "";
    private string? value;
    private ICommand? command;
    private object? parameter;
    private EventHandler? removed;
    public BleeckerFilter() { }
    public BleeckerFilter(string label, string? value = null) { Label = label; Value = value; }
    public string Label { get => label; set { ArgumentNullException.ThrowIfNull(value); Set(ref label, value); } }
    public string? Value { get => value; set => Set(ref this.value, value); }
    public ICommand? RemoveCommand { get => command; set => Set(ref command, value); }
    public object? RemoveCommandParameter { get => parameter; set => Set(ref parameter, value); }
    public event EventHandler? RemoveRequested
    {
        add { removed += value; PropertyChanged?.Invoke(this, new(nameof(HasRemoveHandler))); }
        remove { removed -= value; PropertyChanged?.Invoke(this, new(nameof(HasRemoveHandler))); }
    }
    internal bool HasRemoveHandler => removed is not null;
    internal void NotifyRemove() => removed?.Invoke(this, EventArgs.Empty);
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Set<T>(ref T field,T value,[CallerMemberName]string? name=null)
    { if(EqualityComparer<T>.Default.Equals(field,value))return;field=value;PropertyChanged?.Invoke(this,new(name)); }
}
