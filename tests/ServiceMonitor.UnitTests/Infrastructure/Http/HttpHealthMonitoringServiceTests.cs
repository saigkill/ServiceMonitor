using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using ServiceMonitor.Application.DTOs;
using ServiceMonitor.Infrastructure.Configuration;
using ServiceMonitor.Infrastructure.Http;


namespace ServiceMonitor.Infrastructure.Http.UnitTests;

/// <summary>
/// Tests for the HttpHealthMonitoringService class.
/// </summary>
[TestClass]
public sealed class HttpHealthMonitoringServiceTests
{
    /// <summary>
    /// Tests that MonitorServicesAsync returns empty results when serviceUrls is empty.
    /// </summary>
    [TestMethod]
    public async Task MonitorServicesAsync_EmptyServiceUrls_ReturnsEmptyResults()
    {
        // Arrange
        var mockHttpClient = new Mock<HttpClient>();
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(mockHttpClient.Object);

        var serviceMonitorOptions = new ServiceMonitorOptions
        {
            System = new ServiceMonitor.Infrastructure.Configuration.System
            {
                TimeoutSeconds = 30
            }
        };
        var mockOptions = new Mock<IOptions<ServiceMonitorOptions>>();
        mockOptions.Setup(o => o.Value).Returns(serviceMonitorOptions);

        var service = new HttpHealthMonitoringService(mockHttpClientFactory.Object, mockOptions.Object);
        var serviceUrls = Enumerable.Empty<Uri>();
        var cancellationToken = CancellationToken.None;

        // Act
        var results = await service.MonitorServicesAsync(serviceUrls, cancellationToken);

        // Assert
        Assert.IsNotNull(results);
        Assert.AreEqual(0, results.Count());
    }

}