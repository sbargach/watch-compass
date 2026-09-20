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

        var moodGenre = mood switch
        {
            Mood.Chill => "Drama",
            Mood.FeelGood => "Comedy",
            Mood.Intense => "Thriller",
            Mood.Scary => "Horror",
            _ => throw new ArgumentOutOfRangeException(nameof(mood))
        };
        var titleHint = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        var searchResults = titleHint is null
            ? await _movieCatalog.DiscoverByGenreAsync(moodGenre, 1, RecommendationSearchPageSize,
                releaseYear, cancellationToken)
            : await _movieCatalog.SearchPageAsync(titleHint, 1, RecommendationSearchPageSize,
                releaseYear, cancellationToken);
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
        foreach (var batch in searchResults.Items.Take(RecommendationSearchPageSize).Chunk(CandidateBatchSize))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var evaluatedCandidates = await Task.WhenAll(batch.Select(movie =>
                EvaluateCandidateAsync(movie, mood, timeBudget, avoidSet, moodGenre, cancellationToken)));
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
        string moodGenre,
        CancellationToken cancellationToken)
    {
        var runtime = movie.RuntimeMinutes;
        var genres = movie.Genres ?? Array.Empty<string>();
        var title = movie.Title;
        var posterUrl = movie.PosterUrl;
        var backdropUrl = movie.BackdropUrl;
        var releaseYear = movie.ReleaseYear;
        var overview = movie.Overview;

        if (!runtime.HasValue || runtime.Value <= 0 || !genres.Any(genre => !string.IsNullOrWhiteSpace(genre)))
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
        if (!normalizedGenres.Contains(moodGenre, StringComparer.OrdinalIgnoreCase)
            || normalizedGenres.Any(avoidGenres.Contains))
        {
            return null;
        }

        return new Recommendation(
            movie.MovieId,
            title,
            runtime.Value,
            normalizedGenres,
            BuildReasons(mood, timeBudget.Minutes, runtime.Value, moodGenre),
            Array.Empty<string>(),
            posterUrl,
            backdropUrl,
            releaseYear,
            overview);
    }

    private static IReadOnlyList<string> BuildReasons(Mood mood, int budgetMinutes, int runtimeMinutes, string moodGenre)
    {
        var moodLabel = mood == Mood.FeelGood ? "Feel Good" : mood.ToString();
        return
        [
            $"Fits your {budgetMinutes}-minute budget: {runtimeMinutes} minutes.",
            $"Listed as {moodGenre}; our simple mood rule maps {moodLabel} to {moodGenre}."
        ];
    }
}
