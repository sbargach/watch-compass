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
const string FrontendCorsPolicy = "Frontend";

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
allowedOrigins = allowedOrigins is { Length: > 0 }
    ? allowedOrigins
    : ["http://localhost:5173", "http://127.0.0.1:5173"];

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
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonResponse.DefaultOptions.PropertyNamingPolicy;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonResponse.DefaultOptions.DictionaryKeyPolicy;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonResponse.DefaultOptions.DefaultIgnoreCondition;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var basePath = AppContext.BaseDirectory;
    var apiXml = Path.Combine(basePath, "WatchCompass.Api.xml");
    var contractsXml = Path.Combine(basePath, "WatchCompass.Contracts.xml");

    if (File.Exists(apiXml))
    {
        options.IncludeXmlComments(apiXml, includeControllerXmlComments: true);
    }

    if (File.Exists(contractsXml))
    {
        options.IncludeXmlComments(contractsXml);
    }
});
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
app.UseCors(FrontendCorsPolicy);

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
