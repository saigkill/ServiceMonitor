using CSharpFunctionalExtensions;

using Microsoft.Extensions.Logging;

using ServiceMonitor.Application.DTOs;
using ServiceMonitor.Application.Interfaces;

using System.Globalization;
using System.Net;

namespace ServiceMonitor.Infrastructure.Http;

public sealed class HttpHealthMonitoringService(
    IHttpClientFactory httpClientFactory,
    ILogger<HttpHealthMonitoringService> logger) : IHealthMonitoringService
{
    public async Task<Result<IEnumerable<HealthCheckResult>>> MonitorServicesAsync(
        IEnumerable<Uri> urls,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            var tasks = urls.Select(url => CheckUrlAsync(client, url, cancellationToken));
            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            return Result.Success(results.AsEnumerable());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to monitor services: {Message}", ex.Message);
            return Result.Failure<IEnumerable<HealthCheckResult>>($"Monitoring failed: {ex.Message}");
        }
    }

    private async Task<HealthCheckResult> CheckUrlAsync(
        HttpClient client,
        Uri url,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);

            return new HealthCheckResult(
                url,
                IsHealthy: response.IsSuccessStatusCode,
                StatusCode: response.StatusCode,
                ErrorMessage: response.IsSuccessStatusCode
                    ? Maybe<string>.None
                    : Maybe<string>.From($"HTTP {((int)response.StatusCode).ToString(CultureInfo.InvariantCulture)}"));
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP request failed for {Url}", url);
            return new HealthCheckResult(
                url,
                IsHealthy: false,
                StatusCode: HttpStatusCode.ServiceUnavailable,
                ErrorMessage: Maybe<string>.From(ex.Message));
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Request timeout for {Url}", url);
            return new HealthCheckResult(
                url,
                IsHealthy: false,
                StatusCode: HttpStatusCode.RequestTimeout,
                ErrorMessage: Maybe<string>.From("Request timeout"));
        }
    }
}
