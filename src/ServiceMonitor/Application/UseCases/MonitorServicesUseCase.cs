using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceMonitor.Application.Interfaces;
using ServiceMonitor.Domain.Entities;
using ServiceMonitor.Domain.Interfaces;
using ServiceMonitor.Infrastructure.Configuration;

namespace ServiceMonitor.Application.UseCases;

public sealed class MonitorServicesUseCase(
    IHealthMonitoringService healthMonitoring,
    IAlertService alertService,
    IOptions<ServiceMonitorOptions> options,
    ILogger<MonitorServicesUseCase> logger,
    IServiceHealthStateRepository stateRepository)
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var urls = options.Value.Urls.Select(u => new Uri(u));
        var results = await healthMonitoring.MonitorServicesAsync(urls, cancellationToken).ConfigureAwait(false);

        foreach (var result in results)
        {
            var url = result.ServiceUrl;
            var isHealthy = result.IsHealthy;

            var state = await stateRepository.GetAsync(url, cancellationToken).ConfigureAwait(false) ?? new ServiceHealthState(url, isHealthy);

            state.Update(isHealthy);

            if (!isHealthy)
            {
                if (!state.AlertSent)
                {
                    await alertService.SendAlertAsync(
                        result.ServiceUrl,
                        result.ErrorMessage ?? "Unknown error",
                        options.Value.EmailServer.To,
                        cancellationToken).ConfigureAwait(false);
                    state.MarkAlertSent();
                    logger.LogError("Down-Mail sent for {0} with status: {1}", result.ServiceUrl, result.StatusCode);
                }

                logger.LogInformation(
                    "Service {Url} returned status {Status}. Down-Mail already sent.Interfaces",
                    result.ServiceUrl,
                    result.StatusCode);
            }
        }
    }
}
