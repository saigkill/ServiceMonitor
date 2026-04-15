using CSharpFunctionalExtensions;

namespace ServiceMonitor.Application.Interfaces;

public interface IAlertService
{
    Task<Result> SendAlertAsync(
        Uri serviceUrl,
        string errorMessage,
        IEnumerable<string> recipients,
        CancellationToken cancellationToken);
}
