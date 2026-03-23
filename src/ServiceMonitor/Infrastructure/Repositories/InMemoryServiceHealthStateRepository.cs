using System.Collections.Concurrent;
using Ardalis.GuardClauses;
using ServiceMonitor.Domain.Entities;
using ServiceMonitor.Domain.Interfaces;

namespace ServiceMonitor.Infrastructure.Repositories;

public class InMemoryServiceHealthStateRepository : IServiceHealthStateRepository
{
    private readonly ConcurrentDictionary<Uri, ServiceHealthState> _states = new();

    public Task<ServiceHealthState?> GetAsync(Uri url, CancellationToken cancellationToken)
    {
        Guard.Against.Null(url);
        _states.TryGetValue(url, out var state);
        return Task.FromResult(state);
    }

    public Task SaveAsync(ServiceHealthState state, CancellationToken cancellationToken)
    {
        Guard.Against.Null(state);
        _states[state.Url] = state;
        return Task.CompletedTask;
    }
}

