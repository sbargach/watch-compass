using Microsoft.AspNetCore.Mvc;
using WatchCompass.Api.Serialization;
using System.Text.Json;
using System.Diagnostics;
using System.Net;
using WatchCompass.Infrastructure.Movies.Tmdb;

namespace WatchCompass.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogDebug("Request was cancelled by the client.");
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 499;
            }
        }
        catch (TmdbApiException ex)
        {
            var status = IsUnavailable(ex.StatusCode)
                ? StatusCodes.Status503ServiceUnavailable
                : StatusCodes.Status502BadGateway;
            _logger.LogWarning(ex, "TMDB dependency request failed with status {StatusCode}", (int)ex.StatusCode);
            await WriteProblemAsync(
                context,
                status,
                status == StatusCodes.Status503ServiceUnavailable
                    ? "Movie data is temporarily unavailable."
                    : "The movie data provider returned an invalid response.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
        }
    }

    private static bool IsUnavailable(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout
            || (int)statusCode >= 500;
    }

    private static async Task WriteProblemAsync(HttpContext context, int status, string title)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title
        };
        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;

        await JsonResponse.Problem(problem).ExecuteAsync(context);
    }
}
