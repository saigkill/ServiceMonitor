using Ardalis.GuardClauses;
using FluentEmail.Core;
using FluentEmail.Core.Models;
using Microsoft.Extensions.Logging;
using ServiceMonitor.Application.Interfaces;

namespace ServiceMonitor.Infrastructure.Email;

public sealed class EmailAlertService(
    ILogger<EmailAlertService> logger,
    IFluentEmail fluentEmail) : IAlertService
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

        var recipientsList = recipients.Select(url => new Address(url)).ToList();

        try
        {
            await fluentEmail.To(recipientsList)
                .To(recipientsList)
                .Subject($"Service not reachable: {serviceUrl}")
                .Body($"The service {serviceUrl} is not reachable.\n\nError: {errorMessage}")
                .SendAsync(cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Down-Email for {0} sent. Successful", serviceUrl);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while sending downmail for {0}. Error: {1}", serviceUrl, ex.Message);
            throw;
        }
    }
}
