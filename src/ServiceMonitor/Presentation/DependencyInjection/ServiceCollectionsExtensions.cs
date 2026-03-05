using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Saigkill.Toolbox.Services;
using ServiceMonitor.Application.Interfaces;
using ServiceMonitor.Application.UseCases;
using ServiceMonitor.Infrastructure.Configuration;
using ServiceMonitor.Infrastructure.Email;
using ServiceMonitor.Infrastructure.Http;

namespace ServiceMonitor.Presentation.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServiceMonitoring(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Domain Layer (keine Dependencies)

        // Application Layer
        services.AddScoped<MonitorServicesUseCase>();


        // Infrastructure Layer
        services.AddHttpClient();
        services.AddSingleton<IHealthMonitoringService, HttpHealthMonitoringService>();
        services.AddSingleton<IAlertService, EmailAlertService>();
        services.AddSingleton<IEmailService, EmailServiceWithAuth>();


        // Configuration
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var logDir = Path.Combine(baseDir, "Saigkill", "ServiceMonitor", "logs");
        Directory.CreateDirectory(logDir);
        NLog.LogManager.Configuration!.Variables["logDir"] = logDir;

        services.AddOptions<ServiceMonitorOptions>()
            .Bind(configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
