using Rig.TUnit.Microservices.Inbox;
using Rig.TUnit.Microservices.Inbox.Assertions;

namespace Rig.TUnit.Microservices.Inbox.Tests.Integration;

public sealed class InboxTests
{
    [Test]
    public async Task TryApply_WithIncreasingSequences_ReturnsTrueForEach()
    {
        // Arrange
        var tracker = new SequenceTracker();

        // Act
        var r1 = tracker.TryApply("A", 1);
        var r2 = tracker.TryApply("A", 2);
        var r3 = tracker.TryApply("A", 3);

        // Assert
        await Assert.That(r1).IsTrue();
        await Assert.That(r2).IsTrue();
        await Assert.That(r3).IsTrue();
    }

    [Test]
    public async Task TryApply_WithDuplicateSequence_ReturnsFalse()
    {
        // Arrange
        var tracker = new SequenceTracker();
        tracker.TryApply("A", 5);

        // Act
        var dup = tracker.TryApply("A", 5);

        // Assert
        await Assert.That(dup).IsFalse();
    }

    [Test]
    public async Task TryApply_WithLowerSequence_ReturnsFalse()
    {
        // Arrange
        var tracker = new SequenceTracker();
        tracker.TryApply("A", 5);

        // Act
        var result = tracker.TryApply("A", 4);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task TryApply_AcrossDifferentAggregates_TracksIndependently()
    {
        // Arrange
        var tracker = new SequenceTracker();
        tracker.TryApply("A", 10);

        // Act
        var bFirst = tracker.TryApply("B", 1);

        // Assert
        await Assert.That(bFirst).IsTrue();
        await Assert.That(tracker.LastApplied("A")).IsEqualTo(10L);
        await Assert.That(tracker.LastApplied("B")).IsEqualTo(1L);
    }

    [Test]
    public async Task Idempotent_WhenCalledWithPreviouslyAppliedSequence_Passes()
    {
        // Arrange
        var tracker = new SequenceTracker();
        tracker.TryApply("A", 7);

        // Act
        InboxAssert.SequenceApplied(tracker, "A", 7).Idempotent();

        // Assert
        await Assert.That(tracker.LastApplied("A")).IsEqualTo(7L);
    }

    [Test]
    public async Task SequenceApplied_WhenNeverApplied_ThrowsInboxAssertionException()
    {
        // Arrange
        var tracker = new SequenceTracker();

        // Act
        async Task Action() { InboxAssert.SequenceApplied(tracker, "A", 1); await Task.CompletedTask; }

        // Assert
        await Assert.ThrowsAsync<InboxAssertionException>(Action);
    }

    [Test]
    public async Task TryApply_With100ConcurrentCallers_RetainsHighestSequence()
    {
        // Arrange
        var tracker = new SequenceTracker();

        // Act
        var tasks = Enumerable.Range(1, 100).Select(i =>
            Task.Run(() => tracker.TryApply("A", i))).ToArray();
        await Task.WhenAll(tasks);

        // Assert
        await Assert.That(tracker.LastApplied("A")).IsEqualTo(100L);
    }
}
