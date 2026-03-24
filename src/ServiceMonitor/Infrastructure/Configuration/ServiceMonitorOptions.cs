using System.ComponentModel.DataAnnotations;

namespace ServiceMonitor.Infrastructure.Configuration
{
    public sealed class ServiceMonitorOptions
    {
        [Required] public IList<string> Urls { get; set; } = [];

        public EmailOptions EmailServer { get; set; } = new();

        public System System { get; set; } = new();
    }

    public sealed class EmailOptions
    {
        [Required] public string Host { get; set; } = string.Empty;

        public int Port { get; set; }

        [Required] public string DefaultEmailSenderAddress { get; set; } = string.Empty;
        [Required] public string DefaultSenderName { get; set; } = string.Empty;

        [Required] public IList<string> To { get; set; } = [];

        [Required] public string User { get; set; } = string.Empty;

        [Required] public string Password { get; set; } = string.Empty;
    }

    public sealed class System
    {
        [Required] public int TimeoutSeconds { get; set; }
        [Required] public RunMode RunMode { get; set; } = RunMode.Daemon;
        [Required] public int DaemonIntervalMinutes { get; set; } = 5;
        [Required] public int WebUiPort { get; set; } = 8080;
    }

    public enum RunMode
    {
        Once,
        Daemon
    }
}
