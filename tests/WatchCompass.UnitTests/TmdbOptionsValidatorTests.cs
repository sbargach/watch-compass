using NUnit.Framework;
using Shouldly;
using WatchCompass.Infrastructure.Movies.Tmdb;

namespace WatchCompass.UnitTests;

[TestFixture]
public class TmdbOptionsValidatorTests
{
    private readonly TmdbOptionsValidator _validator = new();

    [Test]
    public void Validate_ReturnsSuccess_ForValidOptions()
    {
        var result = _validator.Validate(null, CreateValidOptions());

        result.Succeeded.ShouldBeTrue();
        result.Failed.ShouldBeFalse();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Validate_ReturnsFailure_WhenApiKeyMissing(string? apiKey)
    {
        var options = CreateValidOptions();
        options.ApiKey = apiKey ?? string.Empty;

        var result = _validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldBe("Tmdb:ApiKey is required.");
    }

    private static TmdbOptions CreateValidOptions()
    {
        return new TmdbOptions
        {
            BaseUrl = "https://api.themoviedb.org/3",
            ApiKey = "key",
            DefaultCountryCode = "US",
            Language = "en-US",
            RequestTimeoutSeconds = 5,
            MaxRetries = 1,
            BackoffBaseMilliseconds = 100,
            BackoffJitterMilliseconds = 50
        };
    }
}
