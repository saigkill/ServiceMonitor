using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceMonitor.Application.Interfaces;
using ServiceMonitor.Domain.Entities;
using ServiceMonitor.Domain.Interfaces;
using ServiceMonitor.Infrastructure.Configuration;

namespace ServiceMonitor.Application.UseCases;

public sealed class MonitorServicesUseCase
{
    private readonly IHealthMonitoringService _healthMonitoring;
    private readonly IAlertService _alertService;
    private readonly IOptions<ServiceMonitorOptions> _options;
    private readonly ILogger<MonitorServicesUseCase> _logger;
    private readonly IServiceHealthStateRepository _stateRepository;

    public MonitorServicesUseCase(
        IHealthMonitoringService healthMonitoring,
        IAlertService alertService,
        IOptions<ServiceMonitorOptions> options,
        ILogger<MonitorServicesUseCase> logger,
        IServiceHealthStateRepository stateRepository)
    {
        _healthMonitoring = healthMonitoring;
        _alertService = alertService;
        _options = options;
        _logger = logger;
        _stateRepository = stateRepository;
    }

#pragma warning disable MA0038 // False positive with primary constructors
    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
#pragma warning restore MA0038
    {
        var urls = _options.Value.Urls.Select(u => new Uri(u));
        var resultsResult = await _healthMonitoring.MonitorServicesAsync(urls, cancellationToken).ConfigureAwait(false);

        if (resultsResult.IsFailure)
        {
            _logger.LogError("Health monitoring failed: {Error}", resultsResult.Error);
            return Result.Failure(resultsResult.Error);
        }

        foreach (var result in resultsResult.Value)
        {
            var url = result.ServiceUrl;
            var isHealthy = result.IsHealthy;

            var stateResult = await _stateRepository.GetAsync(url, cancellationToken).ConfigureAwait(false);
            var state = stateResult.HasValue
                ? stateResult.Value
                : new ServiceHealthState(url, isHealthy);

            state.Update(isHealthy);

            if (!isHealthy)
            {
                if (!state.AlertSent)
                {
                    var alertResult = await _alertService.SendAlertAsync(
                        result.ServiceUrl,
                        result.ErrorMessage.GetValueOrDefault("Unknown error"),
                        _options.Value.EmailServer.To,
                        cancellationToken).ConfigureAwait(false);

                    if (alertResult.IsFailure)
                    {
                        _logger.LogError("Failed to send alert for {Url}: {Error}", result.ServiceUrl, alertResult.Error);
                        continue;
                    }

                    state.MarkAlertSent();
                    _logger.LogError("Down-Mail sent for {Url} with status: {Status}", result.ServiceUrl, result.StatusCode);
                }

                _logger.LogInformation(
                    "Service {Url} returned status {Status}. Down-Mail already sent.",
                    result.ServiceUrl,
                    result.StatusCode);
            }

            var saveResult = await _stateRepository.SaveAsync(state, cancellationToken).ConfigureAwait(false);
            if (saveResult.IsFailure)
            {
                _logger.LogError("Failed to save state for {Url}: {Error}", url, saveResult.Error);
            }
        }

        return Result.Success();
    }
}
