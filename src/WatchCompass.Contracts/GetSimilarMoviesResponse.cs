namespace WatchCompass.Contracts;

public sealed class GetSimilarMoviesResponse
{
    public List<MovieCardDto> Items { get; set; } = new();
}
