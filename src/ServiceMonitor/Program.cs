using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using ServiceMonitor.Presentation.DependencyInjection;
using ServiceMonitor.Presentation.Hosting;

namespace ServiceMonitor;

internal static class Program
{
    static async Task Main(string[] args)
    {
        await Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(ConfigureApp)
            .ConfigureServices((context, services) =>
            {
                //ConfigureLogging(services);
                services.AddHostedService<ConsoleHostedService>();
                services.AddServiceMonitoring(context.Configuration);
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddNLog();
            })
            .RunConsoleAsync();
    }

    private static void ConfigureApp(HostBuilderContext context, IConfigurationBuilder builder)
    {
        var configDir = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Saigkill", "ServiceMonitor");
        Directory.CreateDirectory(configDir);
        var userConfigPath = Path.Join(configDir, "appsettings.user.json");
        var defaultConfigPath = Path.Join(AppContext.BaseDirectory, "appsettings.json");

        if (!File.Exists(userConfigPath))
        {
            if (File.Exists(defaultConfigPath))
            {
                var defaultJson = File.ReadAllText(defaultConfigPath);
                File.WriteAllText(userConfigPath, defaultJson);
            }
            else
            {
                File.WriteAllText(userConfigPath, "{}");
            }
        }

        builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        builder.AddJsonFile(userConfigPath, optional: true, reloadOnChange: true);
    }
}
