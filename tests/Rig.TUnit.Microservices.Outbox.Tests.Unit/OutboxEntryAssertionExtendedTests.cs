using Rig.TUnit.Microservices.Outbox.Assertions;
using Rig.TUnit.Microservices.Outbox.Fixtures;

namespace Rig.TUnit.Microservices.Outbox.Tests.Unit;

public sealed class OutboxEntryAssertionExtendedTests
{
    private sealed record SomeEvent(string Data);

    private static async Task<OutboxFixture> FixtureWithMessageAsync(
        string aggregateId = "agg-1",
        DateTimeOffset? relayedAt = null)
    {
        var fixture = new OutboxFixture();
        var eventType = typeof(SomeEvent).FullName!;
        var msg = new OutboxMessage(
            Guid.NewGuid(), aggregateId, eventType, "{}", DateTimeOffset.UtcNow,
            RelayedAt: relayedAt);
        await fixture.Store.EnqueueAsync(msg);
        return fixture;
    }

    // ─── WithAggregateId ─────────────────────────────────────────────────────

    [Test]
    public async Task WithAggregateId_MatchingId_EntryFoundExactlyOnce()
    {
        // Arrange
        var fixture = await FixtureWithMessageAsync("order-123");

        // Act + Assert
        await OutboxAssert.Contains<SomeEvent>(fixture)
            .WithAggregateId("order-123")
            .ExactlyOnce();
    }

    [Test]
    public async Task WithAggregateId_NonMatchingId_ThrowsOutboxAssertionException()
    {
        // Arrange
        var fixture = await FixtureWithMessageAsync("order-123");

        // Act + Assert
        await Assert.That(() =>
                OutboxAssert.Contains<SomeEvent>(fixture)
                    .WithAggregateId("other-id")
                    .ExactlyOnce())
            .ThrowsExactly<OutboxAssertionException>();
    }

    // ─── OnTopic ─────────────────────────────────────────────────────────────

    [Test]
    public async Task OnTopic_MatchingEventTypeFragment_EntryFoundExactlyOnce()
    {
        // Arrange — event type contains "SomeEvent", so topic fragment matches
        var fixture = await FixtureWithMessageAsync();

        // Act + Assert
        await OutboxAssert.Contains<SomeEvent>(fixture)
            .OnTopic("SomeEvent")
            .ExactlyOnce();
    }

    [Test]
    public async Task OnTopic_NonMatchingFragment_ThrowsOutboxAssertionException()
    {
        // Arrange
        var fixture = await FixtureWithMessageAsync();

        // Act + Assert
        await Assert.That(() =>
                OutboxAssert.Contains<SomeEvent>(fixture)
                    .OnTopic("NoSuchTopic")
                    .ExactlyOnce())
            .ThrowsExactly<OutboxAssertionException>();
    }

    // ─── Relayed ─────────────────────────────────────────────────────────────

    [Test]
    public async Task Relayed_WithRelayedRow_DoesNotThrow()
    {
        // Arrange
        var fixture = await FixtureWithMessageAsync(relayedAt: DateTimeOffset.UtcNow);

        // Act + Assert — should complete without throwing
        await OutboxAssert.Contains<SomeEvent>(fixture).Relayed();
    }

    [Test]
    public async Task Relayed_WithPendingRow_ThrowsOutboxAssertionException()
    {
        // Arrange
        var fixture = await FixtureWithMessageAsync(); // RelayedAt is null

        // Act + Assert — wrap in async lambda to discard Task<T> → Task for ThrowsExactly overload
        await Assert.That(async () => await OutboxAssert.Contains<SomeEvent>(fixture).Relayed())
            .ThrowsExactly<OutboxAssertionException>();
    }
}
