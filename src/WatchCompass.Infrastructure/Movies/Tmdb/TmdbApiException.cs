using System.Net;

namespace WatchCompass.Infrastructure.Movies.Tmdb;

public sealed class TmdbApiException : Exception
{
    public TmdbApiException(HttpStatusCode statusCode, string message, Exception? innerException = null, string? responseBody = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public HttpStatusCode StatusCode { get; }

    public string? ResponseBody { get; }
}
