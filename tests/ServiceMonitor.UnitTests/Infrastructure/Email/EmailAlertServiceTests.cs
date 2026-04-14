using CSharpFunctionalExtensions;
using FluentEmail.Core;
using FluentEmail.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceMonitor.Infrastructure.Email;

namespace ServiceMonitor.UnitTests.Infrastructure.Email;

[TestClass]
public sealed class EmailAlertServiceTests
{
    private Mock<ILogger<EmailAlertService>> _loggerMock = null!;
    private Mock<IFluentEmail> _fluentEmailMock = null!;
    private EmailAlertService _sut = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _loggerMock = new Mock<ILogger<EmailAlertService>>();
        _fluentEmailMock = new Mock<IFluentEmail>();

        _sut = new EmailAlertService(_loggerMock.Object, _fluentEmailMock.Object);
    }

    [TestMethod]
    public async Task SendAlertAsync_SuccessfulEmailSending_ReturnsSuccess()
    {
        // Arrange
        var serviceUrl = new Uri("https://example.com");
        var errorMessage = "Service unavailable";
        var recipients = new[] { "admin@example.com" };
        var cancellationToken = CancellationToken.None;

        var sendResponse = new SendResponse();

        _fluentEmailMock.Setup(x => x.To(It.IsAny<IEnumerable<Address>>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Subject(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Body(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.SendAsync(cancellationToken)).ReturnsAsync(sendResponse);

        // Act
        var result = await _sut.SendAlertAsync(serviceUrl, errorMessage, recipients, cancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public async Task SendAlertAsync_NullServiceUrl_ThrowsArgumentNullException()
    {
        // Arrange
        Uri? serviceUrl = null;
        var errorMessage = "Service unavailable";
        var recipients = new[] { "admin@example.com" };
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.SendAlertAsync(serviceUrl!, errorMessage, recipients, cancellationToken));
    }

    [TestMethod]
    public async Task SendAlertAsync_NullErrorMessage_ThrowsArgumentException()
    {
        // Arrange
        var serviceUrl = new Uri("https://example.com");
        string? errorMessage = null;
        var recipients = new[] { "admin@example.com" };
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.SendAlertAsync(serviceUrl, errorMessage!, recipients, cancellationToken));
    }

    [TestMethod]
    public async Task SendAlertAsync_WhitespaceErrorMessage_ThrowsArgumentException()
    {
        // Arrange
        var serviceUrl = new Uri("https://example.com");
        var errorMessage = "   ";
        var recipients = new[] { "admin@example.com" };
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.SendAlertAsync(serviceUrl, errorMessage, recipients, cancellationToken));
    }

    [TestMethod]
    public async Task SendAlertAsync_NullRecipients_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceUrl = new Uri("https://example.com");
        var errorMessage = "Service unavailable";
        IEnumerable<string>? recipients = null;
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.SendAlertAsync(serviceUrl, errorMessage, recipients!, cancellationToken));
    }

    [TestMethod]
    public async Task SendAlertAsync_FailedEmailSending_ReturnsFailure()
    {
        // Arrange
        var serviceUrl = new Uri("https://example.com");
        var errorMessage = "Service unavailable";
        var recipients = new[] { "admin@example.com" };
        var cancellationToken = CancellationToken.None;

        var sendResponse = new SendResponse();
        sendResponse.ErrorMessages.Add("SMTP connection failed");
        sendResponse.ErrorMessages.Add("Timeout occurred");

        _fluentEmailMock.Setup(x => x.To(It.IsAny<IEnumerable<Address>>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Subject(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Body(It.IsAny<string>(), It.IsAny<bool>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.SendAsync(It.IsAny<CancellationToken>())).ReturnsAsync(sendResponse);

        // Act
        var result = await _sut.SendAlertAsync(serviceUrl, errorMessage, recipients, cancellationToken);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.Error, "SMTP connection failed");
        StringAssert.Contains(result.Error, "Timeout occurred");
    }

    [TestMethod]
    public async Task SendAlertAsync_ExceptionDuringEmailSending_ReturnsFailure()
    {
        // Arrange
        var serviceUrl = new Uri("https://example.com");
        var errorMessage = "Service unavailable";
        var recipients = new[] { "admin@example.com" };
        var cancellationToken = CancellationToken.None;

        _fluentEmailMock.Setup(x => x.To(It.IsAny<IEnumerable<Address>>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Subject(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Body(It.IsAny<string>(), It.IsAny<bool>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.SendAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Network error"));

        // Act
        var result = await _sut.SendAlertAsync(serviceUrl, errorMessage, recipients, cancellationToken);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.Error, "Network error");
    }

    [TestMethod]
    public async Task SendAlertAsync_MultipleRecipients_CreatesCorrectAddressList()
    {
        // Arrange
        var serviceUrl = new Uri("https://example.com");
        var errorMessage = "Service unavailable";
        var recipients = new[] { "admin@example.com", "support@example.com", "ops@example.com" };
        var cancellationToken = CancellationToken.None;

        var sendResponse = new SendResponse();
        IEnumerable<Address>? capturedRecipients = null;

        _fluentEmailMock.Setup(x => x.To(It.IsAny<IEnumerable<Address>>()))
            .Callback<IEnumerable<Address>>(r => capturedRecipients = r)
            .Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Subject(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Body(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.SendAsync(cancellationToken)).ReturnsAsync(sendResponse);

        // Act
        var result = await _sut.SendAlertAsync(serviceUrl, errorMessage, recipients, cancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(capturedRecipients);
        var recipientsList = capturedRecipients.ToList();
        Assert.HasCount(3, recipientsList);
        Assert.AreEqual("admin@example.com", recipientsList[0].EmailAddress);
        Assert.AreEqual("support@example.com", recipientsList[1].EmailAddress);
        Assert.AreEqual("ops@example.com", recipientsList[2].EmailAddress);
    }

    [TestMethod]
    public async Task SendAlertAsync_SuccessfulSending_LogsInformationMessage()
    {
        // Arrange
        var serviceUrl = new Uri("https://example.com");
        var errorMessage = "Service unavailable";
        var recipients = new[] { "admin@example.com" };
        var cancellationToken = CancellationToken.None;

        var sendResponse = new SendResponse();

        _fluentEmailMock.Setup(x => x.To(It.IsAny<IEnumerable<Address>>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Subject(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Body(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.SendAsync(cancellationToken)).ReturnsAsync(sendResponse);

        // Act
        await _sut.SendAlertAsync(serviceUrl, errorMessage, recipients, cancellationToken);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Down-Email") && v.ToString()!.Contains(serviceUrl.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task SendAlertAsync_FailedSending_LogsErrorMessage()
    {
        // Arrange
        var serviceUrl = new Uri("https://example.com");
        var errorMessage = "Service unavailable";
        var recipients = new[] { "admin@example.com" };
        var cancellationToken = CancellationToken.None;

        var sendResponse = new SendResponse();
        sendResponse.ErrorMessages.Add("SMTP error");

        _fluentEmailMock.Setup(x => x.To(It.IsAny<IEnumerable<Address>>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Subject(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Body(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.SendAsync(cancellationToken)).ReturnsAsync(sendResponse);

        // Act
        await _sut.SendAlertAsync(serviceUrl, errorMessage, recipients, cancellationToken);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to send email")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task SendAlertAsync_ExceptionThrown_LogsErrorWithException()
    {
        // Arrange
        var serviceUrl = new Uri("https://example.com");
        var errorMessage = "Service unavailable";
        var recipients = new[] { "admin@example.com" };
        var cancellationToken = CancellationToken.None;
        var exception = new InvalidOperationException("Network error");

        _fluentEmailMock.Setup(x => x.To(It.IsAny<IEnumerable<Address>>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Subject(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Body(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.SendAsync(cancellationToken)).ThrowsAsync(exception);

        // Act
        await _sut.SendAlertAsync(serviceUrl, errorMessage, recipients, cancellationToken);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Exception while sending alert")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task SendAlertAsync_ValidRequest_CallsFluentEmailCorrectly()
    {
        // Arrange
        var serviceUrl = new Uri("https://example.com");
        var errorMessage = "Connection timeout";
        var recipients = new[] { "admin@example.com" };
        var cancellationToken = CancellationToken.None;

        var sendResponse = new SendResponse();

        _fluentEmailMock.Setup(x => x.To(It.IsAny<IEnumerable<Address>>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Subject(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Body(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.SendAsync(cancellationToken)).ReturnsAsync(sendResponse);

        // Act
        var result = await _sut.SendAlertAsync(serviceUrl, errorMessage, recipients, cancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        _fluentEmailMock.Verify(x => x.To(It.IsAny<IEnumerable<Address>>()), Times.Once);
        _fluentEmailMock.Verify(x => x.Subject(It.IsAny<string>()), Times.Once);
        _fluentEmailMock.Verify(x => x.Body(It.IsAny<string>()), Times.Once);
        _fluentEmailMock.Verify(x => x.SendAsync(cancellationToken), Times.Once);
    }
}
