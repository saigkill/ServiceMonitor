using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceMonitor.AppConfig;

namespace ServiceMonitor.Hosting
{
	internal class ServiceMonitor
	{
		internal IOptions<ServiceMonitorOptions> _options;
		internal ILogger<ServiceMonitor> _logger;

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

			async Task SendAlert(string url, string message)
			{
				var mail = new MailMessage(
					smtpConfig.From,
					smtpConfig.To,
					$"Dienst nicht erreichbar: {url}",
					$"Der Dienst {url} ist nicht erreichbar.\n\nFehler: {message}"
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
				}
				catch
				{
					_logger.LogError("Failed to send alert email for service {Url}", url);
				}
			}
		}
	}
}
