using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;
using WatchCompass.Infrastructure.Movies.Tmdb;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace WatchCompass.IntegrationTests;

[TestFixture]
public class TmdbMovieCatalogIntegrationTests
{
    private WireMockServer _server = null!;
    private TmdbOptions _options = null!;

    [SetUp]
    public void SetUp()
    {
        _server = WireMockServer.Start();
        _options = new TmdbOptions
        {
            BaseUrl = $"{_server.Url}/3",
            ApiKey = "stub-key",
            DefaultCountryCode = "US",
            Language = "en-US",
            RequestTimeoutSeconds = 5
        };
    }

    [TearDown]
    public void TearDown()
    {
        _server.Stop();
        _server.Dispose();
    }

    [Test]
    public async Task SearchAsync_ReturnsMappedResults()
    {
        StubGenres("genres.json");
        StubSearch("matrix", "search.json");

        using var httpClient = CreateHttpClient();
        var catalog = CreateCatalog(httpClient);

        var results = await catalog.SearchAsync("matrix");

        results.Count.ShouldBe(2);
        results[0].MovieId.ShouldBe(100);
        results[0].Title.ShouldBe("Example Search Movie");
        results[0].RuntimeMinutes.ShouldBe(125);
        results[0].Genres.ShouldBe(new[] { "Action", "Adventure" });
        results[0].PosterUrl.ShouldBe("https://image.tmdb.org/t/p/w500/poster-100.jpg");
        results[0].BackdropUrl.ShouldBe("https://image.tmdb.org/t/p/w780/backdrop-100.jpg");
        results[0].ReleaseYear.ShouldBe(2020);
        results[0].Overview.ShouldNotBeNullOrWhiteSpace();
        results[1].RuntimeMinutes.ShouldBeNull();
        results[1].Genres.ShouldBe(new[] { "Comedy" });
        results[1].ReleaseYear.ShouldBeNull();
        results[1].Overview.ShouldBeNull();
    }

    [Test]
    public async Task SearchPageAsync_ReturnsRequestedSliceAndMetadata()
    {
        StubGenres("genres.json");
        StubSearch("matrix", "search.json");

        using var httpClient = CreateHttpClient();
        var catalog = CreateCatalog(httpClient);

        var result = await catalog.SearchPageAsync("matrix", page: 2, pageSize: 1);

        result.Items.Count.ShouldBe(1);
        result.Items[0].MovieId.ShouldBe(101);
        result.Page.ShouldBe(2);
        result.PageSize.ShouldBe(1);
        result.TotalResults.ShouldBe(42);
        result.TotalPages.ShouldBe(42);
        result.HasNextPage.ShouldBeTrue();
    }

    [Test]
    public async Task DiscoverByGenreAsync_ReturnsRequestedSliceAndMetadata()
    {
        StubGenres("genres.json");
        StubDiscover(28, "discover-action.json");

        using var httpClient = CreateHttpClient();
        var catalog = CreateCatalog(httpClient);

        var result = await catalog.DiscoverByGenreAsync("Action", page: 2, pageSize: 1);

        result.Items.Count.ShouldBe(1);
        result.Items[0].MovieId.ShouldBe(402);
        result.Items[0].Title.ShouldBe("Action Runner");
        result.Page.ShouldBe(2);
        result.PageSize.ShouldBe(1);
        result.TotalResults.ShouldBe(22);
        result.TotalPages.ShouldBe(22);
        result.HasNextPage.ShouldBeTrue();
    }

    [Test]
    public async Task GetDetailsAsync_ReturnsGenresAndRuntime()
    {
        StubDetails(100, "details.json");

        using var httpClient = CreateHttpClient();
        var catalog = CreateCatalog(httpClient);

        var details = await catalog.GetDetailsAsync(100);

        details.ShouldNotBeNull();
        details!.RuntimeMinutes.ShouldBe(130);
        details.Genres.ShouldBe(new[] { "Adventure", "Crime" });
        details.PosterUrl.ShouldBe("https://image.tmdb.org/t/p/w500/poster-100-details.jpg");
        details.BackdropUrl.ShouldBe("https://image.tmdb.org/t/p/w780/backdrop-100-details.jpg");
        details.ReleaseYear.ShouldBe(2021);
        details.Overview.ShouldBe("Full movie overview from details endpoint.");
    }

    [Test]
    public async Task GetWatchProvidersAsync_UsesCountryParameter()
    {
        StubWatchProviders(100, "CA", "providers.json");

        using var httpClient = CreateHttpClient();
        var catalog = CreateCatalog(httpClient);

        var providers = await catalog.GetWatchProvidersAsync(100, "ca");

        providers.ShouldBe(new[] { "Crave" });
    }

    [Test]
    public async Task GetGenresAsync_ReturnsDistinctSortedNames()
    {
        StubGenres("genres.json");

        using var httpClient = CreateHttpClient();
        var catalog = CreateCatalog(httpClient);

        var genres = await catalog.GetGenresAsync();

        genres.ShouldBe(new[] { "Action", "Adventure", "Comedy", "Drama" });
    }

    [Test]
    public async Task GetSimilarAsync_ReturnsMappedResults()
    {
        StubGenres("genres.json");
        StubSimilar(150, "similar.json");

        using var httpClient = CreateHttpClient();
        var catalog = CreateCatalog(httpClient);

        var results = await catalog.GetSimilarAsync(150);

        results.Count.ShouldBe(2);
        results[0].MovieId.ShouldBe(201);
        results[0].Title.ShouldBe("Similar One");
        results[0].Genres.ShouldBe(new[] { "Action", "Comedy" });
        results[0].RuntimeMinutes.ShouldBe(110);
        results[0].PosterUrl.ShouldBe("https://image.tmdb.org/t/p/w500/poster-201.jpg");
        results[0].BackdropUrl.ShouldBe("https://image.tmdb.org/t/p/w780/backdrop-201.jpg");
        results[0].ReleaseYear.ShouldBe(2018);
        results[0].Overview.ShouldBe("First similar pick.");
    }

    [Test]
    public async Task GetTrendingAsync_ReturnsMappedResults()
    {
        StubGenres("genres.json");
        StubTrending("trending.json");

        using var httpClient = CreateHttpClient();
        var catalog = CreateCatalog(httpClient);

        var results = await catalog.GetTrendingAsync();

        results.Count.ShouldBe(2);
        results[0].MovieId.ShouldBe(301);
        results[0].Title.ShouldBe("Trending Headliner");
        results[0].Genres.ShouldBe(new[] { "Action", "Comedy" });
        results[0].RuntimeMinutes.ShouldBe(115);
        results[0].PosterUrl.ShouldBe("https://image.tmdb.org/t/p/w500/poster-301.jpg");
        results[0].BackdropUrl.ShouldBe("https://image.tmdb.org/t/p/w780/backdrop-301.jpg");
        results[0].ReleaseYear.ShouldBe(2024);
        results[0].Overview.ShouldBe("Top of the charts.");
        results[1].MovieId.ShouldBe(302);
        results[1].Genres.ShouldBe(new[] { "Drama" });
        results[1].ReleaseYear.ShouldBe(2023);
    }

    private void StubSearch(string query, string fixtureName, int tmdbPage = 1)
    {
        _server.Given(Request.Create()
                .WithPath("/3/search/movie")
                .WithParam("query", query)
                .WithParam("page", tmdbPage.ToString())
                .WithParam("language", _options.Language)
                .WithParam("include_adult", "false")
                .WithParam("region", _options.DefaultCountryCode)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode((int)HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile(GetFixturePath(fixtureName)));
    }

    private void StubGenres(string fixtureName)
    {
        _server.Given(Request.Create()
                .WithPath("/3/genre/movie/list")
                .WithParam("language", _options.Language)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode((int)HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile(GetFixturePath(fixtureName)));
    }

    private void StubDiscover(int genreId, string fixtureName, int tmdbPage = 1)
    {
        _server.Given(Request.Create()
                .WithPath("/3/discover/movie")
                .WithParam("with_genres", genreId.ToString())
                .WithParam("page", tmdbPage.ToString())
                .WithParam("language", _options.Language)
                .WithParam("include_adult", "false")
                .WithParam("sort_by", "popularity.desc")
                .WithParam("region", _options.DefaultCountryCode)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode((int)HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile(GetFixturePath(fixtureName)));
    }

    private void StubDetails(int movieId, string fixtureName)
    {
        _server.Given(Request.Create()
                .WithPath($"/3/movie/{movieId}")
                .WithParam("language", _options.Language)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode((int)HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile(GetFixturePath(fixtureName)));
    }

    private void StubWatchProviders(int movieId, string expectedRegion, string fixtureName)
    {
        _server.Given(Request.Create()
                .WithPath($"/3/movie/{movieId}/watch/providers")
                .WithParam("watch_region", expectedRegion)
                .WithParam("language", _options.Language)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode((int)HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile(GetFixturePath(fixtureName)));
    }

    private void StubSimilar(int movieId, string fixtureName)
    {
        _server.Given(Request.Create()
                .WithPath($"/3/movie/{movieId}/similar")
                .WithParam("language", _options.Language)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode((int)HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile(GetFixturePath(fixtureName)));
    }

    private void StubTrending(string fixtureName)
    {
        _server.Given(Request.Create()
                .WithPath("/3/trending/movie/day")
                .WithParam("language", _options.Language)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode((int)HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile(GetFixturePath(fixtureName)));
    }

    private HttpClient CreateHttpClient()
    {
        var baseUrl = _options.BaseUrl.EndsWith("/", StringComparison.Ordinal)
            ? _options.BaseUrl
            : $"{_options.BaseUrl}/";

        return new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(_options.RequestTimeoutSeconds)
        };
    }

    private TmdbMovieCatalog CreateCatalog(HttpClient httpClient)
    {
        var executor = new TmdbRequestExecutor(httpClient, Options.Create(_options), NullLogger<TmdbRequestExecutor>.Instance);
        var apiClient = new TmdbApiClient(executor, Options.Create(_options));
        return new TmdbMovieCatalog(apiClient, Options.Create(_options), NullLogger<TmdbMovieCatalog>.Instance);
    }

    private static string GetFixturePath(string fixtureName)
    {
        return Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "Tmdb", fixtureName);
    }
}
