using System.Windows.Input;

namespace Orchard;

public enum BleeckerSonnerVariant { Default, Success, Error, Warning, Info }
public enum BleeckerSonnerPosition { TopLeft, TopCenter, TopRight, BottomLeft, BottomCenter, BottomRight }

/// <summary>Caller-owned toast content and lifetime overrides.</summary>
public class BleeckerSonnerOptions
{
    private double? duration;
    public string? Description { get; init; }
    /// <summary>Milliseconds; an explicitly supplied null, NaN, zero or negative value is persistent.</summary>
    public double? Duration { get => duration; init { duration = value; HasDuration = true; } }
    public bool HasDuration { get; private set; }
    public BleeckerSonnerAction? Action { get; init; }
    public BleeckerSonnerOptions() { }
    protected BleeckerSonnerOptions(BleeckerSonnerOptions? original)
    { if (original is not null) { Description = original.Description; duration = original.duration; HasDuration = original.HasDuration; Action = original.Action; } }
}

/// <summary>An original action; invocation does not implicitly dismiss its toast.</summary>
public sealed class BleeckerSonnerAction(string label, ICommand command, object? commandParameter = null, BleeckerButtonVariant variant = BleeckerButtonVariant.Primary)
{
    public string Label { get; } = label ?? throw new ArgumentNullException(nameof(label));
    public ICommand Command { get; } = command ?? throw new ArgumentNullException(nameof(command));
    public object? CommandParameter { get; } = commandParameter;
    public BleeckerButtonVariant Variant { get; } = Enum.IsDefined(variant) ? variant : throw new ArgumentOutOfRangeException(nameof(variant));
}

/// <summary>Consumer data passed to a mounted Sonner host; Show returns the generated dismissal ID.</summary>
public sealed class BleeckerSonnerToast : BleeckerSonnerOptions
{
    public string Title { get; }
    public BleeckerSonnerVariant Variant { get; init; }
    public BleeckerSonnerToast(string title) => Title = title ?? throw new ArgumentNullException(nameof(title));
    internal BleeckerSonnerToast(string title, BleeckerSonnerVariant variant, BleeckerSonnerOptions? options) : base(options)
    { Title = title ?? throw new ArgumentNullException(nameof(title)); Variant = variant; }
}

/// <summary>The pinned imperative API routes to the last registered mounted host on its UI thread.</summary>
public static class BleeckerToast
{
    private static BleeckerSonner? host;
    internal static void Register(BleeckerSonner value) => host = value;
    // Source effect cleanup clears the singleton even when an earlier host is
    // removed while a later host remains mounted. Never silently reroute.
    internal static void Unregister() => host = null;
    public static string Show(BleeckerSonnerToast toast) { ArgumentNullException.ThrowIfNull(toast); return host?.Show(toast) ?? string.Empty; }
    public static string Success(string title, BleeckerSonnerOptions? options = null) => Show(new(title, BleeckerSonnerVariant.Success, options));
    public static string Error(string title, BleeckerSonnerOptions? options = null) => Show(new(title, BleeckerSonnerVariant.Error, options));
    public static string Warning(string title, BleeckerSonnerOptions? options = null) => Show(new(title, BleeckerSonnerVariant.Warning, options));
    public static string Info(string title, BleeckerSonnerOptions? options = null) => Show(new(title, BleeckerSonnerVariant.Info, options));
    public static void Dismiss(string id) { ArgumentNullException.ThrowIfNull(id); host?.Dismiss(id); }
}
