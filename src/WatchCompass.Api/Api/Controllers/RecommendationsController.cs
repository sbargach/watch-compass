using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WatchCompass.Application.Abstractions.Movies;
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
    private readonly IMovieCatalog _movieCatalog;
    private readonly ILogger<RecommendationsController> _logger;

    public RecommendationsController(GetRecommendationsUseCase useCase, IMovieCatalog movieCatalog, ILogger<RecommendationsController> logger)
    {
        _useCase = useCase;
        _movieCatalog = movieCatalog;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<GetRecommendationsResponse>> Create([FromBody] GetRecommendationsRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<Mood>(request.Mood, true, out var mood))
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

        if (request.TimeBudgetMinutes < 1 || request.TimeBudgetMinutes > 600)
        {
            return ToProblem(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Time budget must be between 1 and 600 minutes."
            });
        }

        var timeBudget = new TimeBudget(request.TimeBudgetMinutes);
        var countryCode = request.CountryCode ?? string.Empty;

        var recommendations = await _useCase.GetRecommendationsAsync(
            mood,
            timeBudget,
            request.Query,
            avoidGenres,
            cancellationToken);

        var providersByMovie = new Dictionary<int, IReadOnlyList<string>>();
        foreach (var recommendation in recommendations)
        {
            try
            {
                var providers = await _movieCatalog.GetWatchProvidersAsync(recommendation.MovieId, countryCode, cancellationToken);
                providersByMovie[recommendation.MovieId] = providers;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to load providers for movie {MovieId}", recommendation.MovieId);
            }
        }

        var response = new GetRecommendationsResponse
        {
            Items = recommendations
                .Select(rec => new RecommendationDto
                {
                    MovieId = rec.MovieId,
                    Title = rec.Title,
                    RuntimeMinutes = rec.RuntimeMinutes,
                    Reasons = rec.Reasons,
                    Providers = providersByMovie.TryGetValue(rec.MovieId, out var providers)
                        ? providers
                        : rec.Providers,
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
