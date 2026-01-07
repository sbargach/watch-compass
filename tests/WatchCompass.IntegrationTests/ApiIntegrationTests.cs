using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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
        StubSearch("test", LoadFixture("search.json"));

        var response = await _client.GetAsync("/api/movies/search?query=test");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = document.RootElement.GetProperty("items");
        items.ValueKind.ShouldBe(JsonValueKind.Array);
        items.GetArrayLength().ShouldBe(2);
        items[0].GetProperty("movieId").GetInt32().ShouldBe(100);
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
    public async Task RecommendationsEndpoint_ReturnsProvidersFromTmdb()
    {
        var searchBody = """
        {
          "results": [
            {
              "id": 999,
              "title": "Mocked Match",
              "runtime": 100,
              "genre_ids": [18]
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

    private static string LoadFixture(string fixtureName)
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "Tmdb", fixtureName);
        return File.ReadAllText(path);
    }
}
