namespace WatchCompass.Contracts;

/// <summary>
/// Response containing available movie genres.
/// </summary>
public sealed record GetGenresResponse
{
    /// <summary>
    /// List of TMDB genre names sorted alphabetically.
    /// </summary>
    public IReadOnlyList<string> Items { get; init; } = Array.Empty<string>();
}
