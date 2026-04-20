using System.Net;
using Rig.TUnit.Concurrency.Assertions;

namespace Rig.TUnit.Concurrency.Tests.Unit;

/// <summary>
/// Unit coverage for concurrency assertion helpers — no external infrastructure.
/// </summary>
public sealed class ConcurrencyAssertTests
{
    [Test]
    public async Task OneWinsWith_PassesWhenExactlyOneWriterThrows()
    {
        var asserter = ConcurrencyAssert.TwoWriters(new object());

        await asserter.OneWinsWith<InvalidOperationException>(
            writerA: _ => Task.CompletedTask,
            writerB: _ => throw new InvalidOperationException("lost"));
    }

    [Test]
    public async Task OneWinsWith_ThrowsWhenBothWritersSucceed()
    {
        var asserter = ConcurrencyAssert.TwoWriters(new object());

        await Assert.That(async () => await asserter.OneWinsWith<InvalidOperationException>(
            writerA: _ => Task.CompletedTask,
            writerB: _ => Task.CompletedTask))
            .ThrowsExactly<ConcurrencyAssertionException>();
    }

    [Test]
    public async Task Precondition_IfMatchFails_AcceptsHttp412()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.PreconditionFailed) { Content = new StringContent("x") };
        await Precondition.IfMatchFails(response);
    }

    [Test]
    public async Task Precondition_IfMatchFails_RejectsOtherStatus()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") };
        await Assert.That(async () => await Precondition.IfMatchFails(response))
            .ThrowsExactly<ConcurrencyAssertionException>();
    }

    [Test]
    public async Task Precondition_NotModified_AcceptsHttp304()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.NotModified);
        await Precondition.NotModified(response);
    }

    [Test]
    public async Task SequenceIdempotencyChecker_AppliesIncreasingSequences()
    {
        var checker = new SequenceIdempotencyChecker();

        await Assert.That(checker.TryApply("agg-1", 1)).IsTrue();
        await Assert.That(checker.TryApply("agg-1", 2)).IsTrue();
        await Assert.That(checker.TryApply("agg-1", 2)).IsFalse(); // duplicate
        await Assert.That(checker.TryApply("agg-1", 1)).IsFalse(); // out-of-order
        await Assert.That(checker.LastApplied("agg-1")).IsEqualTo(2L);
    }

    [Test]
    public async Task SequenceIdempotencyChecker_TracksPerAggregate()
    {
        var checker = new SequenceIdempotencyChecker();

        await Assert.That(checker.TryApply("agg-A", 5)).IsTrue();
        await Assert.That(checker.TryApply("agg-B", 1)).IsTrue();
        await Assert.That(checker.LastApplied("agg-A")).IsEqualTo(5L);
        await Assert.That(checker.LastApplied("agg-B")).IsEqualTo(1L);
        await Assert.That(checker.LastApplied("agg-C")).IsNull();
    }
}
