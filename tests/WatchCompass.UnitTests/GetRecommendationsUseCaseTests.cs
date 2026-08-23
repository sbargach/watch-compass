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
    private const string CountryCode = "NL";

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

        var recommendations = await useCase.GetRecommendationsAsync(Mood.Chill, new TimeBudget(120), null, Array.Empty<string>(), CountryCode);

        catalog.Queries.ShouldBe(new[] { "drama" });
        recommendations.Count.ShouldBe(1);
        recommendations[0].MovieId.ShouldBe(1);
    }

    [Test]
    public async Task ForwardsReleaseYearToCatalogSearch()
    {
        var catalog = new FakeMovieCatalog
        {
            SearchResults = new[]
            {
                new MovieCard(1, "Comedy Pick", 100, new[] { "Comedy" })
            }
        };
        var useCase = new GetRecommendationsUseCase(catalog);

        var recommendations = await useCase.GetRecommendationsAsync(
            Mood.FeelGood,
            new TimeBudget(120),
            "comedy",
            Array.Empty<string>(),
            CountryCode,
            2020);

        recommendations.Count.ShouldBe(1);
        catalog.ReleaseYears.ShouldBe(new int?[] { 2020 });
        catalog.SearchPageSizes.ShouldBe(new[] { 20 });
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

        var recommendations = await useCase.GetRecommendationsAsync(Mood.Intense, new TimeBudget(120), "thriller", Array.Empty<string>(), CountryCode);

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

        var recommendations = await useCase.GetRecommendationsAsync(Mood.Scary, new TimeBudget(120), string.Empty, new[] { " horror " }, CountryCode);

        recommendations.Count.ShouldBe(1);
        recommendations[0].MovieId.ShouldBe(2);
        recommendations[0].Genres.ShouldBe(new[] { "Comedy" });
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

        var recommendations = await useCase.GetRecommendationsAsync(Mood.Intense, new TimeBudget(100), "thriller", Array.Empty<string>(), CountryCode);

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

        var recommendations = await useCase.GetRecommendationsAsync(Mood.FeelGood, new TimeBudget(200), null, Array.Empty<string>(), CountryCode);

        recommendations.Count.ShouldBe(3);
        recommendations.Select(r => r.MovieId).ShouldBe(new[] { 1, 2, 3 });
    }

    [Test]
    public async Task ContinuesPastRejectedFirstBatch()
    {
        var catalog = new FakeMovieCatalog
        {
            SearchResults = Enumerable.Range(1, 5)
                .Select(id => new MovieCard(id, $"Long {id}", 180, new[] { "Drama" }))
                .Append(new MovieCard(6, "Valid Pick", 90, new[] { "Drama" }))
                .ToArray()
        };
        var useCase = new GetRecommendationsUseCase(catalog);

        var recommendations = await useCase.GetRecommendationsAsync(
            Mood.Chill,
            new TimeBudget(120),
            "drama",
            Array.Empty<string>(),
            CountryCode);

        recommendations.Select(recommendation => recommendation.MovieId).ShouldBe(new[] { 6 });
    }

    [Test]
    public async Task RejectsCandidateWhenRuntimeCannotBeVerified()
    {
        var catalog = new FakeMovieCatalog
        {
            SearchResults = new[]
            {
                new MovieCard(1, "Unknown Runtime", null, new[] { "Drama" })
            }
        };
        var useCase = new GetRecommendationsUseCase(catalog);

        var recommendations = await useCase.GetRecommendationsAsync(
            Mood.Chill,
            new TimeBudget(120),
            "drama",
            Array.Empty<string>(),
            CountryCode);

        recommendations.ShouldBeEmpty();
    }

    [Test]
    public async Task PopulatesProvidersForNormalizedCountryCode()
    {
        var catalog = new FakeMovieCatalog
        {
            SearchResults = new[]
            {
                new MovieCard(1, "Provider Pick", 90, new[] { "Drama" })
            },
            ProvidersById = new Dictionary<int, IReadOnlyList<string>>
            {
                [1] = new[] { "Netflix" }
            }
        };
        var useCase = new GetRecommendationsUseCase(catalog);

        var recommendations = await useCase.GetRecommendationsAsync(
            Mood.Chill,
            new TimeBudget(120),
            "drama",
            Array.Empty<string>(),
            " nl ");

        recommendations[0].Providers.ShouldBe(new[] { "Netflix" });
        catalog.ProviderCountryCodes.ShouldBe(new[] { "NL" });
    }

    [Test]
    public async Task KeepsRecommendationsWhenProviderLookupFails()
    {
        var catalog = new FakeMovieCatalog
        {
            SearchResults = new[]
            {
                new MovieCard(1, "Unavailable Providers", 90, new[] { "Drama" }),
                new MovieCard(2, "Available Providers", 95, new[] { "Drama" })
            },
            ProvidersById = new Dictionary<int, IReadOnlyList<string>>
            {
                [2] = new[] { "Prime Video" }
            },
            ProviderFailures = new HashSet<int> { 1 }
        };
        var useCase = new GetRecommendationsUseCase(catalog);

        var recommendations = await useCase.GetRecommendationsAsync(
            Mood.Chill,
            new TimeBudget(120),
            "drama",
            Array.Empty<string>(),
            CountryCode);

        recommendations.Count.ShouldBe(2);
        recommendations[0].Providers.ShouldBeEmpty();
        recommendations[1].Providers.ShouldBe(new[] { "Prime Video" });
    }

    [Test]
    public async Task PropagatesProviderLookupCancellation()
    {
        var catalog = new FakeMovieCatalog
        {
            SearchResults = new[]
            {
                new MovieCard(1, "Cancelled Pick", 90, new[] { "Drama" })
            },
            CancelProviderLookups = true
        };
        var useCase = new GetRecommendationsUseCase(catalog);

        await Should.ThrowAsync<OperationCanceledException>(() => useCase.GetRecommendationsAsync(
            Mood.Chill,
            new TimeBudget(120),
            "drama",
            Array.Empty<string>(),
            CountryCode));
    }

    private sealed class FakeMovieCatalog : IMovieCatalog
    {
        public List<string> Queries { get; } = new();
        public List<int?> ReleaseYears { get; } = new();
        public List<int> SearchPageSizes { get; } = new();
        public List<string> ProviderCountryCodes { get; } = new();

        public IReadOnlyList<MovieCard> SearchResults { get; set; } = Array.Empty<MovieCard>();

        public Dictionary<int, MovieDetails?> DetailsById { get; set; } = new();

        public Dictionary<int, IReadOnlyList<string>> ProvidersById { get; set; } = new();

        public HashSet<int> ProviderFailures { get; set; } = new();

        public bool CancelProviderLookups { get; set; }

        public Task<IReadOnlyList<MovieCard>> SearchAsync(string query, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Queries.Add(query);
            return Task.FromResult(SearchResults);
        }

        public Task<PagedResult<MovieCard>> SearchPageAsync(
            string query,
            int page,
            int pageSize,
            int? releaseYear = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Queries.Add(query);
            ReleaseYears.Add(releaseYear);
            SearchPageSizes.Add(pageSize);
            var items = SearchResults
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            var totalPages = SearchResults.Count == 0
                ? 0
                : (int)Math.Ceiling(SearchResults.Count / (double)pageSize);
            return Task.FromResult(new PagedResult<MovieCard>(items, page, pageSize, SearchResults.Count, totalPages, page < totalPages));
        }

        public Task<PagedResult<MovieCard>> DiscoverByGenreAsync(
            string genre,
            int page,
            int pageSize,
            int? releaseYear = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = genre;
            _ = releaseYear;
            var items = SearchResults
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            var totalPages = SearchResults.Count == 0
                ? 0
                : (int)Math.Ceiling(SearchResults.Count / (double)pageSize);
            return Task.FromResult(new PagedResult<MovieCard>(items, page, pageSize, SearchResults.Count, totalPages, page < totalPages));
        }

        public Task<MovieDetails?> GetDetailsAsync(int movieId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(DetailsById.TryGetValue(movieId, out var details) ? details : null);
        }

        public Task<IReadOnlyList<string>> GetWatchProvidersAsync(int movieId, string countryCode, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ProviderCountryCodes.Add(countryCode);
            if (CancelProviderLookups)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            if (ProviderFailures.Contains(movieId))
            {
                throw new InvalidOperationException("Provider lookup failed.");
            }

            return Task.FromResult(ProvidersById.TryGetValue(movieId, out var providers) ? providers : Array.Empty<string>());
        }

        public Task<IReadOnlyList<string>> GetGenresAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        }

        public Task<IReadOnlyList<MovieCard>> GetSimilarAsync(int movieId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = movieId;
            return Task.FromResult<IReadOnlyList<MovieCard>>(Array.Empty<MovieCard>());
        }

        public Task<IReadOnlyList<MovieCard>> GetTrendingAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<MovieCard>>(Array.Empty<MovieCard>());
        }

        public Task<IReadOnlyList<MovieCard>> GetNowPlayingAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<MovieCard>>(Array.Empty<MovieCard>());
        }
    }
}
