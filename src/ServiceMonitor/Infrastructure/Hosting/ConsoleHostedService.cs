using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceMonitor.Application.UseCases;
using ServiceMonitor.Infrastructure.Configuration;

namespace ServiceMonitor.Infrastructure.Hosting
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
                    await monitorServiceUseCase.ExecuteAsync(cancellationToken).ConfigureAwait(false);
                    return;
                }

                logger.LogInformation("Starting in Daemon-Modus. Interval: {0} Minutes. Profile '{1}'", options.Value.System.DaemonIntervalMinutes, environment);
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await monitorServiceUseCase.ExecuteAsync(cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error in Daemon run.");
                    }

                    await Task.Delay(TimeSpan.FromMinutes(options.Value.System.DaemonIntervalMinutes), cancellationToken).ConfigureAwait(false);
                }

                logger.LogInformation("Daemon has been stopped.");

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
