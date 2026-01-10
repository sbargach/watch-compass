using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WatchCompass.Application.Abstractions.Movies;
using WatchCompass.Infrastructure.Movies;
using WatchCompass.Infrastructure.Movies.Tmdb;

namespace WatchCompass.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.Configure<MovieCatalogCacheOptions>(configuration.GetSection("Caching:MovieCatalog"));
        services.AddOptions<TmdbOptions>()
            .Bind(configuration.GetSection("Tmdb"))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<TmdbOptions>, TmdbOptionsValidator>();

        services.AddHttpClient<TmdbRequestExecutor>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<TmdbOptions>>().Value;
            var baseUrl = options.BaseUrl.EndsWith("/", StringComparison.Ordinal)
                ? options.BaseUrl
                : options.BaseUrl + "/";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.RequestTimeoutSeconds));
        });

        services.AddScoped<ITmdbApiClient, TmdbApiClient>();
        services.AddScoped<TmdbMovieCatalog>();
        services.AddScoped<IMovieCatalog>(sp =>
        {
            var inner = sp.GetRequiredService<TmdbMovieCatalog>();
            var cache = sp.GetRequiredService<IMemoryCache>();
            var cacheOptions = sp.GetRequiredService<IOptions<MovieCatalogCacheOptions>>();
            var logger = sp.GetRequiredService<ILogger<CachedMovieCatalog>>();
            return new CachedMovieCatalog(inner, cache, cacheOptions, logger);
        });
        return services;
    }
}
