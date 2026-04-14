using CSharpFunctionalExtensions;
using ServiceMonitor.Application.DTOs;

namespace ServiceMonitor.Application.Interfaces;

public interface IHealthMonitoringService
{
    Task<Result<IEnumerable<HealthCheckResult>>> MonitorServicesAsync(
        IEnumerable<Uri> urls,
        CancellationToken cancellationToken);
}
