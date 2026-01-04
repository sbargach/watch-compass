using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Refit;
using Shouldly;
using WatchCompass.Infrastructure.Movies.Tmdb;

namespace WatchCompass.UnitTests;

[TestFixture]
public class TmdbMovieCatalogTests
{
    [Test]
    public async Task SearchAsync_MapsFields()
    {
        var catalog = CreateCatalog("/3/search/movie", "search.json", out var handler, out var logger);

        var results = await catalog.SearchAsync("query");

        handler.CallCount.ShouldBe(1, $"Exceptions: {FormatExceptions(logger.Exceptions)}");
        logger.Exceptions.ShouldBeEmpty();
        results.Count.ShouldBe(2);
        results[0].MovieId.ShouldBe(100);
        results[0].Title.ShouldBe("Example Search Movie");
        results[0].RuntimeMinutes.ShouldBe(125);
        results[0].Genres.ShouldBeEmpty();
        results[1].MovieId.ShouldBe(101);
        results[1].RuntimeMinutes.ShouldBeNull();
    }

    [Test]
    public async Task GetDetailsAsync_MapsFields()
    {
        var catalog = CreateCatalog("/3/movie/100", "details.json", out var handler, out var logger);

        var details = await catalog.GetDetailsAsync(100);

        handler.CallCount.ShouldBe(1, $"Exceptions: {FormatExceptions(logger.Exceptions)}");
        logger.Exceptions.ShouldBeEmpty();
        details.ShouldNotBeNull();
        details!.MovieId.ShouldBe(100);
        details.Title.ShouldBe("Example Search Movie");
        details.RuntimeMinutes.ShouldBe(130);
        details.Genres.ShouldBe(new[] { "Adventure", "Crime" });
    }

    [Test]
    public async Task GetWatchProvidersAsync_UsesUsProvidersAndDistincts()
    {
        var catalog = CreateCatalog("/3/movie/100/watch/providers", "providers.json", out var handler, out var logger);

        var providers = await catalog.GetWatchProvidersAsync(100, "US");

        handler.CallCount.ShouldBe(1, $"Exceptions: {FormatExceptions(logger.Exceptions)}");
        logger.Exceptions.ShouldBeEmpty();
        providers.ShouldBe(new[] { "Netflix", "Hulu", "Apple TV", "Vudu" });
    }

    [Test]
    public async Task SearchAsync_WithoutApiKey_ReturnsEmptyWithoutCallingApi()
    {
        var callCount = 0;
        var stubHandler = new StubHttpMessageHandler(_ =>
        {
            callCount++;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var optionsValue = new TmdbOptions
        {
            BaseUrl = "https://tmdb.test/3",
            ApiKey = string.Empty,
            AuthMode = TmdbAuthMode.Bearer
        };
        var api = CreateApi(stubHandler, optionsValue);
        var options = Options.Create(optionsValue);
        var catalog = new TmdbMovieCatalog(api, options, NullLogger<TmdbMovieCatalog>.Instance);

        var results = await catalog.SearchAsync("anything");

        results.ShouldBeEmpty();
        callCount.ShouldBe(0);
    }

    private static TmdbMovieCatalog CreateCatalog(string expectedPath, string fixtureName, out FixtureHttpMessageHandler handler, out ListLogger<TmdbMovieCatalog> logger)
    {
        var tmdbOptions = new TmdbOptions
        {
            BaseUrl = "https://tmdb.test/3",
            ApiKey = "token",
            AuthMode = TmdbAuthMode.Bearer
        };
        handler = new FixtureHttpMessageHandler(expectedPath, fixtureName);
        var api = CreateApi(handler, tmdbOptions);
        var options = Options.Create(tmdbOptions);
        logger = new ListLogger<TmdbMovieCatalog>();
        var catalog = new TmdbMovieCatalog(api, options, logger);

        var optionsField = typeof(TmdbMovieCatalog).GetField("_options", BindingFlags.NonPublic | BindingFlags.Instance);
        var capturedOptions = (TmdbOptions?)optionsField?.GetValue(catalog);
        capturedOptions.ShouldNotBeNull();
        capturedOptions!.ApiKey.ShouldBe(options.Value.ApiKey);

        var hasApiKey = (bool)typeof(TmdbMovieCatalog)
            .GetMethod("HasApiKey", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(catalog, null)!;
        hasApiKey.ShouldBeTrue();

        return catalog;
    }

    private static ITmdbApi CreateApi(HttpMessageHandler handler, TmdbOptions optionsValue)
    {
        var basePathHandler = new BasePathHandler(Options.Create(optionsValue))
        {
            InnerHandler = handler
        };

        var authority = new Uri(optionsValue.BaseUrl).GetLeftPart(UriPartial.Authority);
        var baseAddress = authority.EndsWith("/", StringComparison.Ordinal) ? authority : $"{authority}/";
        var client = new HttpClient(basePathHandler)
        {
            BaseAddress = new Uri(baseAddress)
        };

        var settings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions(JsonSerializerDefaults.Web))
        };

        return RestService.For<ITmdbApi>(client, settings);
    }

    private static string LoadFixture(string fixtureName)
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "Tmdb", fixtureName);
        return File.ReadAllText(path);
    }

    private sealed class FixtureHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _expectedPath;
        private readonly string _fixtureName;
        private int _callCount;

        public int CallCount => _callCount;

        public FixtureHttpMessageHandler(string expectedPath, string fixtureName)
        {
            _expectedPath = expectedPath;
            _fixtureName = fixtureName;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.RequestUri.ShouldNotBeNull();
            request.RequestUri!.AbsolutePath.ShouldBe(_expectedPath);
            _callCount++;

            var json = LoadFixture(_fixtureName);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responseFactory(request));
        }
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<Exception?> Exceptions { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullLogger.Instance.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Exceptions.Add(exception);
        }
    }

    private static string FormatExceptions(IEnumerable<Exception?> exceptions)
    {
        return string.Join(" | ", exceptions.Select(ex => ex?.Message ?? "null"));
    }
}
