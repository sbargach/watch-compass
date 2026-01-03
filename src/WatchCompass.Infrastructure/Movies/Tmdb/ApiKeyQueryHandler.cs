using Microsoft.Extensions.Options;

namespace WatchCompass.Infrastructure.Movies.Tmdb;

public sealed class ApiKeyQueryHandler : DelegatingHandler
{
    private readonly IOptions<TmdbOptions> _options;

    public ApiKeyQueryHandler(IOptions<TmdbOptions> options)
    {
        _options = options;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var options = _options.Value;
        if (options.AuthMode == TmdbAuthMode.ApiKeyQuery && !string.IsNullOrWhiteSpace(options.ApiKey) && request.RequestUri is not null)
        {
            var uriBuilder = new UriBuilder(request.RequestUri);
            var apiKeyParameter = $"api_key={Uri.EscapeDataString(options.ApiKey)}";
            if (string.IsNullOrEmpty(uriBuilder.Query))
            {
                uriBuilder.Query = apiKeyParameter;
            }
            else
            {
                uriBuilder.Query = $"{uriBuilder.Query.TrimStart('?')}&{apiKeyParameter}";
            }

            request.RequestUri = uriBuilder.Uri;
        }

        return base.SendAsync(request, cancellationToken);
    }
}
