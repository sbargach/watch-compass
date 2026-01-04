using Microsoft.AspNetCore.Mvc;
using WatchCompass.Application.UseCases.SearchMovies;
using WatchCompass.Contracts;

namespace WatchCompass.Api.Api.Controllers;

[ApiController]
[Route("api/movies")]
public sealed class SearchController : ControllerBase
{
    private readonly SearchMoviesUseCase _useCase;

    public SearchController(SearchMoviesUseCase useCase)
    {
        _useCase = useCase;
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

        var results = await _useCase.SearchAsync(query, cancellationToken);
        var response = new SearchMoviesResponse
        {
            Items = results
                .Select(movie => new MovieCardDto
                {
                    MovieId = movie.MovieId,
                    Title = movie.Title,
                    RuntimeMinutes = movie.RuntimeMinutes,
                    Genres = movie.Genres
                })
                .ToList()
        };

        return Ok(response);
    }

    private ActionResult<SearchMoviesResponse> ToProblem(ProblemDetails details)
    {
        return new ObjectResult(details)
        {
            StatusCode = details.Status
        };
    }
}
