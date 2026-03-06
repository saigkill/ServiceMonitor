using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceMonitor.Application.UseCases;
using ServiceMonitor.Infrastructure.Configuration;

namespace ServiceMonitor.Presentation.Hosting
{
    internal sealed class ConsoleHostedService(
    ILogger<ConsoleHostedService> logger,
    IHostApplicationLifetime appLifetime,
    MonitorServicesUseCase monitorServiceUseCase,
    IOptions<ServiceMonitorOptions> options)
    : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

                if (options.Value.System.RunMode == RunMode.Once)
                {
                    await monitorServiceUseCase.ExecuteAsync(cancellationToken);
                    return;
                }

                logger.LogInformation("Starting in Daemon-Modus. Interval: {DaemonIntervalMinutes} Minutes. Profile '{env}'", options.Value.System.DaemonIntervalMinutes, environment);
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await monitorServiceUseCase.ExecuteAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error in Daemon run.");
                    }

                    await Task.Delay(TimeSpan.FromMinutes(options.Value.System.DaemonIntervalMinutes), cancellationToken);
                }

                logger.LogInformation("Daemon wurde beendet.");

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception!");
            }
            finally
            {
                appLifetime.StopApplication();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

}
