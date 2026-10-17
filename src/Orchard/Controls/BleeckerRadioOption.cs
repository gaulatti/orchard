namespace Orchard;

/// <summary>An immutable, value-keyed radio option. Replace a collection entry to update it.</summary>
public sealed record BleeckerRadioOption
{
    public BleeckerRadioOption(string value,string label,string? description=null,bool Disabled=false)
    { ArgumentNullException.ThrowIfNull(value);ArgumentNullException.ThrowIfNull(label);Value=value;Label=label;Description=description;this.Disabled=Disabled; }
    public string Value { get; }
    public string Label { get; }
    public string? Description { get; }
    public bool Disabled { get; }
}

public readonly record struct BleeckerRadioRequestedEventArgs(string Value);

internal static class BleeckerRadioModel
{
    internal static void Validate(IEnumerable<BleeckerRadioOption> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var values = new HashSet<string>(StringComparer.Ordinal);
        foreach (var option in options)
        {
            ArgumentNullException.ThrowIfNull(option);
            ArgumentNullException.ThrowIfNull(option.Value);
            ArgumentNullException.ThrowIfNull(option.Label);
            if (!values.Add(option.Value)) throw new ArgumentException("Radio option values must be unique.", nameof(options));
        }
    }
}
