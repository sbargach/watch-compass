using Microsoft.AspNetCore.Mvc;
using WatchCompass.Application.Dtos;
using WatchCompass.Application.UseCases.MovieDetails;
using WatchCompass.Application.UseCases.SearchMovies;
using WatchCompass.Application.UseCases.SimilarMovies;
using WatchCompass.Contracts;

namespace WatchCompass.Api.Api.Controllers;

[ApiController]
[Route("api/movies")]
public sealed class SearchController : ControllerBase
{
    private readonly SearchMoviesUseCase _searchMoviesUseCase;
    private readonly GetMovieDetailsUseCase _getMovieDetailsUseCase;
    private readonly GetSimilarMoviesUseCase _getSimilarMoviesUseCase;

    public SearchController(
        SearchMoviesUseCase searchMoviesUseCase,
        GetMovieDetailsUseCase getMovieDetailsUseCase,
        GetSimilarMoviesUseCase getSimilarMoviesUseCase)
    {
        _searchMoviesUseCase = searchMoviesUseCase;
        _getMovieDetailsUseCase = getMovieDetailsUseCase;
        _getSimilarMoviesUseCase = getSimilarMoviesUseCase;
    }

    [HttpGet("search")]
    public async Task<ActionResult<SearchMoviesResponse>> Search([FromQuery] string? query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return ToProblem(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Query is required."
            });
        }

        var results = await _searchMoviesUseCase.SearchAsync(query, cancellationToken);
        var response = new SearchMoviesResponse
        {
            Items = results
                .Select(movie => new MovieCardDto
                {
                    MovieId = movie.MovieId,
                    Title = movie.Title,
                    RuntimeMinutes = movie.RuntimeMinutes,
                    Genres = movie.Genres,
                    PosterUrl = movie.PosterUrl,
                    BackdropUrl = movie.BackdropUrl,
                    ReleaseYear = movie.ReleaseYear,
                    Overview = movie.Overview
                })
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
                .Select(movie => new MovieCardDto
                {
                    MovieId = movie.MovieId,
                    Title = movie.Title,
                    RuntimeMinutes = movie.RuntimeMinutes,
                    Genres = movie.Genres,
                    PosterUrl = movie.PosterUrl,
                    BackdropUrl = movie.BackdropUrl,
                    ReleaseYear = movie.ReleaseYear,
                    Overview = movie.Overview
                })
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

    private static ObjectResult ToProblem(ProblemDetails details)
    {
        return new ObjectResult(details)
        {
            StatusCode = details.Status
        };
    }
}
