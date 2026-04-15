using CSharpFunctionalExtensions;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

using ServiceMonitor.Application.DTOs;
using ServiceMonitor.Application.Interfaces;
using ServiceMonitor.Application.UseCases;
using ServiceMonitor.Domain.Interfaces;
using ServiceMonitor.Infrastructure.Configuration;
using ServiceMonitor.Infrastructure.Hosting;

namespace ServiceMonitor.UnitTests.Infrastructure.Hosting;

[TestClass]
public sealed class ConsoleHostedServiceTests
{
    public TestContext TestContext { get; set; } = null!;

    private Mock<ILogger<ConsoleHostedService>> _loggerMock = null!;
    private Mock<IHostApplicationLifetime> _appLifetimeMock = null!;
    private Mock<IHealthMonitoringService> _healthMonitoringServiceMock = null!;
    private Mock<IAlertService> _alertServiceMock = null!;
    private Mock<ILogger<MonitorServicesUseCase>> _useCaseLoggerMock = null!;
    private Mock<IServiceHealthStateRepository> _stateRepositoryMock = null!;
    private Mock<IOptions<ServiceMonitorOptions>> _optionsMock = null!;
    private ServiceMonitorOptions _options = null!;
    private MonitorServicesUseCase _monitorServiceUseCase = null!;
    private ConsoleHostedService _sut = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _loggerMock = new Mock<ILogger<ConsoleHostedService>>();
        _appLifetimeMock = new Mock<IHostApplicationLifetime>();
        _healthMonitoringServiceMock = new Mock<IHealthMonitoringService>();
        _alertServiceMock = new Mock<IAlertService>();
        _useCaseLoggerMock = new Mock<ILogger<MonitorServicesUseCase>>();
        _stateRepositoryMock = new Mock<IServiceHealthStateRepository>();
        _optionsMock = new Mock<IOptions<ServiceMonitorOptions>>();

        _options = new ServiceMonitorOptions
        {
            System = new ServiceMonitor.Infrastructure.Configuration.System
            {
                RunMode = RunMode.Daemon,
                DaemonIntervalMinutes = 5
            }
        };

        _optionsMock.Setup(x => x.Value).Returns(_options);

        _monitorServiceUseCase = new MonitorServicesUseCase(
            _healthMonitoringServiceMock.Object,
            _alertServiceMock.Object,
            _optionsMock.Object,
            _useCaseLoggerMock.Object,
            _stateRepositoryMock.Object);

        _sut = new ConsoleHostedService(
            _loggerMock.Object,
            _appLifetimeMock.Object,
            _monitorServiceUseCase,
            _optionsMock.Object);
    }

    [TestMethod]
    public async Task StartAsync_RunModeOnce_ExecutesOnceAndStopsApplication()
    {
        // Arrange
        _options.System.RunMode = RunMode.Once;
        _options.Urls = ["https://example.com"];
        _healthMonitoringServiceMock
            .Setup(x => x.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(Enumerable.Empty<HealthCheckResult>()));

        // Act
        await _sut.StartAsync(TestContext.CancellationToken);

        // Assert
        _appLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
    }

    [TestMethod]
    public async Task StartAsync_RunModeOnceWithFailure_LogsErrorAndStopsApplication()
    {
        // Arrange
        _options.System.RunMode = RunMode.Once;
        _options.Urls = ["https://example.com"];
        var errorMessage = "Test error";
        _healthMonitoringServiceMock
            .Setup(x => x.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IEnumerable<HealthCheckResult>>(errorMessage));

        // Act
        await _sut.StartAsync(TestContext.CancellationToken);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Monitoring execution failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        _appLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
    }

    [TestMethod]
    public async Task StartAsync_DaemonMode_LogsStartAndStopsApplication()
    {
        // Arrange
        _options.System.RunMode = RunMode.Daemon;
        _options.System.DaemonIntervalMinutes = 1;
        _options.Urls = ["https://example.com"];
        _healthMonitoringServiceMock
            .Setup(x => x.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(Enumerable.Empty<HealthCheckResult>()));

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        await _sut.StartAsync(cts.Token);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting in Daemon-Modus")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        _appLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
    }

    [TestMethod]
    public async Task StartAsync_DaemonModeWithFailure_LogsErrorAndContinues()
    {
        // Arrange
        _options.System.RunMode = RunMode.Daemon;
        _options.System.DaemonIntervalMinutes = 1;
        _options.Urls = ["https://example.com"];
        var errorMessage = "Test error";

        var callCount = 0;
        _healthMonitoringServiceMock
            .Setup(x => x.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return Result.Failure<IEnumerable<HealthCheckResult>>(errorMessage);
            });

        using var cts = new CancellationTokenSource();
        var startTask = Task.Run(() => _sut.StartAsync(cts.Token));

        // Wait for at least one execution
        await Task.Delay(100, cts.Token);

        try
        {
            await startTask;
        }
        catch (TaskCanceledException)
        {
            // Expected when Task.Delay is cancelled
        }

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Monitoring execution failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
        _appLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
    }

    [TestMethod]
    public async Task StartAsync_DaemonModeWithException_LogsErrorAndContinues()
    {
        // Arrange
        _options.System.RunMode = RunMode.Daemon;
        _options.System.DaemonIntervalMinutes = 1;
        _options.Urls = ["https://example.com"];

        var callCount = 0;
        _healthMonitoringServiceMock
            .Setup(x => x.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                throw new InvalidOperationException("Test exception");
            });

        using var cts = new CancellationTokenSource();
        var startTask = Task.Run(() => _sut.StartAsync(cts.Token));

        // Wait for at least one execution
        await Task.Delay(100, cts.Token);

        try
        {
            await startTask;
        }
        catch (TaskCanceledException)
        {
            // Expected when Task.Delay is cancelled
        }

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error in Daemon run")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
        _appLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
    }

    [TestMethod]
    public async Task StartAsync_DaemonModeCancelled_LogsStoppedMessage()
    {
        // Arrange
        _options.System.RunMode = RunMode.Daemon;
        _options.System.DaemonIntervalMinutes = 1;
        _options.Urls = ["https://example.com"];
        _healthMonitoringServiceMock
            .Setup(x => x.MonitorServicesAsync(It.IsAny<IEnumerable<Uri>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(Enumerable.Empty<HealthCheckResult>()));

        using var cts = new CancellationTokenSource();

        // Act
        await _sut.StartAsync(cts.Token);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Daemon has been stopped")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        _appLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
    }

    [TestMethod]
    public async Task StartAsync_UnhandledException_LogsErrorAndStopsApplication()
    {
        // Arrange
        _options.System.RunMode = RunMode.Once;
        _options.Urls = ["https://example.com"];

        // Force an unhandled exception by causing a NullReferenceException
        _optionsMock.Setup(x => x.Value).Returns((ServiceMonitorOptions)null!);

        // Act
        await _sut.StartAsync(TestContext.CancellationToken);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unhandled exception")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        _appLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
    }

    [TestMethod]
    public async Task StopAsync_Always_ReturnsCompletedTask()
    {
        // Arrange
        // Act
        var result = _sut.StopAsync(TestContext.CancellationToken);

        // Assert
        await result;
        Assert.AreEqual(Task.CompletedTask, result);
    }
}
