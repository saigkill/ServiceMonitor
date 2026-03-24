using ServiceMonitor.Application.DTOs;

namespace ServiceMonitor.Application.Interfaces;

public interface IHealthMonitoringService
{
    Task<IEnumerable<HealthCheckResult>> MonitorServicesAsync(
        IEnumerable<Uri> serviceUrls,
        CancellationToken cancellationToken);
}
