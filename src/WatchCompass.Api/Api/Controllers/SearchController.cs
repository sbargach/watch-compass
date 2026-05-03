using Microsoft.AspNetCore.Mvc;
using WatchCompass.Api.Api.Validation;
using WatchCompass.Application.Dtos;
using WatchCompass.Application.UseCases.DiscoverMovies;
using WatchCompass.Application.UseCases.MovieDetails;
using WatchCompass.Application.UseCases.SearchMovies;
using WatchCompass.Application.UseCases.SimilarMovies;
using WatchCompass.Application.UseCases.TrendingMovies;
using WatchCompass.Contracts;

namespace WatchCompass.Api.Api.Controllers;

[ApiController]
[Route("api/movies")]
public sealed class SearchController : ControllerBase
{
    private const int DefaultSearchPage = 1;
    private const int DefaultSearchPageSize = 10;
    private const int MaxSearchPageSize = 50;

    private readonly SearchMoviesUseCase _searchMoviesUseCase;
    private readonly DiscoverMoviesByGenreUseCase _discoverMoviesByGenreUseCase;
    private readonly GetMovieDetailsUseCase _getMovieDetailsUseCase;
    private readonly GetSimilarMoviesUseCase _getSimilarMoviesUseCase;
    private readonly GetTrendingMoviesUseCase _getTrendingMoviesUseCase;

    public SearchController(
        SearchMoviesUseCase searchMoviesUseCase,
        DiscoverMoviesByGenreUseCase discoverMoviesByGenreUseCase,
        GetMovieDetailsUseCase getMovieDetailsUseCase,
        GetSimilarMoviesUseCase getSimilarMoviesUseCase,
        GetTrendingMoviesUseCase getTrendingMoviesUseCase)
    {
        _searchMoviesUseCase = searchMoviesUseCase;
        _discoverMoviesByGenreUseCase = discoverMoviesByGenreUseCase;
        _getMovieDetailsUseCase = getMovieDetailsUseCase;
        _getSimilarMoviesUseCase = getSimilarMoviesUseCase;
        _getTrendingMoviesUseCase = getTrendingMoviesUseCase;
    }

    [HttpGet("search")]
    public async Task<ActionResult<SearchMoviesResponse>> Search(
        [FromQuery] string? query,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] int? releaseYear,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return ToProblem(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Query is required."
            });
        }

        var requestProblem = ValidatePagedRequest(page, pageSize, releaseYear, out var effectivePage, out var effectivePageSize);
        if (requestProblem is not null)
        {
            return ToProblem(requestProblem);
        }

        var results = await _searchMoviesUseCase.SearchAsync(query, effectivePage, effectivePageSize, releaseYear, cancellationToken);
        return Ok(ToPagedResponse(results));
    }

    [HttpGet("discover")]
    public async Task<ActionResult<SearchMoviesResponse>> Discover(
        [FromQuery] string? genre,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] int? releaseYear,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(genre))
        {
            return ToProblem(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Genre is required."
            });
        }

        var requestProblem = ValidatePagedRequest(page, pageSize, releaseYear, out var effectivePage, out var effectivePageSize);
        if (requestProblem is not null)
        {
            return ToProblem(requestProblem);
        }

        var results = await _discoverMoviesByGenreUseCase.GetAsync(genre, effectivePage, effectivePageSize, releaseYear, cancellationToken);
        return Ok(ToPagedResponse(results));
    }

    [HttpGet("trending")]
    public async Task<ActionResult<GetTrendingMoviesResponse>> GetTrending([FromQuery] int? limit, CancellationToken cancellationToken)
    {
        var effectiveLimit = limit ?? 10;
        if (effectiveLimit <= 0 || effectiveLimit > 50)
        {
            return ToProblem(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Limit must be between 1 and 50."
            });
        }

        var results = await _getTrendingMoviesUseCase.GetAsync(effectiveLimit, cancellationToken);
        var response = new GetTrendingMoviesResponse
        {
            Items = results
                .Select(ToMovieCardDto)
                .ToList()
        };

        return Ok(response);
    }

    [HttpGet("{movieId:int}/similar")]
    public async Task<ActionResult<GetSimilarMoviesResponse>> GetSimilar([FromRoute] int movieId, CancellationToken cancellationToken)
    {
        if (movieId <= 0)
        {
            return ToProblem(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Movie id must be positive."
            });
        }

        var results = await _getSimilarMoviesUseCase.GetAsync(movieId, cancellationToken);
        var response = new GetSimilarMoviesResponse
        {
            Items = results
                .Select(ToMovieCardDto)
                .ToList()
        };

        return Ok(response);
    }

    [HttpGet("{movieId:int}")]
    public async Task<ActionResult<MovieDetailsDto>> GetById([FromRoute] int movieId, [FromQuery] string? countryCode, CancellationToken cancellationToken)
    {
        if (movieId <= 0)
        {
            return ToProblem(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Movie id must be positive."
            });
        }

        MovieDetailsDto BuildResponse(MovieDetailsWithProviders detailsWithProviders)
        {
            return new MovieDetailsDto
            {
                MovieId = detailsWithProviders.MovieId,
                Title = detailsWithProviders.Title,
                RuntimeMinutes = detailsWithProviders.RuntimeMinutes > 0 ? detailsWithProviders.RuntimeMinutes : null,
                Genres = detailsWithProviders.Genres,
                Providers = detailsWithProviders.Providers,
                PosterUrl = detailsWithProviders.PosterUrl,
                BackdropUrl = detailsWithProviders.BackdropUrl,
                ReleaseYear = detailsWithProviders.ReleaseYear,
                Overview = detailsWithProviders.Overview
            };
        }

        var details = await _getMovieDetailsUseCase.GetAsync(movieId, countryCode, cancellationToken);
        if (details is null)
        {
            return ToProblem(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Movie not found."
            });
        }

        return Ok(BuildResponse(details));
    }

    private static MovieCardDto ToMovieCardDto(MovieCard movie)
    {
        return new MovieCardDto
        {
            MovieId = movie.MovieId,
            Title = movie.Title,
            RuntimeMinutes = movie.RuntimeMinutes,
            Genres = movie.Genres,
            PosterUrl = movie.PosterUrl,
            BackdropUrl = movie.BackdropUrl,
            ReleaseYear = movie.ReleaseYear,
            Overview = movie.Overview
        };
    }

    private static SearchMoviesResponse ToPagedResponse(PagedResult<MovieCard> results)
    {
        return new SearchMoviesResponse
        {
            Items = results
                .Items
                .Select(ToMovieCardDto)
                .ToList(),
            Page = results.Page,
            PageSize = results.PageSize,
            TotalResults = results.TotalResults,
            TotalPages = results.TotalPages,
            HasNextPage = results.HasNextPage
        };
    }

    private static ObjectResult ToProblem(ProblemDetails details)
    {
        return new ObjectResult(details)
        {
            StatusCode = details.Status
        };
    }
    private static ProblemDetails? ValidatePagedRequest(
        int? page,
        int? pageSize,
        int? releaseYear,
        out int effectivePage,
        out int effectivePageSize)
    {
        effectivePage = page ?? DefaultSearchPage;
        if (effectivePage <= 0)
        {
            effectivePageSize = DefaultSearchPageSize;
            return new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Page must be greater than 0."
            };
        }

        effectivePageSize = pageSize ?? DefaultSearchPageSize;
        if (effectivePageSize <= 0 || effectivePageSize > MaxSearchPageSize)
        {
            return new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = $"PageSize must be between 1 and {MaxSearchPageSize}."
            };
        }

        return ReleaseYearValidation.Validate(releaseYear);
    }
}
