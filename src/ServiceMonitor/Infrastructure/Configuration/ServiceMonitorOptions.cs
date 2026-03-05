using System.ComponentModel.DataAnnotations;

namespace ServiceMonitor.Infrastructure.Configuration
{
    public sealed class ServiceMonitorOptions
    {
        [Required] public List<string> Urls { get; set; } = new();

        public EmailOptions EmailServer { get; set; } = new();

        [Required] public int TimeoutSeconds { get; set; }
    }

    public sealed class EmailOptions
    {
        [Required] public string Host { get; set; } = string.Empty;

        public int Port { get; set; }

        [Required] public string DefaultEmailAddress { get; set; } = string.Empty;
        [Required] public string DefaultSenderName { get; set; } = string.Empty;

        [Required] public List<string> To { get; set; } = new();

        [Required] public string User { get; set; } = string.Empty;

        [Required] public string Password { get; set; } = string.Empty;
    }
}
