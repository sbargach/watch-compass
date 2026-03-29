using Microsoft.Extensions.DependencyInjection;
using WatchCompass.Application.UseCases.DiscoverMovies;
using WatchCompass.Application.UseCases.Recommendations;
using WatchCompass.Application.UseCases.MovieDetails;
using WatchCompass.Application.UseCases.SearchMovies;
using WatchCompass.Application.UseCases.Genres;
using WatchCompass.Application.UseCases.SimilarMovies;
using WatchCompass.Application.UseCases.TrendingMovies;

namespace WatchCompass.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<SearchMoviesUseCase>();
        services.AddScoped<DiscoverMoviesByGenreUseCase>();
        services.AddScoped<GetRecommendationsUseCase>();
        services.AddScoped<GetMovieDetailsUseCase>();
        services.AddScoped<GetGenresUseCase>();
        services.AddScoped<GetSimilarMoviesUseCase>();
        services.AddScoped<GetTrendingMoviesUseCase>();
        return services;
    }
}
