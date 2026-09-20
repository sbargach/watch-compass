namespace WatchCompass.Infrastructure.Movies;

public sealed record CatalogOptions
{
    public string Mode { get; }
    public bool IsDemo => Mode == "demo";

    public CatalogOptions(string mode)
    {
        Mode = mode.Trim().ToLowerInvariant() switch
        {
            "demo" => "demo",
            "tmdb" => "tmdb",
            _ => throw new ArgumentException("Catalog:Mode must be Tmdb or Demo.", nameof(mode))
        };
    }
}
