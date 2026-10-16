using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Orchard;

public enum BleeckerStepStatus { Pending, Active, Completed, Error }

/// <summary>Consumer-owned metadata for a Stepper. A null status follows ActiveStep.</summary>
public sealed class BleeckerStep : INotifyPropertyChanged
{
    private string id = "", label = "";
    private string? description;
    private BleeckerStepStatus? status;
    private bool disabled;

    public BleeckerStep() { }
    public BleeckerStep(string id, string label) { Id = id; Label = label; }
    public string Id { get => id; set { ArgumentNullException.ThrowIfNull(value); Set(ref id, value); } }
    public string Label { get => label; set { ArgumentNullException.ThrowIfNull(value); Set(ref label, value); } }
    public string? Description { get => description; set => Set(ref description, value); }
    public BleeckerStepStatus? Status
    {
        get => status;
        set { if (value is { } state && !Enum.IsDefined(state)) throw new ArgumentOutOfRangeException(nameof(value)); Set(ref status, value); }
    }
    public bool IsDisabled { get => disabled; set => Set(ref disabled, value); }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

internal static class BleeckerStepperState
{
    internal static BleeckerStepStatus Resolve(IReadOnlyList<BleeckerStep> steps, int index, string? activeStep)
    {
        if (steps[index].Status is { } status) return status;
        var active = -1;
        if (!string.IsNullOrEmpty(activeStep))
            for (var i = 0; i < steps.Count; i++) if (steps[i].Id == activeStep) { active = i; break; }
        return index < active ? BleeckerStepStatus.Completed : index == active ? BleeckerStepStatus.Active : BleeckerStepStatus.Pending;
    }
}
