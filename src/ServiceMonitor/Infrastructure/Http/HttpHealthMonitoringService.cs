using Ardalis.GuardClauses;
using Microsoft.Extensions.Options;
using ServiceMonitor.Application.DTOs;
using ServiceMonitor.Application.Interfaces;
using ServiceMonitor.Infrastructure.Configuration;

namespace ServiceMonitor.Infrastructure.Http;

public sealed class HttpHealthMonitoringService(
    IHttpClientFactory httpClientFactory,
    IOptions<ServiceMonitorOptions> options) : IHealthMonitoringService
{
    public async Task<IEnumerable<HealthCheckResult>> MonitorServicesAsync(
        IEnumerable<Uri> serviceUrls,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(serviceUrls);
        Guard.Against.Null(httpClientFactory);
        using var httpClient = httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(options.Value.System.TimeoutSeconds);

        var tasks = serviceUrls.Select(url => CheckServiceAsync(httpClient, url, cancellationToken));
        return await Task.WhenAll(tasks);
    }

    private static async Task<HealthCheckResult> CheckServiceAsync(
        HttpClient client,
        Uri url,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(client);
        Guard.Against.Null(url);
        try
        {
            var response = await client.GetAsync(url, cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy(url, response.StatusCode)
                : HealthCheckResult.Unhealthy(url, $"Status: {response.StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(url, ex.Message);
        }
    }
}
