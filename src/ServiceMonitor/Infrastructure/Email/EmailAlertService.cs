using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;
using MimeKit;
using Saigkill.Toolbox.Services;
using ServiceMonitor.Application.Interfaces;

namespace ServiceMonitor.Infrastructure.Email;

public sealed class EmailAlertService(
    IEmailService emailService,
    ILogger<EmailAlertService> logger) : IAlertService
{
    public async Task SendAlertAsync(
        Uri serviceUrl,
        string errorMessage,
        IEnumerable<string> recipients,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(serviceUrl);
        Guard.Against.NullOrWhiteSpace(errorMessage);
        Guard.Against.Null(recipients);

        var email = new MimeMessage
        {
            Subject = $"Service not reachable: {serviceUrl}",
            Body = new TextPart("plain")
            {
                Text = $"The service {serviceUrl} is not reachable.\n\nError: {errorMessage}"
            }
        };

        foreach (var recipient in recipients)
        {
            email.To.Add(MailboxAddress.Parse(recipient));
        }

        try
        {
            await emailService.SendMessageAsync(email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send alert email for service {Url}", serviceUrl);
        }
    }
}
