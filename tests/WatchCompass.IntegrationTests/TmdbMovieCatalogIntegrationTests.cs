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
        StubSearch("matrix", "search.json");

        using var httpClient = CreateHttpClient();
        var catalog = CreateCatalog(httpClient);

        var results = await catalog.SearchAsync("matrix");

        results.Count.ShouldBe(2);
        results[0].MovieId.ShouldBe(100);
        results[0].Title.ShouldBe("Example Search Movie");
        results[0].RuntimeMinutes.ShouldBe(125);
        results[1].RuntimeMinutes.ShouldBeNull();
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

    private void StubSearch(string query, string fixtureName)
    {
        _server.Given(Request.Create()
                .WithPath("/3/search/movie")
                .WithParam("query", query)
                .WithParam("language", _options.Language)
                .WithParam("include_adult", "false")
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
