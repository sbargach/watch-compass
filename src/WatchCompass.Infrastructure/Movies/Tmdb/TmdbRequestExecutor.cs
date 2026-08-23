using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.Timeout;

namespace WatchCompass.Infrastructure.Movies.Tmdb;

public sealed class TmdbRequestExecutor
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TmdbRequestExecutor> _logger;
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

    public TmdbRequestExecutor(HttpClient httpClient, IOptions<TmdbOptions> options, ILogger<TmdbRequestExecutor> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _ = options.Value;
    }

    public async Task<TResponse> SendAsync<TResponse>(Func<HttpRequestMessage> requestFactory, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            using var request = requestFactory();
            response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (Exception ex) when ((ex is TimeoutRejectedException or TaskCanceledException) && !cancellationToken.IsCancellationRequested)
        {
            throw new TmdbApiException(HttpStatusCode.RequestTimeout, "TMDB request timed out.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new TmdbApiException(HttpStatusCode.ServiceUnavailable, "TMDB request failed to reach the server.", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("TMDB responded with status {StatusCode}: {Reason}", (int)response.StatusCode, response.ReasonPhrase);
                throw new TmdbApiException(response.StatusCode, $"TMDB request failed with status code {(int)response.StatusCode}.");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            try
            {
                var result = await JsonSerializer.DeserializeAsync<TResponse>(stream, _serializerOptions, cancellationToken);
                if (result is null)
                {
                    throw new TmdbApiException(response.StatusCode, "TMDB response was empty.");
                }

                return result;
            }
            catch (JsonException ex)
            {
                throw new TmdbApiException(response.StatusCode, "TMDB response could not be parsed.", ex);
            }
        }
    }
}
