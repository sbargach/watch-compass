using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WatchCompass.Application.Abstractions.Movies;
using WatchCompass.Application.Dtos;

namespace WatchCompass.Infrastructure.Movies;

public sealed class CachedMovieCatalog : IMovieCatalog
{
    private readonly IMovieCatalog _inner;
    private readonly IMemoryCache _cache;
    private readonly MovieCatalogCacheOptions _options;
    private readonly ILogger<CachedMovieCatalog> _logger;
    private const string GenresKey = "genres";

    public CachedMovieCatalog(IMovieCatalog inner, IMemoryCache cache, IOptions<MovieCatalogCacheOptions> options, ILogger<CachedMovieCatalog> logger)
    {
        _inner = inner;
        _cache = cache;
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

        var key = $"search:{query.Trim().ToLowerInvariant()}";
        if (_cache.TryGetValue(key, out IReadOnlyList<MovieCard>? cachedResults) && cachedResults is not null)
        {
            return cachedResults;
        }

        var results = await _inner.SearchAsync(query, cancellationToken);
        return CacheOrReturn(key, results, _options.SearchDuration);
    }

    public async Task<MovieDetails?> GetDetailsAsync(int movieId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (movieId <= 0)
        {
            return null;
        }

        var key = $"details:{movieId}";
        if (_cache.TryGetValue(key, out MovieDetails? cachedDetails) && cachedDetails is not null)
        {
            return cachedDetails;
        }

        var details = await _inner.GetDetailsAsync(movieId, cancellationToken);
        if (details is null)
        {
            return null;
        }

        return CacheOrReturn(key, details, _options.DetailsDuration);
    }

    public async Task<IReadOnlyList<string>> GetWatchProvidersAsync(int movieId, string countryCode, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (movieId <= 0)
        {
            return Array.Empty<string>();
        }

        var normalizedCountry = NormalizeCountry(countryCode);
        var key = $"providers:{movieId}:{normalizedCountry}";
        if (_cache.TryGetValue(key, out IReadOnlyList<string>? cachedProviders) && cachedProviders is not null)
        {
            return cachedProviders;
        }

        var providers = await _inner.GetWatchProvidersAsync(movieId, normalizedCountry, cancellationToken);
        return CacheOrReturn(key, providers, _options.ProvidersDuration);
    }

    public async Task<IReadOnlyList<string>> GetGenresAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_cache.TryGetValue(GenresKey, out IReadOnlyList<string>? cached) && cached is not null)
        {
            return cached;
        }

        var genres = await _inner.GetGenresAsync(cancellationToken);
        return CacheOrReturn(GenresKey, genres, _options.GenresDuration);
    }

    private T CacheOrReturn<T>(string key, T value, TimeSpan duration)
    {
        if (duration > TimeSpan.Zero)
        {
            try
            {
                _cache.Set(key, value, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = duration
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cache entry {CacheKey}", key);
            }
        }

        return value;
    }

    private static string NormalizeCountry(string countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return string.Empty;
        }

        return countryCode.Trim().ToUpperInvariant();
    }
}
