using System.Diagnostics;
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
using ServiceMonitor.Infrastructure.Hosting;

namespace ServiceMonitor;

internal static class Program
{
    private static readonly string ConfigDir = Path.Join(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Saigkill",
        "ServiceMonitor",
        "config");

    private static readonly string UserConfigPath = Path.Join(ConfigDir, "appsettings.user.json");

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    static async Task Main(string[] args)
    {
        Console.WriteLine("UserConfigPath is {0}", UserConfigPath);
        var isFirstRun = !File.Exists(UserConfigPath);

        if (isFirstRun)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                   FIRST RUN DETECTED                       ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("ServiceMonitor needs to be configured before first use.");
            Console.WriteLine("Opening configuration UI in your browser...");
            Console.WriteLine();
        }

        await Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseKestrel();
                webBuilder.ConfigureAppConfiguration((hostingContext, config) =>
                {
                    // Nothing needed - just to have hostingContext
                });

                webBuilder.Configure((context, app) =>
                {
                    var port = context.Configuration["System:WebUiPort"] ?? "8080";

                    // Set addresses via IServerAddressesFeature
                    var addressesFeature = app.ServerFeatures.Get<IServerAddressesFeature>();
                    if (addressesFeature != null)
                    {
                        addressesFeature.Addresses.Clear();
                        addressesFeature.Addresses.Add($"http://*:{port}");
                    }

                    app.UseStaticFiles();
                    app.UseRouting();

                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/api/config",
                            (IOptions<ServiceMonitorOptions> options) =>
                                Results.Json(options.Value, JsonOptions));

                        endpoints.MapPost("/api/config",
                            async (HttpContext ctx) =>
                            {
                                var newConfig = await ctx.Request.ReadFromJsonAsync<ServiceMonitorOptions>(JsonOptions).ConfigureAwait(false);

                                if (newConfig == null)
                                {
                                    return Results.BadRequest("Invalid configuration data");
                                }

                                // Validate configuration
                                if (!ValidateConfiguration(newConfig, out var validationError))
                                {
                                    return Results.BadRequest(validationError);
                                }

                                // Serialize with PascalCase for C# compatibility
                                var json = JsonSerializer.Serialize(newConfig, new JsonSerializerOptions
                                {
                                    WriteIndented = true,
                                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                                });
                                await File.WriteAllTextAsync(UserConfigPath, json).ConfigureAwait(false);

                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("✓ Configuration saved successfully!");
                                Console.ResetColor();
                                Console.WriteLine("Please restart ServiceMonitor for changes to take effect.");

                                return Results.Ok(new { message = "Configuration saved. Please restart the application." });
                            });

                        endpoints.MapGet("/api/status",
                            () => Results.Ok(new
                            {
                                configured = File.Exists(UserConfigPath),
                                version = "1.1.1"
                            }));
                    });

                    // Open browser on first run after app has started
                    if (isFirstRun)
                    {
                        var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
                        lifetime.ApplicationStarted.Register(() =>
                        {
                            var url = $"http://localhost:{port}/index.html";
                            Console.WriteLine($"Configuration UI available at: {url}");
                            Console.WriteLine();
                            OpenBrowser(url);
                        });
                    }
                });
            })
            .ConfigureAppConfiguration(ConfigureApp)
            .ConfigureServices((context, services) =>
            {
                // Only add monitoring service if configuration exists and is valid
                if (!isFirstRun)
                {
                    services.AddHostedService<ConsoleHostedService>();
                    services.AddServiceMonitoring(context.Configuration);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("Running in SETUP MODE - monitoring disabled until configured.");
                    Console.ResetColor();
                    Console.WriteLine("Press Ctrl+C to exit after configuration is complete.");
                    Console.WriteLine();
                }

                // Configure JSON options for API endpoints
                services.Configure<JsonOptions>(options =>
                {
                    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.SerializerOptions.PropertyNameCaseInsensitive = true;
                    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                });
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddNLog();
            })
            .RunConsoleAsync().ConfigureAwait(false);
    }

    private static void ConfigureApp(HostBuilderContext context, IConfigurationBuilder builder)
    {
        Directory.CreateDirectory(ConfigDir);
        var defaultConfigPath = Path.Join(AppContext.BaseDirectory, "appsettings.json");

        if (!File.Exists(UserConfigPath))
        {
            if (File.Exists(defaultConfigPath))
            {
                var defaultJson = File.ReadAllText(defaultConfigPath);
                File.WriteAllText(UserConfigPath, defaultJson);
            }
            else
            {
                File.WriteAllText(UserConfigPath, "{}");
            }
        }

        builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        builder.AddJsonFile(UserConfigPath, optional: true, reloadOnChange: true);
    }

    private static bool ValidateConfiguration(ServiceMonitorOptions config, out string error)
    {
        error = string.Empty;

        if (config.Urls == null || config.Urls.Count < 1 || config.Urls.All(string.IsNullOrWhiteSpace))
        {
            error = "At least one URL must be configured.";
            return false;
        }

        if (config.EmailServer == null)
        {
            error = "Email server configuration is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(config.EmailServer.Host))
        {
            error = "Email server host is required.";
            return false;
        }

        if (config.EmailServer.To == null || config.EmailServer.To.Count < 1 || config.EmailServer.To.All(string.IsNullOrWhiteSpace))
        {
            error = "At least one recipient email address is required.";
            return false;
        }

        return true;
    }

    private static void OpenBrowser(string url)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (OperatingSystem.IsLinux())
            {
                Process.Start("xdg-open", url);
            }
            else if (OperatingSystem.IsIOS())
            {
                Process.Start("open", url);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not open browser automatically: {ex.Message}");
            Console.WriteLine($"Please open {url} manually in your browser.");
        }
    }
}
