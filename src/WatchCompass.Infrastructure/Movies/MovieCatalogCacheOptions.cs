namespace WatchCompass.Infrastructure.Movies;

public sealed class MovieCatalogCacheOptions
{
    public int SearchMinutes { get; set; } = 5;

    public int DetailsMinutes { get; set; } = 30;

    public int ProvidersMinutes { get; set; } = 30;

    public int GenresMinutes { get; set; } = 120;

    internal TimeSpan SearchDuration => TimeSpan.FromMinutes(Math.Max(0, SearchMinutes));

    internal TimeSpan DetailsDuration => TimeSpan.FromMinutes(Math.Max(0, DetailsMinutes));

    internal TimeSpan ProvidersDuration => TimeSpan.FromMinutes(Math.Max(0, ProvidersMinutes));

    internal TimeSpan GenresDuration => TimeSpan.FromMinutes(Math.Max(0, GenresMinutes));
}
