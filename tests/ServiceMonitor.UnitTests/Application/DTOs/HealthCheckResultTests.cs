using System.Net;

namespace ServiceMonitor.Application.DTOs.UnitTests;


/// <summary>
/// Unit tests for the <see cref="HealthCheckResult"/> class.
/// </summary>
[TestClass]
public sealed class HealthCheckResultTests
{
    /// <summary>
    /// Tests that Unhealthy creates a result with all properties set correctly when provided with valid inputs and a status code.
    /// </summary>
    [TestMethod]
    public void Unhealthy_ValidInputsWithStatusCode_ReturnsCorrectResult()
    {
        // Arrange
        var serviceUrl = new Uri("https://example.com");
        var errorMessage = "Service is down";
        var statusCode = HttpStatusCode.InternalServerError;
        var beforeCall = DateTime.UtcNow;

        // Act
        var result = HealthCheckResult.Unhealthy(serviceUrl, errorMessage, statusCode);
        var afterCall = DateTime.UtcNow;

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(serviceUrl, result.ServiceUrl);
        Assert.IsFalse(result.IsHealthy);
        Assert.AreEqual(statusCode, result.StatusCode);
        Assert.AreEqual(errorMessage, result.ErrorMessage);
        Assert.IsTrue(result.CheckedAt >= beforeCall && result.CheckedAt <= afterCall,
            $"CheckedAt should be between {beforeCall} and {afterCall}, but was {result.CheckedAt}");
    }

    /// <summary>
    /// Tests that Unhealthy creates a result with StatusCode set to null when not provided.
    /// </summary>
    [TestMethod]
    public void Unhealthy_ValidInputsWithoutStatusCode_ReturnsResultWithNullStatusCode()
    {
        // Arrange
        var serviceUrl = new Uri("https://example.com");
        var errorMessage = "Connection timeout";
        var beforeCall = DateTime.UtcNow;

        // Act
        var result = HealthCheckResult.Unhealthy(serviceUrl, errorMessage);
        var afterCall = DateTime.UtcNow;

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(serviceUrl, result.ServiceUrl);
        Assert.IsFalse(result.IsHealthy);
        Assert.IsNull(result.StatusCode);
        Assert.AreEqual(errorMessage, result.ErrorMessage);
        Assert.IsTrue(result.CheckedAt >= beforeCall && result.CheckedAt <= afterCall,
            $"CheckedAt should be between {beforeCall} and {afterCall}, but was {result.CheckedAt}");
    }

    /// <summary>
    /// Tests that Unhealthy handles various HttpStatusCode values correctly.
    /// </summary>
    /// <param name="statusCode">The HTTP status code to test.</param>
    [TestMethod]
    [DataRow(HttpStatusCode.BadRequest)]
    [DataRow(HttpStatusCode.Unauthorized)]
    [DataRow(HttpStatusCode.Forbidden)]
    [DataRow(HttpStatusCode.NotFound)]
    [DataRow(HttpStatusCode.InternalServerError)]
    [DataRow(HttpStatusCode.BadGateway)]
    [DataRow(HttpStatusCode.ServiceUnavailable)]
    [DataRow(HttpStatusCode.GatewayTimeout)]
    public void Unhealthy_VariousStatusCodes_SetsStatusCodeCorrectly(HttpStatusCode statusCode)
    {
        // Arrange
        var serviceUrl = new Uri("https://example.com");
        var errorMessage = "Error occurred";

        // Act
        var result = HealthCheckResult.Unhealthy(serviceUrl, errorMessage, statusCode);

        // Assert
        Assert.AreEqual(statusCode, result.StatusCode);
        Assert.IsFalse(result.IsHealthy);
    }

    /// <summary>
    /// Tests that Unhealthy handles a very long error message correctly.
    /// </summary>
    [TestMethod]
    public void Unhealthy_VeryLongErrorMessage_SetsErrorMessageCorrectly()
    {
        // Arrange
        var serviceUrl = new Uri("https://example.com");
        var errorMessage = new string('x', 10000);

        // Act
        var result = HealthCheckResult.Unhealthy(serviceUrl, errorMessage);

        // Assert
        Assert.AreEqual(errorMessage, result.ErrorMessage);
        Assert.IsFalse(result.IsHealthy);
    }

    /// <summary>
    /// Tests that Unhealthy handles whitespace-only error messages correctly.
    /// </summary>
    [TestMethod]
    public void Unhealthy_WhitespaceErrorMessage_ReturnsResult()
    {
        // Arrange
        var serviceUrl = new Uri("https://example.com");
        var errorMessage = "   ";

        // Act
        var result = HealthCheckResult.Unhealthy(serviceUrl, errorMessage);

        // Assert
        Assert.AreEqual(errorMessage, result.ErrorMessage);
        Assert.IsFalse(result.IsHealthy);
    }

    /// <summary>
    /// Tests that Unhealthy handles error messages with special characters correctly.
    /// </summary>
    [TestMethod]
    public void Unhealthy_ErrorMessageWithSpecialCharacters_SetsErrorMessageCorrectly()
    {
        // Arrange
        var serviceUrl = new Uri("https://example.com");
        var errorMessage = "Error: <script>alert('test')</script>\n\r\t\0";

        // Act
        var result = HealthCheckResult.Unhealthy(serviceUrl, errorMessage);

        // Assert
        Assert.AreEqual(errorMessage, result.ErrorMessage);
        Assert.IsFalse(result.IsHealthy);
    }

    /// <summary>
    /// Tests that Unhealthy handles various URI schemes correctly.
    /// </summary>
    /// <param name="uriString">The URI string to test.</param>
    [TestMethod]
    [DataRow("https://example.com")]
    [DataRow("http://example.com")]
    [DataRow("https://example.com:8080/path?query=value")]
    [DataRow("https://localhost")]
    [DataRow("http://192.168.1.1")]
    [DataRow("https://[::1]")]
    public void Unhealthy_VariousUriFormats_SetsServiceUrlCorrectly(string uriString)
    {
        // Arrange
        var serviceUrl = new Uri(uriString);
        var errorMessage = "Error";

        // Act
        var result = HealthCheckResult.Unhealthy(serviceUrl, errorMessage);

        // Assert
        Assert.AreEqual(serviceUrl, result.ServiceUrl);
        Assert.IsFalse(result.IsHealthy);
    }

    /// <summary>
    /// Tests that Unhealthy always sets IsHealthy to false regardless of inputs.
    /// </summary>
    [TestMethod]
    public void Unhealthy_AnyValidInputs_AlwaysSetsIsHealthyToFalse()
    {
        // Arrange
        var serviceUrl = new Uri("https://example.com");
        var errorMessage = "Error";

        // Act
        var resultWithStatusCode = HealthCheckResult.Unhealthy(serviceUrl, errorMessage, HttpStatusCode.OK);
        var resultWithoutStatusCode = HealthCheckResult.Unhealthy(serviceUrl, errorMessage);

        // Assert
        Assert.IsFalse(resultWithStatusCode.IsHealthy);
        Assert.IsFalse(resultWithoutStatusCode.IsHealthy);
    }

    /// <summary>
    /// Tests that Unhealthy sets CheckedAt to a recent DateTime.UtcNow value.
    /// </summary>
    [TestMethod]
    public void Unhealthy_AnyValidInputs_SetsCheckedAtToRecentUtcTime()
    {
        // Arrange
        var serviceUrl = new Uri("https://example.com");
        var errorMessage = "Error";
        var beforeCall = DateTime.UtcNow;

        // Act
        var result = HealthCheckResult.Unhealthy(serviceUrl, errorMessage);

        // Assert
        var afterCall = DateTime.UtcNow;
        var timeDifference = afterCall - beforeCall;
        Assert.IsLessThan(1, timeDifference.TotalSeconds,
            "CheckedAt should be set within 1 second of the method call");
        Assert.IsTrue(result.CheckedAt >= beforeCall && result.CheckedAt <= afterCall,
            $"CheckedAt should be between {beforeCall} and {afterCall}, but was {result.CheckedAt}");
    }

    /// <summary>
    /// Tests that Unhealthy with explicit null statusCode behaves the same as omitting it.
    /// </summary>
    [TestMethod]
    public void Unhealthy_ExplicitNullStatusCode_ReturnsResultWithNullStatusCode()
    {
        // Arrange
        var serviceUrl = new Uri("https://example.com");
        var errorMessage = "Error";

        // Act
        var result = HealthCheckResult.Unhealthy(serviceUrl, errorMessage, null);

        // Assert
        Assert.IsNull(result.StatusCode);
        Assert.IsFalse(result.IsHealthy);
    }

    /// <summary>
    /// Tests that Unhealthy handles undefined HttpStatusCode values correctly.
    /// </summary>
    [TestMethod]
    public void Unhealthy_UndefinedHttpStatusCode_SetsStatusCodeCorrectly()
    {
        // Arrange
        var serviceUrl = new Uri("https://example.com");
        var errorMessage = "Error";
        var undefinedStatusCode = (HttpStatusCode)999;

        // Act
        var result = HealthCheckResult.Unhealthy(serviceUrl, errorMessage, undefinedStatusCode);

        // Assert
        Assert.AreEqual(undefinedStatusCode, result.StatusCode);
        Assert.IsFalse(result.IsHealthy);
    }

    /// <summary>
    /// Tests that Healthy returns a correct HealthCheckResult with valid inputs.
    /// Validates all properties including ServiceUrl, IsHealthy, StatusCode, ErrorMessage, and CheckedAt.
    /// </summary>
    /// <param name="url">The service URL to test.</param>
    /// <param name="statusCode">The HTTP status code to test.</param>
    [TestMethod]
    [DataRow("https://example.com", HttpStatusCode.OK)]
    [DataRow("http://example.com", HttpStatusCode.Created)]
    [DataRow("https://example.com:8080/api/health", HttpStatusCode.Accepted)]
    [DataRow("http://localhost", HttpStatusCode.NoContent)]
    [DataRow("https://example.com/path/to/resource", HttpStatusCode.Continue)]
    [DataRow("ftp://example.com", HttpStatusCode.SwitchingProtocols)]
    [DataRow("https://192.168.1.1", HttpStatusCode.MovedPermanently)]
    [DataRow("https://example.com", HttpStatusCode.BadRequest)]
    [DataRow("https://example.com", HttpStatusCode.InternalServerError)]
    public void Healthy_ValidInputs_ReturnsCorrectHealthCheckResult(string url, HttpStatusCode statusCode)
    {
        // Arrange
        Uri serviceUrl = new Uri(url);
        DateTime beforeCall = DateTime.UtcNow;

        // Act
        HealthCheckResult result = HealthCheckResult.Healthy(serviceUrl, statusCode);
        DateTime afterCall = DateTime.UtcNow;

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(serviceUrl, result.ServiceUrl);
        Assert.IsTrue(result.IsHealthy);
        Assert.AreEqual(statusCode, result.StatusCode);
        Assert.IsNull(result.ErrorMessage);
        Assert.IsTrue(result.CheckedAt >= beforeCall && result.CheckedAt <= afterCall,
            $"CheckedAt should be between {beforeCall} and {afterCall}, but was {result.CheckedAt}");
    }

    /// <summary>
    /// Tests that Healthy always sets IsHealthy to true regardless of the status code value.
    /// </summary>
    [TestMethod]
    public void Healthy_AnyValidStatusCode_IsHealthyIsTrue()
    {
        // Arrange
        Uri serviceUrl = new Uri("https://example.com");
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError;

        // Act
        HealthCheckResult result = HealthCheckResult.Healthy(serviceUrl, statusCode);

        // Assert
        Assert.IsTrue(result.IsHealthy);
    }

    /// <summary>
    /// Tests that Healthy always sets ErrorMessage to null.
    /// </summary>
    [TestMethod]
    public void Healthy_AnyValidInputs_ErrorMessageIsNull()
    {
        // Arrange
        Uri serviceUrl = new Uri("https://example.com");
        HttpStatusCode statusCode = HttpStatusCode.OK;

        // Act
        HealthCheckResult result = HealthCheckResult.Healthy(serviceUrl, statusCode);

        // Assert
        Assert.IsNull(result.ErrorMessage);
    }

    /// <summary>
    /// Tests that Healthy sets CheckedAt to a timestamp very close to DateTime.UtcNow.
    /// </summary>
    [TestMethod]
    public void Healthy_ValidInputs_CheckedAtIsRecentUtcTime()
    {
        // Arrange
        Uri serviceUrl = new Uri("https://example.com");
        HttpStatusCode statusCode = HttpStatusCode.OK;
        DateTime beforeCall = DateTime.UtcNow;

        // Act
        HealthCheckResult result = HealthCheckResult.Healthy(serviceUrl, statusCode);
        DateTime afterCall = DateTime.UtcNow;

        // Assert
        Assert.IsTrue(result.CheckedAt >= beforeCall);
        Assert.IsTrue(result.CheckedAt <= afterCall);
        Assert.AreEqual(DateTimeKind.Utc, result.CheckedAt.Kind);
    }

    /// <summary>
    /// Tests that Healthy correctly handles various URI schemes including http, https, and ftp.
    /// </summary>
    /// <param name="uriString">The URI string to test.</param>
    [TestMethod]
    [DataRow("http://example.com")]
    [DataRow("https://example.com")]
    [DataRow("ftp://example.com")]
    [DataRow("https://subdomain.example.com:8080/path?query=value")]
    [DataRow("http://192.168.0.1")]
    [DataRow("https://[::1]")]
    public void Healthy_DifferentUriSchemes_PreservesServiceUrl(string uriString)
    {
        // Arrange
        Uri serviceUrl = new Uri(uriString);
        HttpStatusCode statusCode = HttpStatusCode.OK;

        // Act
        HealthCheckResult result = HealthCheckResult.Healthy(serviceUrl, statusCode);

        // Assert
        Assert.AreEqual(serviceUrl, result.ServiceUrl);
        Assert.AreEqual(serviceUrl.AbsoluteUri, result.ServiceUrl.AbsoluteUri);
    }

    /// <summary>
    /// Tests that Healthy handles boundary values for HttpStatusCode enum.
    /// Tests minimum valid value (Continue = 100).
    /// </summary>
    [TestMethod]
    public void Healthy_MinimumValidHttpStatusCode_ReturnsCorrectResult()
    {
        // Arrange
        Uri serviceUrl = new Uri("https://example.com");
        HttpStatusCode statusCode = HttpStatusCode.Continue; // 100

        // Act
        HealthCheckResult result = HealthCheckResult.Healthy(serviceUrl, statusCode);

        // Assert
        Assert.AreEqual(statusCode, result.StatusCode);
        Assert.IsTrue(result.IsHealthy);
    }

    /// <summary>
    /// Tests that Healthy handles maximum defined HttpStatusCode enum values.
    /// Tests a high status code value (HttpUpgradeRequired = 426).
    /// </summary>
    [TestMethod]
    public void Healthy_HighHttpStatusCode_ReturnsCorrectResult()
    {
        // Arrange
        Uri serviceUrl = new Uri("https://example.com");
        HttpStatusCode statusCode = (HttpStatusCode)599; // Maximum typical HTTP status code

        // Act
        HealthCheckResult result = HealthCheckResult.Healthy(serviceUrl, statusCode);

        // Assert
        Assert.AreEqual(statusCode, result.StatusCode);
        Assert.IsTrue(result.IsHealthy);
    }
}
