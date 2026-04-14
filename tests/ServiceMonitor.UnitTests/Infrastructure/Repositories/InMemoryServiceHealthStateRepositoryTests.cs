using CSharpFunctionalExtensions;
using ServiceMonitor.Domain.Entities;
using ServiceMonitor.Infrastructure.Repositories;

namespace ServiceMonitor.UnitTests.Infrastructure.Repositories;

[TestClass]
public sealed class InMemoryServiceHealthStateRepositoryTests
{
    private InMemoryServiceHealthStateRepository _sut = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _sut = new InMemoryServiceHealthStateRepository();
    }

    [TestMethod]
    public async Task GetAsync_ExistingUrl_ReturnsMaybeWithState()
    {
        // Arrange
        var url = new Uri("https://example.com");
        var state = new ServiceHealthState(url, true);
        var cancellationToken = CancellationToken.None;

        await _sut.SaveAsync(state, cancellationToken);

        // Act
        var result = await _sut.GetAsync(url, cancellationToken);

        // Assert
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(url, result.Value.Url);
        Assert.IsTrue(result.Value.IsHealthy);
    }

    [TestMethod]
    public async Task GetAsync_NonExistingUrl_ReturnsMaybeNone()
    {
        // Arrange
        var url = new Uri("https://nonexisting.com");
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _sut.GetAsync(url, cancellationToken);

        // Assert
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public async Task GetAsync_NullUrl_ThrowsArgumentNullException()
    {
        // Arrange
        Uri url = null!;
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.GetAsync(url, cancellationToken));
    }

    [TestMethod]
    public async Task SaveAsync_ValidState_ReturnsSuccess()
    {
        // Arrange
        var url = new Uri("https://example.com");
        var state = new ServiceHealthState(url, true);
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _sut.SaveAsync(state, cancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public async Task SaveAsync_ValidState_StateIsStored()
    {
        // Arrange
        var url = new Uri("https://example.com");
        var state = new ServiceHealthState(url, true);
        var cancellationToken = CancellationToken.None;

        // Act
        await _sut.SaveAsync(state, cancellationToken);
        var retrievedState = await _sut.GetAsync(url, cancellationToken);

        // Assert
        Assert.IsTrue(retrievedState.HasValue);
        Assert.AreEqual(url, retrievedState.Value.Url);
        Assert.IsTrue(retrievedState.Value.IsHealthy);
    }

    [TestMethod]
    public async Task SaveAsync_UpdateExistingState_StateIsUpdated()
    {
        // Arrange
        var url = new Uri("https://example.com");
        var initialState = new ServiceHealthState(url, true);
        var updatedState = new ServiceHealthState(url, false);
        var cancellationToken = CancellationToken.None;

        await _sut.SaveAsync(initialState, cancellationToken);

        // Act
        var result = await _sut.SaveAsync(updatedState, cancellationToken);
        var retrievedState = await _sut.GetAsync(url, cancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(retrievedState.HasValue);
        Assert.IsFalse(retrievedState.Value.IsHealthy);
    }

    [TestMethod]
    public async Task SaveAsync_NullState_ThrowsArgumentNullException()
    {
        // Arrange
        ServiceHealthState state = null!;
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.SaveAsync(state, cancellationToken));
    }
}
