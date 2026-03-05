namespace ServiceMonitor.Application.Interfaces;

public interface IAlertService
{
    Task SendAlertAsync(
        Uri serviceUrl,
        string errorMessage,
        IEnumerable<string> recipients,
        CancellationToken cancellationToken);
}
