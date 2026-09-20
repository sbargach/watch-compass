using System.Text.Json;
using WatchCompass.Application.Abstractions.Movies;
using WatchCompass.Application.Dtos;

namespace WatchCompass.Infrastructure.Movies.Demo;

public sealed class DemoMovieCatalog : IMovieCatalog
{
    private readonly DemoData _data;
    private readonly IReadOnlyDictionary<int, DemoMovie> _movies;

    public DemoMovieCatalog()
    {
        using var stream = typeof(DemoMovieCatalog).Assembly.GetManifestResourceStream(
            "WatchCompass.Infrastructure.Movies.Demo.catalog.json")
            ?? throw new InvalidOperationException("The bundled demo catalog is missing.");
        _data = JsonSerializer.Deserialize<DemoData>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("The bundled demo catalog is empty.");
        _movies = _data.Movies.ToDictionary(movie => movie.Details.MovieId);
    }

    public async Task<IReadOnlyList<MovieCard>> SearchAsync(string query, CancellationToken cancellationToken = default)
        => (await SearchPageAsync(query, 1, 20, cancellationToken: cancellationToken)).Items;

    public Task<PagedResult<MovieCard>> SearchPageAsync(string query, int page, int pageSize,
        int? releaseYear = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var matches = _data.Movies.Where(movie => !string.IsNullOrWhiteSpace(query)
            && movie.Details.Title.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(Page(matches, page, pageSize, releaseYear));
    }

    public Task<PagedResult<MovieCard>> DiscoverByGenreAsync(string genre, int page, int pageSize,
        int? releaseYear = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var matches = _data.Movies.Where(movie => movie.Details.Genres.Contains(
            genre.Trim(), StringComparer.OrdinalIgnoreCase));
        return Task.FromResult(Page(matches, page, pageSize, releaseYear));
    }

    public Task<MovieDetails?> GetDetailsAsync(int movieId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_movies.GetValueOrDefault(movieId)?.Details);
    }

    public Task<IReadOnlyList<string>> GetWatchProvidersAsync(int movieId, string countryCode,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var region = string.IsNullOrWhiteSpace(countryCode) ? "NL" : countryCode.Trim().ToUpperInvariant();
        IReadOnlyList<string> providers = _movies.GetValueOrDefault(movieId)?.Providers.GetValueOrDefault(region) ?? [];
        return Task.FromResult(providers);
    }

    public Task<IReadOnlyList<string>> GetGenresAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<string> genres = _data.Movies.SelectMany(movie => movie.Details.Genres)
            .Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray();
        return Task.FromResult(genres);
    }

    public Task<IReadOnlyList<MovieCard>> GetSimilarAsync(int movieId, CancellationToken cancellationToken = default)
        => GetCards(_movies.GetValueOrDefault(movieId)?.SimilarIds ?? [], cancellationToken);

    public Task<IReadOnlyList<MovieCard>> GetTrendingAsync(CancellationToken cancellationToken = default)
        => GetCards(_data.TrendingIds, cancellationToken);

    public Task<IReadOnlyList<MovieCard>> GetNowPlayingAsync(CancellationToken cancellationToken = default)
        => GetCards(_data.NowPlayingIds, cancellationToken);

    private Task<IReadOnlyList<MovieCard>> GetCards(IEnumerable<int> ids, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<MovieCard> cards = ids.Select(id => ToCard(_movies[id].Details)).ToArray();
        return Task.FromResult(cards);
    }

    private static PagedResult<MovieCard> Page(IEnumerable<DemoMovie> movies, int page, int pageSize, int? releaseYear)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(page);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);
        var matches = movies.Where(movie => releaseYear is null || movie.Details.ReleaseYear == releaseYear).ToArray();
        var totalPages = (int)Math.Ceiling(matches.Length / (double)pageSize);
        var offset = (long)(page - 1) * pageSize;
        var items = offset >= matches.Length ? [] : matches.Skip((int)offset).Take(pageSize)
            .Select(movie => ToCard(movie.Details)).ToArray();
        return new PagedResult<MovieCard>(items, page, pageSize, matches.Length, totalPages, page < totalPages);
    }

    private static MovieCard ToCard(MovieDetails details) => new(details.MovieId, details.Title,
        details.RuntimeMinutes, details.Genres, details.PosterUrl, details.BackdropUrl, details.ReleaseYear, details.Overview);

    private sealed record DemoData(DemoMovie[] Movies, int[] TrendingIds, int[] NowPlayingIds);
    private sealed record DemoMovie(MovieDetails Details, Dictionary<string, string[]> Providers, int[] SimilarIds);
}
