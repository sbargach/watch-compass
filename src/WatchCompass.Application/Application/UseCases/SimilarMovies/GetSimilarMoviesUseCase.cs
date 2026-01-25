using WatchCompass.Application.Abstractions.Movies;
using WatchCompass.Application.Dtos;

namespace WatchCompass.Application.UseCases.SimilarMovies;

public sealed class GetSimilarMoviesUseCase
{
    private readonly IMovieCatalog _movieCatalog;

    public GetSimilarMoviesUseCase(IMovieCatalog movieCatalog)
    {
        _movieCatalog = movieCatalog;
    }

    public Task<IReadOnlyList<MovieCard>> GetAsync(int movieId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (movieId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(movieId), "Movie id must be positive.");
        }

        return _movieCatalog.GetSimilarAsync(movieId, cancellationToken);
    }
}
