using System.Net;
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
}
