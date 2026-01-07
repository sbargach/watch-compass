namespace WatchCompass.Infrastructure.Movies.Tmdb;

public sealed class TmdbOptions
{
    public string BaseUrl { get; set; } = "https://api.themoviedb.org/3";

    public string ApiKey { get; set; } = string.Empty;

    public string DefaultCountryCode { get; set; } = "US";

    public string Language { get; set; } = "en-US";

    public int RequestTimeoutSeconds { get; set; } = 10;

    public int MaxRetries { get; set; } = 2;

    public int BackoffBaseMilliseconds { get; set; } = 200;

    public int BackoffJitterMilliseconds { get; set; } = 150;
}
