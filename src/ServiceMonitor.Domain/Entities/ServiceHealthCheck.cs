namespace ServiceMonitor.Domain.Entities;

public sealed class ServiceHealthCheck
{
    public required Uri ServiceUrl { get; init; }
    public HealthStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }
    public HttpStatusCode? StatusCode { get; private set; }
    public DateTime CheckedAt { get; private set; }

    public void MarkAsHealthy(HttpStatusCode statusCode)
    {
        Status = HealthStatus.Healthy;
        StatusCode = statusCode;
        ErrorMessage = null;
        CheckedAt = DateTime.UtcNow;
    }

    public void MarkAsUnhealthy(string errorMessage, HttpStatusCode? statusCode = null)
    {
        Status = HealthStatus.Unhealthy;
        ErrorMessage = errorMessage;
        StatusCode = statusCode;
        CheckedAt = DateTime.UtcNow;
    }
}
