using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ServiceMonitor.Application.DTOs;
using ServiceMonitor.Application.Interfaces;
using ServiceMonitor.Infrastructure.Configuration;

namespace ServiceMonitor.Application.UseCases.UnitTests;

/// <summary>
/// Tests for the MonitorServicesUseCase class.
/// </summary>
[TestClass]
public sealed class MonitorServicesUseCaseTests
{
    /// <summary>
    /// Tests that ExecuteAsync completes successfully when there are no URLs to monitor.
    /// No monitoring or alerts should occur.
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_EmptyUrlList_CompletesWithoutMonitoringOrAlerts()
    {
        // Arrange
        var healthMonitoringMock = new Mock<IHealthMonitoringService>();
        var alertServiceMock = new Mock<IAlertService>();
        var loggerMock = new Mock<ILogger<MonitorServicesUseCase>>();
        var options = CreateOptions(new List<string>(), new List<string> { "test@example.com" });

        var useCase = new MonitorServicesUseCase(
            healthMonitoringMock.Object,
            alertServiceMock.Object,
            options,
            loggerMock.Object);

        // Act
        await useCase.ExecuteAsync(CancellationToken.None);

        // Assert
        healthMonitoringMock.Verify(
            x => x.MonitorServicesAsync(
                It.Is<IEnumerable<Uri>>(urls => !urls.Any()),
                It.IsAny<CancellationToken>()),
            Times.Once);
        alertServiceMock.Verify(
            x => x.SendAlertAsync(
                It.IsAny<Uri>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Tests that ExecuteAsync does not send alerts when all services are healthy.
    /// Monitoring should occur but no alerts or warnings should be generated.
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_AllServicesHealthy_NoAlertsOrWarnings()
    {
        // Arrange
        var healthMonitoringMock = new Mock<IHealthMonitoringService>();
        var alertServiceMock = new Mock<IAlertService>();
        var loggerMock = new Mock<ILogger<MonitorServicesUseCase>>();
        var urls = new List<string> { "https://example1.com", "https://example2.com" };
        var options = CreateOptions(urls, new List<string> { "admin@example.com" });

        var healthyResults = new List<HealthCheckResult>
        {
            HealthCheckResult.Healthy(new Uri("https://example1.com"), HttpStatusCode.OK),
            HealthCheckResult.Healthy(new Uri("https://example2.com"), HttpStatusCode.OK)
        };

        healthMonitoringMock
            .Setup(x => x.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthyResults);

        var useCase = new MonitorServicesUseCase(
            healthMonitoringMock.Object,
            alertServiceMock.Object,
            options,
            loggerMock.Object);

        // Act
        await useCase.ExecuteAsync(CancellationToken.None);

        // Assert
        alertServiceMock.Verify(
            x => x.SendAlertAsync(
                It.IsAny<Uri>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    /// <summary>
    /// Tests that ExecuteAsync sends an alert and logs a warning when a single service is unhealthy.
    /// The alert should use the error message from the result and the configured recipients.
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_SingleUnhealthyService_SendsAlertAndLogsWarning()
    {
        // Arrange
        var healthMonitoringMock = new Mock<IHealthMonitoringService>();
        var alertServiceMock = new Mock<IAlertService>();
        var loggerMock = new Mock<ILogger<MonitorServicesUseCase>>();
        var urls = new List<string> { "https://example.com" };
        var recipients = new List<string> { "admin@example.com" };
        var options = CreateOptions(urls, recipients);

        var serviceUrl = new Uri("https://example.com");
        var errorMessage = "Service unavailable";
        var unhealthyResult = HealthCheckResult.Unhealthy(
            serviceUrl,
            errorMessage,
            HttpStatusCode.ServiceUnavailable);

        healthMonitoringMock
            .Setup(x => x.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<HealthCheckResult> { unhealthyResult });

        var useCase = new MonitorServicesUseCase(
            healthMonitoringMock.Object,
            alertServiceMock.Object,
            options,
            loggerMock.Object);

        // Act
        await useCase.ExecuteAsync(CancellationToken.None);

        // Assert
        alertServiceMock.Verify(
            x => x.SendAlertAsync(
                serviceUrl,
                errorMessage,
                recipients,
                It.IsAny<CancellationToken>()),
            Times.Once);
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that ExecuteAsync uses "Unknown error" as the error message when the result has a null ErrorMessage.
    /// This ensures a meaningful message is always sent in alerts.
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_UnhealthyServiceWithNullErrorMessage_UsesUnknownError()
    {
        // Arrange
        var healthMonitoringMock = new Mock<IHealthMonitoringService>();
        var alertServiceMock = new Mock<IAlertService>();
        var loggerMock = new Mock<ILogger<MonitorServicesUseCase>>();
        var urls = new List<string> { "https://example.com" };
        var recipients = new List<string> { "admin@example.com" };
        var options = CreateOptions(urls, recipients);

        var serviceUrl = new Uri("https://example.com");
        var unhealthyResult = new HealthCheckResult
        {
            ServiceUrl = serviceUrl,
            IsHealthy = false,
            StatusCode = HttpStatusCode.InternalServerError,
            ErrorMessage = null,
            CheckedAt = DateTime.UtcNow
        };

        healthMonitoringMock
            .Setup(x => x.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<HealthCheckResult> { unhealthyResult });

        var useCase = new MonitorServicesUseCase(
            healthMonitoringMock.Object,
            alertServiceMock.Object,
            options,
            loggerMock.Object);

        // Act
        await useCase.ExecuteAsync(CancellationToken.None);

        // Assert
        alertServiceMock.Verify(
            x => x.SendAlertAsync(
                serviceUrl,
                "Unknown error",
                recipients,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that ExecuteAsync sends multiple alerts when multiple services are unhealthy.
    /// Each unhealthy service should trigger its own alert and warning log.
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_MultipleUnhealthyServices_SendsMultipleAlerts()
    {
        // Arrange
        var healthMonitoringMock = new Mock<IHealthMonitoringService>();
        var alertServiceMock = new Mock<IAlertService>();
        var loggerMock = new Mock<ILogger<MonitorServicesUseCase>>();
        var urls = new List<string> { "https://example1.com", "https://example2.com", "https://example3.com" };
        var recipients = new List<string> { "admin@example.com" };
        var options = CreateOptions(urls, recipients);

        var unhealthyResults = new List<HealthCheckResult>
        {
            HealthCheckResult.Unhealthy(new Uri("https://example1.com"), "Error 1", HttpStatusCode.InternalServerError),
            HealthCheckResult.Unhealthy(new Uri("https://example2.com"), "Error 2", HttpStatusCode.BadGateway),
            HealthCheckResult.Unhealthy(new Uri("https://example3.com"), "Error 3", HttpStatusCode.GatewayTimeout)
        };

        healthMonitoringMock
            .Setup(x => x.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(unhealthyResults);

        var useCase = new MonitorServicesUseCase(
            healthMonitoringMock.Object,
            alertServiceMock.Object,
            options,
            loggerMock.Object);

        // Act
        await useCase.ExecuteAsync(CancellationToken.None);

        // Assert
        alertServiceMock.Verify(
            x => x.SendAlertAsync(
                It.IsAny<Uri>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(3));
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(3));
    }

    /// <summary>
    /// Tests that ExecuteAsync only sends alerts for unhealthy services when results are mixed.
    /// Healthy services should not trigger alerts or warnings.
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_MixedHealthyAndUnhealthy_OnlySendsAlertsForUnhealthy()
    {
        // Arrange
        var healthMonitoringMock = new Mock<IHealthMonitoringService>();
        var alertServiceMock = new Mock<IAlertService>();
        var loggerMock = new Mock<ILogger<MonitorServicesUseCase>>();
        var urls = new List<string> { "https://healthy.com", "https://unhealthy1.com", "https://unhealthy2.com" };
        var recipients = new List<string> { "admin@example.com" };
        var options = CreateOptions(urls, recipients);

        var mixedResults = new List<HealthCheckResult>
        {
            HealthCheckResult.Healthy(new Uri("https://healthy.com"), HttpStatusCode.OK),
            HealthCheckResult.Unhealthy(new Uri("https://unhealthy1.com"), "Error 1", HttpStatusCode.ServiceUnavailable),
            HealthCheckResult.Unhealthy(new Uri("https://unhealthy2.com"), "Error 2", HttpStatusCode.InternalServerError)
        };

        healthMonitoringMock
            .Setup(x => x.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mixedResults);

        var useCase = new MonitorServicesUseCase(
            healthMonitoringMock.Object,
            alertServiceMock.Object,
            options,
            loggerMock.Object);

        // Act
        await useCase.ExecuteAsync(CancellationToken.None);

        // Assert
        alertServiceMock.Verify(
            x => x.SendAlertAsync(
                It.IsAny<Uri>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(2));
    }

    /// <summary>
    /// Tests that ExecuteAsync propagates the CancellationToken to MonitorServicesAsync.
    /// This ensures proper cancellation support throughout the async operation chain.
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_CancellationToken_PropagatedToMonitoringService()
    {
        // Arrange
        var healthMonitoringMock = new Mock<IHealthMonitoringService>();
        var alertServiceMock = new Mock<IAlertService>();
        var loggerMock = new Mock<ILogger<MonitorServicesUseCase>>();
        var urls = new List<string> { "https://example.com" };
        var options = CreateOptions(urls, new List<string> { "admin@example.com" });

        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        healthMonitoringMock
            .Setup(x => x.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), cancellationToken))
            .ReturnsAsync(new List<HealthCheckResult>());

        var useCase = new MonitorServicesUseCase(
            healthMonitoringMock.Object,
            alertServiceMock.Object,
            options,
            loggerMock.Object);

        // Act
        await useCase.ExecuteAsync(cancellationToken);

        // Assert
        healthMonitoringMock.Verify(
            x => x.MonitorServicesAsync(
                It.IsAny<IEnumerable<Uri>>(),
                cancellationToken),
            Times.Once);
    }

    /// <summary>
    /// Tests that ExecuteAsync propagates the CancellationToken to SendAlertAsync.
    /// This ensures proper cancellation support when sending alerts.
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_CancellationToken_PropagatedToAlertService()
    {
        // Arrange
        var healthMonitoringMock = new Mock<IHealthMonitoringService>();
        var alertServiceMock = new Mock<IAlertService>();
        var loggerMock = new Mock<ILogger<MonitorServicesUseCase>>();
        var urls = new List<string> { "https://example.com" };
        var recipients = new List<string> { "admin@example.com" };
        var options = CreateOptions(urls, recipients);

        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var serviceUrl = new Uri("https://example.com");
        var unhealthyResult = HealthCheckResult.Unhealthy(
            serviceUrl,
            "Error",
            HttpStatusCode.InternalServerError);

        healthMonitoringMock
            .Setup(x => x.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<HealthCheckResult> { unhealthyResult });

        var useCase = new MonitorServicesUseCase(
            healthMonitoringMock.Object,
            alertServiceMock.Object,
            options,
            loggerMock.Object);

        // Act
        await useCase.ExecuteAsync(cancellationToken);

        // Assert
        alertServiceMock.Verify(
            x => x.SendAlertAsync(
                It.IsAny<Uri>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(),
                cancellationToken),
            Times.Once);
    }

    /// <summary>
    /// Tests that ExecuteAsync correctly converts URL strings to Uri objects.
    /// The monitoring service should receive properly formatted Uri instances.
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_UrlConversion_ConvertsStringsToUris()
    {
        // Arrange
        var healthMonitoringMock = new Mock<IHealthMonitoringService>();
        var alertServiceMock = new Mock<IAlertService>();
        var loggerMock = new Mock<ILogger<MonitorServicesUseCase>>();
        var urlStrings = new List<string> { "https://example1.com", "https://example2.com/path" };
        var options = CreateOptions(urlStrings, new List<string> { "admin@example.com" });

        IEnumerable<Uri>? capturedUris = null;
        healthMonitoringMock
            .Setup(x => x.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<Uri>, CancellationToken>((urls, ct) => capturedUris = urls.ToList())
            .ReturnsAsync(new List<HealthCheckResult>());

        var useCase = new MonitorServicesUseCase(
            healthMonitoringMock.Object,
            alertServiceMock.Object,
            options,
            loggerMock.Object);

        // Act
        await useCase.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.IsNotNull(capturedUris);
        var uriList = capturedUris.ToList();
        Assert.HasCount(2, uriList);
        Assert.AreEqual("https://example1.com/", uriList[0].ToString());
        Assert.AreEqual("https://example2.com/path", uriList[1].ToString());
    }

    /// <summary>
    /// Tests that ExecuteAsync sends alerts to multiple recipients when configured.
    /// All recipients in the configuration should receive alerts for unhealthy services.
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_MultipleRecipients_SendsAlertToAll()
    {
        // Arrange
        var healthMonitoringMock = new Mock<IHealthMonitoringService>();
        var alertServiceMock = new Mock<IAlertService>();
        var loggerMock = new Mock<ILogger<MonitorServicesUseCase>>();
        var urls = new List<string> { "https://example.com" };
        var recipients = new List<string> { "admin1@example.com", "admin2@example.com", "admin3@example.com" };
        var options = CreateOptions(urls, recipients);

        var serviceUrl = new Uri("https://example.com");
        var unhealthyResult = HealthCheckResult.Unhealthy(
            serviceUrl,
            "Service down",
            HttpStatusCode.ServiceUnavailable);

        healthMonitoringMock
            .Setup(x => x.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<HealthCheckResult> { unhealthyResult });

        var useCase = new MonitorServicesUseCase(
            healthMonitoringMock.Object,
            alertServiceMock.Object,
            options,
            loggerMock.Object);

        // Act
        await useCase.ExecuteAsync(CancellationToken.None);

        // Assert
        alertServiceMock.Verify(
            x => x.SendAlertAsync(
                serviceUrl,
                "Service down",
                It.Is<IEnumerable<string>>(r => r.SequenceEqual(recipients)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that ExecuteAsync logs the correct service URL and status code for unhealthy services.
    /// Warning logs should contain structured information about the failure.
    /// </summary>
    /// <param name="statusCode">The HTTP status code to test.</param>
    [TestMethod]
    [DataRow(HttpStatusCode.BadRequest)]
    [DataRow(HttpStatusCode.Unauthorized)]
    [DataRow(HttpStatusCode.NotFound)]
    [DataRow(HttpStatusCode.InternalServerError)]
    [DataRow(HttpStatusCode.BadGateway)]
    [DataRow(HttpStatusCode.ServiceUnavailable)]
    [DataRow(HttpStatusCode.GatewayTimeout)]
    public async Task ExecuteAsync_UnhealthyService_LogsCorrectStatusCode(HttpStatusCode statusCode)
    {
        // Arrange
        var healthMonitoringMock = new Mock<IHealthMonitoringService>();
        var alertServiceMock = new Mock<IAlertService>();
        var loggerMock = new Mock<ILogger<MonitorServicesUseCase>>();
        var urls = new List<string> { "https://example.com" };
        var options = CreateOptions(urls, new List<string> { "admin@example.com" });

        var serviceUrl = new Uri("https://example.com");
        var unhealthyResult = HealthCheckResult.Unhealthy(
            serviceUrl,
            "Error",
            statusCode);

        healthMonitoringMock
            .Setup(x => x.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<HealthCheckResult> { unhealthyResult });

        var useCase = new MonitorServicesUseCase(
            healthMonitoringMock.Object,
            alertServiceMock.Object,
            options,
            loggerMock.Object);

        // Act
        await useCase.ExecuteAsync(CancellationToken.None);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(serviceUrl.ToString()) && v.ToString()!.Contains(statusCode.ToString())),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that ExecuteAsync handles an unhealthy result with null StatusCode correctly.
    /// This can occur when the service is unreachable or times out.
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_UnhealthyServiceWithNullStatusCode_SendsAlertAndLogs()
    {
        // Arrange
        var healthMonitoringMock = new Mock<IHealthMonitoringService>();
        var alertServiceMock = new Mock<IAlertService>();
        var loggerMock = new Mock<ILogger<MonitorServicesUseCase>>();
        var urls = new List<string> { "https://example.com" };
        var recipients = new List<string> { "admin@example.com" };
        var options = CreateOptions(urls, recipients);

        var serviceUrl = new Uri("https://example.com");
        var unhealthyResult = HealthCheckResult.Unhealthy(serviceUrl, "Connection timeout");

        healthMonitoringMock
            .Setup(x => x.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<HealthCheckResult> { unhealthyResult });

        var useCase = new MonitorServicesUseCase(
            healthMonitoringMock.Object,
            alertServiceMock.Object,
            options,
            loggerMock.Object);

        // Act
        await useCase.ExecuteAsync(CancellationToken.None);

        // Assert
        alertServiceMock.Verify(
            x => x.SendAlertAsync(
                serviceUrl,
                "Connection timeout",
                recipients,
                It.IsAny<CancellationToken>()),
            Times.Once);
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private static IOptions<ServiceMonitorOptions> CreateOptions(List<string> urls, List<string> recipients)
    {
        var options = new ServiceMonitorOptions
        {
            Urls = urls,
            EmailServer = new EmailOptions
            {
                Host = "smtp.example.com",
                Port = 587,
                DefaultEmailSenderAddress = "monitor@example.com",
                DefaultSenderName = "Service Monitor",
                To = recipients,
                Username = "user",
                Password = "password"
            },
            System = new ServiceMonitor.Infrastructure.Configuration.System
            {
                TimeoutSeconds = 30,
                RunMode = RunMode.Once,
                DaemonIntervalMinutes = 5,
                WebUiPort = 8080
            }
        };

        return Options.Create(options);
    }
}
