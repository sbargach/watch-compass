using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using Shouldly;

namespace WatchCompass.IntegrationTests;

[TestFixture]
public class ApiIntegrationTests
{
    private WebApplicationFactory<Program> _factory = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
    }

    [TearDown]
    public void TearDown()
    {
        _factory.Dispose();
    }

    [Test]
    public async Task SearchEndpoint_ReturnsItemsArray()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/movies/search?query=test");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);
        document.RootElement.TryGetProperty("items", out var items).ShouldBeTrue();
        items.ValueKind.ShouldBe(JsonValueKind.Array);
    }

    [Test]
    public async Task MetricsEndpoint_Returns200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/metrics");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Length.ShouldBeGreaterThan(0);
    }

    [Test]
    public async Task RecommendationsEndpoint_ReturnsItemsArray()
    {
        var client = _factory.CreateClient();
        var payload = new
        {
            mood = "Chill",
            timeBudgetMinutes = 120,
            query = "matrix",
            avoidGenres = new[] { "Horror" },
            countryCode = "US"
        };

        var response = await client.PostAsJsonAsync("/api/recommendations", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);
        document.RootElement.TryGetProperty("items", out var items).ShouldBeTrue();
        items.ValueKind.ShouldBe(JsonValueKind.Array);
    }

    [Test]
    public async Task RecommendationsEndpoint_WithInvalidBudget_ReturnsProblemDetails()
    {
        var client = _factory.CreateClient();
        var payload = new
        {
            mood = "Chill",
            timeBudgetMinutes = 0,
            countryCode = "US"
        };

        var response = await client.PostAsJsonAsync("/api/recommendations", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);
        document.RootElement.TryGetProperty("title", out var title).ShouldBeTrue();
        var titleText = title.GetString();
        titleText.ShouldNotBeNull();
        titleText!.ToLowerInvariant().ShouldContain("time budget");
    }

    [Test]
    public async Task RecommendationsEndpoint_WithInvalidMood_ReturnsProblemDetails()
    {
        var client = _factory.CreateClient();
        var payload = new
        {
            mood = "Sleepy",
            timeBudgetMinutes = 120,
            countryCode = "US"
        };

        var response = await client.PostAsJsonAsync("/api/recommendations", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);
        document.RootElement.TryGetProperty("title", out var title).ShouldBeTrue();
        var titleText = title.GetString();
        titleText.ShouldNotBeNull();
        titleText!.ToLowerInvariant().ShouldContain("invalid mood");
    }
}
