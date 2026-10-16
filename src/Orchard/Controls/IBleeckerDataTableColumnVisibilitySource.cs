namespace Orchard;

/// <summary>The original consumer table's column-visibility boundary.</summary>
public interface IBleeckerDataTableColumnVisibilitySource
{
    event EventHandler? Changed;
    /// <summary>Return the original top-level columns, in the consumer's order.</summary>
    IReadOnlyList<IBleeckerDataTableVisibilityColumn> GetAllColumns();
}

/// <summary>An original consumer column. The view owns no visibility or row state.</summary>
public interface IBleeckerDataTableVisibilityColumn
{
    string Id { get; }
    bool HasAccessor { get; }
    bool CanHide { get; }
    bool IsVisible { get; }
    /// <summary>A string is displayed literally, including empty; other header values display Id.</summary>
    object? Header { get; }
    void ToggleVisibility(bool visible);
}
