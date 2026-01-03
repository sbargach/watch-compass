namespace WatchCompass.Infrastructure.Movies.Tmdb;

public enum TmdbAuthMode
{
    Bearer,
    ApiKeyQuery
}

public sealed class TmdbOptions
{
    public string BaseUrl { get; set; } = "https://api.themoviedb.org/3";

    public string ApiKey { get; set; } = string.Empty;

    public TmdbAuthMode AuthMode { get; set; } = TmdbAuthMode.Bearer;
}
