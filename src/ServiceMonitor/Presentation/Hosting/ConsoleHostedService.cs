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
                logger.LogInformation("Running the App.");
                var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
                logger.LogInformation($"Starting Pipeline in Profile '{environment}'");

                if (options.Value.System.RunMode == RunMode.Once)
                {
                    await monitorServiceUseCase.ExecuteAsync(cancellationToken);
                    Console.WriteLine("The app stops directly after execution. This means, that you maybe have enough time to change the config via Web UI. So better use Daemon Mode for changes.");
                    return;
                }
                logger.LogInformation("Starting in Daemon-Modus. Interval: " + $"{options.Value.System.DaemonIntervalMinutes} Minutes");
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
