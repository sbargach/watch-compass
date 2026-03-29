using WatchCompass.Application.Abstractions.Movies;
using WatchCompass.Application.Dtos;

namespace WatchCompass.Application.UseCases.DiscoverMovies;

public sealed class DiscoverMoviesByGenreUseCase
{
    private readonly IMovieCatalog _movieCatalog;

    public DiscoverMoviesByGenreUseCase(IMovieCatalog movieCatalog)
    {
        _movieCatalog = movieCatalog;
    }

    public Task<PagedResult<MovieCard>> GetAsync(string genre, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(genre);
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        return _movieCatalog.DiscoverByGenreAsync(genre, page, pageSize, cancellationToken);
    }
}
