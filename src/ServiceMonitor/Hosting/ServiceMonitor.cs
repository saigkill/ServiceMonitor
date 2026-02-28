using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceMonitor.AppConfig;

namespace ServiceMonitor.Hosting
{
	internal class ServiceMonitor
	{
		readonly internal IOptions<ServiceMonitorOptions> _options;
		readonly internal ILogger<ServiceMonitor> _logger;

		public ServiceMonitor(IOptions<ServiceMonitorOptions> options, ILogger<ServiceMonitor> logger)
		{
			_options = options;
			_logger = logger;
		}

		internal async Task StartAsync(CancellationToken cancellationToken)
		{
			var urls = _options.Value.Urls;
			var smtpConfig = _options.Value.Smtp;
			var timeout = _options.Value.TimeoutSeconds;

			var http = new HttpClient
			{
				Timeout = TimeSpan.FromSeconds(timeout)
			};

			foreach (var url in urls)
			{
				try
				{
					var response = await http.GetAsync(url);

					if (!response.IsSuccessStatusCode)
					{
						await SendAlert(url, $"Status: {response.StatusCode}");
						_logger.LogWarning("Service {Url} returned status code {StatusCode}", url, response.StatusCode);
					}
					_logger.LogInformation("Service {Url} is reachable", url);
				}
				catch (Exception ex)
				{
					await SendAlert(url, ex.Message);
					_logger.LogError(ex, "Service {Url} is not reachable", url);
				}
			}

			http.Dispose();

			async Task SendAlert(string url, string message)
			{
				var mail = new MailMessage(
					smtpConfig.From,
					smtpConfig.To,
					$"Service not reachable: {url}",
					$"The service {url} is not reachable.\n\nError: {message}"
				);

				using var smtp = new SmtpClient(smtpConfig.Server, smtpConfig.Port)
				{
					EnableSsl = smtpConfig.UseSsl,
					Credentials = new NetworkCredential(
						smtpConfig.Username,
						smtpConfig.Password
					)
				};

				try
				{
					await smtp.SendMailAsync(mail);
					mail.Dispose();
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Failed to send alert email for service {Url}", url);
					mail.Dispose();
				}
			}
		}
	}
}
