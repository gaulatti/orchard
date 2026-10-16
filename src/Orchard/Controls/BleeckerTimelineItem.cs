using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Orchard;

public enum BleeckerTimelineStatus { Completed, Active, Pending, Error }

/// <summary>Consumer-owned Timeline metadata. A null status follows the current ordinal.</summary>
public sealed class BleeckerTimelineItem : INotifyPropertyChanged
{
    private string id = "", title = "";
    private string? description, timestamp;
    private BleeckerTimelineStatus? status;
    private object? icon;
    public BleeckerTimelineItem() { }
    public BleeckerTimelineItem(string id, string title) { Id = id; Title = title; }
    public string Id { get => id; set { ArgumentNullException.ThrowIfNull(value); Set(ref id, value); } }
    public string Title { get => title; set { ArgumentNullException.ThrowIfNull(value); Set(ref title, value); } }
    public string? Description { get => description; set => Set(ref description, value); }
    public string? Timestamp { get => timestamp; set => Set(ref timestamp, value); }
    public object? Icon { get => icon; set => Set(ref icon, value); }
    public BleeckerTimelineStatus? Status
    {
        get => status;
        set { if (value is { } state && !Enum.IsDefined(state)) throw new ArgumentOutOfRangeException(nameof(value)); Set(ref status, value); }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    internal BleeckerTimelineStatus ResolveStatus(int index) => Status ?? (index == 0 ? BleeckerTimelineStatus.Active : BleeckerTimelineStatus.Pending);
}
