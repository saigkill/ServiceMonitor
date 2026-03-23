using FluentEmail.Core;
using FluentEmail.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceMonitor.Infrastructure.Email;


namespace ServiceMonitor.UnitTests.Infrastructure.Email;

/// <summary>
/// Tests for the EmailAlertService class.
/// </summary>
[TestClass]
public sealed class EmailAlertServiceTests
{
    /// <summary>
    /// Tests that SendAlertAsync successfully sends an email with valid inputs.
    /// </summary>
    [TestMethod]
    public async Task SendAlertAsync_ValidInputs_SendsEmailSuccessfully()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<EmailAlertService>>();

        var mockFluentEmail = new Mock<IFluentEmail>();
        var sendResponse = new SendResponse();

        mockFluentEmail.Setup(f => f.To(It.IsAny<IEnumerable<Address>>())).Returns(mockFluentEmail.Object);
        mockFluentEmail.Setup(f => f.Subject(It.IsAny<string>())).Returns(mockFluentEmail.Object);
        mockFluentEmail.Setup(f => f.Body(It.IsAny<string>())).Returns(mockFluentEmail.Object);
        mockFluentEmail.Setup(f => f.SendAsync(It.IsAny<CancellationToken?>())).ReturnsAsync(sendResponse);

        var service = new EmailAlertService(mockLogger.Object, mockFluentEmail.Object);
        var serviceUrl = new Uri("http://example.com");
        var errorMessage = "Service is down";
        var recipients = new List<string> { "test@example.com" };
        var cancellationToken = CancellationToken.None;

        // Act
        await service.SendAlertAsync(serviceUrl, errorMessage, recipients, cancellationToken);

        // Assert
        mockFluentEmail.Verify(f => f.To(It.IsAny<IEnumerable<Address>>()), Times.Exactly(2));
        mockFluentEmail.Verify(f => f.Subject($"Service not reachable: {serviceUrl}"), Times.Once);
        mockFluentEmail.Verify(f => f.Body($"The service {serviceUrl} is not reachable.\n\nError: {errorMessage}"), Times.Once);
        mockFluentEmail.Verify(f => f.SendAsync(cancellationToken), Times.Once);
    }

    /// <summary>
    /// Tests that SendAlertAsync throws ArgumentNullException when serviceUrl is null.
    /// </summary>
    [TestMethod]
    public async Task SendAlertAsync_NullServiceUrl_ThrowsArgumentNullException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<EmailAlertService>>();
        var mockFluentEmail = new Mock<IFluentEmail>();

        var service = new EmailAlertService(mockLogger.Object, mockFluentEmail.Object);
        Uri? serviceUrl = null;
        var errorMessage = "Service is down";
        var recipients = new List<string> { "test@example.com" };
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        try
        {
            await service.SendAlertAsync(serviceUrl!, errorMessage, recipients, cancellationToken);
            Assert.Fail("Expected ArgumentNullException was not thrown.");
        }
        catch (ArgumentNullException)
        {
            // Expected exception
        }
    }

    /// <summary>
    /// Tests that SendAlertAsync throws ArgumentException when errorMessage is null.
    /// </summary>
    [TestMethod]
    public async Task SendAlertAsync_NullErrorMessage_ThrowsArgumentException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<EmailAlertService>>();
        var mockFluentEmail = new Mock<IFluentEmail>();

        var service = new EmailAlertService(mockLogger.Object, mockFluentEmail.Object);
        var serviceUrl = new Uri("http://example.com");
        string? errorMessage = null;
        var recipients = new List<string> { "test@example.com" };
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        try
        {
            await service.SendAlertAsync(serviceUrl, errorMessage!, recipients, cancellationToken);
            Assert.Fail("Expected ArgumentException was not thrown.");
        }
        catch (ArgumentException)
        {
            // Expected exception
        }
    }

    /// <summary>
    /// Tests that SendAlertAsync throws ArgumentException when errorMessage is empty.
    /// </summary>
    [TestMethod]
    public async Task SendAlertAsync_EmptyErrorMessage_ThrowsArgumentException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<EmailAlertService>>();

        var mockFluentEmail = new Mock<IFluentEmail>();

        var service = new EmailAlertService(mockLogger.Object, mockFluentEmail.Object);
        var serviceUrl = new Uri("http://example.com");
        var errorMessage = string.Empty;
        var recipients = new List<string> { "test@example.com" };
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        try
        {
            await service.SendAlertAsync(serviceUrl, errorMessage, recipients, cancellationToken);
            Assert.Fail("Expected ArgumentException was not thrown.");
        }
        catch (ArgumentException)
        {
            // Expected exception
        }
    }

    /// <summary>
    /// Tests that SendAlertAsync throws ArgumentException when errorMessage is whitespace.
    /// </summary>
    [TestMethod]
    public async Task SendAlertAsync_WhitespaceErrorMessage_ThrowsArgumentException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<EmailAlertService>>();

        var mockFluentEmail = new Mock<IFluentEmail>();

        var service = new EmailAlertService(mockLogger.Object, mockFluentEmail.Object);
        var serviceUrl = new Uri("http://example.com");
        var errorMessage = "   ";
        var recipients = new List<string> { "test@example.com" };
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        try
        {
            await service.SendAlertAsync(serviceUrl, errorMessage, recipients, cancellationToken);
            Assert.Fail("Expected ArgumentException was not thrown.");
        }
        catch (ArgumentException)
        {
            // Expected exception
        }
    }

    /// <summary>
    /// Tests that SendAlertAsync throws ArgumentNullException when recipients is null.
    /// </summary>
    [TestMethod]
    public async Task SendAlertAsync_NullRecipients_ThrowsArgumentNullException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<EmailAlertService>>();

        var mockFluentEmail = new Mock<IFluentEmail>();

        var service = new EmailAlertService(mockLogger.Object, mockFluentEmail.Object);
        var serviceUrl = new Uri("http://example.com");
        var errorMessage = "Service is down";
        IEnumerable<string>? recipients = null;
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        try
        {
            await service.SendAlertAsync(serviceUrl, errorMessage, recipients!, cancellationToken);
            Assert.Fail("Expected ArgumentNullException was not thrown.");
        }
        catch (ArgumentNullException)
        {
            // Expected exception
        }
    }

    /// <summary>
    /// Tests that SendAlertAsync sends email with multiple recipients.
    /// </summary>
    [TestMethod]
    public async Task SendAlertAsync_MultipleRecipients_SendsEmailSuccessfully()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<EmailAlertService>>();

        var mockFluentEmail = new Mock<IFluentEmail>();
        var sendResponse = new SendResponse();

        mockFluentEmail.Setup(f => f.To(It.IsAny<IEnumerable<Address>>())).Returns(mockFluentEmail.Object);
        mockFluentEmail.Setup(f => f.Subject(It.IsAny<string>())).Returns(mockFluentEmail.Object);
        mockFluentEmail.Setup(f => f.Body(It.IsAny<string>())).Returns(mockFluentEmail.Object);
        mockFluentEmail.Setup(f => f.SendAsync(It.IsAny<CancellationToken?>())).ReturnsAsync(sendResponse);

        var service = new EmailAlertService(mockLogger.Object, mockFluentEmail.Object);
        var serviceUrl = new Uri("http://example.com");
        var errorMessage = "Service is down";
        var recipients = new List<string> { "test1@example.com", "test2@example.com", "test3@example.com" };
        var cancellationToken = CancellationToken.None;

        // Act
        await service.SendAlertAsync(serviceUrl, errorMessage, recipients, cancellationToken);

        // Assert
        mockFluentEmail.Verify(f => f.To(It.Is<IEnumerable<Address>>(list => list.Count() == 3)), Times.Exactly(2));
        mockFluentEmail.Verify(f => f.SendAsync(cancellationToken), Times.Once);
    }

    /// <summary>
    /// Tests that SendAlertAsync logs information after sending email.
    /// </summary>
    [TestMethod]
    public async Task SendAlertAsync_ValidInputs_LogsInformation()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<EmailAlertService>>();

        var mockFluentEmail = new Mock<IFluentEmail>();
        var sendResponse = new SendResponse();

        mockFluentEmail.Setup(f => f.To(It.IsAny<IEnumerable<Address>>())).Returns(mockFluentEmail.Object);
        mockFluentEmail.Setup(f => f.Subject(It.IsAny<string>())).Returns(mockFluentEmail.Object);
        mockFluentEmail.Setup(f => f.Body(It.IsAny<string>())).Returns(mockFluentEmail.Object);
        mockFluentEmail.Setup(f => f.SendAsync(It.IsAny<CancellationToken?>())).ReturnsAsync(sendResponse);

        var service = new EmailAlertService(mockLogger.Object, mockFluentEmail.Object);
        var serviceUrl = new Uri("http://example.com");
        var errorMessage = "Service is down";
        var recipients = new List<string> { "test@example.com" };
        var cancellationToken = CancellationToken.None;

        // Act
        await service.SendAlertAsync(serviceUrl, errorMessage, recipients, cancellationToken);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Down-Email")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that SendAlertAsync uses cancellation token correctly.
    /// </summary>
    [TestMethod]
    public async Task SendAlertAsync_WithCancellationToken_PassesTokenToSendAsync()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<EmailAlertService>>();

        var mockFluentEmail = new Mock<IFluentEmail>();
        var sendResponse = new SendResponse();

        mockFluentEmail.Setup(f => f.To(It.IsAny<IEnumerable<Address>>())).Returns(mockFluentEmail.Object);
        mockFluentEmail.Setup(f => f.Subject(It.IsAny<string>())).Returns(mockFluentEmail.Object);
        mockFluentEmail.Setup(f => f.Body(It.IsAny<string>())).Returns(mockFluentEmail.Object);
        mockFluentEmail.Setup(f => f.SendAsync(It.IsAny<CancellationToken?>())).ReturnsAsync(sendResponse);

        var service = new EmailAlertService(mockLogger.Object, mockFluentEmail.Object);
        var serviceUrl = new Uri("http://example.com");
        var errorMessage = "Service is down";
        var recipients = new List<string> { "test@example.com" };
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        // Act
        await service.SendAlertAsync(serviceUrl, errorMessage, recipients, cancellationToken);

        // Assert
        mockFluentEmail.Verify(f => f.SendAsync(cancellationToken), Times.Once);
    }
}
