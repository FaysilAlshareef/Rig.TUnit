namespace Rig.TUnit.Microservices.Inbox.Tests.Unit;

public sealed class InboxUnitTests
{
    [Test]
    public async Task TryApply_WithIncreasingSequence_ReturnsTrue()
    {
        var tracker = new SequenceTracker();

        await Assert.That(tracker.TryApply("agg", 1)).IsTrue();
        await Assert.That(tracker.TryApply("agg", 2)).IsTrue();
    }

    [Test]
    public async Task TryApply_WithDuplicateSequence_ReturnsFalse()
    {
        var tracker = new SequenceTracker();
        tracker.TryApply("agg", 5);

        await Assert.That(tracker.TryApply("agg", 5)).IsFalse();
    }

    [Test]
    public async Task TryApply_WithOutOfOrderSequence_ReturnsFalse()
    {
        var tracker = new SequenceTracker();
        tracker.TryApply("agg", 10);

        await Assert.That(tracker.TryApply("agg", 3)).IsFalse();
    }

    [Test]
    public async Task TryApply_WithNegativeSequence_ThrowsArgumentOutOfRange()
    {
        var tracker = new SequenceTracker();

        await Assert.That(() => tracker.TryApply("agg", -1))
            .ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task LastApplied_WithUntrackedAggregate_ReturnsNull()
    {
        var tracker = new SequenceTracker();

        await Assert.That(tracker.LastApplied("none")).IsNull();
    }

    [Test]
    public async Task LastApplied_AfterApply_ReturnsLatestSequence()
    {
        var tracker = new SequenceTracker();
        tracker.TryApply("agg", 7);
        tracker.TryApply("agg", 9);

        await Assert.That(tracker.LastApplied("agg")).IsEqualTo(9L);
    }

    [Test]
    public async Task InboxFixture_ConnectionString_IsInMemoryMarker()
    {
        await using var fixture = new InboxFixture();
        await Assert.That(fixture.ConnectionString).IsEqualTo("inbox-in-memory");
    }
}
