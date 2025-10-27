namespace keyset.Model;


public abstract record PagedResponse<T>
{
    public IReadOnlyCollection<T> Items { get; set; } = Array.Empty<T>();
    public int PageSize { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
    public int TotalPages { get; init ; }
}