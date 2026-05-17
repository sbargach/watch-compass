namespace WatchCompass.Contracts;

/// <summary>
/// Response containing movies that are currently playing in theaters.
/// </summary>
public sealed record GetNowPlayingMoviesResponse
{
    /// <summary>
    /// Currently playing movie cards.
    /// </summary>
    public IReadOnlyList<MovieCardDto> Items { get; init; } = Array.Empty<MovieCardDto>();
}
