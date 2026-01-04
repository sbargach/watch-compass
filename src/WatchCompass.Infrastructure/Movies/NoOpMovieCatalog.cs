using WatchCompass.Application.Abstractions.Movies;
using WatchCompass.Application.Dtos;

namespace WatchCompass.Infrastructure.Movies;

public sealed class NoOpMovieCatalog : IMovieCatalog
{
    public Task<IReadOnlyList<MovieCard>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        _ = query;
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<MovieCard>>(Array.Empty<MovieCard>());
    }

    public Task<MovieDetails?> GetDetailsAsync(int movieId, CancellationToken cancellationToken = default)
    {
        _ = movieId;
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<MovieDetails?>(null);
    }

    public Task<IReadOnlyList<string>> GetWatchProvidersAsync(int movieId, string countryCode, CancellationToken cancellationToken = default)
    {
        _ = movieId;
        _ = countryCode;
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    }
}
