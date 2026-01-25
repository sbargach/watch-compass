using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Shouldly;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace WatchCompass.IntegrationTests;

[TestFixture]
public class ApiIntegrationTests
{
    private WireMockServer _server = null!;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _server = WireMockServer.Start();
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Tmdb:BaseUrl"] = $"{_server.Url}/3",
                        ["Tmdb:ApiKey"] = "stub-key",
                        ["Tmdb:DefaultCountryCode"] = "US",
                        ["Tmdb:Language"] = "en-US",
                        ["Tmdb:RequestTimeoutSeconds"] = "3"
                    });
                });
            });

        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
        _server.Stop();
        _server.Dispose();
    }

    [Test]
    public async Task SearchEndpoint_ReturnsItemsArray()
    {
        StubGenres(LoadFixture("genres.json"));
        StubSearch("test", LoadFixture("search.json"));

        var response = await _client.GetAsync("/api/movies/search?query=test");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = document.RootElement.GetProperty("items");
        items.ValueKind.ShouldBe(JsonValueKind.Array);
        items.GetArrayLength().ShouldBe(2);
        items[0].GetProperty("movieId").GetInt32().ShouldBe(100);
        items[0].GetProperty("posterUrl").GetString().ShouldBe("https://image.tmdb.org/t/p/w500/poster-100.jpg");
        items[0].GetProperty("backdropUrl").GetString().ShouldBe("https://image.tmdb.org/t/p/w780/backdrop-100.jpg");
        items[0].GetProperty("releaseYear").GetInt32().ShouldBe(2020);
        items[0].GetProperty("overview").GetString().ShouldNotBeNull();
        var genres = items[0].GetProperty("genres");
        genres.GetArrayLength().ShouldBe(2);
        genres[0].GetString().ShouldBe("Action");
        genres[1].GetString().ShouldBe("Adventure");
    }

    [Test]
    public async Task GenresEndpoint_ReturnsSortedList()
    {
        StubGenres(LoadFixture("genres.json"));

        var response = await _client.GetAsync("/api/genres");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = document.RootElement.GetProperty("items");
        items.GetArrayLength().ShouldBe(4);
        items[0].GetString().ShouldBe("Action");
        items[1].GetString().ShouldBe("Adventure");
        items[2].GetString().ShouldBe("Comedy");
        items[3].GetString().ShouldBe("Drama");
    }

    [Test]
    public async Task MetricsEndpoint_Returns200()
    {
        var response = await _client.GetAsync("/metrics");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Length.ShouldBeGreaterThan(0);
    }

    [Test]
    public async Task MovieDetailsEndpoint_ReturnsDetailsAndProviders()
    {
        StubDetails(100, LoadFixture("details.json"));
        StubWatchProviders(100, LoadFixture("providers.json"));

        var response = await _client.GetAsync("/api/movies/100?countryCode=US");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var runtime = document.RootElement.GetProperty("runtimeMinutes").GetInt32();
        runtime.ShouldBe(130);
        var genres = document.RootElement.GetProperty("genres");
        genres.GetArrayLength().ShouldBeGreaterThan(0);
        var providers = document.RootElement.GetProperty("providers");
        providers.GetArrayLength().ShouldBeGreaterThan(0);
        providers.EnumerateArray().Select(e => e.GetString()).ShouldContain("Netflix");
        document.RootElement.GetProperty("posterUrl").GetString().ShouldBe("https://image.tmdb.org/t/p/w500/poster-100-details.jpg");
        document.RootElement.GetProperty("backdropUrl").GetString().ShouldBe("https://image.tmdb.org/t/p/w780/backdrop-100-details.jpg");
        document.RootElement.GetProperty("releaseYear").GetInt32().ShouldBe(2021);
        document.RootElement.GetProperty("overview").GetString().ShouldBe("Full movie overview from details endpoint.");
    }

    [Test]
    public async Task RecommendationsEndpoint_ReturnsProvidersFromTmdb()
    {
        StubGenres(LoadFixture("genres.json"));
        var searchBody = """
        {
          "results": [
            {
              "id": 999,
              "title": "Mocked Match",
              "runtime": 100,
              "genre_ids": [18],
              "poster_path": "/poster-999.jpg",
              "backdrop_path": "/backdrop-999.jpg",
              "release_date": "2019-11-11",
              "overview": "Recommendation overview."
            }
          ]
        }
        """;
        StubSearch("matrix", searchBody);

        var providersBody = """
        {
          "id": 999,
          "results": {
            "US": {
              "flatrate": [
                { "provider_name": "Netflix" }
              ],
              "rent": [],
              "buy": []
            }
          }
        }
        """;
        StubWatchProviders(999, providersBody);

        var payload = new
        {
            mood = "Chill",
            timeBudgetMinutes = 120,
            query = "matrix",
            avoidGenres = Array.Empty<string>(),
            countryCode = "US"
        };

        var response = await _client.PostAsJsonAsync("/api/recommendations", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = document.RootElement.GetProperty("items");
        items.GetArrayLength().ShouldBeGreaterThan(0);
        var providers = items[0].GetProperty("providers");
        providers.GetArrayLength().ShouldBeGreaterThan(0);
        providers[0].GetString().ShouldBe("Netflix");
        items[0].GetProperty("posterUrl").GetString().ShouldBe("https://image.tmdb.org/t/p/w500/poster-999.jpg");
        items[0].GetProperty("backdropUrl").GetString().ShouldBe("https://image.tmdb.org/t/p/w780/backdrop-999.jpg");
        items[0].GetProperty("releaseYear").GetInt32().ShouldBe(2019);
        items[0].GetProperty("overview").GetString().ShouldBe("Recommendation overview.");
    }

    [Test]
    public async Task RecommendationsEndpoint_FiltersAvoidGenresFromSearch()
    {
        StubGenres(LoadFixture("genres.json"));
        var searchBody = """
        {
          "results": [
            {
              "id": 501,
              "title": "Skip Drama",
              "runtime": 90,
              "genre_ids": [18],
              "poster_path": "/poster-501.jpg",
              "backdrop_path": "/backdrop-501.jpg",
              "release_date": "2018-01-01"
            },
            {
              "id": 502,
              "title": "Keep Comedy",
              "runtime": 95,
              "genre_ids": [35],
              "poster_path": "/poster-502.jpg",
              "backdrop_path": "/backdrop-502.jpg",
              "release_date": "2018-02-02"
            }
          ]
        }
        """;
        StubSearch("feelgood", searchBody);

        var providersBody = """
        {
          "id": 502,
          "results": {
            "US": {
              "flatrate": [
                { "provider_name": "Prime Video" }
              ],
              "rent": [],
              "buy": []
            }
          }
        }
        """;
        StubWatchProviders(502, providersBody);

        var payload = new
        {
            mood = "FeelGood",
            timeBudgetMinutes = 120,
            query = "feelgood",
            avoidGenres = new[] { "Drama" },
            countryCode = "US"
        };

        var response = await _client.PostAsJsonAsync("/api/recommendations", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = document.RootElement.GetProperty("items");
        items.GetArrayLength().ShouldBe(1);
        items[0].GetProperty("movieId").GetInt32().ShouldBe(502);
        var providers = items[0].GetProperty("providers");
        providers.GetArrayLength().ShouldBe(1);
        providers[0].GetString().ShouldBe("Prime Video");
    }

    [Test]
    public async Task RecommendationsEndpoint_WithInvalidBudget_ReturnsProblemDetails()
    {
        var payload = new
        {
            mood = "Chill",
            timeBudgetMinutes = 0,
            countryCode = "US"
        };

        var response = await _client.PostAsJsonAsync("/api/recommendations", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var title = document.RootElement.GetProperty("title").GetString();
        title.ShouldNotBeNull();
        title!.ToLowerInvariant().ShouldContain("time budget");
    }

    [Test]
    public async Task RecommendationsEndpoint_WithInvalidMood_ReturnsProblemDetails()
    {
        var payload = new
        {
            mood = "Sleepy",
            timeBudgetMinutes = 120,
            countryCode = "US"
        };

        var response = await _client.PostAsJsonAsync("/api/recommendations", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var title = document.RootElement.GetProperty("title").GetString();
        title.ShouldNotBeNull();
        title!.ToLowerInvariant().ShouldContain("invalid mood");
    }

    [Test]
    public async Task SimilarMoviesEndpoint_ReturnsItemsArray()
    {
        StubGenres(LoadFixture("genres.json"));
        StubSimilar(150, LoadFixture("similar.json"));

        var response = await _client.GetAsync("/api/movies/150/similar");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = document.RootElement.GetProperty("items");
        items.ValueKind.ShouldBe(JsonValueKind.Array);
        items.GetArrayLength().ShouldBe(2);
        items[0].GetProperty("movieId").GetInt32().ShouldBe(201);
        items[0].GetProperty("genres").EnumerateArray().Select(e => e.GetString()).ShouldContain("Action");
        items[0].GetProperty("posterUrl").GetString().ShouldBe("https://image.tmdb.org/t/p/w500/poster-201.jpg");
        items[0].GetProperty("backdropUrl").GetString().ShouldBe("https://image.tmdb.org/t/p/w780/backdrop-201.jpg");
        items[0].GetProperty("releaseYear").GetInt32().ShouldBe(2018);
        items[0].GetProperty("overview").GetString().ShouldBe("First similar pick.");
    }

    private void StubSearch(string query, string body)
    {
        _server.Given(Request.Create()
                .UsingGet()
                .WithPath("/3/search/movie")
                .WithParam("query", query)
                .WithParam("language", "en-US")
                .WithParam("include_adult", "false")
                .WithParam("region", "US"))
            .RespondWith(Response.Create()
                .WithStatusCode((int)HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(body));
    }

    private void StubWatchProviders(int movieId, string body)
    {
        _server.Given(Request.Create()
                .UsingGet()
                .WithPath($"/3/movie/{movieId}/watch/providers")
                .WithParam("watch_region", "US")
                .WithParam("language", "en-US"))
            .RespondWith(Response.Create()
                .WithStatusCode((int)HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(body));
    }

    private void StubGenres(string body)
    {
        _server.Given(Request.Create()
                .UsingGet()
                .WithPath("/3/genre/movie/list")
                .WithParam("language", "en-US"))
            .RespondWith(Response.Create()
                .WithStatusCode((int)HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(body));
    }

    private void StubSimilar(int movieId, string body)
    {
        _server.Given(Request.Create()
                .UsingGet()
                .WithPath($"/3/movie/{movieId}/similar")
                .WithParam("language", "en-US"))
            .RespondWith(Response.Create()
                .WithStatusCode((int)HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(body));
    }

    private void StubDetails(int movieId, string body)
    {
        _server.Given(Request.Create()
                .UsingGet()
                .WithPath($"/3/movie/{movieId}")
                .WithParam("language", "en-US"))
            .RespondWith(Response.Create()
                .WithStatusCode((int)HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(body));
    }

    private static string LoadFixture(string fixtureName)
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "Tmdb", fixtureName);
        return File.ReadAllText(path);
    }
}
