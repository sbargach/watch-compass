using Microsoft.Extensions.Options;

namespace WatchCompass.Infrastructure.Movies.Tmdb;

public sealed class BasePathHandler : DelegatingHandler
{
    private readonly string _basePath;

    public BasePathHandler(IOptions<TmdbOptions> options)
    {
        var uri = new Uri(options.Value.BaseUrl);
        _basePath = uri.AbsolutePath.TrimEnd('/');
        if (_basePath == "/" || string.IsNullOrEmpty(_basePath))
        {
            _basePath = string.Empty;
        }
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri is not null && !string.IsNullOrEmpty(_basePath))
        {
            var path = request.RequestUri.AbsolutePath;
            if (!path.StartsWith(_basePath, StringComparison.Ordinal))
            {
                var adjustedPath = $"{_basePath}{(path.StartsWith("/", StringComparison.Ordinal) ? path : "/" + path)}";
                var builder = new UriBuilder(request.RequestUri)
                {
                    Path = adjustedPath
                };
                request.RequestUri = builder.Uri;
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}
