using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLog.Extensions.Logging;
using ServiceMonitor.Infrastructure.Configuration;
using ServiceMonitor.Presentation.DependencyInjection;
using ServiceMonitor.Presentation.Hosting;

namespace ServiceMonitor;

internal static class Program
{
    static async Task Main(string[] args)
    {
        await Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseKestrel();
                webBuilder.ConfigureAppConfiguration((hostingContext, config) =>
                {
                    // nichts nötig – nur damit wir hostingContext haben
                });

                webBuilder.Configure((context, app) =>
                {
                    var port = context.Configuration["System:WebUiPort"] ?? "8080";

                    // Korrekt: Adressen über IServerAddressesFeature setzen
                    var addressesFeature = app.ServerFeatures.Get<IServerAddressesFeature>();
                    if (addressesFeature != null)
                    {
                        // optional vorhandene Einträge entfernen
                        addressesFeature.Addresses.Clear();
                        addressesFeature.Addresses.Add($"http://*:{port}");
                    }

                    app.UseStaticFiles();
                    app.UseRouting();

                    app.UseEndpoints(endpoints =>
                    {
                        var configDir = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Saigkill", "ServiceMonitor");
                        Directory.CreateDirectory(configDir);
                        var userConfigPath = Path.Join(configDir, "appsettings.user.json");

                        endpoints.MapGet("/api/config",
                            (IOptions<ServiceMonitorOptions> options) =>
                                Results.Json(options.Value, new JsonSerializerOptions
                                {
                                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                                }));

                        endpoints.MapPost("/api/config",
                            async (HttpContext context) =>
                            {
                                var jsonOptions = new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true,
                                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
                                };

                                var newConfig = await context.Request.ReadFromJsonAsync<ServiceMonitorOptions>(jsonOptions);

                                if (newConfig == null)
                                {
                                    return Results.BadRequest("Invalid configuration data");
                                }

                                // Serialize with PascalCase for C# compatibility
                                var json = JsonSerializer.Serialize(newConfig, new JsonSerializerOptions
                                {
                                    WriteIndented = true,
                                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                                });
                                await File.WriteAllTextAsync(userConfigPath, json);

                                return Results.Ok();
                            });
                    });
                });
            })
            .ConfigureAppConfiguration(ConfigureApp)
            .ConfigureServices((context, services) =>
            {
                services.AddHostedService<ConsoleHostedService>();
                services.AddServiceMonitoring(context.Configuration);

                // Configure JSON options for API endpoints
                services.Configure<JsonOptions>(options =>
                {
                    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.SerializerOptions.PropertyNameCaseInsensitive = true;
                });
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
