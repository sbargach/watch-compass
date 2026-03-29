namespace WatchCompass.Contracts;

/// <summary>
/// Response containing a paged set of movie cards.
/// </summary>
public sealed record SearchMoviesResponse
{
    /// <summary>
    /// Movie results for the current query or browse request.
    /// </summary>
    public IReadOnlyList<MovieCardDto> Items { get; init; } = Array.Empty<MovieCardDto>();

    /// <summary>
    /// 1-based page requested by the client.
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// Number of items requested per page.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Total number of matching results reported by the provider.
    /// </summary>
    public int TotalResults { get; init; }

    /// <summary>
    /// Total number of pages available for the requested page size.
    /// </summary>
    public int TotalPages { get; init; }

    /// <summary>
    /// Indicates whether another page is available after the current page.
    /// </summary>
    public bool HasNextPage { get; init; }
}
