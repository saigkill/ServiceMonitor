using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ServiceMonitor.Application.DTOs;
using ServiceMonitor.Application.Interfaces;
using ServiceMonitor.Application.UseCases;
using ServiceMonitor.Domain.Entities;
using ServiceMonitor.Domain.Interfaces;
using ServiceMonitor.Infrastructure.Configuration;
using System.Net;

namespace ServiceMonitor.UnitTests.Application.UseCases;

/// <summary>
/// Tests for the MonitorServicesUseCase class.
/// </summary>
[TestClass]
public sealed class MonitorServicesUseCaseTests
{
    /// <summary>
    /// Tests that ExecuteAsync processes healthy service correctly.
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_HealthyService_UpdatesStateWithoutSendingAlert()
    {
        // Arrange
        var mockHealthMonitoring = new Mock<IHealthMonitoringService>();
        var mockAlertService = new Mock<IAlertService>();
        var mockLogger = new Mock<ILogger<MonitorServicesUseCase>>();
        var mockStateRepository = new Mock<IServiceHealthStateRepository>();

        var serviceUrl = new Uri("https://example.com");
        var options = Options.Create(new ServiceMonitorOptions
        {
            Urls = new List<string> { serviceUrl.ToString() },
            EmailServer = new EmailOptions
            {
                To = new List<string> { "admin@example.com" }
            }
        });

        var healthCheckResult = HealthCheckResult.Healthy(serviceUrl, HttpStatusCode.OK);
        mockHealthMonitoring
            .Setup(h => h.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { healthCheckResult });

        var existingState = new ServiceHealthState(serviceUrl, false);
        mockStateRepository
            .Setup(r => r.GetAsync(serviceUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingState);

        var useCase = new MonitorServicesUseCase(
            mockHealthMonitoring.Object,
            mockAlertService.Object,
            options,
            mockLogger.Object,
            mockStateRepository.Object);

        // Act
        await useCase.ExecuteAsync(CancellationToken.None);

        // Assert
        mockHealthMonitoring.Verify(
            h => h.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        mockAlertService.Verify(
            a => a.SendAlertAsync(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.IsTrue(existingState.IsHealthy);
    }

    /// <summary>
    /// Tests that ExecuteAsync sends alert for unhealthy service when no alert was sent before.
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_UnhealthyServiceWithoutAlertSent_SendsAlert()
    {
        // Arrange
        var mockHealthMonitoring = new Mock<IHealthMonitoringService>();
        var mockAlertService = new Mock<IAlertService>();
        var mockLogger = new Mock<ILogger<MonitorServicesUseCase>>();
        var mockStateRepository = new Mock<IServiceHealthStateRepository>();

        var serviceUrl = new Uri("https://example.com");
        var options = Options.Create(new ServiceMonitorOptions
        {
            Urls = new List<string> { serviceUrl.ToString() },
            EmailServer = new EmailOptions
            {
                To = new List<string> { "admin@example.com" }
            }
        });

        var healthCheckResult = HealthCheckResult.Unhealthy(serviceUrl, "Service unreachable", HttpStatusCode.ServiceUnavailable);
        mockHealthMonitoring
            .Setup(h => h.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { healthCheckResult });

        var existingState = new ServiceHealthState(serviceUrl, true);
        mockStateRepository
            .Setup(r => r.GetAsync(serviceUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingState);

        mockAlertService
            .Setup(a => a.SendAlertAsync(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new MonitorServicesUseCase(
            mockHealthMonitoring.Object,
            mockAlertService.Object,
            options,
            mockLogger.Object,
            mockStateRepository.Object);

        // Act
        await useCase.ExecuteAsync(CancellationToken.None);

        // Assert
        mockAlertService.Verify(
            a => a.SendAlertAsync(
                serviceUrl,
                "Service unreachable",
                options.Value.EmailServer.To,
                It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.IsTrue(existingState.AlertSent);
        Assert.IsFalse(existingState.IsHealthy);
    }

    /// <summary>
    /// Tests that ExecuteAsync does not send alert for unhealthy service when alert was already sent.
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_UnhealthyServiceWithAlertAlreadySent_DoesNotSendAlert()
    {
        // Arrange
        var mockHealthMonitoring = new Mock<IHealthMonitoringService>();
        var mockAlertService = new Mock<IAlertService>();
        var mockLogger = new Mock<ILogger<MonitorServicesUseCase>>();
        var mockStateRepository = new Mock<IServiceHealthStateRepository>();

        var serviceUrl = new Uri("https://example.com");
        var options = Options.Create(new ServiceMonitorOptions
        {
            Urls = new List<string> { serviceUrl.ToString() },
            EmailServer = new EmailOptions
            {
                To = new List<string> { "admin@example.com" }
            }
        });

        var healthCheckResult = HealthCheckResult.Unhealthy(serviceUrl, "Service unreachable", HttpStatusCode.ServiceUnavailable);
        mockHealthMonitoring
            .Setup(h => h.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { healthCheckResult });

        var existingState = new ServiceHealthState(serviceUrl, false);
        existingState.MarkAlertSent();
        mockStateRepository
            .Setup(r => r.GetAsync(serviceUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingState);

        var useCase = new MonitorServicesUseCase(
            mockHealthMonitoring.Object,
            mockAlertService.Object,
            options,
            mockLogger.Object,
            mockStateRepository.Object);

        // Act
        await useCase.ExecuteAsync(CancellationToken.None);

        // Assert
        mockAlertService.Verify(
            a => a.SendAlertAsync(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.IsTrue(existingState.AlertSent);
        Assert.IsFalse(existingState.IsHealthy);
    }

    /// <summary>
    /// Tests that ExecuteAsync creates new state when repository returns null.
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_NoExistingState_CreatesNewState()
    {
        // Arrange
        var mockHealthMonitoring = new Mock<IHealthMonitoringService>();
        var mockAlertService = new Mock<IAlertService>();
        var mockLogger = new Mock<ILogger<MonitorServicesUseCase>>();
        var mockStateRepository = new Mock<IServiceHealthStateRepository>();

        var serviceUrl = new Uri("https://example.com");
        var options = Options.Create(new ServiceMonitorOptions
        {
            Urls = new List<string> { serviceUrl.ToString() },
            EmailServer = new EmailOptions
            {
                To = new List<string> { "admin@example.com" }
            }
        });

        var healthCheckResult = HealthCheckResult.Healthy(serviceUrl, HttpStatusCode.OK);
        mockHealthMonitoring
            .Setup(h => h.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { healthCheckResult });

        mockStateRepository
            .Setup(r => r.GetAsync(serviceUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceHealthState?)null);

        var useCase = new MonitorServicesUseCase(
            mockHealthMonitoring.Object,
            mockAlertService.Object,
            options,
            mockLogger.Object,
            mockStateRepository.Object);

        // Act
        await useCase.ExecuteAsync(CancellationToken.None);

        // Assert
        mockStateRepository.Verify(
            r => r.GetAsync(serviceUrl, It.IsAny<CancellationToken>()),
            Times.Once);
        mockHealthMonitoring.Verify(
            h => h.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that ExecuteAsync handles multiple services correctly.
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_MultipleServices_ProcessesAll()
    {
        // Arrange
        var mockHealthMonitoring = new Mock<IHealthMonitoringService>();
        var mockAlertService = new Mock<IAlertService>();
        var mockLogger = new Mock<ILogger<MonitorServicesUseCase>>();
        var mockStateRepository = new Mock<IServiceHealthStateRepository>();

        var serviceUrl1 = new Uri("https://example1.com");
        var serviceUrl2 = new Uri("https://example2.com");
        var options = Options.Create(new ServiceMonitorOptions
        {
            Urls = new List<string> { serviceUrl1.ToString(), serviceUrl2.ToString() },
            EmailServer = new EmailOptions
            {
                To = new List<string> { "admin@example.com" }
            }
        });

        var healthCheckResult1 = HealthCheckResult.Healthy(serviceUrl1, HttpStatusCode.OK);
        var healthCheckResult2 = HealthCheckResult.Unhealthy(serviceUrl2, "Service down", HttpStatusCode.ServiceUnavailable);
        mockHealthMonitoring
            .Setup(h => h.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { healthCheckResult1, healthCheckResult2 });

        var state1 = new ServiceHealthState(serviceUrl1, true);
        var state2 = new ServiceHealthState(serviceUrl2, true);
        mockStateRepository
            .Setup(r => r.GetAsync(serviceUrl1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(state1);
        mockStateRepository
            .Setup(r => r.GetAsync(serviceUrl2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(state2);

        mockAlertService
            .Setup(a => a.SendAlertAsync(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new MonitorServicesUseCase(
            mockHealthMonitoring.Object,
            mockAlertService.Object,
            options,
            mockLogger.Object,
            mockStateRepository.Object);

        // Act
        await useCase.ExecuteAsync(CancellationToken.None);

        // Assert
        mockStateRepository.Verify(
            r => r.GetAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        mockAlertService.Verify(
            a => a.SendAlertAsync(serviceUrl2, "Service down", options.Value.EmailServer.To, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that ExecuteAsync uses "Unknown error" when error message is null.
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_UnhealthyServiceWithNullErrorMessage_UsesDefaultErrorMessage()
    {
        // Arrange
        var mockHealthMonitoring = new Mock<IHealthMonitoringService>();
        var mockAlertService = new Mock<IAlertService>();
        var mockLogger = new Mock<ILogger<MonitorServicesUseCase>>();
        var mockStateRepository = new Mock<IServiceHealthStateRepository>();

        var serviceUrl = new Uri("https://example.com");
        var options = Options.Create(new ServiceMonitorOptions
        {
            Urls = new List<string> { serviceUrl.ToString() },
            EmailServer = new EmailOptions
            {
                To = new List<string> { "admin@example.com" }
            }
        });

        var healthCheckResult = new HealthCheckResult
        {
            ServiceUrl = serviceUrl,
            IsHealthy = false,
            StatusCode = HttpStatusCode.InternalServerError,
            ErrorMessage = null,
            CheckedAt = DateTime.UtcNow
        };
        mockHealthMonitoring
            .Setup(h => h.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { healthCheckResult });

        var existingState = new ServiceHealthState(serviceUrl, true);
        mockStateRepository
            .Setup(r => r.GetAsync(serviceUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingState);

        mockAlertService
            .Setup(a => a.SendAlertAsync(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new MonitorServicesUseCase(
            mockHealthMonitoring.Object,
            mockAlertService.Object,
            options,
            mockLogger.Object,
            mockStateRepository.Object);

        // Act
        await useCase.ExecuteAsync(CancellationToken.None);

        // Assert
        mockAlertService.Verify(
            a => a.SendAlertAsync(
                serviceUrl,
                "Unknown error",
                options.Value.EmailServer.To,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that ExecuteAsync logs error when sending alert for unhealthy service.
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_UnhealthyServiceSendsAlert_LogsError()
    {
        // Arrange
        var mockHealthMonitoring = new Mock<IHealthMonitoringService>();
        var mockAlertService = new Mock<IAlertService>();
        var mockLogger = new Mock<ILogger<MonitorServicesUseCase>>();
        var mockStateRepository = new Mock<IServiceHealthStateRepository>();

        var serviceUrl = new Uri("https://example.com");
        var options = Options.Create(new ServiceMonitorOptions
        {
            Urls = new List<string> { serviceUrl.ToString() },
            EmailServer = new EmailOptions
            {
                To = new List<string> { "admin@example.com" }
            }
        });

        var healthCheckResult = HealthCheckResult.Unhealthy(serviceUrl, "Service unreachable", HttpStatusCode.ServiceUnavailable);
        mockHealthMonitoring
            .Setup(h => h.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { healthCheckResult });

        var existingState = new ServiceHealthState(serviceUrl, true);
        mockStateRepository
            .Setup(r => r.GetAsync(serviceUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingState);

        mockAlertService
            .Setup(a => a.SendAlertAsync(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new MonitorServicesUseCase(
            mockHealthMonitoring.Object,
            mockAlertService.Object,
            options,
            mockLogger.Object,
            mockStateRepository.Object);

        // Act
        await useCase.ExecuteAsync(CancellationToken.None);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that ExecuteAsync logs information when service is unhealthy.
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_UnhealthyService_LogsInformation()
    {
        // Arrange
        var mockHealthMonitoring = new Mock<IHealthMonitoringService>();
        var mockAlertService = new Mock<IAlertService>();
        var mockLogger = new Mock<ILogger<MonitorServicesUseCase>>();
        var mockStateRepository = new Mock<IServiceHealthStateRepository>();

        var serviceUrl = new Uri("https://example.com");
        var options = Options.Create(new ServiceMonitorOptions
        {
            Urls = new List<string> { serviceUrl.ToString() },
            EmailServer = new EmailOptions
            {
                To = new List<string> { "admin@example.com" }
            }
        });

        var healthCheckResult = HealthCheckResult.Unhealthy(serviceUrl, "Service unreachable", HttpStatusCode.ServiceUnavailable);
        mockHealthMonitoring
            .Setup(h => h.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { healthCheckResult });

        var existingState = new ServiceHealthState(serviceUrl, false);
        existingState.MarkAlertSent();
        mockStateRepository
            .Setup(r => r.GetAsync(serviceUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingState);

        var useCase = new MonitorServicesUseCase(
            mockHealthMonitoring.Object,
            mockAlertService.Object,
            options,
            mockLogger.Object,
            mockStateRepository.Object);

        // Act
        await useCase.ExecuteAsync(CancellationToken.None);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that ExecuteAsync converts URLs from options to Uri objects.
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_UrlsInOptions_ConvertsToUriObjects()
    {
        // Arrange
        var mockHealthMonitoring = new Mock<IHealthMonitoringService>();
        var mockAlertService = new Mock<IAlertService>();
        var mockLogger = new Mock<ILogger<MonitorServicesUseCase>>();
        var mockStateRepository = new Mock<IServiceHealthStateRepository>();

        var serviceUrl1 = "https://example1.com/";
        var serviceUrl2 = "https://example2.com/";
        var options = Options.Create(new ServiceMonitorOptions
        {
            Urls = new List<string> { serviceUrl1, serviceUrl2 },
            EmailServer = new EmailOptions
            {
                To = new List<string> { "admin@example.com" }
            }
        });

        IEnumerable<Uri>? capturedUris = null;
        mockHealthMonitoring
            .Setup(h => h.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<Uri>, CancellationToken>((urls, ct) => capturedUris = urls.ToList())
            .ReturnsAsync(Array.Empty<HealthCheckResult>());

        var useCase = new MonitorServicesUseCase(
            mockHealthMonitoring.Object,
            mockAlertService.Object,
            options,
            mockLogger.Object,
            mockStateRepository.Object);

        // Act
        await useCase.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.IsNotNull(capturedUris);
        Assert.AreEqual(2, capturedUris.Count());
        Assert.IsTrue(capturedUris.Any(u => u.ToString() == serviceUrl1));
        Assert.IsTrue(capturedUris.Any(u => u.ToString() == serviceUrl2));
    }

    /// <summary>
    /// Tests that ExecuteAsync passes CancellationToken to all async operations.
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_CancellationToken_PassedToAllAsyncOperations()
    {
        // Arrange
        var mockHealthMonitoring = new Mock<IHealthMonitoringService>();
        var mockAlertService = new Mock<IAlertService>();
        var mockLogger = new Mock<ILogger<MonitorServicesUseCase>>();
        var mockStateRepository = new Mock<IServiceHealthStateRepository>();

        var serviceUrl = new Uri("https://example.com");
        var options = Options.Create(new ServiceMonitorOptions
        {
            Urls = new List<string> { serviceUrl.ToString() },
            EmailServer = new EmailOptions
            {
                To = new List<string> { "admin@example.com" }
            }
        });

        var healthCheckResult = HealthCheckResult.Unhealthy(serviceUrl, "Service down", HttpStatusCode.ServiceUnavailable);
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        mockHealthMonitoring
            .Setup(h => h.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), cancellationToken))
            .ReturnsAsync(new[] { healthCheckResult });

        var existingState = new ServiceHealthState(serviceUrl, true);
        mockStateRepository
            .Setup(r => r.GetAsync(serviceUrl, cancellationToken))
            .ReturnsAsync(existingState);

        mockAlertService
            .Setup(a => a.SendAlertAsync(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), cancellationToken))
            .Returns(Task.CompletedTask);

        var useCase = new MonitorServicesUseCase(
            mockHealthMonitoring.Object,
            mockAlertService.Object,
            options,
            mockLogger.Object,
            mockStateRepository.Object);

        // Act
        await useCase.ExecuteAsync(cancellationToken);

        // Assert
        mockHealthMonitoring.Verify(
            h => h.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), cancellationToken),
            Times.Once);
        mockStateRepository.Verify(
            r => r.GetAsync(serviceUrl, cancellationToken),
            Times.Once);
        mockAlertService.Verify(
            a => a.SendAlertAsync(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), cancellationToken),
            Times.Once);
    }
}
