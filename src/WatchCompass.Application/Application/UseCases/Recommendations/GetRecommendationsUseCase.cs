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

    public async Task<IReadOnlyList<Recommendation>> GetRecommendationsAsync(Mood mood, TimeBudget timeBudget, string? query, IReadOnlyList<string> avoidGenres, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

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

        var searchResults = await _movieCatalog.SearchAsync(effectiveQuery, cancellationToken);
        if (searchResults.Count == 0)
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

        var recommendations = new List<Recommendation>();
        foreach (var movie in searchResults.Take(5))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var runtime = movie.RuntimeMinutes;
            var genres = movie.Genres ?? Array.Empty<string>();
            var title = movie.Title;
            var runtimeMissing = !runtime.HasValue || runtime.Value <= 0;

            if (runtimeMissing)
            {
                var details = await _movieCatalog.GetDetailsAsync(movie.MovieId, cancellationToken);
                if (details is not null)
                {
                    runtime = details.RuntimeMinutes;
                    genres = details.Genres;
                    title = details.Title;
                }
            }

            var normalizedGenres = genres
                .Where(g => !string.IsNullOrWhiteSpace(g))
                .Select(g => g.Trim())
                .ToArray();

            if (avoidSet.Count > 0 && normalizedGenres.Any(g => avoidSet.Contains(g)))
            {
                continue;
            }

            var runtimeMinutes = runtime.GetValueOrDefault();
            var runtimeKnown = runtime.HasValue && runtimeMinutes > 0;
            if (runtimeKnown && runtimeMinutes > timeBudget.Minutes)
            {
                continue;
            }

            var runtimeValue = runtimeKnown ? runtimeMinutes : timeBudget.Minutes;
            var reasons = BuildReasons(mood, timeBudget.Minutes, runtimeKnown, runtimeValue, normalizedGenres, effectiveQuery);

            recommendations.Add(new Recommendation(movie.MovieId, title, runtimeValue, reasons, Array.Empty<string>()));
            if (recommendations.Count == 3)
            {
                break;
            }
        }

        return recommendations;
    }

    private static IReadOnlyList<string> BuildReasons(Mood mood, int budgetMinutes, bool runtimeKnown, int runtimeMinutes, IReadOnlyList<string> genres, string effectiveQuery)
    {
        var reasons = new List<string>(2);
        var budgetReason = runtimeKnown
            ? $"Fits your {budgetMinutes}-minute budget with a {runtimeMinutes}-minute runtime."
            : $"Picked with your {budgetMinutes}-minute budget in mind despite an unknown runtime.";
        reasons.Add(budgetReason);

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
