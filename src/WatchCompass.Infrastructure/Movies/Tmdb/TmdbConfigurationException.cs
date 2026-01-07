namespace WatchCompass.Infrastructure.Movies.Tmdb;

public sealed class TmdbConfigurationException : InvalidOperationException
{
    public TmdbConfigurationException(string message)
        : base(message)
    {
    }
}
