using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using Shouldly;
using WatchCompass.Application.Abstractions.Movies;
using WatchCompass.Infrastructure.Movies.Demo;
using WatchCompass.Infrastructure.Movies.Tmdb;

namespace WatchCompass.IntegrationTests;

[TestFixture]
public class DemoApiIntegrationTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private RejectOutboundRequests _outbound = null!;

    [SetUp]
    public void SetUp()
    {
        _outbound = new RejectOutboundRequests();
        _factory = CreateFactory("Demo", outbound: _outbound);
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client?.Dispose();
        _factory.Dispose();
        _outbound.Attempts.ShouldBe(0, "demo requests must never use an upstream service");
    }

    [Test]
    public async Task StartsWithoutCredentialsAndDoesNotRegisterTmdb()
    {
        using var info = await GetJson("/api/app-info");
        info.RootElement.GetProperty("catalogMode").GetString().ShouldBe("demo");
        _factory.Services.GetRequiredService<IMovieCatalog>().ShouldBeOfType<DemoMovieCatalog>();
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetService<ITmdbApiClient>().ShouldBeNull();
        scope.ServiceProvider.GetService<TmdbRequestExecutor>().ShouldBeNull();
        (await _client.GetStringAsync("/health")).ShouldBe("ok");
    }

    [TestCase("/api/genres")]
    [TestCase("/api/movies/search?query=the&pageSize=12")]
    [TestCase("/api/movies/discover?genre=Comedy&pageSize=12")]
    [TestCase("/api/movies/trending?limit=12")]
    [TestCase("/api/movies/now-playing?limit=12")]
    [TestCase("/api/movies/1001/similar")]
    public async Task CatalogEndpointsUseBundledData(string route)
    {
        using var response = await GetJson(route);
        response.RootElement.GetProperty("items").GetArrayLength().ShouldBeGreaterThan(0);
    }

    [Test]
    public async Task FilteringAndPaginationWorkThroughTheApi()
    {
        using var page = await GetJson("/api/movies/discover?genre=Comedy&page=2&pageSize=12");
        page.RootElement.GetProperty("totalResults").GetInt32().ShouldBe(13);
        page.RootElement.GetProperty("hasNextPage").GetBoolean().ShouldBeFalse();
        page.RootElement.GetProperty("items")[0].GetProperty("movieId").GetInt32().ShouldBe(1013);
        using var filtered = await GetJson("/api/movies/search?query=THE&releaseYear=2024");
        filtered.RootElement.GetProperty("totalResults").GetInt32().ShouldBe(4);
    }

    [Test]
    public async Task DetailsRefreshRegionalSampleAvailability()
    {
        using var nl = await GetJson("/api/movies/1001?countryCode=NL");
        using var us = await GetJson("/api/movies/1001?countryCode=US");
        nl.RootElement.GetProperty("providers")[0].GetString().ShouldBe("Canal Cinema (sample)");
        us.RootElement.GetProperty("providers")[0].GetString().ShouldBe("Evening Screen (sample)");
        nl.RootElement.GetProperty("posterUrl").GetString().ShouldBe("/demo/posters/1001.svg");
        using var response = await _client.GetAsync("/api/movies/9999");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [TestCase("FeelGood", "Comedy")]
    [TestCase("Chill", "Drama")]
    [TestCase("Intense", "Thriller")]
    [TestCase("Scary", "Horror")]
    public async Task EveryMoodProducesThreeVerifiedPicks(string mood, string genre)
    {
        using var response = await _client.PostAsJsonAsync("/api/recommendations", new
        {
            mood, timeBudgetMinutes = 120, countryCode = "NL", avoidGenres = Array.Empty<string>()
        });
        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = body.RootElement.GetProperty("items");
        items.GetArrayLength().ShouldBe(3);
        foreach (var movie in items.EnumerateArray())
        {
            movie.GetProperty("runtimeMinutes").GetInt32().ShouldBeLessThanOrEqualTo(120);
            movie.GetProperty("genres").EnumerateArray().Select(item => item.GetString()).ShouldContain(genre);
            movie.GetProperty("reasons")[1].GetString()!.ShouldContain("simple mood rule");
        }
    }

    [TestCase("Letters", "FeelGood", "")]
    [TestCase("", "Scary", "Horror")]
    public async Task ConflictingConstraintsReturnAnEmptyShortlist(string query, string mood, string excluded)
    {
        using var response = await _client.PostAsJsonAsync("/api/recommendations", new
        {
            mood, query, timeBudgetMinutes = 120, countryCode = "NL", avoidGenres = new[] { excluded }
        });
        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("items").GetArrayLength().ShouldBe(0);
    }

    [Test]
    public async Task UndefinedNumericMoodReturnsBadRequest()
    {
        using var response = await _client.PostAsJsonAsync("/api/recommendations", new
        {
            mood = "999", timeBudgetMinutes = 120, countryCode = "NL"
        });
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [TestCase(null)]
    [TestCase("Tmdb")]
    public void LiveModeStillRequiresCredentials(string? mode)
    {
        using var factory = CreateFactory(mode);
        var error = Assert.Catch<Exception>(() => factory.CreateClient());
        error!.ToString().ShouldContain("ApiKey");
    }

    [TestCase("")]
    [TestCase("Automatic")]
    [TestCase("1")]
    public void InvalidModeFailsClearly(string mode)
    {
        using var factory = CreateFactory(mode);
        var error = Assert.Catch<Exception>(() => factory.CreateClient());
        error!.ToString().ShouldContain("Catalog:Mode must be Tmdb or Demo");
    }

    [Test]
    public async Task LiveModeReportsItsSourceWithoutFetchingMovieData()
    {
        using var factory = CreateFactory("Tmdb", "test-token", _outbound);
        using var client = factory.CreateClient();
        using var body = JsonDocument.Parse(await client.GetStringAsync("/api/app-info"));
        body.RootElement.GetProperty("catalogMode").GetString().ShouldBe("tmdb");
    }

    private async Task<JsonDocument> GetJson(string route)
    {
        using var response = await _client.GetAsync(route);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    private static WebApplicationFactory<Program> CreateFactory(string? mode, string apiKey = "",
        RejectOutboundRequests? outbound = null)
        => new CatalogApiFactory(mode, apiKey).WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            if (outbound is not null)
                builder.ConfigureServices(services => services.AddSingleton<IHttpMessageHandlerBuilderFilter>(outbound));
        });

    private sealed class CatalogApiFactory(string? mode, string apiKey) : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            // Catalog mode selects services during Program startup, before deferred app configuration runs.
            builder.ConfigureHostConfiguration(configuration =>
            {
                var values = new Dictionary<string, string?>
                {
                    ["Tmdb:ApiKey"] = apiKey,
                    ["Tmdb:BaseUrl"] = mode == "Demo" ? "invalid-and-unused" : "http://127.0.0.1:1/3"
                };
                if (mode is not null) values["Catalog:Mode"] = mode;
                configuration.AddInMemoryCollection(values);
            });
            return base.CreateHost(builder);
        }
    }

    private sealed class RejectOutboundRequests : IHttpMessageHandlerBuilderFilter
    {
        public int Attempts { get; private set; }

        public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next) => builder =>
        {
            next(builder);
            builder.PrimaryHandler = new RejectHandler(this);
        };

        private sealed class RejectHandler(RejectOutboundRequests owner) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                owner.Attempts++;
                throw new InvalidOperationException("Unexpected outbound request during demo verification.");
            }
        }
    }
}
