namespace Orchard;

// Keep the original prop separate from Radix's internal state when an empty
// prop without an empty option makes its Root uncontrolled.
internal sealed class BleeckerSelectModel
{
    internal const string EmptyOption="__bleecker_empty_option__";
    private string? uncontrolled;
    internal readonly record struct Metrics(double Height,double Font,double Line,double Padding,double Gap,double OptionHeight,double OptionPadding);
    internal static Metrics Sizes(int size)=>size switch{0=>new(36,13,19.5,12,8,32,6),1=>new(40,14,20,14,12,36,6),2=>new(44,15,22.5,16,12,40,8),_=>throw new ArgumentOutOfRangeException(nameof(size))};
    internal static string Key(BleeckerSelectOption option)=>option.Value.Length==0?EmptyOption:option.Value;
    internal static string? Prop(string value,IReadOnlyList<BleeckerSelectOption> options)=>value.Length==0?options.Any(option=>option.Value.Length==0)?EmptyOption:null:value;
    internal string? Effective(string value,IReadOnlyList<BleeckerSelectOption> options)=>Prop(value,options)??uncontrolled;
    internal bool Matches(string value,IReadOnlyList<BleeckerSelectOption> options,BleeckerSelectOption option)=>Effective(value,options) is {} current&&current==Key(option);
    internal string Display(string value,string placeholder,IReadOnlyList<BleeckerSelectOption> options)=>Effective(value,options) is not {} current?placeholder:string.Concat(options.Where(option=>Key(option)==current).Select(option=>option.Label));
    internal string? Request(string value,IReadOnlyList<BleeckerSelectOption> options,BleeckerSelectOption option)
    {
        if(!options.Any(original=>ReferenceEquals(original,option))||option.IsDisabled)return null;
        var next=Key(option);if(Effective(value,options)==next)return null;
        if(Prop(value,options)is null)uncontrolled=next;
        return next==EmptyOption?"":next;
    }
}
