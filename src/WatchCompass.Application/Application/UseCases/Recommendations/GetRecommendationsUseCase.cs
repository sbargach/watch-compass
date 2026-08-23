using WatchCompass.Application.Abstractions.Movies;
using WatchCompass.Application.Dtos;
using WatchCompass.Domain.Enums;
using WatchCompass.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace WatchCompass.Application.UseCases.Recommendations;

public sealed class GetRecommendationsUseCase
{
    private const int RecommendationSearchPageSize = 20;
    private const int CandidateBatchSize = 5;
    private readonly IMovieCatalog _movieCatalog;
    private readonly ILogger<GetRecommendationsUseCase> _logger;

    public GetRecommendationsUseCase(
        IMovieCatalog movieCatalog,
        ILogger<GetRecommendationsUseCase>? logger = null)
    {
        _movieCatalog = movieCatalog;
        _logger = logger ?? NullLogger<GetRecommendationsUseCase>.Instance;
    }

    public async Task<IReadOnlyList<Recommendation>> GetRecommendationsAsync(
        Mood mood,
        TimeBudget timeBudget,
        string? query,
        IReadOnlyList<string> avoidGenres,
        string countryCode,
        int? releaseYear = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(countryCode);

        var normalizedCountryCode = countryCode.Trim().ToUpperInvariant();

        var effectiveQuery = string.IsNullOrWhiteSpace(query)
            ? mood switch
            {
                Mood.Chill => "drama",
                Mood.FeelGood => "comedy",
                Mood.Intense => "thriller",
                Mood.Scary => "horror",
                _ => "movie"
            }
            : query.Trim();

        var searchResults = await _movieCatalog.SearchPageAsync(
            effectiveQuery,
            page: 1,
            pageSize: RecommendationSearchPageSize,
            releaseYear: releaseYear,
            cancellationToken: cancellationToken);
        if (searchResults.Items.Count == 0)
        {
            return Array.Empty<Recommendation>();
        }

        var avoidSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var genre in avoidGenres)
        {
            if (string.IsNullOrWhiteSpace(genre))
            {
                continue;
            }

            avoidSet.Add(genre.Trim());
        }

        var recommendations = new List<Recommendation>(3);
        foreach (var batch in searchResults.Items.Chunk(CandidateBatchSize))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var evaluatedCandidates = await Task.WhenAll(batch.Select(movie =>
                EvaluateCandidateAsync(movie, mood, timeBudget, avoidSet, effectiveQuery, cancellationToken)));
            foreach (var candidate in evaluatedCandidates)
            {
                if (candidate is not null)
                {
                    recommendations.Add(candidate);
                }

                if (recommendations.Count == 3)
                {
                    break;
                }
            }

            if (recommendations.Count == 3)
            {
                break;
            }
        }

        return await AddProvidersAsync(recommendations, normalizedCountryCode, cancellationToken);
    }

    private async Task<IReadOnlyList<Recommendation>> AddProvidersAsync(
        IReadOnlyList<Recommendation> recommendations,
        string countryCode,
        CancellationToken cancellationToken)
    {
        return await Task.WhenAll(recommendations.Select(async recommendation =>
        {
            try
            {
                var providers = await _movieCatalog.GetWatchProvidersAsync(
                    recommendation.MovieId,
                    countryCode,
                    cancellationToken);
                return recommendation with { Providers = providers };
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to load providers for recommendation {MovieId}", recommendation.MovieId);
                return recommendation;
            }
        }));
    }

    private async Task<Recommendation?> EvaluateCandidateAsync(
        MovieCard movie,
        Mood mood,
        TimeBudget timeBudget,
        IReadOnlySet<string> avoidGenres,
        string effectiveQuery,
        CancellationToken cancellationToken)
    {
        var runtime = movie.RuntimeMinutes;
        var genres = movie.Genres ?? Array.Empty<string>();
        var title = movie.Title;
        var posterUrl = movie.PosterUrl;
        var backdropUrl = movie.BackdropUrl;
        var releaseYear = movie.ReleaseYear;
        var overview = movie.Overview;

        if (!runtime.HasValue || runtime.Value <= 0 || (avoidGenres.Count > 0 && genres.Count == 0))
        {
            try
            {
                var details = await _movieCatalog.GetDetailsAsync(movie.MovieId, cancellationToken);
                if (details is null)
                {
                    return null;
                }

                runtime = details.RuntimeMinutes;
                genres = details.Genres;
                title = details.Title;
                posterUrl ??= details.PosterUrl;
                backdropUrl ??= details.BackdropUrl;
                releaseYear ??= details.ReleaseYear;
                overview ??= details.Overview;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to evaluate recommendation candidate {MovieId}", movie.MovieId);
                return null;
            }
        }

        if (!runtime.HasValue || runtime.Value <= 0 || runtime.Value > timeBudget.Minutes)
        {
            return null;
        }

        var normalizedGenres = genres
            .Where(genre => !string.IsNullOrWhiteSpace(genre))
            .Select(genre => genre.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (normalizedGenres.Any(avoidGenres.Contains))
        {
            return null;
        }

        return new Recommendation(
            movie.MovieId,
            title,
            runtime.Value,
            normalizedGenres,
            BuildReasons(mood, timeBudget.Minutes, runtime.Value, normalizedGenres, effectiveQuery),
            Array.Empty<string>(),
            posterUrl,
            backdropUrl,
            releaseYear,
            overview);
    }

    private static IReadOnlyList<string> BuildReasons(Mood mood, int budgetMinutes, int runtimeMinutes, IReadOnlyList<string> genres, string effectiveQuery)
    {
        var reasons = new List<string>(2);
        reasons.Add($"Fits your {budgetMinutes}-minute budget with a {runtimeMinutes}-minute runtime.");

        var genrePhrase = genres.Count > 0
            ? string.Join("/", genres.Take(2))
            : effectiveQuery;
        var moodReason = $"Matches the {mood} mood through {genrePhrase}.";
        reasons.Add(moodReason);

        if (genres.Count == 0 && !string.IsNullOrWhiteSpace(effectiveQuery))
        {
            reasons.Add($"Aligned to your search for \"{effectiveQuery}\".");
        }

        return reasons;
    }
}
