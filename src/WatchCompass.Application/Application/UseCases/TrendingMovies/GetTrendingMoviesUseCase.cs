using System.Linq;
using WatchCompass.Application.Abstractions.Movies;
using WatchCompass.Application.Dtos;

namespace WatchCompass.Application.UseCases.TrendingMovies;

public sealed class GetTrendingMoviesUseCase
{
    private readonly IMovieCatalog _movieCatalog;

    public GetTrendingMoviesUseCase(IMovieCatalog movieCatalog)
    {
        _movieCatalog = movieCatalog;
    }

    public async Task<IReadOnlyList<MovieCard>> GetAsync(int limit, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be positive.");
        }

        var movies = await _movieCatalog.GetTrendingAsync(cancellationToken);
        return movies.Take(limit).ToList();
    }
}
