using Rig.TUnit.Microservices.Outbox.Assertions;
using Rig.TUnit.Microservices.Outbox.Fixtures;

namespace Rig.TUnit.Microservices.Outbox.Tests.Unit;

public sealed class OutboxAssertDeadLetterTests
{
    private sealed record MyDeadEvent(string Id);

    // ─── InDeadLetter null guard ──────────────────────────────────────────────

    [Test]
    public async Task InDeadLetter_NullFixture_ThrowsArgumentNullException()
    {
        await Assert.That(async () => await OutboxAssert.InDeadLetter<MyDeadEvent>(null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    // ─── InDeadLetter match / no-match ───────────────────────────────────────

    [Test]
    public async Task InDeadLetter_NoMatchingEntry_ThrowsOutboxAssertionException()
    {
        // Arrange
        var fixture = new OutboxFixture();

        // Act + Assert
        await Assert.That(async () => await OutboxAssert.InDeadLetter<MyDeadEvent>(fixture))
            .ThrowsExactly<OutboxAssertionException>();
    }

    [Test]
    public async Task InDeadLetter_MatchingEntry_ReturnsDeadLetterAssertion()
    {
        // Arrange
        var fixture = new OutboxFixture();
        var eventType = typeof(MyDeadEvent).FullName!;
        fixture.PushDeadLetter(new OutboxMessage(
            Guid.NewGuid(), "agg-1", eventType, "{}", DateTimeOffset.UtcNow,
            FailureReason: "downstream failure"));

        // Act
        var assertion = await OutboxAssert.InDeadLetter<MyDeadEvent>(fixture);

        // Assert — matching reason does not throw
        await assertion.WithReason("downstream");
    }

    // ─── WithReason ───────────────────────────────────────────────────────────

    [Test]
    public async Task WithReason_NoMatchingReason_ThrowsOutboxAssertionException()
    {
        // Arrange
        var fixture = new OutboxFixture();
        var eventType = typeof(MyDeadEvent).FullName!;
        fixture.PushDeadLetter(new OutboxMessage(
            Guid.NewGuid(), "agg-1", eventType, "{}", DateTimeOffset.UtcNow,
            FailureReason: "network error"));
        var assertion = await OutboxAssert.InDeadLetter<MyDeadEvent>(fixture);

        // Act + Assert
        await Assert.That(async () => await assertion.WithReason("timeout"))
            .ThrowsExactly<OutboxAssertionException>();
    }

    [Test]
    public async Task WithReason_WhiteSpaceArgument_ThrowsArgumentException()
    {
        // Arrange
        var fixture = new OutboxFixture();
        var eventType = typeof(MyDeadEvent).FullName!;
        fixture.PushDeadLetter(new OutboxMessage(
            Guid.NewGuid(), "agg-1", eventType, "{}", DateTimeOffset.UtcNow,
            FailureReason: "some error"));
        var assertion = await OutboxAssert.InDeadLetter<MyDeadEvent>(fixture);

        // Act + Assert
        await Assert.That(async () => await assertion.WithReason("   "))
            .ThrowsExactly<ArgumentException>();
    }

    // ─── PushDeadLetter roundtrip ─────────────────────────────────────────────

    [Test]
    public async Task PushDeadLetter_AddsMessageToDeadLetterCollection()
    {
        // Arrange
        var fixture = new OutboxFixture();
        var message = new OutboxMessage(
            Guid.NewGuid(), "agg-1", "SomeEvent", "{}", DateTimeOffset.UtcNow,
            FailureReason: "processing error");

        // Act
        fixture.PushDeadLetter(message);

        // Assert
        await Assert.That(fixture.DeadLetter.Count).IsEqualTo(1);
        await Assert.That(fixture.DeadLetter.First().FailureReason).IsEqualTo("processing error");
    }
}
