using Microsoft.Extensions.DependencyInjection;
using WatchCompass.Application.UseCases.Recommendations;
using WatchCompass.Application.UseCases.SearchMovies;

namespace WatchCompass.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<SearchMoviesUseCase>();
        services.AddScoped<GetRecommendationsUseCase>();
        return services;
    }
}
