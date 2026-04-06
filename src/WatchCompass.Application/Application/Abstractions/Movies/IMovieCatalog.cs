using WatchCompass.Application.Dtos;

namespace WatchCompass.Application.Abstractions.Movies;

public interface IMovieCatalog
{
    Task<IReadOnlyList<MovieCard>> SearchAsync(string query, CancellationToken cancellationToken = default);

    Task<PagedResult<MovieCard>> SearchPageAsync(
        string query,
        int page,
        int pageSize,
        int? releaseYear = null,
        CancellationToken cancellationToken = default);

    Task<PagedResult<MovieCard>> DiscoverByGenreAsync(
        string genre,
        int page,
        int pageSize,
        int? releaseYear = null,
        CancellationToken cancellationToken = default);

    Task<MovieDetails?> GetDetailsAsync(int movieId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetWatchProvidersAsync(int movieId, string countryCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetGenresAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MovieCard>> GetSimilarAsync(int movieId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MovieCard>> GetTrendingAsync(CancellationToken cancellationToken = default);
}
