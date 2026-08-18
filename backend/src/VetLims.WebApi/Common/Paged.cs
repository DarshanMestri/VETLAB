namespace VetLims.WebApi.Common;

/// <summary>
/// Query params for paged lists. Settable properties are required for [FromQuery] to bind.
/// </summary>
public class ListQuery
{
    public int Page { get; set; } = 1;             // 1-based
    public int PageSize { get; set; } = 20;        // max-capped in Normalize
    public string? Search { get; set; }            // optional search text
    public int? ClientId { get; set; }             // optional filters (used where applicable)
    public int? SpeciesId { get; set; }
    public DateTime? DateFrom { get; set; }         // date range filters
    public DateTime? DateTo { get; set; }
}

/// <summary>
/// Standard paged response wrapper.
/// </summary>
public class PagedResult<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
}

/// <summary>
/// Helper to clamp/normalize page and size.
/// </summary>
public static class PagingHelpers
{
    public static (int page, int size) Normalize(int page, int size, int min = 1, int max = 200)
    {
        var p = page < 1 ? 1 : page;
        var s = size < 1 ? 1 : (size > max ? max : size);
        return (p, s);
    }
}
