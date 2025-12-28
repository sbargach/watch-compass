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

    public Task<IReadOnlyList<MovieCard>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        return _movieCatalog.SearchAsync(query, cancellationToken);
    }
}
