using WatchCompass.Application.Abstractions.Movies;

namespace WatchCompass.Application.UseCases.Genres;

public sealed class GetGenresUseCase
{
    private readonly IMovieCatalog _movieCatalog;

    public GetGenresUseCase(IMovieCatalog movieCatalog)
    {
        _movieCatalog = movieCatalog;
    }

    public Task<IReadOnlyList<string>> GetAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _movieCatalog.GetGenresAsync(cancellationToken);
    }
}
