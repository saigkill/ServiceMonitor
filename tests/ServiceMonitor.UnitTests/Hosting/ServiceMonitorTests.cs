using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Moq;
using Moq.Protected;
using Saigkill.Toolbox.Services;
using ServiceMonitor.Infrastructure.Configuration;

namespace ServiceMonitor.UnitTests.Hosting
{
    [TestClass]
    public class ServiceMonitorTests
    {
        /// <summary>
        /// Tests that StartAsync logs information and does not send alert when HTTP response is successful.
        /// Input: Single URL with successful HTTP response (200 OK).
        /// Expected: Information logged, no alert sent.
        /// </summary>
        [TestMethod]
        public async Task StartAsync_SuccessfulHttpResponse_LogsInformationOnly()
        {
            // Arrange
            var options = CreateOptions(new List<string> { "http://test.com" }, 30);
            var mockLogger = new Mock<ILogger<ServiceMonitor.Hosting.ServiceMonitor>>();
            var mockEmailService = new Mock<IEmailService>();
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var sut = new ServiceMonitor.Hosting.ServiceMonitor(options, mockLogger.Object, mockHttpClientFactory.Object, mockEmailService.Object);

            // Act
            await sut.StartAsync(CancellationToken.None);

            // Assert
            mockEmailService.Verify(e => e.SendMessageAsync(It.IsAny<MimeMessage>()), Times.Never);
            mockLogger.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("is reachable")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        /// <summary>
        /// Tests that StartAsync sends alert and logs warning when HTTP response has unsuccessful status code.
        /// Input: Single URL with unsuccessful HTTP response (404 Not Found).
        /// Expected: Alert email sent with status code, warning logged.
        /// </summary>
        [TestMethod]
        [DataRow(HttpStatusCode.NotFound)]
        [DataRow(HttpStatusCode.InternalServerError)]
        [DataRow(HttpStatusCode.BadRequest)]
        [DataRow(HttpStatusCode.Unauthorized)]
        [DataRow(HttpStatusCode.ServiceUnavailable)]
        public async Task StartAsync_UnsuccessfulHttpResponse_SendsAlertAndLogsWarning(HttpStatusCode statusCode)
        {
            // Arrange
            var testUrl = "http://test.com";
            var options = CreateOptions(new List<string> { testUrl }, 30);
            var mockLogger = new Mock<ILogger<ServiceMonitor.Hosting.ServiceMonitor>>();
            var mockEmailService = new Mock<IEmailService>();
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(statusCode));

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var sut = new ServiceMonitor.Hosting.ServiceMonitor(options, mockLogger.Object, mockHttpClientFactory.Object, mockEmailService.Object);

            // Act
            await sut.StartAsync(CancellationToken.None);

            // Assert
            mockEmailService.Verify(
                e => e.SendMessageAsync(It.Is<MimeMessage>(m =>
                    m.Subject.Contains(testUrl) &&
                    m.Body.ToString().Contains(statusCode.ToString()))),
                Times.Once);
            mockLogger.Verify(
                l => l.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(statusCode.ToString())),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        /// <summary>
        /// Tests that StartAsync sends alert and logs error when HTTP request throws exception.
        /// Input: Single URL where HTTP request throws HttpRequestException.
        /// Expected: Alert email sent with exception message, error logged.
        /// </summary>
        [TestMethod]
        public async Task StartAsync_HttpRequestThrowsException_SendsAlertAndLogsError()
        {
            // Arrange
            var testUrl = "http://test.com";
            var exceptionMessage = "Network error occurred";
            var options = CreateOptions(new List<string> { testUrl }, 30);
            var mockLogger = new Mock<ILogger<ServiceMonitor.Hosting.ServiceMonitor>>();
            var mockEmailService = new Mock<IEmailService>();
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException(exceptionMessage));

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var sut = new ServiceMonitor.Hosting.ServiceMonitor(options, mockLogger.Object, mockHttpClientFactory.Object, mockEmailService.Object);

            // Act
            await sut.StartAsync(CancellationToken.None);

            // Assert
            mockEmailService.Verify(
                e => e.SendMessageAsync(It.Is<MimeMessage>(m =>
                    m.Subject.Contains(testUrl) &&
                    m.Body.ToString().Contains(exceptionMessage))),
                Times.Once);
            mockLogger.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not reachable")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        /// <summary>
        /// Tests that StartAsync logs error but continues execution when email service fails.
        /// Input: Single URL with unsuccessful response, email service throws exception.
        /// Expected: Error logged for email failure, method completes without throwing.
        /// </summary>
        [TestMethod]
        public async Task StartAsync_EmailServiceFails_LogsErrorButContinues()
        {
            // Arrange
            var testUrl = "http://test.com";
            var options = CreateOptions(new List<string> { testUrl }, 30);
            var mockLogger = new Mock<ILogger<ServiceMonitor.Hosting.ServiceMonitor>>();
            var mockEmailService = new Mock<IEmailService>();
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

            mockEmailService.Setup(e => e.SendMessageAsync(It.IsAny<MimeMessage>()))
                .ThrowsAsync(new InvalidOperationException("SMTP connection failed"));

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var sut = new ServiceMonitor.Hosting.ServiceMonitor(options, mockLogger.Object, mockHttpClientFactory.Object, mockEmailService.Object);

            // Act
            await sut.StartAsync(CancellationToken.None);

            // Assert
            mockLogger.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to send alert email")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        /// <summary>
        /// Tests that StartAsync completes successfully when URL list is empty.
        /// Input: Empty URL list.
        /// Expected: Method completes without errors, no HTTP requests or alerts.
        /// </summary>
        [TestMethod]
        public async Task StartAsync_EmptyUrlList_CompletesWithoutError()
        {
            // Arrange
            var options = CreateOptions(new List<string>(), 30);
            var mockLogger = new Mock<ILogger<ServiceMonitor.Hosting.ServiceMonitor>>();
            var mockEmailService = new Mock<IEmailService>();
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var sut = new ServiceMonitor.Hosting.ServiceMonitor(options, mockLogger.Object, mockHttpClientFactory.Object, mockEmailService.Object);

            // Act
            await sut.StartAsync(CancellationToken.None);

            // Assert
            mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Never(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
            mockEmailService.Verify(e => e.SendMessageAsync(It.IsAny<MimeMessage>()), Times.Never);
        }

        /// <summary>
        /// Tests that StartAsync handles multiple URLs with different outcomes independently.
        /// Input: Three URLs with success, failure, and exception respectively.
        /// Expected: Each URL processed independently, appropriate actions taken for each.
        /// </summary>
        [TestMethod]
        public async Task StartAsync_MultipleUrlsMixedResults_HandlesEachIndependently()
        {
            // Arrange
            var urls = new List<string> { "http://success.com", "http://failure.com", "http://exception.com" };
            var options = CreateOptions(urls, 30);
            var mockLogger = new Mock<ILogger<ServiceMonitor.Hosting.ServiceMonitor>>();
            var mockEmailService = new Mock<IEmailService>();
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == "http://success.com/"),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == "http://failure.com/"),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == "http://exception.com/"),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Connection refused"));

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var sut = new ServiceMonitor.Hosting.ServiceMonitor(options, mockLogger.Object, mockHttpClientFactory.Object, mockEmailService.Object);

            // Act
            await sut.StartAsync(CancellationToken.None);

            // Assert
            mockEmailService.Verify(e => e.SendMessageAsync(It.IsAny<MimeMessage>()), Times.Exactly(2));
            mockLogger.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
            mockLogger.Verify(
                l => l.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
            mockLogger.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        /// <summary>
        /// Tests that StartAsync adds all recipients from configuration to alert email.
        /// Input: Single URL with failure, multiple recipients in configuration.
        /// Expected: Email sent with all recipients added.
        /// </summary>
        [TestMethod]
        public async Task StartAsync_MultipleRecipients_AddsAllToEmail()
        {
            // Arrange
            var testUrl = "http://test.com";
            var recipients = new List<string> { "admin1@example.com", "admin2@example.com", "admin3@example.com" };
            var options = CreateOptionsWithRecipients(new List<string> { testUrl }, 30, recipients);
            var mockLogger = new Mock<ILogger<ServiceMonitor.Hosting.ServiceMonitor>>();
            var mockEmailService = new Mock<IEmailService>();
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var sut = new ServiceMonitor.Hosting.ServiceMonitor(options, mockLogger.Object, mockHttpClientFactory.Object, mockEmailService.Object);

            // Act
            await sut.StartAsync(CancellationToken.None);

            // Assert
            mockEmailService.Verify(
                e => e.SendMessageAsync(It.Is<MimeMessage>(m => m.To.Count == 3)),
                Times.Once);
        }

        /// <summary>
        /// Tests that StartAsync sends alert when HTTP request times out.
        /// Input: Single URL where HTTP request throws TaskCanceledException (timeout).
        /// Expected: Alert email sent, error logged.
        /// </summary>
        [TestMethod]
        public async Task StartAsync_HttpRequestTimeout_SendsAlertAndLogsError()
        {
            // Arrange
            var testUrl = "http://test.com";
            var options = CreateOptions(new List<string> { testUrl }, 30);
            var mockLogger = new Mock<ILogger<ServiceMonitor.Hosting.ServiceMonitor>>();
            var mockEmailService = new Mock<IEmailService>();
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new TaskCanceledException("The request was canceled due to timeout"));

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var sut = new ServiceMonitor.Hosting.ServiceMonitor(options, mockLogger.Object, mockHttpClientFactory.Object, mockEmailService.Object);

            // Act
            await sut.StartAsync(CancellationToken.None);

            // Assert
            mockEmailService.Verify(e => e.SendMessageAsync(It.IsAny<MimeMessage>()), Times.Once);
            mockLogger.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        /// <summary>
        /// Tests that StartAsync correctly sets HTTP client timeout from configuration.
        /// Input: Timeout value of 60 seconds.
        /// Expected: HttpClient timeout set to 60 seconds.
        /// </summary>
        [TestMethod]
        [DataRow(1)]
        [DataRow(30)]
        [DataRow(60)]
        [DataRow(300)]
        public async Task StartAsync_VariousTimeoutValues_SetsHttpClientTimeout(int timeoutSeconds)
        {
            // Arrange
            var options = CreateOptions(new List<string> { "http://test.com" }, timeoutSeconds);
            var mockLogger = new Mock<ILogger<ServiceMonitor.Hosting.ServiceMonitor>>();
            var mockEmailService = new Mock<IEmailService>();
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

            HttpClient? capturedClient = null;
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(() =>
                {
                    var client = new HttpClient(mockHttpMessageHandler.Object);
                    capturedClient = client;
                    return client;
                });

            var sut = new ServiceMonitor.Hosting.ServiceMonitor(options, mockLogger.Object, mockHttpClientFactory.Object, mockEmailService.Object);

            // Act
            await sut.StartAsync(CancellationToken.None);

            // Assert
            Assert.IsNotNull(capturedClient);
            Assert.AreEqual(TimeSpan.FromSeconds(timeoutSeconds), capturedClient.Timeout);
        }

        /// <summary>
        /// Tests that StartAsync creates correct alert email with expected subject and body.
        /// Input: Single URL with failure.
        /// Expected: Email has correct subject format and body content.
        /// </summary>
        [TestMethod]
        public async Task StartAsync_AlertEmail_HasCorrectSubjectAndBody()
        {
            // Arrange
            var testUrl = "http://test.com";
            var options = CreateOptions(new List<string> { testUrl }, 30);
            var mockLogger = new Mock<ILogger<ServiceMonitor.Hosting.ServiceMonitor>>();
            MimeMessage? capturedEmail = null;
            var mockEmailService = new Mock<IEmailService>();
            mockEmailService.Setup(e => e.SendMessageAsync(It.IsAny<MimeMessage>()))
                .Callback<MimeMessage>(msg => capturedEmail = msg)
                .Returns(Task.CompletedTask);
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var sut = new ServiceMonitor.Hosting.ServiceMonitor(options, mockLogger.Object, mockHttpClientFactory.Object, mockEmailService.Object);

            // Act
            await sut.StartAsync(CancellationToken.None);

            // Assert
            Assert.IsNotNull(capturedEmail);
            Assert.AreEqual($"Service not reachable: {testUrl}", capturedEmail.Subject);
            Assert.IsTrue(capturedEmail.Body.ToString().Contains(testUrl));
            Assert.IsTrue(capturedEmail.Body.ToString().Contains("not reachable"));
        }

        /// <summary>
        /// Tests that StartAsync handles single recipient correctly.
        /// Input: Single URL with failure, single recipient in configuration.
        /// Expected: Email sent with one recipient.
        /// </summary>
        [TestMethod]
        public async Task StartAsync_SingleRecipient_AddsOneToEmail()
        {
            // Arrange
            var testUrl = "http://test.com";
            var recipients = new List<string> { "admin@example.com" };
            var options = CreateOptionsWithRecipients(new List<string> { testUrl }, 30, recipients);
            var mockLogger = new Mock<ILogger<ServiceMonitor.Hosting.ServiceMonitor>>();
            var mockEmailService = new Mock<IEmailService>();
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var sut = new ServiceMonitor.Hosting.ServiceMonitor(options, mockLogger.Object, mockHttpClientFactory.Object, mockEmailService.Object);

            // Act
            await sut.StartAsync(CancellationToken.None);

            // Assert
            mockEmailService.Verify(
                e => e.SendMessageAsync(It.Is<MimeMessage>(m => m.To.Count == 1)),
                Times.Once);
        }

        /// <summary>
        /// Tests that StartAsync handles empty recipient list correctly.
        /// Input: Single URL with failure, empty recipient list.
        /// Expected: Email sent with no recipients added.
        /// </summary>
        [TestMethod]
        public async Task StartAsync_EmptyRecipientList_SendsEmailWithNoRecipients()
        {
            // Arrange
            var testUrl = "http://test.com";
            var recipients = new List<string>();
            var options = CreateOptionsWithRecipients(new List<string> { testUrl }, 30, recipients);
            var mockLogger = new Mock<ILogger<ServiceMonitor.Hosting.ServiceMonitor>>();
            var mockEmailService = new Mock<IEmailService>();
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var sut = new ServiceMonitor.Hosting.ServiceMonitor(options, mockLogger.Object, mockHttpClientFactory.Object, mockEmailService.Object);

            // Act
            await sut.StartAsync(CancellationToken.None);

            // Assert
            mockEmailService.Verify(
                e => e.SendMessageAsync(It.Is<MimeMessage>(m => m.To.Count == 0)),
                Times.Once);
        }

        private static IOptions<ServiceMonitorOptions> CreateOptions(List<string> urls, int timeoutSeconds)
        {
            return CreateOptionsWithRecipients(urls, timeoutSeconds, new List<string> { "test@example.com" });
        }

        private static IOptions<ServiceMonitorOptions> CreateOptionsWithRecipients(List<string> urls, int timeoutSeconds, List<string> recipients)
        {
            var options = new ServiceMonitorOptions
            {
                Urls = urls,
                TimeoutSeconds = timeoutSeconds,
                EmailServer = new EmailOptions
                {
                    Host = "smtp.example.com",
                    Port = 587,
                    DefaultEmailAddress = "noreply@example.com",
                    DefaultSenderName = "Service Monitor",
                    To = recipients,
                    User = "user",
                    Password = "password"
                }
            };

            var mockOptions = new Mock<IOptions<ServiceMonitorOptions>>();
            mockOptions.Setup(o => o.Value).Returns(options);
            return mockOptions.Object;
        }
    }
}
