using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WatchCompass.Infrastructure.Movies.Tmdb;

public interface ITmdbApiClient
{
    Task<TmdbSearchResponse> SearchMoviesAsync(string query, CancellationToken cancellationToken = default);

    Task<TmdbMovieDetailsResponse> GetMovieDetailsAsync(int movieId, CancellationToken cancellationToken = default);

    Task<TmdbWatchProvidersResponse> GetWatchProvidersAsync(int movieId, string countryCode, CancellationToken cancellationToken = default);

    Task<TmdbGenreListResponse> GetGenresAsync(CancellationToken cancellationToken = default);
}

public sealed class TmdbApiClient : ITmdbApiClient
{
    private const string CountryFallback = "US";

    private readonly TmdbRequestExecutor _executor;
    private readonly TmdbOptions _options;

    public TmdbApiClient(TmdbRequestExecutor executor, IOptions<TmdbOptions> options)
    {
        _executor = executor;
        _options = options.Value;
    }

    public Task<TmdbSearchResponse> SearchMoviesAsync(string query, CancellationToken cancellationToken = default)
    {
        return _executor.SendAsync<TmdbSearchResponse>(() =>
        {
            return BuildRequest(
                "search/movie",
                new Dictionary<string, string?>
                {
                    ["query"] = query,
                    ["language"] = _options.Language,
                    ["include_adult"] = "false",
                    ["region"] = NormalizeCountry(_options.DefaultCountryCode)
                });
        }, cancellationToken);
    }

    public Task<TmdbMovieDetailsResponse> GetMovieDetailsAsync(int movieId, CancellationToken cancellationToken = default)
    {
        return _executor.SendAsync<TmdbMovieDetailsResponse>(() =>
        {
            return BuildRequest(
                $"movie/{movieId}",
                new Dictionary<string, string?>
                {
                    ["language"] = _options.Language
                });
        }, cancellationToken);
    }

    public Task<TmdbWatchProvidersResponse> GetWatchProvidersAsync(int movieId, string countryCode, CancellationToken cancellationToken = default)
    {
        return _executor.SendAsync<TmdbWatchProvidersResponse>(() =>
        {
            var region = NormalizeCountry(string.IsNullOrWhiteSpace(countryCode) ? _options.DefaultCountryCode : countryCode);
            return BuildRequest(
                $"movie/{movieId}/watch/providers",
                new Dictionary<string, string?>
                {
                    ["watch_region"] = region,
                    ["language"] = _options.Language
                });
        }, cancellationToken);
    }

    public Task<TmdbGenreListResponse> GetGenresAsync(CancellationToken cancellationToken = default)
    {
        return _executor.SendAsync<TmdbGenreListResponse>(() =>
        {
            return BuildRequest(
                "genre/movie/list",
                new Dictionary<string, string?>
                {
                    ["language"] = _options.Language
                });
        }, cancellationToken);
    }

    private HttpRequestMessage BuildRequest(string relativePath, IReadOnlyDictionary<string, string?> queryParameters)
    {
        EnsureApiKey();

        var query = string.Join("&", queryParameters
            .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value))
            .Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value!.Trim())}"));

        var uri = string.IsNullOrEmpty(query) ? relativePath : $"{relativePath}?{query}";
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private static string NormalizeCountry(string countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return CountryFallback;
        }

        return countryCode.Trim().ToUpperInvariant();
    }

    private void EnsureApiKey()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new TmdbConfigurationException("Tmdb:ApiKey is required to call TMDB.");
        }
    }
}
