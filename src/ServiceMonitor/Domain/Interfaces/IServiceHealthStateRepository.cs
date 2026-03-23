using ServiceMonitor.Domain.Entities;

namespace ServiceMonitor.Domain.Interfaces;

public interface IServiceHealthStateRepository
{
    Task<ServiceHealthState?> GetAsync(Uri url, CancellationToken cancellationToken);
    Task SaveAsync(ServiceHealthState state, CancellationToken cancellationToken);
}

