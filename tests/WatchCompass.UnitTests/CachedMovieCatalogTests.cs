using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;
using WatchCompass.Application.Abstractions.Movies;
using WatchCompass.Application.Dtos;
using WatchCompass.Infrastructure.Movies;

namespace WatchCompass.UnitTests;

[TestFixture]
public class CachedMovieCatalogTests
{
    [Test]
    public async Task SearchAsync_CachesByNormalizedQuery()
    {
        var inner = new CountingMovieCatalog();
        var catalog = CreateCatalog(inner);

        await catalog.SearchAsync(" Matrix ");
        await catalog.SearchAsync("matrix");

        inner.SearchCalls.ShouldBe(1);
    }

    [Test]
    public async Task GetWatchProvidersAsync_CachesByMovieAndCountry()
    {
        var inner = new CountingMovieCatalog();
        var catalog = CreateCatalog(inner);

        await catalog.GetWatchProvidersAsync(1, "us");
        await catalog.GetWatchProvidersAsync(1, "US");

        inner.ProviderCalls.ShouldBe(1);
    }

    private static CachedMovieCatalog CreateCatalog(CountingMovieCatalog inner)
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new MovieCatalogCacheOptions
        {
            SearchMinutes = 10,
            DetailsMinutes = 10,
            ProvidersMinutes = 10
        });

        return new CachedMovieCatalog(inner, cache, options, NullLogger<CachedMovieCatalog>.Instance);
    }

    private sealed class CountingMovieCatalog : IMovieCatalog
    {
        public int SearchCalls { get; private set; }
        public int ProviderCalls { get; private set; }

        public Task<IReadOnlyList<MovieCard>> SearchAsync(string query, CancellationToken cancellationToken = default)
        {
            SearchCalls++;
            return Task.FromResult<IReadOnlyList<MovieCard>>(new[] { new MovieCard(1, query.Trim(), 100, Array.Empty<string>()) });
        }

        public Task<MovieDetails?> GetDetailsAsync(int movieId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<MovieDetails?>(new MovieDetails(movieId, "Title", 90, Array.Empty<string>()));
        }

        public Task<IReadOnlyList<string>> GetWatchProvidersAsync(int movieId, string countryCode, CancellationToken cancellationToken = default)
        {
            ProviderCalls++;
            return Task.FromResult<IReadOnlyList<string>>(new[] { $"Provider-{countryCode}" });
        }
    }
}
