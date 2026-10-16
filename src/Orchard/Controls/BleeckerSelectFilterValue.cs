namespace Orchard;

// The source Select uses a strict internal value, not DOM input coercion.
internal static class BleeckerSelectFilterValue
{
    internal const string EmptyOption = "__bleecker_empty_option__";
    internal static object? Internal(object? value) => value is null or "" ? EmptyOption : BleeckerDataTableValue.Truthy(value) ? value : null;
    internal static string Key(BleeckerSelectOption option) => option.Value.Length == 0 ? EmptyOption : option.Value;
    internal static bool Matches(object? value, BleeckerSelectOption option) => value is string key && key == Key(option);
    internal static string Display(object? value, string placeholder, IEnumerable<BleeckerSelectOption> options) => value is null ? placeholder : string.Concat(options.Where(option => Matches(value, option)).Select(option => option.Label));
}
