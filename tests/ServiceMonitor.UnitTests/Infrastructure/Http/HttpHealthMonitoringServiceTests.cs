using System.Net;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceMonitor.Application.DTOs;
using ServiceMonitor.Infrastructure.Http;

namespace ServiceMonitor.UnitTests.Infrastructure.Http;

[TestClass]
public sealed class HttpHealthMonitoringServiceTests
{
    private Mock<IHttpClientFactory> _httpClientFactoryMock = null!;
    private Mock<ILogger<HttpHealthMonitoringService>> _loggerMock = null!;
    private HttpHealthMonitoringService _sut = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<HttpHealthMonitoringService>>();

        _sut = new HttpHealthMonitoringService(
            _httpClientFactoryMock.Object,
            _loggerMock.Object);
    }

    [TestMethod]
    public async Task MonitorServicesAsync_WithValidUrls_ReturnsSuccess()
    {
        // Arrange
        var urls = new[] { new Uri("https://example.com"), new Uri("https://test.com") };
        var cancellationToken = CancellationToken.None;

        var mockHttpMessageHandler = new MockHttpMessageHandler();
        mockHttpMessageHandler.AddResponse(new Uri("https://example.com"), HttpStatusCode.OK);
        mockHttpMessageHandler.AddResponse(new Uri("https://test.com"), HttpStatusCode.OK);

        var httpClient = new HttpClient(mockHttpMessageHandler);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = await _sut.MonitorServicesAsync(urls, cancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(2, result.Value.Count());
    }

    [TestMethod]
    public async Task MonitorServicesAsync_WithEmptyUrls_ReturnsSuccess()
    {
        // Arrange
        var urls = Array.Empty<Uri>();
        var cancellationToken = CancellationToken.None;

        var httpClient = new HttpClient(new MockHttpMessageHandler());
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = await _sut.MonitorServicesAsync(urls, cancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(0, result.Value.Count());
    }

    [TestMethod]
    public async Task MonitorServicesAsync_WhenExceptionOccurs_ReturnsFailure()
    {
        // Arrange
        var urls = new[] { new Uri("https://example.com") };
        var cancellationToken = CancellationToken.None;
        var expectedException = new InvalidOperationException("Test exception");

        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Throws(expectedException);

        // Act
        var result = await _sut.MonitorServicesAsync(urls, cancellationToken);

        // Assert
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("Monitoring failed: Test exception", result.Error);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to monitor services")),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task MonitorServicesAsync_WithSingleUrl_ReturnsSuccess()
    {
        // Arrange
        var urls = new[] { new Uri("https://example.com") };
        var cancellationToken = CancellationToken.None;

        var mockHttpMessageHandler = new MockHttpMessageHandler();
        mockHttpMessageHandler.AddResponse(new Uri("https://example.com"), HttpStatusCode.OK);

        var httpClient = new HttpClient(mockHttpMessageHandler);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = await _sut.MonitorServicesAsync(urls, cancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(1, result.Value.Count());
    }

    [TestMethod]
    public async Task MonitorServicesAsync_WithMixedStatusCodes_ReturnsSuccess()
    {
        // Arrange
        var urls = new[]
        {
            new Uri("https://example.com"),
            new Uri("https://test.com"),
            new Uri("https://fail.com")
        };
        var cancellationToken = CancellationToken.None;

        var mockHttpMessageHandler = new MockHttpMessageHandler();
        mockHttpMessageHandler.AddResponse(new Uri("https://example.com"), HttpStatusCode.OK);
        mockHttpMessageHandler.AddResponse(new Uri("https://test.com"), HttpStatusCode.InternalServerError);
        mockHttpMessageHandler.AddResponse(new Uri("https://fail.com"), HttpStatusCode.NotFound);

        var httpClient = new HttpClient(mockHttpMessageHandler);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = await _sut.MonitorServicesAsync(urls, cancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(3, result.Value.Count());
    }

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Dictionary<Uri, HttpStatusCode> _responses = new();

        public void AddResponse(Uri uri, HttpStatusCode statusCode)
        {
            _responses[uri] = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.RequestUri != null && _responses.TryGetValue(request.RequestUri, out var statusCode))
            {
                return Task.FromResult(new HttpResponseMessage(statusCode));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
