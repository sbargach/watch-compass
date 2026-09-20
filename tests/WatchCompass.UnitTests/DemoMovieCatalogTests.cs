using NUnit.Framework;
using Shouldly;
using WatchCompass.Infrastructure.Movies.Demo;

namespace WatchCompass.UnitTests;

[TestFixture]
public class DemoMovieCatalogTests
{
    private DemoMovieCatalog _catalog = null!;

    [SetUp]
    public void SetUp() => _catalog = new DemoMovieCatalog();

    [Test]
    public async Task SearchIsCaseInsensitiveAndFiltersYearBeforePaging()
    {
        var result = await _catalog.SearchPageAsync("  THE  ", 2, 2, 2024);
        result.TotalResults.ShouldBe(4);
        result.TotalPages.ShouldBe(2);
        result.Page.ShouldBe(2);
        result.PageSize.ShouldBe(2);
        result.HasNextPage.ShouldBeFalse();
        result.Items.Select(movie => movie.MovieId).ShouldBe(new[] { 1008, 1022 });
    }

    [Test]
    public async Task DiscoveryHasStableNonOverlappingPages()
    {
        var first = await _catalog.DiscoverByGenreAsync(" comedy ", 1, 12);
        var second = await _catalog.DiscoverByGenreAsync("Comedy", 2, 12);
        first.TotalResults.ShouldBe(13);
        first.Items.Count.ShouldBe(12);
        first.HasNextPage.ShouldBeTrue();
        second.Items.Single().MovieId.ShouldBe(1013);
        second.HasNextPage.ShouldBeFalse();
        first.Items.Select(movie => movie.MovieId).Intersect(second.Items.Select(movie => movie.MovieId)).ShouldBeEmpty();
        var year = await _catalog.DiscoverByGenreAsync("Comedy", 1, 12, 2024);
        year.Items.Select(movie => movie.MovieId).ShouldBe(new[] { 1001, 1002, 1008 });
    }

    [Test]
    public async Task EmptyAndOutOfRangeResultsKeepAccurateMetadata()
    {
        var missing = await _catalog.SearchPageAsync("not in the catalog", 1, 12);
        missing.Items.ShouldBeEmpty();
        missing.TotalResults.ShouldBe(0);
        missing.TotalPages.ShouldBe(0);
        missing.HasNextPage.ShouldBeFalse();
        var beyond = await _catalog.DiscoverByGenreAsync("Comedy", int.MaxValue, 50);
        beyond.Items.ShouldBeEmpty();
        beyond.TotalResults.ShouldBe(13);
        beyond.HasNextPage.ShouldBeFalse();
        (await _catalog.DiscoverByGenreAsync("Unknown", 1, 12)).Items.ShouldBeEmpty();
    }

    [Test]
    public async Task ProvidersAreRegionalSamplesAndUnknownRecordsAreEmpty()
    {
        (await _catalog.GetWatchProvidersAsync(1001, " nl ")).ShouldBe(new[] { "Canal Cinema (sample)" });
        (await _catalog.GetWatchProvidersAsync(1001, "US")).ShouldBe(new[] { "Evening Screen (sample)" });
        (await _catalog.GetWatchProvidersAsync(1001, "DE")).ShouldBeEmpty();
        (await _catalog.GetWatchProvidersAsync(1024, "NL")).ShouldBeEmpty();
        (await _catalog.GetDetailsAsync(9999)).ShouldBeNull();
        (await _catalog.GetSimilarAsync(9999)).ShouldBeEmpty();
        (await _catalog.GetWatchProvidersAsync(9999, "NL")).ShouldBeEmpty();
    }

    [Test]
    public async Task BundledCatalogHasValidLinksAndEnoughCandidatesForEveryMood()
    {
        var genres = await _catalog.GetGenresAsync();
        genres.ShouldBe(new[] { "Comedy", "Drama", "Horror", "Thriller" });
        var ids = new HashSet<int>();
        foreach (var genre in genres)
        {
            var movies = (await _catalog.DiscoverByGenreAsync(genre, 1, 50)).Items;
            movies.Count(movie => movie.RuntimeMinutes is > 0 and <= 120).ShouldBeGreaterThanOrEqualTo(3);
            foreach (var movie in movies)
            {
                ids.Add(movie.MovieId).ShouldBeTrue();
                movie.PosterUrl.ShouldStartWith("/demo/posters/");
                movie.BackdropUrl.ShouldBeNull();
                (await _catalog.GetDetailsAsync(movie.MovieId)).ShouldNotBeNull();
                var similar = await _catalog.GetSimilarAsync(movie.MovieId);
                similar.ShouldNotBeEmpty();
                similar.Any(item => item.MovieId == movie.MovieId).ShouldBeFalse();
            }
        }
        ids.Count.ShouldBe(24);
        foreach (var movie in (await _catalog.GetTrendingAsync()).Concat(await _catalog.GetNowPlayingAsync()))
            ids.ShouldContain(movie.MovieId);
    }

    [Test]
    public void AllOperationsHonorCancellation()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var token = source.Token;
        Func<Task>[] operations =
        [
            () => _catalog.SearchAsync("the", token),
            () => _catalog.SearchPageAsync("the", 1, 12, cancellationToken: token),
            () => _catalog.DiscoverByGenreAsync("Comedy", 1, 12, cancellationToken: token),
            () => _catalog.GetDetailsAsync(1001, token),
            () => _catalog.GetWatchProvidersAsync(1001, "NL", token),
            () => _catalog.GetGenresAsync(token),
            () => _catalog.GetSimilarAsync(1001, token),
            () => _catalog.GetTrendingAsync(token),
            () => _catalog.GetNowPlayingAsync(token)
        ];
        foreach (var operation in operations)
            Assert.ThrowsAsync<OperationCanceledException>(async () => await operation());
    }
}
