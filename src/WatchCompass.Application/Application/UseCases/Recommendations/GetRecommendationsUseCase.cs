using WatchCompass.Application.Abstractions.Movies;
using WatchCompass.Application.Dtos;
using WatchCompass.Domain.Enums;
using WatchCompass.Domain.ValueObjects;

namespace WatchCompass.Application.UseCases.Recommendations;

public sealed class GetRecommendationsUseCase
{
    private readonly IMovieCatalog _movieCatalog;

    public GetRecommendationsUseCase(IMovieCatalog movieCatalog)
    {
        _movieCatalog = movieCatalog;
    }

    public Task<IReadOnlyList<Recommendation>> GetRecommendationsAsync(Mood mood, TimeBudget timeBudget, string? query, IReadOnlyList<string> avoidGenres, string countryCode, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = _movieCatalog;
        _ = mood;
        _ = timeBudget;
        _ = query;
        _ = avoidGenres;
        _ = countryCode;

        return Task.FromResult<IReadOnlyList<Recommendation>>(Array.Empty<Recommendation>());
    }
}
