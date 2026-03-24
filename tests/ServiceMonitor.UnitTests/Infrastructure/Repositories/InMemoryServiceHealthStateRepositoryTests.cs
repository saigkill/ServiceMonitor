using ServiceMonitor.Domain.Entities;
using ServiceMonitor.Infrastructure.Repositories;

namespace ServiceMonitor.UnitTests;

/// <summary>
/// Unit tests for the <see cref="InMemoryServiceHealthStateRepository"/> class.
/// </summary>
[TestClass]
public sealed class InMemoryServiceHealthStateRepositoryTests
{
    /// <summary>
    /// Tests that GetAsync returns null when the URL does not exist in the repository.
    /// </summary>
    [TestMethod]
    public async Task GetAsync_NonExistentUrl_ReturnsNull()
    {
        // Arrange
        var repository = new InMemoryServiceHealthStateRepository();
        var url = new Uri("https://example.com");
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await repository.GetAsync(url, cancellationToken);

        // Assert
        Assert.IsNull(result);
    }

    /// <summary>
    /// Tests that GetAsync throws ArgumentNullException when the URL is null.
    /// </summary>
    [TestMethod]
    public async Task GetAsync_NullUrl_ThrowsArgumentNullException()
    {
        // Arrange
        var repository = new InMemoryServiceHealthStateRepository();
        Uri? url = null;
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await repository.GetAsync(url!, cancellationToken));
    }

    /// <summary>
    /// Tests that GetAsync returns the saved state when the URL exists in the repository.
    /// </summary>
    [TestMethod]
    public async Task GetAsync_ExistingUrl_ReturnsSavedState()
    {
        // Arrange
        var repository = new InMemoryServiceHealthStateRepository();
        var url = new Uri("https://example.com");
        var state = new ServiceHealthState(url, true);
        var cancellationToken = CancellationToken.None;

        await repository.SaveAsync(state, cancellationToken);

        // Act
        var result = await repository.GetAsync(url, cancellationToken);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(url, result.Url);
        Assert.AreEqual(state.IsHealthy, result.IsHealthy);
    }

    /// <summary>
    /// Tests that GetAsync returns the most recently saved state when the same URL is saved multiple times.
    /// </summary>
    [TestMethod]
    public async Task GetAsync_MultipleSavesForSameUrl_ReturnsLatestState()
    {
        // Arrange
        var repository = new InMemoryServiceHealthStateRepository();
        var url = new Uri("https://example.com");
        var firstState = new ServiceHealthState(url, true);
        var secondState = new ServiceHealthState(url, false);
        var cancellationToken = CancellationToken.None;

        await repository.SaveAsync(firstState, cancellationToken);
        await repository.SaveAsync(secondState, cancellationToken);

        // Act
        var result = await repository.GetAsync(url, cancellationToken);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(url, result.Url);
        Assert.IsFalse(result.IsHealthy);
    }

    /// <summary>
    /// Tests that SaveAsync stores the state correctly.
    /// </summary>
    [TestMethod]
    public async Task SaveAsync_ValidState_StoresStateSuccessfully()
    {
        // Arrange
        var repository = new InMemoryServiceHealthStateRepository();
        var url = new Uri("https://example.com");
        var state = new ServiceHealthState(url, true);
        var cancellationToken = CancellationToken.None;

        // Act
        await repository.SaveAsync(state, cancellationToken);

        // Assert
        var result = await repository.GetAsync(url, cancellationToken);
        Assert.IsNotNull(result);
        Assert.AreEqual(state.Url, result.Url);
        Assert.AreEqual(state.IsHealthy, result.IsHealthy);
    }

    /// <summary>
    /// Tests that SaveAsync throws ArgumentNullException when the state is null.
    /// </summary>
    [TestMethod]
    public async Task SaveAsync_NullState_ThrowsArgumentNullException()
    {
        // Arrange
        var repository = new InMemoryServiceHealthStateRepository();
        ServiceHealthState? state = null;
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await repository.SaveAsync(state!, cancellationToken));
    }

    /// <summary>
    /// Tests that SaveAsync overwrites the existing state when called with the same URL.
    /// </summary>
    [TestMethod]
    public async Task SaveAsync_ExistingUrl_OverwritesState()
    {
        // Arrange
        var repository = new InMemoryServiceHealthStateRepository();
        var url = new Uri("https://example.com");
        var firstState = new ServiceHealthState(url, true);
        var secondState = new ServiceHealthState(url, false);
        var cancellationToken = CancellationToken.None;

        // Act
        await repository.SaveAsync(firstState, cancellationToken);
        await repository.SaveAsync(secondState, cancellationToken);

        // Assert
        var result = await repository.GetAsync(url, cancellationToken);
        Assert.IsNotNull(result);
        Assert.IsFalse(result.IsHealthy);
    }

    /// <summary>
    /// Tests that SaveAsync can store multiple states with different URLs.
    /// </summary>
    [TestMethod]
    public async Task SaveAsync_MultipleUrls_StoresAllStatesIndependently()
    {
        // Arrange
        var repository = new InMemoryServiceHealthStateRepository();
        var url1 = new Uri("https://example1.com");
        var url2 = new Uri("https://example2.com");
        var state1 = new ServiceHealthState(url1, true);
        var state2 = new ServiceHealthState(url2, false);
        var cancellationToken = CancellationToken.None;

        // Act
        await repository.SaveAsync(state1, cancellationToken);
        await repository.SaveAsync(state2, cancellationToken);

        // Assert
        var result1 = await repository.GetAsync(url1, cancellationToken);
        var result2 = await repository.GetAsync(url2, cancellationToken);

        Assert.IsNotNull(result1);
        Assert.IsTrue(result1.IsHealthy);
        Assert.AreEqual(url1, result1.Url);

        Assert.IsNotNull(result2);
        Assert.IsFalse(result2.IsHealthy);
        Assert.AreEqual(url2, result2.Url);
    }

    /// <summary>
    /// Tests that GetAsync handles various URI schemes correctly.
    /// </summary>
    /// <param name="uriString">The URI string to test.</param>
    [TestMethod]
    [DataRow("https://example.com")]
    [DataRow("http://example.com")]
    [DataRow("https://example.com:8080/path?query=value")]
    [DataRow("https://localhost")]
    [DataRow("http://192.168.1.1")]
    [DataRow("https://[::1]")]
    public async Task GetAsync_VariousUriFormats_HandlesCorrectly(string uriString)
    {
        // Arrange
        var repository = new InMemoryServiceHealthStateRepository();
        var url = new Uri(uriString);
        var state = new ServiceHealthState(url, true);
        var cancellationToken = CancellationToken.None;

        await repository.SaveAsync(state, cancellationToken);

        // Act
        var result = await repository.GetAsync(url, cancellationToken);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(url, result.Url);
    }

    /// <summary>
    /// Tests that SaveAsync completes successfully with a cancelled cancellation token.
    /// </summary>
    [TestMethod]
    public async Task SaveAsync_CancelledToken_CompletesSuccessfully()
    {
        // Arrange
        var repository = new InMemoryServiceHealthStateRepository();
        var url = new Uri("https://example.com");
        var state = new ServiceHealthState(url, true);
        var cancellationToken = new CancellationToken(canceled: true);

        // Act
        await repository.SaveAsync(state, cancellationToken);

        // Assert
        var result = await repository.GetAsync(url, CancellationToken.None);
        Assert.IsNotNull(result);
        Assert.AreEqual(url, result.Url);
    }

    /// <summary>
    /// Tests that GetAsync completes successfully with a cancelled cancellation token.
    /// </summary>
    [TestMethod]
    public async Task GetAsync_CancelledToken_CompletesSuccessfully()
    {
        // Arrange
        var repository = new InMemoryServiceHealthStateRepository();
        var url = new Uri("https://example.com");
        var state = new ServiceHealthState(url, true);
        var cancellationToken = new CancellationToken(canceled: true);

        await repository.SaveAsync(state, CancellationToken.None);

        // Act
        var result = await repository.GetAsync(url, cancellationToken);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(url, result.Url);
    }

    /// <summary>
    /// Tests that SaveAsync handles healthy and unhealthy states correctly.
    /// </summary>
    /// <param name="isHealthy">The health status to test.</param>
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task SaveAsync_VariousHealthStates_StoresCorrectly(bool isHealthy)
    {
        // Arrange
        var repository = new InMemoryServiceHealthStateRepository();
        var url = new Uri("https://example.com");
        var state = new ServiceHealthState(url, isHealthy);
        var cancellationToken = CancellationToken.None;

        // Act
        await repository.SaveAsync(state, cancellationToken);

        // Assert
        var result = await repository.GetAsync(url, cancellationToken);
        Assert.IsNotNull(result);
        Assert.AreEqual(isHealthy, result.IsHealthy);
    }
}
