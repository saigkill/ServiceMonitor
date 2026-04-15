using CSharpFunctionalExtensions;

namespace ServiceMonitor.Application.DTOs;

public sealed record HealthCheckResult(
    Uri ServiceUrl,
    bool IsHealthy,
    System.Net.HttpStatusCode StatusCode,
    Maybe<string> ErrorMessage);
