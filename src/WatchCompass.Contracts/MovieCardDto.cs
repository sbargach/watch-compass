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

    /// <summary>
    /// Poster image URL sized for list tiles.
    /// </summary>
    public string? PosterUrl { get; init; }

    /// <summary>
    /// Backdrop image URL for hero or banner displays.
    /// </summary>
    public string? BackdropUrl { get; init; }

    /// <summary>
    /// Release year parsed from the release date.
    /// </summary>
    public int? ReleaseYear { get; init; }

    /// <summary>
    /// Short description of the movie.
    /// </summary>
    public string? Overview { get; init; }
}
