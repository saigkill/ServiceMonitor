using System.Collections.Concurrent;
using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using ServiceMonitor.Domain.Entities;
using ServiceMonitor.Domain.Interfaces;

namespace ServiceMonitor.Infrastructure.Repositories;

public class InMemoryServiceHealthStateRepository : IServiceHealthStateRepository
{
    private readonly ConcurrentDictionary<Uri, ServiceHealthState> _states = new();

    public Task<Maybe<ServiceHealthState>> GetAsync(Uri url, CancellationToken cancellationToken)
    {
        Guard.Against.Null(url);

        return Task.FromResult(
            _states.TryGetValue(url, out var state) 
                ? Maybe<ServiceHealthState>.From(state) 
                : Maybe<ServiceHealthState>.None);
    }

    public Task<Result> SaveAsync(ServiceHealthState state, CancellationToken cancellationToken)
    {
        Guard.Against.Null(state);

        try
        {
            _states[state.Url] = state;
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure($"Failed to save state: {ex.Message}"));
        }
    }
}

