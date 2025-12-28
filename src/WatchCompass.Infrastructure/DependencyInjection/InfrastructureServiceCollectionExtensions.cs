using Microsoft.Extensions.DependencyInjection;
using WatchCompass.Application.Abstractions.Movies;
using WatchCompass.Infrastructure.Movies;

namespace WatchCompass.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IMovieCatalog, NoOpMovieCatalog>();
        return services;
    }
}
