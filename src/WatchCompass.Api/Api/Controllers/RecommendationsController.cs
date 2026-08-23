using Microsoft.AspNetCore.Mvc;
using WatchCompass.Api.Api.Validation;
using WatchCompass.Application.UseCases.Recommendations;
using WatchCompass.Contracts;
using WatchCompass.Domain.Enums;
using WatchCompass.Domain.ValueObjects;

namespace WatchCompass.Api.Api.Controllers;

[ApiController]
[Route("api/recommendations")]
public sealed class RecommendationsController : ControllerBase
{
    private readonly GetRecommendationsUseCase _useCase;

    public RecommendationsController(GetRecommendationsUseCase useCase)
    {
        _useCase = useCase;
    }

    [HttpPost]
    public async Task<ActionResult<GetRecommendationsResponse>> Create([FromBody] GetRecommendationsRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<Mood>(request.Mood?.Trim(), true, out var mood))
        {
            return ToProblem(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid mood provided."
            });
        }

        var avoidGenres = request.AvoidGenres ?? Array.Empty<string>();
        if (avoidGenres.Count > 10)
        {
            return ToProblem(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "AvoidGenres cannot contain more than 10 entries."
            });
        }

        if (avoidGenres.Any(genre => genre is not null && genre.Trim().Length > RequestValidation.MaxGenreLength))
        {
            return ToProblem(RequestValidation.BadRequest($"AvoidGenres entries cannot exceed {RequestValidation.MaxGenreLength} characters."));
        }

        if (request.Query is not null)
        {
            var queryProblem = RequestValidation.ValidateText(request.Query, "Query", RequestValidation.MaxQueryLength);
            if (queryProblem is not null)
            {
                return ToProblem(queryProblem);
            }
        }

        if (request.TimeBudgetMinutes < 1 || request.TimeBudgetMinutes > 600)
        {
            return ToProblem(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Time budget must be between 1 and 600 minutes."
            });
        }

        var releaseYearProblem = ReleaseYearValidation.Validate(request.ReleaseYear);
        if (releaseYearProblem is not null)
        {
            return ToProblem(releaseYearProblem);
        }

        if (!RequestValidation.IsCountryCode(request.CountryCode?.Trim()))
        {
            return ToProblem(RequestValidation.BadRequest("CountryCode must be a two-letter country code."));
        }

        var timeBudget = new TimeBudget(request.TimeBudgetMinutes);
        var normalizedAvoidGenres = avoidGenres
            .Where(genre => !string.IsNullOrWhiteSpace(genre))
            .Select(genre => genre.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var countryCode = request.CountryCode!.Trim().ToUpperInvariant();

        var recommendations = await _useCase.GetRecommendationsAsync(
            mood,
            timeBudget,
            request.Query?.Trim(),
            normalizedAvoidGenres,
            countryCode,
            request.ReleaseYear,
            cancellationToken);

        var response = new GetRecommendationsResponse
        {
            Items = recommendations
                .Select(rec => new RecommendationDto
                {
                    MovieId = rec.MovieId,
                    Title = rec.Title,
                    RuntimeMinutes = rec.RuntimeMinutes,
                    Genres = rec.Genres,
                    Reasons = rec.Reasons,
                    Providers = rec.Providers,
                    PosterUrl = rec.PosterUrl,
                    BackdropUrl = rec.BackdropUrl,
                    ReleaseYear = rec.ReleaseYear,
                    Overview = rec.Overview
                })
                .ToList()
        };

        return Ok(response);
    }

    private ActionResult<GetRecommendationsResponse> ToProblem(ProblemDetails details)
    {
        return new ObjectResult(details)
        {
            StatusCode = details.Status
        };
    }
}
