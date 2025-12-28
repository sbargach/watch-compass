namespace WatchCompass.Contracts;

/// <summary>
/// Request payload for generating personalized movie recommendations.
/// </summary>
public sealed record GetRecommendationsRequest
{
    /// <summary>
    /// Mood keyword driving the recommendation tone (e.g., Chill, FeelGood).
    /// </summary>
    public required string Mood { get; init; } = string.Empty;

    /// <summary>
    /// Available viewing time in minutes; must be between 1 and 600.
    /// </summary>
    public int TimeBudgetMinutes { get; init; }

    /// <summary>
    /// Optional free-text hint such as actor, title fragment, or vibe.
    /// </summary>
    public string? Query { get; init; }

    /// <summary>
    /// Genres to avoid in the recommendations (case-insensitive).
    /// </summary>
    public IReadOnlyList<string> AvoidGenres { get; init; } = Array.Empty<string>();

    /// <summary>
    /// ISO 3166-1 alpha-2 country code to localize availability and catalog sources.
    /// </summary>
    public required string CountryCode { get; init; } = string.Empty;
}
