using System.Text.Json.Serialization;

namespace WatchCompass.Infrastructure.Movies.Tmdb;

public sealed class TmdbSearchResponse
{
    [JsonPropertyName("results")]
    public List<TmdbMovieSearchResult> Results { get; init; } = [];
}

public sealed class TmdbMovieSearchResult
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("genre_ids")]
    public List<int> GenreIds { get; init; } = [];

    [JsonPropertyName("runtime")]
    public int? Runtime { get; init; }
}

public sealed class TmdbMovieDetailsResponse
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("runtime")]
    public int? Runtime { get; init; }

    [JsonPropertyName("genres")]
    public List<TmdbGenre> Genres { get; init; } = [];
}

public sealed class TmdbGenre
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
}

public sealed class TmdbWatchProvidersResponse
{
    [JsonPropertyName("results")]
    public Dictionary<string, TmdbProviderCountry> Results { get; init; } = new();
}

public sealed class TmdbProviderCountry
{
    [JsonPropertyName("flatrate")]
    public List<TmdbProvider> FlatRate { get; init; } = [];

    [JsonPropertyName("rent")]
    public List<TmdbProvider> Rent { get; init; } = [];

    [JsonPropertyName("buy")]
    public List<TmdbProvider> Buy { get; init; } = [];
}

public sealed class TmdbProvider
{
    [JsonPropertyName("provider_name")]
    public string ProviderName { get; init; } = string.Empty;
}
