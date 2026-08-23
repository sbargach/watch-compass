using Microsoft.AspNetCore.Mvc;

namespace WatchCompass.Api.Api.Validation;

public static class RequestValidation
{
    public const int MaxQueryLength = 100;
    public const int MaxGenreLength = 50;

    public static ProblemDetails? ValidateText(string value, string fieldName, int maxLength)
    {
        if (value.Trim().Length > maxLength)
        {
            return BadRequest($"{fieldName} cannot exceed {maxLength} characters.");
        }

        return null;
    }

    public static bool IsCountryCode(string? value)
    {
        if (value is null || value.Length != 2)
        {
            return false;
        }

        return IsAsciiLetter(value[0]) && IsAsciiLetter(value[1]);
    }

    public static ProblemDetails BadRequest(string title)
    {
        return new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = title
        };
    }

    private static bool IsAsciiLetter(char value)
    {
        return value is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
    }
}
