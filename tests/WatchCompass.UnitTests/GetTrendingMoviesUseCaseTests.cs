using NUnit.Framework;
using Shouldly;
using System.Linq;
using WatchCompass.Application.Abstractions.Movies;
using WatchCompass.Application.Dtos;
using WatchCompass.Application.UseCases.TrendingMovies;

namespace WatchCompass.UnitTests;

[TestFixture]
public class GetTrendingMoviesUseCaseTests
{
    [Test]
    public async Task ReturnsLimitedTrendingMovies()
    {
        var movies = new[]
        {
            new MovieCard(1, "First", 100, Array.Empty<string>()),
            new MovieCard(2, "Second", 95, Array.Empty<string>()),
            new MovieCard(3, "Third", 90, Array.Empty<string>())
        };
        var catalog = new FakeTrendingCatalog(movies);
        var useCase = new GetTrendingMoviesUseCase(catalog);

        var results = await useCase.GetAsync(2);

        results.Select(m => m.MovieId).ShouldBe(new[] { 1, 2 });
    }

    [Test]
    public void ThrowsWhenLimitIsNonPositive()
    {
        var catalog = new FakeTrendingCatalog(Array.Empty<MovieCard>());
        var useCase = new GetTrendingMoviesUseCase(catalog);

        Should.Throw<ArgumentOutOfRangeException>(() => useCase.GetAsync(0));
    }

    private sealed class FakeTrendingCatalog : IMovieCatalog
    {
        private readonly IReadOnlyList<MovieCard> _movies;

        public FakeTrendingCatalog(IReadOnlyList<MovieCard> movies)
        {
            _movies = movies;
        }

        public Task<IReadOnlyList<MovieCard>> GetTrendingAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_movies);
        }

        public Task<IReadOnlyList<MovieCard>> SearchAsync(string query, CancellationToken cancellationToken = default)
        {
            _ = query;
            throw new NotImplementedException();
        }

        public Task<MovieDetails?> GetDetailsAsync(int movieId, CancellationToken cancellationToken = default)
        {
            _ = movieId;
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<string>> GetWatchProvidersAsync(int movieId, string countryCode, CancellationToken cancellationToken = default)
        {
            _ = movieId;
            _ = countryCode;
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<string>> GetGenresAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<MovieCard>> GetSimilarAsync(int movieId, CancellationToken cancellationToken = default)
        {
            _ = movieId;
            throw new NotImplementedException();
        }
    }
}
