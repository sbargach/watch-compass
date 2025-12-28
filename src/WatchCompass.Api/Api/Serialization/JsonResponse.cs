using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace WatchCompass.Api.Serialization;

public static class JsonResponse
{
    public static JsonSerializerOptions DefaultOptions { get; } = new(JsonSerializerDefaults.Web);

    public static IResult Ok<T>(T value, JsonSerializerOptions? options = null)
    {
        var payload = Serialize(value, options);
        return Results.Content(payload, "application/json", statusCode: StatusCodes.Status200OK);
    }

    public static IResult Problem(ProblemDetails details, JsonSerializerOptions? options = null)
    {
        var payload = Serialize(details, options);
        var statusCode = details.Status ?? StatusCodes.Status500InternalServerError;
        return Results.Content(payload, "application/problem+json", statusCode: statusCode);
    }

    private static string Serialize<T>(T value, JsonSerializerOptions? options)
    {
        return JsonSerializer.Serialize(value, options ?? DefaultOptions);
    }
}
