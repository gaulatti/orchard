using System.Collections;

namespace Orchard;

public sealed partial class BleeckerDataTableModel
{
    private readonly record struct ResolvedFilter(BleeckerDataTableColumnContext Column,object? Value,BleeckerDataTableFiltering Kind,string? LowerText,double Minimum,double Maximum);

    private ResolvedFilter ResolveFilter(BleeckerDataTableColumnContext column,object? value)
    {
        if(column.Definition.Filter is not null)return new(column,value,BleeckerDataTableFiltering.Auto,null,0,0);
        var kind=Filtering(column);var min=double.NegativeInfinity;var max=double.PositiveInfinity;
        if(kind==BleeckerDataTableFiltering.NumberRange)
        {
            var bounds=Bounds(value);min=ParseFloat(bounds.Low);max=ParseFloat(bounds.High);
            if(bounds.Low is null||double.IsNaN(min))min=double.NegativeInfinity;
            if(bounds.High is null||double.IsNaN(max))max=double.PositiveInfinity;
            if(min>max)(min,max)=(max,min);
        }
        // Only immutable strings are normalized here. Other caller values keep
        // their existing per-row conversion/iteration behavior.
        var lower=value is string text&&kind is BleeckerDataTableFiltering.IncludesString or BleeckerDataTableFiltering.EqualsString?text.ToLowerInvariant():null;
        return new(column,value,kind,lower,min,max);
    }
    private static bool Matches(BleeckerDataTableRow row,ResolvedFilter filter)
    {
        var column=filter.Column;var input=filter.Value;
        if(column.Definition.Filter is{} custom)return custom(row,column.Id,input);
        var value=row.GetValue(column.Id);
        return filter.Kind switch
        {
            BleeckerDataTableFiltering.IncludesString=>!Nullish(value)&&CellText(value).ToLowerInvariant().Contains(filter.LowerText??CellText(input).ToLowerInvariant(),StringComparison.Ordinal),
            BleeckerDataTableFiltering.IncludesStringSensitive=>!Nullish(value)&&CellText(value).Contains(CellText(input),StringComparison.Ordinal),
            BleeckerDataTableFiltering.EqualsString=>!Nullish(value)&&input is string&&string.Equals(CellText(value).ToLowerInvariant(),filter.LowerText,StringComparison.Ordinal),
            BleeckerDataTableFiltering.Equals=>StrictEqual(value,input),
            BleeckerDataTableFiltering.WeakEquals=>WeakEqual(value,input),
            BleeckerDataTableFiltering.NumberRange=>InRange(value,filter.Minimum,filter.Maximum),
            BleeckerDataTableFiltering.ArrayIncludes=>value is IEnumerable array&&array.Cast<object?>().Any(item=>StrictEqual(item,input)),
            BleeckerDataTableFiltering.ArrayIncludesAll=>input is IEnumerable every&&every.Cast<object?>().All(item=>value is IEnumerable elements&&elements.Cast<object?>().Any(element=>StrictEqual(element,item))),
            BleeckerDataTableFiltering.ArrayIncludesSome=>input is IEnumerable some&&some.Cast<object?>().Any(item=>value is IEnumerable elements&&elements.Cast<object?>().Any(element=>StrictEqual(element,item))),
            _=>throw new InvalidOperationException("An automatic filter must be resolved before use.")
        };
    }
    private static bool InRange(object? value,double minimum,double maximum){var number=Numeric(value);return number>=minimum&&number<=maximum;}
    private IReadOnlyList<BleeckerDataTableRow> FilterRows()
    {
        if(core.Length==0)return core;
        // Match table-core8.21.3: resolve active filters and global eligibility
        // once for this pass, before evaluating original rows. Nothing is
        // cached across data, column, filter or global-query changes.
        var active=new List<ResolvedFilter>(filters.Length);
        foreach(var filter in filters)if(GetColumn(filter.Id)is{} column)active.Add(ResolveFilter(column,filter.Value));
        var query=GlobalFilter;var globalColumns=new List<BleeckerDataTableColumnContext>();
        if(!Missing(query)&&BleeckerDataTableValue.Truthy(query))foreach(var column in LeafColumns)
            if(column.HasAccessor&&column.Definition.EnableGlobalFilter&&core[0].GetValue(column.Id)is{} first&&(first is string||Number(first)))globalColumns.Add(column);
        // An active query with no globally eligible columns is not a rejecting
        // predicate. Ordinary column filters still determine the result.
        if(active.Count==0&&globalColumns.Count==0)return core;
        var lowerQuery=query is string text?text.ToLowerInvariant():null;
        var result=new List<BleeckerDataTableRow>(core.Length);
        foreach(var row in core)
        {
            var passes=true;
            // Source evaluates each column filter for each row, including
            // caller predicates following an earlier rejecting predicate.
            foreach(var filter in active)if(!Matches(row,filter))passes=false;
            var globalPasses=globalColumns.Count==0;
            foreach(var column in globalColumns)
            {
                var value=row.GetValue(column.Id);
                if(!Nullish(value)&&CellText(value).ToLowerInvariant().Contains(lowerQuery??CellText(query).ToLowerInvariant(),StringComparison.Ordinal)){globalPasses=true;break;}
            }
            if(passes&&globalPasses)result.Add(row);
        }
        return result.ToArray();
    }
}
