using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceMonitor.AppConfig
{
	public sealed class ServiceMonitorOptions
	{
		[Required] public List<string> Urls { get; set; } = new();

		public SmtpOptions Smtp { get; set; } = new();

		[Required] public int TimeoutSeconds { get; set; }

		[Required] public LoggingOptions Logging { get; set; } = new();
	}

	public sealed class SmtpOptions
	{
		[Required] public string Server { get; set; } = string.Empty;

		public int Port { get; set; }

		public bool UseSsl { get; set; }

		[Required] public string From { get; set; } = string.Empty;

		[Required] public string To { get; set; } = string.Empty;

		[Required] public string Username { get; set; } = string.Empty;

		[Required] public string Password { get; set; } = string.Empty;
	}

	public sealed class LoggingOptions
	{
		[Required] public string LogDirectory { get; set; } = string.Empty;
	}
}
