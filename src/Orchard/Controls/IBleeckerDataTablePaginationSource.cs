namespace Orchard;

/// <summary>The original consumer's live pagination and filtered selection boundary.</summary>
public interface IBleeckerDataTablePaginationSource
{
    event EventHandler? Changed;
    int PageIndex { get; }
    int PageSize { get; }
    /// <summary>The producer's page count, including -1 for an unknown manual count.</summary>
    int PageCount { get; }
    int FilteredRowCount { get; }
    int FilteredSelectedRowCount { get; }
    bool CanPreviousPage { get; }
    bool CanNextPage { get; }
    void SetPageIndex(int pageIndex);
    void SetPageSize(int pageSize);
    void PreviousPage();
    void NextPage();
}
