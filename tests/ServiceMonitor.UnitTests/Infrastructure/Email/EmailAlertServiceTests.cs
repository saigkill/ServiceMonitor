using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Moq;
using Saigkill.Toolbox.Services;
using ServiceMonitor.Infrastructure.Configuration;


namespace ServiceMonitor.Infrastructure.Email.UnitTests;

/// <summary>
/// Unit tests for the <see cref="EmailAlertService"/> class.
/// </summary>
[TestClass]
public sealed class EmailAlertServiceTests
{
    /// <summary>
    /// Tests that SendAlertAsync sends email successfully with valid inputs and single recipient.
    /// </summary>
    [TestMethod]
    public async Task SendAlertAsync_ValidInputsSingleRecipient_SendsEmailSuccessfully()
    {
        // Arrange
        var mockEmailService = new Mock<IEmailService>();
        var mockLogger = new Mock<ILogger<EmailAlertService>>();
        var service = new EmailAlertService(mockEmailService.Object, mockLogger.Object);
        var serviceUrl = new Uri("https://example.com");
        var errorMessage = "Service is down";
        var recipients = new List<string> { "test@example.com" };
        var cancellationToken = CancellationToken.None;
        MimeMessage? capturedMessage = null;
        mockEmailService.Setup(x => x.SendMessageAsync(It.IsAny<MimeMessage>()))
            .Callback<MimeMessage>(msg => capturedMessage = msg)
            .Returns(Task.CompletedTask);

        // Act
        await service.SendAlertAsync(serviceUrl, errorMessage, recipients, cancellationToken);

        // Assert
        mockEmailService.Verify(x => x.SendMessageAsync(It.IsAny<MimeMessage>()), Times.Once);
        Assert.IsNotNull(capturedMessage);
        Assert.AreEqual($"Service not reachable: {serviceUrl}", capturedMessage.Subject);
        Assert.AreEqual(1, capturedMessage.To.Count);
        Assert.IsTrue(capturedMessage.To.Any(addr => addr.ToString().Contains("test@example.com")));
        var textPart = capturedMessage.Body as TextPart;
        Assert.IsNotNull(textPart);
        Assert.Contains(serviceUrl.ToString(), textPart.Text);
        Assert.Contains(errorMessage, textPart.Text);
    }

    /// <summary>
    /// Tests that SendAlertAsync sends email successfully with multiple recipients.
    /// </summary>
    [TestMethod]
    public async Task SendAlertAsync_ValidInputsMultipleRecipients_SendsEmailWithAllRecipients()
    {
        // Arrange
        var mockEmailService = new Mock<IEmailService>();
        var mockLogger = new Mock<ILogger<EmailAlertService>>();
        var service = new EmailAlertService(mockEmailService.Object, mockLogger.Object);
        var serviceUrl = new Uri("https://example.com");
        var errorMessage = "Service is down";
        var recipients = new List<string> { "test1@example.com", "test2@example.com", "test3@example.com" };
        var cancellationToken = CancellationToken.None;
        MimeMessage? capturedMessage = null;
        mockEmailService.Setup(x => x.SendMessageAsync(It.IsAny<MimeMessage>()))
            .Callback<MimeMessage>(msg => capturedMessage = msg)
            .Returns(Task.CompletedTask);

        // Act
        await service.SendAlertAsync(serviceUrl, errorMessage, recipients, cancellationToken);

        // Assert
        mockEmailService.Verify(x => x.SendMessageAsync(It.IsAny<MimeMessage>()), Times.Once);
        Assert.IsNotNull(capturedMessage);
        Assert.AreEqual(3, capturedMessage.To.Count);
        Assert.IsTrue(capturedMessage.To.Any(addr => addr.ToString().Contains("test1@example.com")));
        Assert.IsTrue(capturedMessage.To.Any(addr => addr.ToString().Contains("test2@example.com")));
        Assert.IsTrue(capturedMessage.To.Any(addr => addr.ToString().Contains("test3@example.com")));
    }

    /// <summary>
    /// Tests that SendAlertAsync completes successfully with empty recipients collection.
    /// </summary>
    [TestMethod]
    public async Task SendAlertAsync_EmptyRecipients_CompletesSuccessfully()
    {
        // Arrange
        var mockEmailService = new Mock<IEmailService>();
        var mockLogger = new Mock<ILogger<EmailAlertService>>();
        var service = new EmailAlertService(mockEmailService.Object, mockLogger.Object);
        var serviceUrl = new Uri("https://example.com");
        var errorMessage = "Service is down";
        var recipients = new List<string>();
        var cancellationToken = CancellationToken.None;
        MimeMessage? capturedMessage = null;
        mockEmailService.Setup(x => x.SendMessageAsync(It.IsAny<MimeMessage>()))
            .Callback<MimeMessage>(msg => capturedMessage = msg)
            .Returns(Task.CompletedTask);

        // Act
        await service.SendAlertAsync(serviceUrl, errorMessage, recipients, cancellationToken);

        // Assert
        mockEmailService.Verify(x => x.SendMessageAsync(It.IsAny<MimeMessage>()), Times.Once);
        Assert.IsNotNull(capturedMessage);
        Assert.AreEqual(0, capturedMessage.To.Count);
    }

    /// <summary>
    /// Tests that SendAlertAsync catches and logs exception when email service fails.
    /// </summary>
    [TestMethod]
    public async Task SendAlertAsync_EmailServiceThrowsException_CatchesAndLogsError()
    {
        // Arrange
        var mockEmailService = new Mock<IEmailService>();
        var mockLogger = new Mock<ILogger<EmailAlertService>>();
        var service = new EmailAlertService(mockEmailService.Object, mockLogger.Object);
        var serviceUrl = new Uri("https://example.com");
        var errorMessage = "Service is down";
        var recipients = new List<string> { "test@example.com" };
        var cancellationToken = CancellationToken.None;
        var expectedException = new InvalidOperationException("SMTP server not available");
        mockEmailService.Setup(x => x.SendMessageAsync(It.IsAny<MimeMessage>()))
            .ThrowsAsync(expectedException);

        // Act
        await service.SendAlertAsync(serviceUrl, errorMessage, recipients, cancellationToken);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(serviceUrl.ToString())),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that SendAlertAsync handles special characters in error message correctly.
    /// </summary>
    [TestMethod]
    public async Task SendAlertAsync_ErrorMessageWithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var mockEmailService = new Mock<IEmailService>();
        var mockLogger = new Mock<ILogger<EmailAlertService>>();
        var service = new EmailAlertService(mockEmailService.Object, mockLogger.Object);
        var serviceUrl = new Uri("https://example.com");
        var errorMessage = "Error: <html>&\"special\"</html> characters\nand\nnewlines\ttabs";
        var recipients = new List<string> { "test@example.com" };
        var cancellationToken = CancellationToken.None;
        MimeMessage? capturedMessage = null;
        mockEmailService.Setup(x => x.SendMessageAsync(It.IsAny<MimeMessage>()))
            .Callback<MimeMessage>(msg => capturedMessage = msg)
            .Returns(Task.CompletedTask);

        // Act
        await service.SendAlertAsync(serviceUrl, errorMessage, recipients, cancellationToken);

        // Assert
        mockEmailService.Verify(x => x.SendMessageAsync(It.IsAny<MimeMessage>()), Times.Once);
        Assert.IsNotNull(capturedMessage);
        var textPart = capturedMessage.Body as TextPart;
        Assert.IsNotNull(textPart);

        // Verify that special characters are preserved correctly in the email body
        Assert.Contains("<html>&\"special\"</html>", textPart.Text);
        Assert.Contains("characters", textPart.Text);
        Assert.IsTrue(textPart.Text.Contains("and"), "Newline characters should be preserved");
        Assert.IsTrue(textPart.Text.Contains("newlines"), "Newline characters should be preserved");
        Assert.IsTrue(textPart.Text.Contains("tabs"), "Tab characters should be preserved");
    }

    /// <summary>
    /// Tests that SendAlertAsync handles very long error messages correctly.
    /// </summary>
    [TestMethod]
    public async Task SendAlertAsync_VeryLongErrorMessage_HandlesCorrectly()
    {
        // Arrange
        var mockEmailService = new Mock<IEmailService>();
        var mockOptions = new Mock<IOptions<ServiceMonitorOptions>>();
        var mockLogger = new Mock<ILogger<EmailAlertService>>();
        var service = new EmailAlertService(mockEmailService.Object, mockLogger.Object);
        var serviceUrl = new Uri("https://example.com");
        var errorMessage = new string('A', 10000);
        var recipients = new List<string> { "test@example.com" };
        var cancellationToken = CancellationToken.None;
        MimeMessage? capturedMessage = null;
        mockEmailService.Setup(x => x.SendMessageAsync(It.IsAny<MimeMessage>()))
            .Callback<MimeMessage>(msg => capturedMessage = msg)
            .Returns(Task.CompletedTask);

        // Act
        await service.SendAlertAsync(serviceUrl, errorMessage, recipients, cancellationToken);

        // Assert
        mockEmailService.Verify(x => x.SendMessageAsync(It.IsAny<MimeMessage>()), Times.Once);
        Assert.IsNotNull(capturedMessage);
        var textPart = capturedMessage.Body as TextPart;
        Assert.IsNotNull(textPart);
        Assert.Contains(errorMessage, textPart.Text);
    }
}
