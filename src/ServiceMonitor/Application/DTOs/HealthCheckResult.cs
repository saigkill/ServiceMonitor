using System.Net;
using Ardalis.GuardClauses;

namespace ServiceMonitor.Application.DTOs;

/// <summary>
/// Represents the result of a health check operation for a service.
/// </summary>
public sealed class HealthCheckResult
{
    /// <summary>
    /// Gets the URL of the checked service.
    /// </summary>
    public required Uri ServiceUrl { get; init; }

    /// <summary>
    /// Gets a value indicating whether the service is healthy.
    /// </summary>
    public bool IsHealthy { get; init; }

    /// <summary>
    /// Gets the HTTP status code returned by the service, if applicable.
    /// </summary>
    public HttpStatusCode? StatusCode { get; init; }

    /// <summary>
    /// Gets the error message if the service is unhealthy.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the timestamp when the health check was performed.
    /// </summary>
    public DateTime CheckedAt { get; init; }

    /// <summary>
    /// Creates a healthy result.
    /// </summary>
    /// <param name="serviceUrl">The service URL that was checked.</param>
    /// <param name="statusCode">The successful HTTP status code.</param>
    /// <returns>A healthy health check result.</returns>
    public static HealthCheckResult Healthy(Uri serviceUrl, HttpStatusCode statusCode)
    {
        Guard.Against.Null(serviceUrl);
        Guard.Against.NegativeOrZero((int)statusCode, "Status code must be a valid HTTP success code.");
        return new HealthCheckResult
        {
            ServiceUrl = serviceUrl,
            IsHealthy = true,
            StatusCode = statusCode,
            ErrorMessage = null,
            CheckedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates an unhealthy result with an HTTP status code.
    /// </summary>
    /// <param name="serviceUrl">The service URL that was checked.</param>
    /// <param name="errorMessage">The error message describing the issue.</param>
    /// <param name="statusCode">The unsuccessful HTTP status code, if available.</param>
    /// <returns>An unhealthy health check result.</returns>
    public static HealthCheckResult Unhealthy(Uri serviceUrl, string errorMessage, HttpStatusCode? statusCode = null)
    {
        Guard.Against.Null(serviceUrl);
        Guard.Against.NullOrEmpty(errorMessage);

        return new HealthCheckResult
        {
            ServiceUrl = serviceUrl,
            IsHealthy = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage,
            CheckedAt = DateTime.UtcNow
        };
    }
}
