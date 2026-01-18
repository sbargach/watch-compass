using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WatchCompass.Application.Abstractions.Movies;
using WatchCompass.Application.Dtos;

namespace WatchCompass.Infrastructure.Movies.Tmdb;

public sealed class TmdbMovieCatalog : IMovieCatalog
{
    private const string DefaultCountryFallback = "US";
    private const string ImageBaseUrl = "https://image.tmdb.org/t/p";
    private const string PosterSize = "w500";
    private const string BackdropSize = "w780";

    private static readonly Meter Meter = new("watch-compass.tmdb");
    private static readonly Counter<long> CallsCounter = Meter.CreateCounter<long>("tmdb.calls.count");
    private static readonly KeyValuePair<string, object?> SearchTag = new("operation", "search");
    private static readonly KeyValuePair<string, object?> DetailsTag = new("operation", "details");
    private static readonly KeyValuePair<string, object?> ProvidersTag = new("operation", "providers");

    private readonly ITmdbApiClient _apiClient;
    private readonly ILogger<TmdbMovieCatalog> _logger;
    private readonly TmdbOptions _options;
    private readonly SemaphoreSlim _genreLock = new(1, 1);
    private IReadOnlyDictionary<int, string> _genreLookup = new Dictionary<int, string>();
    private DateTimeOffset? _genreExpiresAt;

    public TmdbMovieCatalog(ITmdbApiClient apiClient, IOptions<TmdbOptions> options, ILogger<TmdbMovieCatalog> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<MovieCard>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<MovieCard>();
        }

        EnsureApiKey();

        var trimmedQuery = query.Trim();
        CallsCounter.Add(1, SearchTag);
        try
        {
            var response = await _apiClient.SearchMoviesAsync(trimmedQuery, cancellationToken);
            var genreLookup = response.Results.Any(result => result.GenreIds.Count > 0)
                ? await GetGenreLookupAsync(cancellationToken)
                : _genreLookup;

            return response.Results
                .Where(result => result.Id > 0 && !string.IsNullOrWhiteSpace(result.Title))
                .Select(result => MapSearchResult(result, genreLookup))
                .ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "TMDB search failed for query {Query}", trimmedQuery);
            throw;
        }
    }

    public async Task<IReadOnlyList<string>> GetGenresAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureApiKey();

        var lookup = await GetGenreLookupAsync(cancellationToken);
        return lookup.Values
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<MovieDetails?> GetDetailsAsync(int movieId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (movieId <= 0)
        {
            return null;
        }

        EnsureApiKey();

        CallsCounter.Add(1, DetailsTag);
        try
        {
            var response = await _apiClient.GetMovieDetailsAsync(movieId, cancellationToken);
            if (response.Id <= 0 || string.IsNullOrWhiteSpace(response.Title))
            {
                return null;
            }

            var genres = response.Genres
                .Select(genre => genre.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim())
                .ToArray();

            var runtime = NormalizeRuntime(response.Runtime) ?? 0;
            return new MovieDetails(
                response.Id,
                response.Title.Trim(),
                runtime,
                genres,
                BuildImageUrl(response.PosterPath, PosterSize),
                BuildImageUrl(response.BackdropPath, BackdropSize),
                ParseYear(response.ReleaseDate),
                NormalizeOverview(response.Overview));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "TMDB movie details failed for {MovieId}", movieId);
            throw;
        }
    }

    public async Task<IReadOnlyList<string>> GetWatchProvidersAsync(int movieId, string countryCode, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (movieId <= 0)
        {
            return Array.Empty<string>();
        }

        EnsureApiKey();

        var normalizedCountry = NormalizeCountry(countryCode);
        CallsCounter.Add(1, ProvidersTag);
        try
        {
            var response = await _apiClient.GetWatchProvidersAsync(movieId, normalizedCountry, cancellationToken);
            if (response.Results.Count == 0)
            {
                return Array.Empty<string>();
            }

            var country = response.Results.TryGetValue(normalizedCountry, out var selected)
                ? selected
                : response.Results.OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase).FirstOrDefault().Value;

            if (country is null)
            {
                return Array.Empty<string>();
            }

            return country.FlatRate
                .Concat(country.Rent)
                .Concat(country.Buy)
                .Select(provider => provider.ProviderName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "TMDB watch providers failed for {MovieId}", movieId);
            throw;
        }
    }

    private void EnsureApiKey()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new TmdbConfigurationException("Tmdb:ApiKey is required to call TMDB.");
        }
    }

    private async Task<IReadOnlyDictionary<int, string>> GetGenreLookupAsync(CancellationToken cancellationToken)
    {
        if (_genreLookup.Count > 0 && _genreExpiresAt is not null && _genreExpiresAt > DateTimeOffset.UtcNow)
        {
            return _genreLookup;
        }

        await _genreLock.WaitAsync(cancellationToken);
        try
        {
            if (_genreLookup.Count > 0 && _genreExpiresAt is not null && _genreExpiresAt > DateTimeOffset.UtcNow)
            {
                return _genreLookup;
            }

            var response = await _apiClient.GetGenresAsync(cancellationToken);
            _genreLookup = response.Genres
                .Where(genre => genre.Id > 0 && !string.IsNullOrWhiteSpace(genre.Name))
                .GroupBy(genre => genre.Id)
                .ToDictionary(
                    group => group.Key,
                    group => group.First().Name.Trim(),
                    comparer: EqualityComparer<int>.Default);
            _genreExpiresAt = DateTimeOffset.UtcNow.AddHours(6);

            return _genreLookup;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "TMDB genre lookup failed.");
            _genreLookup = new Dictionary<int, string>();
            _genreExpiresAt = null;
            return _genreLookup;
        }
        finally
        {
            _genreLock.Release();
        }
    }

    private MovieCard MapSearchResult(TmdbMovieSearchResult result, IReadOnlyDictionary<int, string> genreLookup)
    {
        var genres = result.GenreIds
            .Select(id => genreLookup.TryGetValue(id, out var name) ? name : null)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new MovieCard(
            result.Id,
            result.Title.Trim(),
            NormalizeRuntime(result.Runtime),
            genres,
            BuildImageUrl(result.PosterPath, PosterSize),
            BuildImageUrl(result.BackdropPath, BackdropSize),
            ParseYear(result.ReleaseDate),
            NormalizeOverview(result.Overview));
    }

    private static int? NormalizeRuntime(int? runtime)
    {
        return runtime.HasValue && runtime.Value > 0 ? runtime : null;
    }

    private string NormalizeCountry(string countryCode)
    {
        var code = string.IsNullOrWhiteSpace(countryCode)
            ? _options.DefaultCountryCode
            : countryCode;

        if (string.IsNullOrWhiteSpace(code))
        {
            return DefaultCountryFallback;
        }

        return code.Trim().ToUpperInvariant();
    }

    private static string? BuildImageUrl(string? path, string size)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var trimmedPath = path.TrimStart('/');
        return $"{ImageBaseUrl}/{size}/{trimmedPath}";
    }

    private static int? ParseYear(string? releaseDate)
    {
        if (string.IsNullOrWhiteSpace(releaseDate) || releaseDate.Length < 4)
        {
            return null;
        }

        return int.TryParse(releaseDate.AsSpan(0, 4), out var year) ? year : null;
    }

    private static string? NormalizeOverview(string? overview)
    {
        if (string.IsNullOrWhiteSpace(overview))
        {
            return null;
        }

        var trimmed = overview.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }
}
