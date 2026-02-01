using System.Net;
using System.Net.Mail;
using System.Net.Http.Json;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLog.Extensions.Logging;
using ServiceMonitor.Hosting;
using ServiceMonitor.AppConfig;

namespace MagazineFetcher
{
	internal class Program
	{
		static async Task Main(string[] args)
		{
			await Host.CreateDefaultBuilder(args)				
				.ConfigureAppConfiguration((context, services) =>
				{
					services.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
					services.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

					if (context.HostingEnvironment.IsDevelopment())
					{
						services.AddUserSecrets<Program>();
					}
					services.AddCommandLine(args);
				})
				.ConfigureServices((hostContext, services) =>
				{
					var logDir = hostContext.Configuration["Configuration:Logging:LogDirectory"];
					NLog.LogManager.Configuration.Variables["logDir"] = logDir;

					services.AddHostedService<ConsoleHostedService>();
					services.AddOptions<ServiceMonitorOptions>()
						.Bind(hostContext.Configuration)
						.ValidateDataAnnotations()
						.ValidateOnStart();
					services.AddSingleton<ServiceMonitor.Hosting.ServiceMonitor>();					
				})
				.ConfigureLogging(logging =>
				{
					logging.ClearProviders();
					logging.AddNLog();
				})
				.RunConsoleAsync();
		}
	}
}
