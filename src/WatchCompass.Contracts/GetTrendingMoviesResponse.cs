namespace WatchCompass.Contracts;

public sealed class GetTrendingMoviesResponse
{
    public List<MovieCardDto> Items { get; set; } = new();
}
