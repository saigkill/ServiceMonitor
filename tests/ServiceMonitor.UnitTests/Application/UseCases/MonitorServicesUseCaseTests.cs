using System.Net;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ServiceMonitor.Application.DTOs;
using ServiceMonitor.Application.Interfaces;
using ServiceMonitor.Application.UseCases;
using ServiceMonitor.Domain.Entities;
using ServiceMonitor.Domain.Interfaces;
using ServiceMonitor.Infrastructure.Configuration;

namespace ServiceMonitor.UnitTests.Application.UseCases;

[TestClass]
public sealed class MonitorServicesUseCaseTests
{
    private Mock<IHealthMonitoringService> _healthMonitoringServiceMock = null!;
    private Mock<IAlertService> _alertServiceMock = null!;
    private Mock<IOptions<ServiceMonitorOptions>> _optionsMock = null!;
    private Mock<ILogger<MonitorServicesUseCase>> _loggerMock = null!;
    private Mock<IServiceHealthStateRepository> _stateRepositoryMock = null!;
    private MonitorServicesUseCase _sut = null!;
    private ServiceMonitorOptions _options = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _healthMonitoringServiceMock = new Mock<IHealthMonitoringService>();
        _alertServiceMock = new Mock<IAlertService>();
        _optionsMock = new Mock<IOptions<ServiceMonitorOptions>>();
        _loggerMock = new Mock<ILogger<MonitorServicesUseCase>>();
        _stateRepositoryMock = new Mock<IServiceHealthStateRepository>();

        _options = new ServiceMonitorOptions
        {
            Urls = ["https://example.com"],
            EmailServer = new EmailOptions
            {
                To = ["admin@example.com"]
            }
        };

        _optionsMock.Setup(x => x.Value).Returns(_options);

        _sut = new MonitorServicesUseCase(
            _healthMonitoringServiceMock.Object,
            _alertServiceMock.Object,
            _optionsMock.Object,
            _loggerMock.Object,
            _stateRepositoryMock.Object);
    }

    [TestMethod]
    public async Task ExecuteAsync_HealthMonitoringFails_ReturnsFailure()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var errorMessage = "Monitoring service unavailable";
        _healthMonitoringServiceMock
            .Setup(x => x.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), cancellationToken))
            .ReturnsAsync(Result.Failure<IEnumerable<HealthCheckResult>>(errorMessage));

        // Act
        var result = await _sut.ExecuteAsync(cancellationToken);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(errorMessage, result.Error);
    }

    [TestMethod]
    public async Task ExecuteAsync_HealthyService_SavesStateAndReturnsSuccess()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var url = new Uri("https://example.com");
        var healthCheckResults = new[]
        {
            new HealthCheckResult(url, true, HttpStatusCode.OK, Maybe<string>.None)
        };

        _healthMonitoringServiceMock
            .Setup(x => x.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), cancellationToken))
            .ReturnsAsync(Result.Success<IEnumerable<HealthCheckResult>>(healthCheckResults));

        _stateRepositoryMock
            .Setup(x => x.GetAsync(url, cancellationToken))
            .ReturnsAsync(Maybe<ServiceHealthState>.None);

        _stateRepositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<ServiceHealthState>(), cancellationToken))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _sut.ExecuteAsync(cancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        _stateRepositoryMock.Verify(x => x.SaveAsync(It.IsAny<ServiceHealthState>(), cancellationToken), Times.Once);
        _alertServiceMock.Verify(x => x.SendAlertAsync(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<IList<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task ExecuteAsync_UnhealthyServiceWithoutPreviousState_SendsAlertAndSavesState()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var url = new Uri("https://example.com");
        var errorMessage = "Service unavailable";
        var healthCheckResults = new[]
        {
            new HealthCheckResult(url, false, HttpStatusCode.ServiceUnavailable, Maybe<string>.From(errorMessage))
        };

        _healthMonitoringServiceMock
            .Setup(x => x.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), cancellationToken))
            .ReturnsAsync(Result.Success<IEnumerable<HealthCheckResult>>(healthCheckResults));

        _stateRepositoryMock
            .Setup(x => x.GetAsync(url, cancellationToken))
            .ReturnsAsync(Maybe<ServiceHealthState>.None);

        _alertServiceMock
            .Setup(x => x.SendAlertAsync(url, errorMessage, _options.EmailServer.To, cancellationToken))
            .ReturnsAsync(Result.Success());

        _stateRepositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<ServiceHealthState>(), cancellationToken))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _sut.ExecuteAsync(cancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        _alertServiceMock.Verify(x => x.SendAlertAsync(url, errorMessage, _options.EmailServer.To, cancellationToken), Times.Once);
        _stateRepositoryMock.Verify(x => x.SaveAsync(It.IsAny<ServiceHealthState>(), cancellationToken), Times.Once);
    }

    [TestMethod]
    public async Task ExecuteAsync_UnhealthyServiceWithErrorMessageNone_SendsAlertWithUnknownError()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var url = new Uri("https://example.com");
        var healthCheckResults = new[]
        {
            new HealthCheckResult(url, false, HttpStatusCode.ServiceUnavailable, Maybe<string>.None)
        };

        _healthMonitoringServiceMock
            .Setup(x => x.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), cancellationToken))
            .ReturnsAsync(Result.Success<IEnumerable<HealthCheckResult>>(healthCheckResults));

        _stateRepositoryMock
            .Setup(x => x.GetAsync(url, cancellationToken))
            .ReturnsAsync(Maybe<ServiceHealthState>.None);

        _alertServiceMock
            .Setup(x => x.SendAlertAsync(url, "Unknown error", _options.EmailServer.To, cancellationToken))
            .ReturnsAsync(Result.Success());

        _stateRepositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<ServiceHealthState>(), cancellationToken))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _sut.ExecuteAsync(cancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        _alertServiceMock.Verify(x => x.SendAlertAsync(url, "Unknown error", _options.EmailServer.To, cancellationToken), Times.Once);
    }

    [TestMethod]
    public async Task ExecuteAsync_UnhealthyServiceWithAlertAlreadySent_DoesNotSendAlert()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var url = new Uri("https://example.com");
        var errorMessage = "Service unavailable";
        var healthCheckResults = new[]
        {
            new HealthCheckResult(url, false, HttpStatusCode.ServiceUnavailable, Maybe<string>.From(errorMessage))
        };

        var existingState = new ServiceHealthState(url, false);
        existingState.MarkAlertSent();

        _healthMonitoringServiceMock
            .Setup(x => x.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), cancellationToken))
            .ReturnsAsync(Result.Success<IEnumerable<HealthCheckResult>>(healthCheckResults));

        _stateRepositoryMock
            .Setup(x => x.GetAsync(url, cancellationToken))
            .ReturnsAsync(Maybe<ServiceHealthState>.From(existingState));

        _stateRepositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<ServiceHealthState>(), cancellationToken))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _sut.ExecuteAsync(cancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        _alertServiceMock.Verify(x => x.SendAlertAsync(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<IList<string>>(), It.IsAny<CancellationToken>()), Times.Never);
        _stateRepositoryMock.Verify(x => x.SaveAsync(It.IsAny<ServiceHealthState>(), cancellationToken), Times.Once);
    }

    [TestMethod]
    public async Task ExecuteAsync_AlertSendingFails_ContinuesWithoutSavingState()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var url = new Uri("https://example.com");
        var errorMessage = "Service unavailable";
        var alertError = "SMTP server unavailable";
        var healthCheckResults = new[]
        {
            new HealthCheckResult(url, false, HttpStatusCode.ServiceUnavailable, Maybe<string>.From(errorMessage))
        };

        _healthMonitoringServiceMock
            .Setup(x => x.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), cancellationToken))
            .ReturnsAsync(Result.Success<IEnumerable<HealthCheckResult>>(healthCheckResults));

        _stateRepositoryMock
            .Setup(x => x.GetAsync(url, cancellationToken))
            .ReturnsAsync(Maybe<ServiceHealthState>.None);

        _alertServiceMock
            .Setup(x => x.SendAlertAsync(url, errorMessage, _options.EmailServer.To, cancellationToken))
            .ReturnsAsync(Result.Failure(alertError));

        _stateRepositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<ServiceHealthState>(), cancellationToken))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _sut.ExecuteAsync(cancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        _alertServiceMock.Verify(x => x.SendAlertAsync(url, errorMessage, _options.EmailServer.To, cancellationToken), Times.Once);
        _stateRepositoryMock.Verify(x => x.SaveAsync(It.IsAny<ServiceHealthState>(), cancellationToken), Times.Never);
    }

    [TestMethod]
    public async Task ExecuteAsync_StateSavingFails_LogsErrorButContinues()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var url = new Uri("https://example.com");
        var saveError = "Database unavailable";
        var healthCheckResults = new[]
        {
            new HealthCheckResult(url, true, HttpStatusCode.OK, Maybe<string>.None)
        };

        _healthMonitoringServiceMock
            .Setup(x => x.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), cancellationToken))
            .ReturnsAsync(Result.Success<IEnumerable<HealthCheckResult>>(healthCheckResults));

        _stateRepositoryMock
            .Setup(x => x.GetAsync(url, cancellationToken))
            .ReturnsAsync(Maybe<ServiceHealthState>.None);

        _stateRepositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<ServiceHealthState>(), cancellationToken))
            .ReturnsAsync(Result.Failure(saveError));

        // Act
        var result = await _sut.ExecuteAsync(cancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        _stateRepositoryMock.Verify(x => x.SaveAsync(It.IsAny<ServiceHealthState>(), cancellationToken), Times.Once);
    }

    [TestMethod]
    public async Task ExecuteAsync_MultipleServices_ProcessesAll()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var url1 = new Uri("https://example1.com");
        var url2 = new Uri("https://example2.com");
        _options.Urls = ["https://example1.com", "https://example2.com"];

        var healthCheckResults = new[]
        {
            new HealthCheckResult(url1, true, HttpStatusCode.OK, Maybe<string>.None),
            new HealthCheckResult(url2, false, HttpStatusCode.InternalServerError, Maybe<string>.From("Error"))
        };

        _healthMonitoringServiceMock
            .Setup(x => x.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), cancellationToken))
            .ReturnsAsync(Result.Success<IEnumerable<HealthCheckResult>>(healthCheckResults));

        _stateRepositoryMock
            .Setup(x => x.GetAsync(It.IsAny<Uri>(), cancellationToken))
            .ReturnsAsync(Maybe<ServiceHealthState>.None);

        _alertServiceMock
            .Setup(x => x.SendAlertAsync(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<IList<string>>(), cancellationToken))
            .ReturnsAsync(Result.Success());

        _stateRepositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<ServiceHealthState>(), cancellationToken))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _sut.ExecuteAsync(cancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        _stateRepositoryMock.Verify(x => x.SaveAsync(It.IsAny<ServiceHealthState>(), cancellationToken), Times.Exactly(2));
        _alertServiceMock.Verify(x => x.SendAlertAsync(url2, "Error", _options.EmailServer.To, cancellationToken), Times.Once);
    }

    [TestMethod]
    public async Task ExecuteAsync_UnhealthyServiceWithExistingHealthyState_SendsAlert()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var url = new Uri("https://example.com");
        var errorMessage = "Service down";
        var healthCheckResults = new[]
        {
            new HealthCheckResult(url, false, HttpStatusCode.ServiceUnavailable, Maybe<string>.From(errorMessage))
        };

        var existingState = new ServiceHealthState(url, true);

        _healthMonitoringServiceMock
            .Setup(x => x.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), cancellationToken))
            .ReturnsAsync(Result.Success<IEnumerable<HealthCheckResult>>(healthCheckResults));

        _stateRepositoryMock
            .Setup(x => x.GetAsync(url, cancellationToken))
            .ReturnsAsync(Maybe<ServiceHealthState>.From(existingState));

        _alertServiceMock
            .Setup(x => x.SendAlertAsync(url, errorMessage, _options.EmailServer.To, cancellationToken))
            .ReturnsAsync(Result.Success());

        _stateRepositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<ServiceHealthState>(), cancellationToken))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _sut.ExecuteAsync(cancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        _alertServiceMock.Verify(x => x.SendAlertAsync(url, errorMessage, _options.EmailServer.To, cancellationToken), Times.Once);
        _stateRepositoryMock.Verify(x => x.SaveAsync(
            It.Is<ServiceHealthState>(s => s.AlertSent && !s.IsHealthy),
            cancellationToken), Times.Once);
    }

    [TestMethod]
    public async Task ExecuteAsync_ServiceRecovers_ResetsAlertSentFlag()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var url = new Uri("https://example.com");
        var healthCheckResults = new[]
        {
            new HealthCheckResult(url, true, HttpStatusCode.OK, Maybe<string>.None)
        };

        var existingState = new ServiceHealthState(url, false);
        existingState.MarkAlertSent();

        _healthMonitoringServiceMock
            .Setup(x => x.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), cancellationToken))
            .ReturnsAsync(Result.Success<IEnumerable<HealthCheckResult>>(healthCheckResults));

        _stateRepositoryMock
            .Setup(x => x.GetAsync(url, cancellationToken))
            .ReturnsAsync(Maybe<ServiceHealthState>.From(existingState));

        _stateRepositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<ServiceHealthState>(), cancellationToken))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _sut.ExecuteAsync(cancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        _stateRepositoryMock.Verify(x => x.SaveAsync(
            It.Is<ServiceHealthState>(s => !s.AlertSent && s.IsHealthy),
            cancellationToken), Times.Once);
    }

    [TestMethod]
    public async Task ExecuteAsync_MultipleServicesOneAlertFails_ProcessesRemainingButSkipsFailedOne()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var url1 = new Uri("https://example1.com");
        var url2 = new Uri("https://example2.com");
        _options.Urls = ["https://example1.com", "https://example2.com"];

        var healthCheckResults = new[]
        {
            new HealthCheckResult(url1, false, HttpStatusCode.InternalServerError, Maybe<string>.From("Error1")),
            new HealthCheckResult(url2, false, HttpStatusCode.ServiceUnavailable, Maybe<string>.From("Error2"))
        };

        _healthMonitoringServiceMock
            .Setup(x => x.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), cancellationToken))
            .ReturnsAsync(Result.Success<IEnumerable<HealthCheckResult>>(healthCheckResults));

        _stateRepositoryMock
            .Setup(x => x.GetAsync(It.IsAny<Uri>(), cancellationToken))
            .ReturnsAsync(Maybe<ServiceHealthState>.None);

        _alertServiceMock
            .Setup(x => x.SendAlertAsync(url1, "Error1", _options.EmailServer.To, cancellationToken))
            .ReturnsAsync(Result.Failure("Alert failed"));

        _alertServiceMock
            .Setup(x => x.SendAlertAsync(url2, "Error2", _options.EmailServer.To, cancellationToken))
            .ReturnsAsync(Result.Success());

        _stateRepositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<ServiceHealthState>(), cancellationToken))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _sut.ExecuteAsync(cancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        _stateRepositoryMock.Verify(x => x.SaveAsync(It.IsAny<ServiceHealthState>(), cancellationToken), Times.Once);
        _alertServiceMock.Verify(x => x.SendAlertAsync(url1, "Error1", _options.EmailServer.To, cancellationToken), Times.Once);
        _alertServiceMock.Verify(x => x.SendAlertAsync(url2, "Error2", _options.EmailServer.To, cancellationToken), Times.Once);
    }
}
