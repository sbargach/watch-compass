using WatchCompass.Application.Abstractions.Movies;
using WatchCompass.Application.Dtos;

namespace WatchCompass.Application.UseCases.MovieDetails;

public sealed class GetMovieDetailsUseCase
{
    private readonly IMovieCatalog _movieCatalog;

    public GetMovieDetailsUseCase(IMovieCatalog movieCatalog)
    {
        _movieCatalog = movieCatalog;
    }

    public async Task<MovieDetailsWithProviders?> GetAsync(int movieId, string? countryCode, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (movieId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(movieId), "Movie id must be positive.");
        }

        var details = await _movieCatalog.GetDetailsAsync(movieId, cancellationToken);
        if (details is null)
        {
            return null;
        }

        var providers = await _movieCatalog.GetWatchProvidersAsync(movieId, countryCode ?? string.Empty, cancellationToken);
        return new MovieDetailsWithProviders(
            details.MovieId,
            details.Title,
            details.RuntimeMinutes,
            details.Genres,
            providers,
            details.PosterUrl,
            details.BackdropUrl,
            details.ReleaseYear,
            details.Overview);
    }
}
