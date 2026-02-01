using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ServiceMonitor.Hosting
{
	internal sealed class ConsoleHostedService(
	ILogger<ConsoleHostedService> logger,
	IHostApplicationLifetime appLifetime,
	ServiceMonitor serviceMonitorService)
	: IHostedService
	{
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			try
			{
				logger.LogInformation("Running the App.");
				await serviceMonitorService.StartAsync(cancellationToken);
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
