using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using ServiceMonitor.AppConfig;
using ServiceMonitor.Hosting;

namespace ServiceMonitor
{
	internal class Program
	{
		static async Task Main(string[] args)
		{
			await Host.CreateDefaultBuilder(args)
				.ConfigureAppConfiguration((context, services) =>
				{
					var configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ServiceMonitor");
					Directory.CreateDirectory(configDir);
					var userConfigPath = Path.Combine(configDir, "appsettings.user.json");
					var defaultConfigPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

					if (!File.Exists(userConfigPath))
					{
						if (!File.Exists(defaultConfigPath))
						{
							var defaultJson = File.ReadAllText(defaultConfigPath);
							File.WriteAllText(userConfigPath, defaultJson);
						}
						else
						{
							File.WriteAllText(userConfigPath, "{}");
						}
					}

					services.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
					services.AddJsonFile(userConfigPath, optional: true, reloadOnChange: true);

					if (context.HostingEnvironment.IsDevelopment())
					{
						services.AddUserSecrets<Program>();
					}
					services.AddCommandLine(args);
				})
				.ConfigureServices((hostContext, services) =>
				{
					var logDir = hostContext.Configuration["Configuration:Logging:LogDirectory"];
					NLog.LogManager.Configuration!.Variables["logDir"] = logDir;

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
