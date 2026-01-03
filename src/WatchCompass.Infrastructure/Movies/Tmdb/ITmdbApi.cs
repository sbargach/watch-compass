using Refit;

namespace WatchCompass.Infrastructure.Movies.Tmdb;

public interface ITmdbApi
{
    [Get("/search/movie")]
    Task<TmdbSearchResponse> SearchMoviesAsync([AliasAs("query")] string query, CancellationToken cancellationToken = default);

    [Get("/movie/{movieId}")]
    Task<TmdbMovieDetailsResponse> GetMovieDetailsAsync(int movieId, CancellationToken cancellationToken = default);

    [Get("/movie/{movieId}/watch/providers")]
    Task<TmdbWatchProvidersResponse> GetWatchProvidersAsync(int movieId, CancellationToken cancellationToken = default);
}
