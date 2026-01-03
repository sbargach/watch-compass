using Microsoft.Extensions.Options;

namespace WatchCompass.Infrastructure.Movies.Tmdb;

public sealed class TmdbOptionsValidator : IValidateOptions<TmdbOptions>
{
    public ValidateOptionsResult Validate(string? name, TmdbOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            return ValidateOptionsResult.Fail("Tmdb:BaseUrl is required.");
        }

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _))
        {
            return ValidateOptionsResult.Fail("Tmdb:BaseUrl must be an absolute URI.");
        }

        return ValidateOptionsResult.Success;
    }
}
