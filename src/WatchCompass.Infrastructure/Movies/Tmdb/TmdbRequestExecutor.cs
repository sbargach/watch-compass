using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WatchCompass.Infrastructure.Movies.Tmdb;

public sealed class TmdbRequestExecutor
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TmdbRequestExecutor> _logger;
    private readonly TmdbOptions _options;
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

    public TmdbRequestExecutor(HttpClient httpClient, IOptions<TmdbOptions> options, ILogger<TmdbRequestExecutor> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<TResponse> SendAsync<TResponse>(Func<HttpRequestMessage> requestFactory, CancellationToken cancellationToken)
    {
        var attempt = 0;
        var maxAttempts = Math.Max(1, _options.MaxRetries + 1);

        while (true)
        {
            attempt++;
            HttpResponseMessage response;
            try
            {
                using var request = requestFactory();
                response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                if (attempt < maxAttempts)
                {
                    await DelayAsync(attempt, cancellationToken);
                    continue;
                }

                throw new TmdbApiException(HttpStatusCode.RequestTimeout, "TMDB request timed out.", ex);
            }
            catch (HttpRequestException ex)
            {
                if (attempt < maxAttempts)
                {
                    await DelayAsync(attempt, cancellationToken);
                    continue;
                }

                throw new TmdbApiException(HttpStatusCode.ServiceUnavailable, "TMDB request failed to reach the server.", ex);
            }

            using (response)
            {
                if (IsTransient(response.StatusCode) && attempt < maxAttempts)
                {
                    await DelayAsync(attempt, cancellationToken);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("TMDB responded with status {StatusCode}: {Reason}", (int)response.StatusCode, response.ReasonPhrase);
                    throw new TmdbApiException(response.StatusCode, $"TMDB request failed with status code {(int)response.StatusCode}.", null, body);
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

    private static bool IsTransient(HttpStatusCode statusCode)
    {
        var numericStatus = (int)statusCode;
        return statusCode == HttpStatusCode.TooManyRequests || numericStatus >= 500;
    }

    private async Task DelayAsync(int attempt, CancellationToken cancellationToken)
    {
        var baseDelay = TimeSpan.FromMilliseconds(_options.BackoffBaseMilliseconds * attempt);
        var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, Math.Max(1, _options.BackoffJitterMilliseconds)));
        var delay = baseDelay + jitter;
        await Task.Delay(delay, cancellationToken);
    }
}
