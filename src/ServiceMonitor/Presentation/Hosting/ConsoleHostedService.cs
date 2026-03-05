using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServiceMonitor.Application.UseCases;

namespace ServiceMonitor.Presentation.Hosting
{
    internal sealed class ConsoleHostedService(
    ILogger<ConsoleHostedService> logger,
    IHostApplicationLifetime appLifetime,
    MonitorServicesUseCase monitorServiceUseCase)
    : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                logger.LogInformation("Running the App.");
                await monitorServiceUseCase.ExecuteAsync(cancellationToken);
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
