using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceMonitor.Application.Interfaces;
using ServiceMonitor.Infrastructure.Configuration;

namespace ServiceMonitor.Application.UseCases;

public sealed class MonitorServicesUseCase(
    IHealthMonitoringService healthMonitoring,
    IAlertService alertService,
    IOptions<ServiceMonitorOptions> options,
    ILogger<MonitorServicesUseCase> logger)
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var urls = options.Value.Urls.Select(u => new Uri(u));
        var results = await healthMonitoring.MonitorServicesAsync(urls, cancellationToken);

        foreach (var result in results.Where(r => !r.IsHealthy))
        {
            await alertService.SendAlertAsync(
                result.ServiceUrl,
                result.ErrorMessage ?? "Unknown error",
                options.Value.EmailServer.To,
                cancellationToken);

            logger.LogWarning(
                "Service {Url} returned status {Status}",
                result.ServiceUrl,
                result.StatusCode);
        }
    }
}
