using System.Net.Http.Headers;
using Microsoft.Extensions.Options;

namespace WatchCompass.Infrastructure.Movies.Tmdb;

public sealed class BearerAuthHandler : DelegatingHandler
{
    private readonly IOptions<TmdbOptions> _options;

    public BearerAuthHandler(IOptions<TmdbOptions> options)
    {
        _options = options;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var options = _options.Value;
        if (options.AuthMode == TmdbAuthMode.Bearer && !string.IsNullOrWhiteSpace(options.ApiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
