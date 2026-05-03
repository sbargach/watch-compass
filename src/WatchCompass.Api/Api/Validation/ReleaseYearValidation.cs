using Microsoft.AspNetCore.Mvc;

namespace WatchCompass.Api.Api.Validation;

internal static class ReleaseYearValidation
{
    private const int MinReleaseYear = 1888;

    public static ProblemDetails? Validate(int? releaseYear)
    {
        if (releaseYear is null)
        {
            return null;
        }

        var maxReleaseYear = DateTime.UtcNow.Year + 1;
        if (releaseYear.Value >= MinReleaseYear && releaseYear.Value <= maxReleaseYear)
        {
            return null;
        }

        return new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = $"ReleaseYear must be between {MinReleaseYear} and {maxReleaseYear}."
        };
    }
}
