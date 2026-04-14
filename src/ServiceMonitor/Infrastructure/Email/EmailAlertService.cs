using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using FluentEmail.Core;
using FluentEmail.Core.Models;
using Microsoft.Extensions.Logging;
using ServiceMonitor.Application.Interfaces;

namespace ServiceMonitor.Infrastructure.Email;

public sealed class EmailAlertService(
    ILogger<EmailAlertService> logger,
    IFluentEmail fluentEmail) : IAlertService
{
    public async Task<Result> SendAlertAsync(
        Uri serviceUrl,
        string errorMessage,
        IEnumerable<string> recipients,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(serviceUrl);
        Guard.Against.NullOrWhiteSpace(errorMessage);
        Guard.Against.Null(recipients);

        try
        {
            var recipientsList = recipients.Select(url => new Address(url)).ToList();

            var email = await fluentEmail.To(recipientsList)
                .Subject($"Service not reachable: {serviceUrl}")
                .Body($"The service {serviceUrl} is not reachable.\n\nError: {errorMessage}")
                .SendAsync(cancellationToken).ConfigureAwait(false);

            if (!email.Successful)
            {
                var errors = string.Join(", ", email.ErrorMessages);
                logger.LogError("Failed to send email for {Url}: {Errors}", serviceUrl, errors);
                return Result.Failure($"Email sending failed: {errors}");
            }

            logger.LogInformation("Down-Email for {Url} sent successfully", serviceUrl);
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception while sending alert for {Url}", serviceUrl);
            return Result.Failure($"Exception: {ex.Message}");
        }
    }
}
