using Rig.TUnit.Microservices.Outbox.Fixtures;
using Rig.TUnit.Microservices.Outbox.Simulators;

namespace Rig.TUnit.Microservices.Outbox.Tests.Unit;

public sealed class OutboxRelaySimulatorTests
{
    private static OutboxMessage CreateMessage(string eventType = "TestEvent") =>
        new(Guid.NewGuid(), "agg-1", eventType, "{}", DateTimeOffset.UtcNow);

    // ─── Constructor guards ───────────────────────────────────────────────────

    [Test]
    public async Task OutboxRelaySimulator_NullStore_ThrowsArgumentNullException()
    {
        await Assert.That(() => new OutboxRelaySimulator(null!, (_, _) => Task.CompletedTask))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task OutboxRelaySimulator_NullPublish_ThrowsArgumentNullException()
    {
        // Arrange
        var fixture = new OutboxFixture();

        // Act + Assert
        await Assert.That(() => new OutboxRelaySimulator(fixture.Store, null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    // ─── DrainAsync — happy paths ─────────────────────────────────────────────

    [Test]
    public async Task DrainAsync_EmptyStore_ReturnsZero()
    {
        // Arrange
        var fixture = new OutboxFixture();
        var simulator = new OutboxRelaySimulator(fixture.Store, (_, _) => Task.CompletedTask);

        // Act
        var count = await simulator.DrainAsync();

        // Assert
        await Assert.That(count).IsEqualTo(0);
    }

    [Test]
    public async Task DrainAsync_SuccessfulPublish_ReturnsOneAndMarksRowRelayed()
    {
        // Arrange
        var fixture = new OutboxFixture();
        await fixture.Store.EnqueueAsync(CreateMessage());
        var simulator = new OutboxRelaySimulator(fixture.Store, (_, _) => Task.CompletedTask);

        // Act
        var count = await simulator.DrainAsync();

        // Assert
        await Assert.That(count).IsEqualTo(1);
        var all = await fixture.Store.ReadAllAsync();
        await Assert.That(all[0].RelayedAt).IsNotNull();
    }

    // ─── DrainAsync — transient failure capture ───────────────────────────────

    [Test]
    public async Task DrainAsync_PublishThrowsInvalidOperationException_MarksRowFailed()
    {
        // Arrange
        var fixture = new OutboxFixture();
        await fixture.Store.EnqueueAsync(CreateMessage());
        var simulator = new OutboxRelaySimulator(fixture.Store,
            async (_, _) => { await Task.CompletedTask; throw new InvalidOperationException("unavailable"); });

        // Act
        var count = await simulator.DrainAsync();

        // Assert
        await Assert.That(count).IsEqualTo(0);
        var all = await fixture.Store.ReadAllAsync();
        await Assert.That(all[0].FailureReason).IsNotNull();
    }

    [Test]
    public async Task DrainAsync_PublishThrowsTimeoutException_MarksRowFailed()
    {
        // Arrange
        var fixture = new OutboxFixture();
        await fixture.Store.EnqueueAsync(CreateMessage());
        var simulator = new OutboxRelaySimulator(fixture.Store,
            async (_, _) => { await Task.CompletedTask; throw new TimeoutException("timed out"); });

        // Act
        var count = await simulator.DrainAsync();

        // Assert
        await Assert.That(count).IsEqualTo(0);
        var all = await fixture.Store.ReadAllAsync();
        await Assert.That(all[0].FailureReason).IsNotNull();
    }

    [Test]
    public async Task DrainAsync_PublishThrowsHttpRequestException_MarksRowFailed()
    {
        // Arrange
        var fixture = new OutboxFixture();
        await fixture.Store.EnqueueAsync(CreateMessage());
        var simulator = new OutboxRelaySimulator(fixture.Store,
            async (_, _) => { await Task.CompletedTask; throw new HttpRequestException("network error"); });

        // Act
        var count = await simulator.DrainAsync();

        // Assert
        await Assert.That(count).IsEqualTo(0);
        var all = await fixture.Store.ReadAllAsync();
        await Assert.That(all[0].FailureReason).IsNotNull();
    }

    [Test]
    public async Task DrainAsync_PublishThrowsIOException_MarksRowFailed()
    {
        // Arrange
        var fixture = new OutboxFixture();
        await fixture.Store.EnqueueAsync(CreateMessage());
        var simulator = new OutboxRelaySimulator(fixture.Store,
            async (_, _) => { await Task.CompletedTask; throw new IOException("disk error"); });

        // Act
        var count = await simulator.DrainAsync();

        // Assert
        await Assert.That(count).IsEqualTo(0);
        var all = await fixture.Store.ReadAllAsync();
        await Assert.That(all[0].FailureReason).IsNotNull();
    }
}
