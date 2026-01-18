using Microsoft.AspNetCore.Mvc;
using WatchCompass.Application.UseCases.Genres;
using WatchCompass.Contracts;

namespace WatchCompass.Api.Api.Controllers;

[ApiController]
[Route("api/genres")]
public sealed class GenresController : ControllerBase
{
    private readonly GetGenresUseCase _useCase;

    public GenresController(GetGenresUseCase useCase)
    {
        _useCase = useCase;
    }

    [HttpGet]
    public async Task<ActionResult<GetGenresResponse>> Get(CancellationToken cancellationToken)
    {
        var genres = await _useCase.GetAsync(cancellationToken);
        var response = new GetGenresResponse
        {
            Items = genres
        };

        return Ok(response);
    }
}
