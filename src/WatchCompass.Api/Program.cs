using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using WatchCompass.Api.Middleware;
using WatchCompass.Api.Serialization;
using WatchCompass.Application.DependencyInjection;
using WatchCompass.Infrastructure.DependencyInjection;

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
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonResponse.DefaultOptions.PropertyNamingPolicy;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonResponse.DefaultOptions.DictionaryKeyPolicy;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonResponse.DefaultOptions.DefaultIgnoreCondition;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("watch-compass"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("watch-compass.tmdb")
        .AddPrometheusExporter());
// No additional JSON configuration needed; controllers use configured options.

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPrometheusScrapingEndpoint();

app.MapGet("/health", () => Results.Text("ok", "text/plain"))
    .WithName("Health")
    .Produces(StatusCodes.Status200OK);

app.MapControllers();

await app.RunAsync();

public partial class Program;
