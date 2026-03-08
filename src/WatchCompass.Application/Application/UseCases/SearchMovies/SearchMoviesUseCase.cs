using WatchCompass.Application.Abstractions.Movies;
using WatchCompass.Application.Dtos;

namespace WatchCompass.Application.UseCases.SearchMovies;

public sealed class SearchMoviesUseCase
{
    private readonly IMovieCatalog _movieCatalog;

    public SearchMoviesUseCase(IMovieCatalog movieCatalog)
    {
        _movieCatalog = movieCatalog;
    }

    public Task<PagedResult<MovieCard>> SearchAsync(string query, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        return _movieCatalog.SearchPageAsync(query, page, pageSize, cancellationToken);
    }
}
