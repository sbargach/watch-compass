namespace WatchCompass.Contracts;

/// <summary>
/// Response containing the recommended movies.
/// </summary>
public sealed record GetRecommendationsResponse
{
    /// <summary>
    /// Ranked list of recommendations.
    /// </summary>
    public IReadOnlyList<RecommendationDto> Items { get; init; } = Array.Empty<RecommendationDto>();
}
