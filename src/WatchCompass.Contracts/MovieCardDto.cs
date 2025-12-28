namespace WatchCompass.Contracts;

/// <summary>
/// Lightweight movie summary used for search results.
/// </summary>
public sealed record MovieCardDto
{
    /// <summary>
    /// Provider-specific movie identifier.
    /// </summary>
    public int MovieId { get; init; }

    /// <summary>
    /// Display title of the movie.
    /// </summary>
    public required string Title { get; init; } = string.Empty;

    /// <summary>
    /// Optional runtime in minutes when available.
    /// </summary>
    public int? RuntimeMinutes { get; init; }

    /// <summary>
    /// Genres associated with the movie.
    /// </summary>
    public IReadOnlyList<string> Genres { get; init; } = Array.Empty<string>();
}
