using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceMonitor.Application.Interfaces;
using ServiceMonitor.Application.UseCases;
using ServiceMonitor.Domain.Interfaces;
using ServiceMonitor.Infrastructure.Configuration;
using ServiceMonitor.Infrastructure.Email;
using ServiceMonitor.Infrastructure.Http;
using ServiceMonitor.Infrastructure.Repositories;

namespace ServiceMonitor;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServiceMonitoring(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Domain Layer (no Dependencies)

        // Application Layer
        services.AddScoped<MonitorServicesUseCase>();


        // Infrastructure Layer
        services.AddHttpClient();
        services.AddSingleton<IHealthMonitoringService, HttpHealthMonitoringService>();
        services.AddFluentEmail(configuration["EmailServer:DefaultEmailSenderAddress"], configuration["EmailServer:DefaultSenderName"])
            .AddSmtpSender(configuration["EmailServer:Host"],
                Convert.ToInt32(configuration["EmailServer:Port"], CultureInfo.InvariantCulture),
                configuration["EmailServer:User"],
                configuration["EmailServer:Password"]);
        services.AddScoped<IAlertService, EmailAlertService>();
        services.AddScoped<IServiceHealthStateRepository, InMemoryServiceHealthStateRepository>();

        // Configuration
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var logDir = Path.Join(baseDir, "Saigkill", "ServiceMonitor", "logs");
        Directory.CreateDirectory(logDir);
        NLog.LogManager.Configuration!.Variables["logDir"] = logDir;

        services.AddOptions<ServiceMonitorOptions>()
            .Bind(configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
