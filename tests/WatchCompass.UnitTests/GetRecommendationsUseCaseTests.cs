using NUnit.Framework;
using Shouldly;
using WatchCompass.Application.Abstractions.Movies;
using WatchCompass.Application.Dtos;
using WatchCompass.Application.UseCases.Recommendations;
using WatchCompass.Domain.Enums;
using WatchCompass.Domain.ValueObjects;

namespace WatchCompass.UnitTests;

[TestFixture]
public class GetRecommendationsUseCaseTests
{
    [Test]
    public async Task UsesMoodFallbackQueryWhenQueryMissing()
    {
        var catalog = new FakeMovieCatalog
        {
            SearchResults = new[]
            {
                new MovieCard(1, "Drama Pick", 100, new[] { "Drama" })
            }
        };
        var useCase = new GetRecommendationsUseCase(catalog);

        var recommendations = await useCase.GetRecommendationsAsync(Mood.Chill, new TimeBudget(120), null, Array.Empty<string>());

        catalog.Queries.ShouldBe(new[] { "drama" });
        recommendations.Count.ShouldBe(1);
        recommendations[0].MovieId.ShouldBe(1);
    }

    [Test]
    public async Task FiltersOutMoviesExceedingBudget()
    {
        var catalog = new FakeMovieCatalog
        {
            SearchResults = new[]
            {
                new MovieCard(1, "Long Movie", null, new[] { "Drama" }),
                new MovieCard(2, "Short Movie", 80, new[] { "Drama" })
            },
            DetailsById = new Dictionary<int, MovieDetails?>
            {
                [1] = new MovieDetails(1, "Long Movie", 200, new[] { "Drama" })
            }
        };
        var useCase = new GetRecommendationsUseCase(catalog);

        var recommendations = await useCase.GetRecommendationsAsync(Mood.Intense, new TimeBudget(120), "thriller", Array.Empty<string>());

        recommendations.Count.ShouldBe(1);
        recommendations[0].MovieId.ShouldBe(2);
    }

    [Test]
    public async Task FiltersOutAvoidGenresCaseInsensitive()
    {
        var catalog = new FakeMovieCatalog
        {
            SearchResults = new[]
            {
                new MovieCard(1, "Horror Movie", 90, new[] { "Horror" }),
                new MovieCard(2, "Comedy Movie", 95, new[] { "Comedy" })
            }
        };
        var useCase = new GetRecommendationsUseCase(catalog);

        var recommendations = await useCase.GetRecommendationsAsync(Mood.Scary, new TimeBudget(120), string.Empty, new[] { " horror " });

        recommendations.Count.ShouldBe(1);
        recommendations[0].MovieId.ShouldBe(2);
    }

    [Test]
    public async Task ReasonsContainAtLeastTwoEntriesIncludingBudget()
    {
        var catalog = new FakeMovieCatalog
        {
            SearchResults = new[]
            {
                new MovieCard(1, "Budget Match", 90, new[] { "Thriller" })
            }
        };
        var useCase = new GetRecommendationsUseCase(catalog);

        var recommendations = await useCase.GetRecommendationsAsync(Mood.Intense, new TimeBudget(100), "thriller", Array.Empty<string>());

        recommendations.Count.ShouldBe(1);
        recommendations[0].Reasons.Count.ShouldBeGreaterThanOrEqualTo(2);
        recommendations[0].Reasons.Any(r => r.Contains("budget", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
    }

    [Test]
    public async Task ReturnsAtMostThreeRecommendationsPreservingOrder()
    {
        var catalog = new FakeMovieCatalog
        {
            SearchResults = new[]
            {
                new MovieCard(1, "First", 90, new[] { "Drama" }),
                new MovieCard(2, "Second", 90, new[] { "Drama" }),
                new MovieCard(3, "Third", 90, new[] { "Drama" }),
                new MovieCard(4, "Fourth", 90, new[] { "Drama" })
            }
        };
        var useCase = new GetRecommendationsUseCase(catalog);

        var recommendations = await useCase.GetRecommendationsAsync(Mood.FeelGood, new TimeBudget(200), null, Array.Empty<string>());

        recommendations.Count.ShouldBe(3);
        recommendations.Select(r => r.MovieId).ShouldBe(new[] { 1, 2, 3 });
    }

    private sealed class FakeMovieCatalog : IMovieCatalog
    {
        public List<string> Queries { get; } = new();

        public IReadOnlyList<MovieCard> SearchResults { get; set; } = Array.Empty<MovieCard>();

        public Dictionary<int, MovieDetails?> DetailsById { get; set; } = new();

        public Dictionary<int, IReadOnlyList<string>> ProvidersById { get; set; } = new();

        public Task<IReadOnlyList<MovieCard>> SearchAsync(string query, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Queries.Add(query);
            return Task.FromResult(SearchResults);
        }

        public Task<MovieDetails?> GetDetailsAsync(int movieId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(DetailsById.TryGetValue(movieId, out var details) ? details : null);
        }

        public Task<IReadOnlyList<string>> GetWatchProvidersAsync(int movieId, string countryCode, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(ProvidersById.TryGetValue(movieId, out var providers) ? providers : Array.Empty<string>());
        }

        public Task<IReadOnlyList<string>> GetGenresAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        }
    }
}
