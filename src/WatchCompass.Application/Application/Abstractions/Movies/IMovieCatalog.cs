using WatchCompass.Application.Dtos;

namespace WatchCompass.Application.Abstractions.Movies;

public interface IMovieCatalog
{
    Task<IReadOnlyList<MovieCard>> SearchAsync(string query, CancellationToken cancellationToken = default);

    Task<MovieDetails?> GetDetailsAsync(int movieId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetWatchProvidersAsync(int movieId, string countryCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetGenresAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MovieCard>> GetSimilarAsync(int movieId, CancellationToken cancellationToken = default);
}
