namespace WatchCompass.Contracts;

/// <summary>
/// A recommended movie tailored to the requested mood and constraints.
/// </summary>
public sealed record RecommendationDto
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
    /// Runtime in minutes.
    /// </summary>
    public int RuntimeMinutes { get; init; }

    /// <summary>
    /// Human-readable reasons why the title was chosen.
    /// </summary>
    public IReadOnlyList<string> Reasons { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Streaming or purchase providers where the title is available.
    /// </summary>
    public IReadOnlyList<string> Providers { get; init; } = Array.Empty<string>();

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
