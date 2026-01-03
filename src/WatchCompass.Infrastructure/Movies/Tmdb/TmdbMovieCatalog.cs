using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WatchCompass.Application.Abstractions.Movies;
using WatchCompass.Application.Dtos;

namespace WatchCompass.Infrastructure.Movies.Tmdb;

public sealed class TmdbMovieCatalog : IMovieCatalog
{
    private static readonly Meter Meter = new("watch-compass.tmdb");
    private static readonly Counter<long> CallsCounter = Meter.CreateCounter<long>("tmdb.calls.count");
    private static readonly KeyValuePair<string, object?> SearchTag = new("operation", "search");
    private static readonly KeyValuePair<string, object?> DetailsTag = new("operation", "details");
    private static readonly KeyValuePair<string, object?> ProvidersTag = new("operation", "providers");

    private readonly ITmdbApi _api;
    private readonly ILogger<TmdbMovieCatalog> _logger;
    private readonly TmdbOptions _options;

    public TmdbMovieCatalog(ITmdbApi api, IOptions<TmdbOptions> options, ILogger<TmdbMovieCatalog> logger)
    {
        _api = api;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<MovieCard>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!HasApiKey())
        {
            _logger.LogWarning("TMDB API key missing; search returning no results.");
            return Array.Empty<MovieCard>();
        }

        try
        {
            CallsCounter.Add(1, SearchTag);
            var response = await _api.SearchMoviesAsync(query, cancellationToken);
            if (response.Results.Count == 0)
            {
                return Array.Empty<MovieCard>();
            }

            return response.Results
                .Where(result => result.Id > 0 && !string.IsNullOrWhiteSpace(result.Title))
                .Select(result => new MovieCard(result.Id, result.Title, result.Runtime, Array.Empty<string>()))
                .ToList();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TMDB search failed for query {Query}", query);
            return Array.Empty<MovieCard>();
        }
    }

    public async Task<MovieDetails?> GetDetailsAsync(int movieId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!HasApiKey())
        {
            _logger.LogWarning("TMDB API key missing; movie details unavailable for {MovieId}.", movieId);
            return null;
        }

        try
        {
            CallsCounter.Add(1, DetailsTag);
            var response = await _api.GetMovieDetailsAsync(movieId, cancellationToken);
            if (response.Id == 0 || string.IsNullOrWhiteSpace(response.Title))
            {
                return null;
            }

            var genres = response.Genres
                .Select(genre => genre.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToArray();

            var runtime = response.Runtime ?? 0;
            return new MovieDetails(response.Id, response.Title, runtime, genres);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TMDB details fetch failed for {MovieId}", movieId);
            return null;
        }
    }

    public async Task<IReadOnlyList<string>> GetWatchProvidersAsync(int movieId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!HasApiKey())
        {
            _logger.LogWarning("TMDB API key missing; providers unavailable for {MovieId}.", movieId);
            return Array.Empty<string>();
        }

        try
        {
            CallsCounter.Add(1, ProvidersTag);
            var response = await _api.GetWatchProvidersAsync(movieId, cancellationToken);
            if (response.Results.Count == 0)
            {
                return Array.Empty<string>();
            }

            var country = response.Results.TryGetValue("US", out var usResult)
                ? usResult
                : response.Results.Values.FirstOrDefault();

            if (country is null)
            {
                return Array.Empty<string>();
            }

            var providers = country.FlatRate
                .Concat(country.Rent)
                .Concat(country.Buy)
                .Select(provider => provider.ProviderName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return providers;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TMDB providers fetch failed for {MovieId}", movieId);
            return Array.Empty<string>();
        }
    }

    private bool HasApiKey()
    {
        return !string.IsNullOrWhiteSpace(_options.ApiKey);
    }
}
