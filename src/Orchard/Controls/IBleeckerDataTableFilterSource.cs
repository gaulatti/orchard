namespace Orchard;

/// <summary>The consumer's live table filtering boundary. The toolbar never owns or replaces its data.</summary>
public interface IBleeckerDataTableFilterSource
{
    event EventHandler? Changed;
    int ColumnFilterCount { get; }
    object? GlobalFilter { get; }
    IBleeckerDataTableFilterColumn? GetColumn(string id);
    /// <summary>Restore the consumer table's initial column filters, preserving its global filter.</summary>
    void ResetColumnFilters();
}

/// <summary>The original consumer column used for controlled filter reads and writes.</summary>
public interface IBleeckerDataTableFilterColumn
{
    object? GetFilterValue();
    void SetFilterValue(object? value);
}
