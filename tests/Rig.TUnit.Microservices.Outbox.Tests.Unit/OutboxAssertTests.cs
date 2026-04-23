using Rig.TUnit.Microservices.Outbox.Assertions;
using Rig.TUnit.Microservices.Outbox.Fixtures;

namespace Rig.TUnit.Microservices.Outbox.Tests.Unit;

public sealed class OutboxAssertTests
{
    private sealed record SampleOutboxEvent(string Id);

    [Test]
    public async Task Contains_NullFixture_ThrowsArgumentNullException()
    {
        await Assert.That(() => OutboxAssert.Contains<SampleOutboxEvent>(null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Contains_ExactlyOnce_WithMatchingEntry_DoesNotThrow()
    {
        var fixture = new OutboxFixture();
        var eventType = typeof(SampleOutboxEvent).FullName!;
        await fixture.Store.EnqueueAsync(
            new OutboxMessage(Guid.NewGuid(), "agg-1", eventType, "{}", DateTimeOffset.UtcNow));

        await OutboxAssert.Contains<SampleOutboxEvent>(fixture).ExactlyOnce();
    }

    [Test]
    public async Task Contains_ExactlyOnce_WithNoMatchingEntry_ThrowsOutboxAssertionException()
    {
        var fixture = new OutboxFixture();

        await Assert.That(() => OutboxAssert.Contains<SampleOutboxEvent>(fixture).ExactlyOnce())
            .ThrowsExactly<OutboxAssertionException>();
    }
}
