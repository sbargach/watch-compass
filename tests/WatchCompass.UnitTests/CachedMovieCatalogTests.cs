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

        inner.SearchPageCalls.ShouldBe(1);
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

    [Test]
    public async Task DiscoverByGenreAsync_CachesByNormalizedGenre()
    {
        var inner = new CountingMovieCatalog();
        var catalog = CreateCatalog(inner);

        await catalog.DiscoverByGenreAsync(" Action ", 1, 12);
        await catalog.DiscoverByGenreAsync("action", 1, 12);

        inner.DiscoverCalls.ShouldBe(1);
    }

    [Test]
    public async Task GetSimilarAsync_CachesByMovie()
    {
        var inner = new CountingMovieCatalog();
        var catalog = CreateCatalog(inner);

        await catalog.GetSimilarAsync(5);
        await catalog.GetSimilarAsync(5);

        inner.SimilarCalls.ShouldBe(1);
    }

    [Test]
    public async Task GetTrendingAsync_CachesTrendingResults()
    {
        var inner = new CountingMovieCatalog();
        var catalog = CreateCatalog(inner);

        await catalog.GetTrendingAsync();
        await catalog.GetTrendingAsync();

        inner.TrendingCalls.ShouldBe(1);
    }

    private static CachedMovieCatalog CreateCatalog(CountingMovieCatalog inner)
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new MovieCatalogCacheOptions
        {
            SearchMinutes = 10,
            DetailsMinutes = 10,
            ProvidersMinutes = 10,
            SimilarMinutes = 10,
            TrendingMinutes = 10
        });

        return new CachedMovieCatalog(inner, cache, options, NullLogger<CachedMovieCatalog>.Instance);
    }

    private sealed class CountingMovieCatalog : IMovieCatalog
    {
        public int SearchPageCalls { get; private set; }
        public int DiscoverCalls { get; private set; }
        public int ProviderCalls { get; private set; }
        public int SimilarCalls { get; private set; }
        public int TrendingCalls { get; private set; }

        public Task<IReadOnlyList<MovieCard>> SearchAsync(string query, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<MovieCard>>(new[] { new MovieCard(1, query.Trim(), 100, Array.Empty<string>()) });
        }

        public Task<PagedResult<MovieCard>> SearchPageAsync(string query, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            SearchPageCalls++;
            var items = new[] { new MovieCard(1, query.Trim(), 100, Array.Empty<string>()) }.Take(pageSize).ToList();
            return Task.FromResult(new PagedResult<MovieCard>(items, page, pageSize, 1, 1, false));
        }

        public Task<PagedResult<MovieCard>> DiscoverByGenreAsync(string genre, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            DiscoverCalls++;
            var items = new[] { new MovieCard(1, genre.Trim(), 100, Array.Empty<string>()) }.Take(pageSize).ToList();
            return Task.FromResult(new PagedResult<MovieCard>(items, page, pageSize, 1, 1, false));
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

        public Task<IReadOnlyList<string>> GetGenresAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        }

        public Task<IReadOnlyList<MovieCard>> GetSimilarAsync(int movieId, CancellationToken cancellationToken = default)
        {
            SimilarCalls++;
            return Task.FromResult<IReadOnlyList<MovieCard>>(new[] { new MovieCard(movieId, $"Similar-{movieId}", null, Array.Empty<string>()) });
        }

        public Task<IReadOnlyList<MovieCard>> GetTrendingAsync(CancellationToken cancellationToken = default)
        {
            TrendingCalls++;
            return Task.FromResult<IReadOnlyList<MovieCard>>(new[] { new MovieCard(999, "Trending", 110, Array.Empty<string>()) });
        }
    }
}
