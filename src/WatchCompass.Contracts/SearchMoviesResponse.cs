namespace WatchCompass.Contracts;

/// <summary>
/// Response containing the movies matching a search query.
/// </summary>
public sealed record SearchMoviesResponse
{
    /// <summary>
    /// Search results ordered by relevance.
    /// </summary>
    public IReadOnlyList<MovieCardDto> Items { get; init; } = Array.Empty<MovieCardDto>();
}
