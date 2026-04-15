using CSharpFunctionalExtensions;
using ServiceMonitor.Domain.Entities;

namespace ServiceMonitor.Domain.Interfaces;

public interface IServiceHealthStateRepository
{
    Task<Maybe<ServiceHealthState>> GetAsync(Uri url, CancellationToken cancellationToken);
    Task<Result> SaveAsync(ServiceHealthState state, CancellationToken cancellationToken);
}

