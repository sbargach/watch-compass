using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;
using WatchCompass.Application.Abstractions.Movies;
using WatchCompass.Infrastructure.Movies.Tmdb;
using System.Text.Json;

namespace WatchCompass.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<TmdbOptions>()
            .BindConfiguration("Tmdb")
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<TmdbOptions>, TmdbOptionsValidator>();
        services.AddTransient<BasePathHandler>();
        services.AddTransient<BearerAuthHandler>();
        services.AddTransient<ApiKeyQueryHandler>();

        var refitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions(JsonSerializerDefaults.Web))
        };

        services.AddRefitClient<ITmdbApi>(refitSettings)
            .ConfigureHttpClient((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<TmdbOptions>>().Value;
                var baseUri = new Uri(options.BaseUrl);
                var authority = baseUri.GetLeftPart(UriPartial.Authority);
                var normalizedBase = authority.EndsWith("/", StringComparison.Ordinal)
                    ? authority
                    : $"{authority}/";
                client.BaseAddress = new Uri(normalizedBase);
            })
            .AddHttpMessageHandler<BasePathHandler>()
            .AddHttpMessageHandler(sp =>
            {
                var options = sp.GetRequiredService<IOptions<TmdbOptions>>().Value;
                return options.AuthMode == TmdbAuthMode.ApiKeyQuery
                    ? sp.GetRequiredService<ApiKeyQueryHandler>()
                    : sp.GetRequiredService<BearerAuthHandler>();
            });

        services.AddScoped<IMovieCatalog, TmdbMovieCatalog>();
        return services;
    }
}
