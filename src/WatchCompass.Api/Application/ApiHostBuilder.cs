using Microsoft.AspNetCore.Mvc;
using Serilog;
using Serilog.Events;
using WatchCompass.Api.Middleware;
using WatchCompass.Api.Serialization;
using WatchCompass.Application.DependencyInjection;
using WatchCompass.Application.UseCases.Recommendations;
using WatchCompass.Application.UseCases.SearchMovies;
using WatchCompass.Contracts;
using WatchCompass.Domain.Enums;
using WatchCompass.Domain.ValueObjects;
using WatchCompass.Infrastructure.DependencyInjection;

namespace WatchCompass.Api.Application;

public static class ApiHostBuilder
{
    public static WebApplication BuildApp(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseSerilog((context, services, loggerConfiguration) =>
        {
            loggerConfiguration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console();
        });

        builder.Services.AddApplication();
        builder.Services.AddInfrastructure();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonResponse.DefaultOptions.PropertyNamingPolicy;
            options.SerializerOptions.DictionaryKeyPolicy = JsonResponse.DefaultOptions.DictionaryKeyPolicy;
            options.SerializerOptions.DefaultIgnoreCondition = JsonResponse.DefaultOptions.DefaultIgnoreCondition;
        });

        var app = builder.Build();

        app.UseSerilogRequestLogging();
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        MapEndpoints(app);

        return app;
    }

    private static void MapEndpoints(WebApplication app)
    {
        app.MapGet("/health", () => Results.Text("ok", "text/plain"))
            .WithName("Health")
            .Produces(StatusCodes.Status200OK);

        app.MapGet("/api/movies/search", async ([FromQuery] string query, SearchMoviesUseCase useCase, CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return JsonResponse.Problem(new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Query is required."
                    });
                }

                var results = await useCase.SearchAsync(query, cancellationToken);
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

                return JsonResponse.Ok(response);
            })
            .WithName("SearchMovies")
            .Produces<SearchMoviesResponse>(StatusCodes.Status200OK);

        app.MapPost("/api/recommendations", async (GetRecommendationsRequest request, GetRecommendationsUseCase useCase, CancellationToken cancellationToken) =>
            {
                if (!Enum.TryParse<Mood>(request.Mood, true, out var mood))
                {
                    return JsonResponse.Problem(new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Invalid mood provided."
                    });
                }

                if (request.TimeBudgetMinutes < 1 || request.TimeBudgetMinutes > 600)
                {
                    return JsonResponse.Problem(new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Time budget must be between 1 and 600 minutes."
                    });
                }

                var timeBudget = new TimeBudget(request.TimeBudgetMinutes);

                var recommendations = await useCase.GetRecommendationsAsync(
                    mood,
                    timeBudget,
                    request.Query,
                    request.AvoidGenres,
                    request.CountryCode,
                    cancellationToken);

                var response = new GetRecommendationsResponse
                {
                    Items = recommendations
                        .Select(rec => new RecommendationDto
                        {
                            MovieId = rec.MovieId,
                            Title = rec.Title,
                            RuntimeMinutes = rec.RuntimeMinutes,
                            Reasons = rec.Reasons,
                            Providers = rec.Providers
                        })
                        .ToList()
                };

                return JsonResponse.Ok(response);
            })
            .WithName("GetRecommendations")
            .Produces<GetRecommendationsResponse>(StatusCodes.Status200OK);
    }
}
